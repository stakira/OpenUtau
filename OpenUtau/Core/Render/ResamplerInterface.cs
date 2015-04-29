using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using OpenUtau.Core.USTx;

namespace OpenUtau.Core.Render
{
    class ResamplerInterface
    {
        const string DefaultEnvelope = "0 5 35 0 100 100 0";
        public void RenderAll(UProject project)
        {
            foreach (UPart part in project.Parts)
                if (part is UVoicePart)
                    Render((UVoicePart)part, project);
        }

        public void Render(UVoicePart part, UProject project)
        {
            System.Diagnostics.Debug.WriteLine(PathManager.Inst.HomePath);
            lock (part)
            {
                string cache_dir = PathManager.Inst.GetCachePath(project.FilePath);
                var file = new StreamWriter(Path.Combine(cache_dir, "render.bat"), false, Encoding.Default);

                UNote lastNote = null;
                foreach (UNote note in part.Notes)
                {
                    if (lastNote == null || lastNote.EndTick < note.PosTick)
                    {
                        if (!note.Phonemes[0].Overlapped)
                        {
                            USinger singer = project.Tracks[part.TrackNo].Singer;

                            string inputfile = Path.Combine(singer.Path, "R.wav");

                            string args2 = BuildConnectorArgsR(lastNote, note, part, project);
                            string tool2_cmd = string.Format("{0} {1} {2} {3} {4}",
                                PathManager.Inst.GetTool2Path(), Path.Combine(cache_dir, "out.wav"), inputfile, args2, DefaultEnvelope);
                            //System.Diagnostics.Debug.WriteLine(tool2_cmd);
                            file.WriteLine(tool2_cmd);
                        }
                    }
                    foreach (UPhoneme phoneme in note.Phonemes)
                    {
                        USinger singer = project.Tracks[part.TrackNo].Singer;
                        
                        string inputfile = Lib.EncodingUtil.ConvertEncoding(singer.FileEncoding, singer.PathEncoding, phoneme.Oto.File);
                        inputfile = Path.Combine(singer.Path, inputfile);

                        string args1 = BuildResamplerArgs(phoneme, part, project);
                        string args2 = BuildConnectorArgs(phoneme, part, project);
                        string cachefile = string.Format("{0:x}.wav", Lib.xxHash.CalcStringHash(inputfile + " " + args1));
                        cachefile = Path.Combine(cache_dir, cachefile);

                        string tool1_cmd = File.Exists(cachefile) ? "" : string.Format("{0} {1} {2} {3}", PathManager.Inst.GetTool1Path(), inputfile, cachefile, args1);
                        string tool2_cmd = string.Format("{0} {1} {2} {3}", PathManager.Inst.GetTool2Path(), cache_dir + "\\out.wav", cachefile, args2);

                        //System.Diagnostics.Debug.WriteLine(tool1_cmd);
                        //System.Diagnostics.Debug.WriteLine(tool2_cmd);
                        file.WriteLine(tool1_cmd);
                        file.WriteLine(tool2_cmd);
                    }
                    lastNote = note;
                }
                string cmd = string.Format("copy /Y \"{0}\" /B + \"{1}\" /B \"{2}\"",
                    Path.Combine(cache_dir, "out.wav.whd"), Path.Combine(cache_dir, "out.wav.dat"), Path.Combine(cache_dir, "out.wav"));
                file.WriteLine(cmd);
                cmd = string.Format("del \"{0}\"", Path.Combine(cache_dir, "out.wav.whd"));
                file.WriteLine(cmd);
                cmd = string.Format("del \"{0}\"", Path.Combine(cache_dir, "out.wav.dat"));
                file.WriteLine(cmd);
                file.Close();
                var p = System.Diagnostics.Process.Start(Path.Combine(cache_dir, "render.bat"));
                p.WaitForExit();
            }
        }

        private string BuildResamplerArgs(UPhoneme phoneme, UVoicePart part, UProject project)
        {
            USinger singer = project.Tracks[part.TrackNo].Singer;
            string noteName = MusicMath.GetNoteString(phoneme.Parent.NoteNum);

            double strechRatio = Math.Pow(2, 1.0 - (double)(int)phoneme.Parent.Expressions["velocity"].Data / 100);
            double length = phoneme.Oto.Preutter * strechRatio + phoneme.Envelope.Points[4].X;
            double requiredLength = Math.Ceiling(length / 50 + 1) * 50;
            // fresamp.exe <infile> <outfile> <tone> <velocity> <flags> <offset> <length_req>
            // <fixed_length> <endblank> <volume> <modulation> <pitch>
            string args = string.Format(
                "{0} {1:D} {2} {3:D} {4:D} {5:D} {6:D} {7:D} {8:D} {9}",
                noteName,
                (int)phoneme.Parent.Expressions["velocity"].Data,
                phoneme.Parent.GetResamplerFlags(),
                phoneme.Oto.Offset,
                (int)requiredLength,
                phoneme.Oto.Consonant,
                phoneme.Oto.Cutoff,
                (int)phoneme.Parent.Expressions["volume"].Data,
                0,
                BuildPitchArgs(phoneme, part, project)
                );
            return args;
        }

