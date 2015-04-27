using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenUtau.Core.USTx;

namespace OpenUtau.Core.Render
{
    class ResamplerInterface
    {
        public void RenderAll(UProject project)
        {
            foreach (UPart part in project.Parts)
                if (part is UVoicePart) Render((UVoicePart)part, project);
        }

        public void Render(UVoicePart part, UProject project)
        {
            lock (part)
            {
                foreach (UNote note in part.Notes)
                {
                    foreach (UPhoneme phoneme in note.Phonemes)
                    {
                        System.Diagnostics.Debug.WriteLine(BuildResamplerArgs(phoneme, part, project));
                        System.Diagnostics.Debug.WriteLine(BuildConnectorArgs(phoneme, part, project));
                    }
                }
            }
        }

        private string BuildResamplerArgs(UPhoneme phoneme, UPart part, UProject project)
        {
            USinger singer = project.Tracks[part.TrackNo].Singer;
            string noteName = MusicMath.GetNoteString(phoneme.Parent.NoteNum);
            string inputfile = Lib.EncodingUtil.ConvertEncoding(singer.FileEncoding, singer.PathEncoding, phoneme.Oto.File);
            inputfile = System.IO.Path.Combine(singer.ActualPath, inputfile);

            double strechRatio = Math.Pow(2, 1.0 - (double)(int)phoneme.Parent.Expressions["velocity"].Data / 100);
            double length = phoneme.Oto.Preutter * strechRatio + phoneme.Envelope.Points[4].X;
            double requiredLength = Math.Ceiling(length / 50 + 0.5) * 50;
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
            string outputfile = string.Format("{0:x}_{1:x}.wav", phoneme.GetHashCode(), Lib.xxHash.CalcStringHash(inputfile + " " + args));
            args = string.Format("{0} {1} {2}", inputfile, outputfile, args);
            return args;
        }

        private string BuildConnectorArgs(UPhoneme phoneme, UPart part, UProject project)
        {
            double strechRatio = Math.Pow(2, 1.0 - ((double)(int)phoneme.Parent.Expressions["velocity"].Data / 100));
            string offset = string.Format("{0:0.###}", phoneme.Oto.Preutter * strechRatio - phoneme.PreUtter);
            double lengthAdjustment = phoneme.TailIntrude == 0 ? phoneme.PreUtter : phoneme.PreUtter - phoneme.TailIntrude + phoneme.TailOverlap;
            string length = phoneme.DurTick + "@" + project.BPM + (lengthAdjustment >= 0 ? "+" : "-") + string.Format("{0:0.###}", Math.Abs(lengthAdjustment));
            var pts = phoneme.Envelope.Points;
            string envelope = string.Format("{0:0.#####} {1:0.#####} {2:0.#####}", 0, pts[1].X - pts[0].X, pts[4].X - pts[3].X);
            envelope += string.Format(" {0:0.#####} {1:0.#####} {2:0.#####}", pts[0].Y, pts[1].Y, pts[3].Y);
            envelope += string.Format(" {0:0.#####} {1:0.#####} {2:0.#####}", 0, phoneme.Overlap, pts[4].Y);
            return string.Format("{0} {1} {2}", offset, length, envelope);
        }

        private string BuildPitchArgs(UPhoneme phoneme, UPart part, UProject project)
        {
            return "";
        }
    }
}