        private string BuildConnectorArgs(UPhoneme phoneme, UVoicePart part, UProject project)
        {
            double strechRatio = Math.Pow(2, 1.0 - ((double)(int)phoneme.Parent.Expressions["velocity"].Data / 100));
            string offset = string.Format("{0:0.###}", phoneme.Oto.Preutter * strechRatio - phoneme.Preutter);
            double lengthAdjustment = phoneme.TailIntrude == 0 ? phoneme.Preutter : phoneme.Preutter - phoneme.TailIntrude + phoneme.TailOverlap;
            string length = phoneme.DurTick + "@" + project.BPM + (lengthAdjustment >= 0 ? "+" : "-") + string.Format("{0:0.###}", Math.Abs(lengthAdjustment));
            var pts = phoneme.Envelope.Points;
            string envelope = string.Format("{0:0.#####} {1:0.#####} {2:0.#####}", 0, pts[1].X - pts[0].X, pts[4].X - pts[3].X);
            envelope += string.Format(" {0:0.#####} {1:0.#####} {2:0.#####}", pts[0].Y, pts[1].Y, pts[3].Y);
            envelope += string.Format(" {0:0.#####} {1:0.#####} {2:0.#####}", 0, phoneme.Overlap, pts[4].Y);
            return string.Format("{0} {1} {2}", offset, length, envelope);
        }

        private string BuildConnectorArgsR(UNote lastNote, UNote note, UVoicePart part, UProject project)
        {
            int durTick = lastNote == null ? note.PosTick : note.PosTick - lastNote.EndTick;
            double lengthAdjustment = -note.Phonemes[0].Preutter + note.Phonemes[0].Overlap;
            string length = durTick + "@" + project.BPM + (lengthAdjustment >= 0 ? "+" : "-") + string.Format("{0:0.###}", Math.Abs(lengthAdjustment));
            return string.Format("0 {0}", length);
        }

        private string BuildPitchArgs(UPhoneme phoneme, UVoicePart part, UProject project)
        {
            List<int> pitches = new List<int>();
            int noteIdx = part.Notes.IndexOf(phoneme.Parent);
            UNote lastNote = noteIdx == 0 ? null : part.Notes[noteIdx - 1];
            UNote nextNote = noteIdx == part.Notes.Count - 1 ? null : part.Notes[noteIdx + 1];

            // Get relevant pitch points
            List<PitchPoint> pps = new List<PitchPoint>();
            
            bool lastNoteInvolved = lastNote != null && phoneme.Overlapped;
            bool nextNoteInvolved = nextNote != null && nextNote.Phonemes[0].Overlapped;

            double lastVibratoStartMs = 0;
            double lastVibratoEndMs = 0;
            double vibratoStartMs = 0;
            double vibratoEndMs = 0;

            if (lastNoteInvolved)
            {
                double offsetMs = DocManager.Inst.Project.TickToMillisecond(phoneme.Parent.PosTick - lastNote.PosTick);
                foreach (PitchPoint pp in lastNote.PitchBend.Points)
                {
                    var newpp = pp.Clone();
                    newpp.X -= offsetMs;
                    newpp.Y -= (phoneme.Parent.NoteNum - lastNote.NoteNum) * 10;
                    pps.Add(newpp);
                }
                if (lastNote.Vibrato.Depth != 0)
                {
                    lastVibratoStartMs = -DocManager.Inst.Project.TickToMillisecond(lastNote.DurTick) * lastNote.Vibrato.Length / 100;
                    lastVibratoEndMs = 0;
                }
            }

            foreach (PitchPoint pp in phoneme.Parent.PitchBend.Points) pps.Add(pp);
            if (phoneme.Parent.Vibrato.Depth !=0)
            {
                vibratoEndMs = DocManager.Inst.Project.TickToMillisecond(phoneme.Parent.DurTick);
                vibratoStartMs = vibratoEndMs * (1 - phoneme.Parent.Vibrato.Length / 100);
            }

            if (nextNoteInvolved)
            {
                double offsetMs = DocManager.Inst.Project.TickToMillisecond(phoneme.Parent.PosTick - nextNote.PosTick);
                foreach (PitchPoint pp in nextNote.PitchBend.Points)
                {
                    var newpp = pp.Clone();
                    newpp.X -= offsetMs;
                    newpp.Y -= (phoneme.Parent.NoteNum - nextNote.NoteNum) * 10;
                    pps.Add(newpp);
                }
            }

            double startMs = DocManager.Inst.Project.TickToMillisecond(phoneme.PosTick) - phoneme.Oto.Preutter;
            double endMs = DocManager.Inst.Project.TickToMillisecond(phoneme.DurTick) -
                (nextNote != null && nextNote.Phonemes[0].Overlapped ? nextNote.Phonemes[0].Preutter - nextNote.Phonemes[0].Overlap : 0);
            if (pps.Count > 0)
            {
                if (pps.First().X > startMs) pps.Insert(0, new PitchPoint(startMs, pps.First().Y));
                if (pps.Last().X < endMs) pps.Add(new PitchPoint(endMs, pps.Last().Y));
            }
            else
            {
                throw new Exception("Zero pitch points.");
            }

            // Interpolation
            const int intervalTick = 5;
            double intervalMs = DocManager.Inst.Project.TickToMillisecond(intervalTick);
            double currMs = startMs;
            int i = 0;

            while (currMs < endMs)
            {
                while (pps[i + 1].X < currMs) i++;
                double pit = pps[i].Shape == PitchPointShape.SineIn ? MusicMath.SinEasingIn(pps[i].X, pps[i + 1].X, pps[i].Y, pps[i + 1].Y, currMs) :
                    pps[i].Shape == PitchPointShape.SineOut ? MusicMath.SinEasingOut(pps[i].X, pps[i + 1].X, pps[i].Y, pps[i + 1].Y, currMs) :
                    pps[i].Shape == PitchPointShape.SineInOut ? MusicMath.SinEasingInOut(pps[i].X, pps[i + 1].X, pps[i].Y, pps[i + 1].Y, currMs) :
                    MusicMath.Liner(pps[i].X, pps[i + 1].X, pps[i].Y, pps[i + 1].Y, currMs);

                pit *= 10;

                // Apply vibratos
                if (currMs < lastVibratoEndMs && currMs >= lastVibratoStartMs)
                    pit += InterpolateVibrato(lastNote.Vibrato, currMs - lastVibratoStartMs);

                if (currMs < vibratoEndMs && currMs >= vibratoStartMs)
                    pit += InterpolateVibrato(phoneme.Parent.Vibrato, currMs - vibratoStartMs);

                pitches.Add((int)pit);
                currMs += intervalMs;
            }

            return string.Format("!{0} {1}", project.BPM, Base64EncodeForResampler(pitches));
        }

        private double InterpolateVibrato(VibratoExpression vibrato, double posMs)
        {
            double lengthMs = vibrato.Length / 100 * DocManager.Inst.Project.TickToMillisecond(vibrato.Parent.DurTick);
            double inMs = lengthMs * vibrato.In / 100;
            double outMs = lengthMs * vibrato.Out / 100;
            
            double value = -Math.Sin(2 * Math.PI * (posMs / vibrato.Period + vibrato.Shift / 100)) *vibrato.Depth;

            if (posMs < inMs) value *= posMs / inMs;
            else if (posMs > lengthMs - outMs) value *= (lengthMs - posMs) / outMs;

            return value;
        }

        private const string intToBase64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

        private string Base64EncodeForResampler(int data)
        {
            if (data < 0) data += 4096;
            char[] base64 = new char[2];
            base64[0] = intToBase64[(data >> 6) & 0x003F];
            base64[1] = intToBase64[data & 0x003F];
            return new String(base64);
        }

        private string Base64EncodeForResampler(List<int> data)
        {
            List<string> l = new List<string>();
            foreach (int d in data) l.Add(Base64EncodeForResampler(d));
            StringBuilder base64 = new StringBuilder();
            string last = "";
            int dups = 0;
            foreach (string b in l)
            {
                if (last == b) dups++;
                else if (dups == 0) base64.Append(b);
                else
                {
                    base64.Append('#');
                    base64.Append(dups + 1);
                    base64.Append('#');
                    dups = 0;
                    base64.Append(b);
                }
                last = b;
            }
            if (dups != 0)
            {
                base64.Append('#');
                base64.Append(dups + 1);
                base64.Append('#');
            }
            return base64.ToString();
        }
    }
}
