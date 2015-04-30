using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using OpenUtau.Core.USTx;
using OpenUtau.Core.Lib;

namespace OpenUtau.Core.Formats
{
    public static class Ust
    {
        private enum UstVersion { Early, V1_0, V1_1, V1_2, Unknown };
        private enum UstBlock { Version, Setting, Note, Trackend, None };

        private const string versionTag = "[#VERSION]";
        private const string settingTag = "[#SETTING]";
        private const string endTag = "[#TRACKEND]";

        static public void Load(string[] files)
        {
            bool ustTracks = true;
            foreach (string file in files)
            {
                if (OpenUtau.Core.Formats.Formats.DetectProjectFormat(file) != Core.Formats.ProjectFormats.Ust) { ustTracks = false; break; }
            }

            if (!ustTracks)
            {
                DocManager.Inst.ExecuteCmd(new UserMessageNotification("Multiple files must be all Ust files"));
                return;
            }

            List<UProject> projects = new List<UProject>();
            foreach (string file in files)
            {
                projects.Add(Load(file));
            }

            double bpm = projects.First().BPM;
            UProject project = new UProject() { BPM = bpm, Name = "Merged Project" };
            foreach (UProject p in projects)
            {
                project.Tracks.Add(p.Tracks[0]);
                project.Parts.Add(p.Parts[0]);
                project.Tracks.Last().TrackNo = project.Tracks.Count - 1;
                project.Parts.Last().TrackNo = project.Tracks.Count - 1;
            }

            if (project != null) DocManager.Inst.ExecuteCmd(new LoadProjectNotification(project));
        }

        static public UProject Load(string file, Encoding encoding = null)
        {
            int currentNoteIndex = 0;
            UstVersion version = UstVersion.Early;
            UstBlock currentBlock = UstBlock.None;
            string[] lines;

            try
            {
                if (encoding == null) lines = File.ReadAllLines(file, EncodingUtil.DetectFileEncoding(file));
                else lines = File.ReadAllLines(file, encoding);
            }
            catch (Exception e)
            {
                DocManager.Inst.ExecuteCmd(new UserMessageNotification(e.GetType().ToString() + "\n" + e.Message));
                return null;
            }

            UProject project = new UProject() { Resolution = 480, FilePath = file };
            project.RegisterExpression(new IntExpression(null, "velocity","VEL") { Data = 100, Min = 0, Max = 200});
            project.RegisterExpression(new IntExpression(null, "volume","VOL") { Data = 100, Min = 0, Max = 200});
            project.RegisterExpression(new IntExpression(null, "gender","GEN") { Data = 0, Min = -100, Max = 100});
            project.RegisterExpression(new IntExpression(null, "lowpass","LPF") { Data = 0, Min = 0, Max = 100});
            project.RegisterExpression(new IntExpression(null, "highpass", "HPF") { Data = 0, Min = 0, Max = 100 });
            project.RegisterExpression(new IntExpression(null, "accent", "ACC") { Data = 100, Min = 0, Max = 200 });
            project.RegisterExpression(new IntExpression(null, "decay", "DEC") { Data = 0, Min = 0, Max = 100 });

            project.Tracks.Add(new UTrack());
            project.Tracks.First().TrackNo = 0;
            UVoicePart part = new UVoicePart() { TrackNo = 0, PosTick = 0 };
            project.Parts.Add(part);

            List<string> currentLines = new List<string>();
            int currentTick = 0;
            UNote currentNote = null;

            foreach (string line in lines)
            {
                if (line.Trim().StartsWith(@"[#") && line.Trim().EndsWith(@"]"))
                {
                    if (line.Equals(versionTag)) currentBlock = UstBlock.Version;
                    else if (line.Equals(settingTag)) currentBlock = UstBlock.Setting;
                    else
                    {
                        if (line.Equals(endTag)) currentBlock = UstBlock.Trackend;
                        else
                        {
                            try { currentNoteIndex = int.Parse(line.Replace("[#", "").Replace("]", "")); }
                            catch { DocManager.Inst.ExecuteCmd(new UserMessageNotification("Unknown ust format")); return null; }
                            currentBlock = UstBlock.Note;
                        }

                        if (currentLines.Count != 0)
                        {
                            currentNote = NoteFromUst(project.CreateNote(), currentLines, version);
                            currentNote.PosTick = currentTick;
                            if (!currentNote.Lyric.Replace("R", "").Replace("r", "").Equals("")) part.Notes.Add(currentNote);
                            currentTick += currentNote.DurTick;
                            currentLines.Clear();
                        }
                    }
                }
                else
                {
                    if (currentBlock == UstBlock.Version) {
                        if (line.StartsWith("UST Version"))
                        {
                            string v = line.Trim().Replace("UST Version", "");
                            if (v == "1.0") version = UstVersion.V1_0;
                            else if (v == "1.1") version = UstVersion.V1_1;
                            else if (v == "1.2") version = UstVersion.V1_2;
                            else version = UstVersion.Unknown;
                        }
                    }
                    if (currentBlock == UstBlock.Setting)
                    {
                        if (line.StartsWith("Tempo="))
                        {
                            project.BPM = double.Parse(line.Trim().Replace("Tempo=", ""));
                            if (project.BPM == 0) project.BPM = 120;
                        }
                        if (line.StartsWith("ProjectName=")) project.Name = line.Trim().Replace("ProjectName=", "");
                        if (line.StartsWith("VoiceDir="))
                        {
                            string singerpath = line.Trim().Replace("VoiceDir=", "");
                            var singer = UtauSoundbank.GetSinger(singerpath, EncodingUtil.DetectFileEncoding(file), DocManager.Inst.Singers);
                            project.Singers.Add(singer);
                            project.Tracks[0].Singer = singer;
                        }
                    }
                    else if (currentBlock == UstBlock.Note)
                    {
                        currentLines.Add(line);
                    }
                    else if (currentBlock == UstBlock.Trackend)
                    {
                        break;
                    }
                }
            }

            if (currentBlock != UstBlock.Trackend)
                DocManager.Inst.ExecuteCmd(new UserMessageNotification("Unexpected ust file end"));
            part.DurTick = currentTick;
            return project;
        }

        static UNote NoteFromUst(UNote note, List<string> lines, UstVersion version)
        {
            string pbs = "", pbw = "", pby = "", pbm = "";

            foreach (string line in lines)
            {
                if (line.StartsWith("Lyric="))
                {
                    note.Phonemes[0].Phoneme = note.Lyric = line.Trim().Replace("Lyric=", "");
                    if (note.Phonemes[0].Phoneme.StartsWith("?"))
                    {
                        note.Phonemes[0].Phoneme = note.Phonemes[0].Phoneme.Substring(1);
                        note.Phonemes[0].AutoRemapped = false;
                    }
                }
                if (line.StartsWith("Length=")) note.DurTick = int.Parse(line.Trim().Replace("Length=", ""));
                if (line.StartsWith("NoteNum=")) note.NoteNum = int.Parse(line.Trim().Replace("NoteNum=", ""));
                if (line.StartsWith("Velocity=")) note.Expressions["velocity"].Data = int.Parse(line.Trim().Replace("Velocity=", ""));
                if (line.StartsWith("Intensity=")) note.Expressions["volume"].Data = int.Parse(line.Trim().Replace("Intensity=", ""));
                if (line.StartsWith("PreUtterance="))
                {
                    if (line.Trim() == "PreUtterance=") note.Phonemes[0].AutoTiming = true;
                    else { note.Phonemes[0].AutoTiming = false; note.Phonemes[0].Preutter = double.Parse(line.Trim().Replace("PreUtterance=", "")); }
                }
                if (line.StartsWith("VoiceOverlap=")) note.Phonemes[0].Overlap = double.Parse(line.Trim().Replace("VoiceOverlap=", ""));
                if (line.StartsWith("Envelope="))
                {
                    var pts = line.Trim().Replace("Envelope=", "").Split(new[] { ',' });
                    if (pts.Count() > 5) note.Expressions["decay"].Data = 100 - (int)double.Parse(pts[5]);
                }
                if (line.StartsWith("VBR=")) VibratoFromUst(note.Vibrato, line.Trim().Replace("VBR=", ""));
                if (line.StartsWith("PBS=")) pbs = line.Trim().Replace("PBS=", "");
                if (line.StartsWith("PBW=")) pbw = line.Trim().Replace("PBW=", "");
                if (line.StartsWith("PBY=")) pby = line.Trim().Replace("PBY=", "");
                if (line.StartsWith("PBM=")) pbm = line.Trim().Replace("PBM=", "");
            }

            if (pbs != "")
            {
                var pts = note.PitchBend.Data as List<PitchPoint>;
                pts.Clear();
                // PBS
                if (pbs.Contains(';'))
                {
                    pts.Add(new PitchPoint(double.Parse(pbs.Split(new[] { ';' })[0]), double.Parse(pbs.Split(new[] { ';' })[1])));
                        note.PitchBend.SnapFirst = false;
                }
                else
                {
                    pts.Add(new PitchPoint(double.Parse(pbs), 0));
                    note.PitchBend.SnapFirst = true;
                }
                double x = pts.First().X;
                if (pbw != "")
                {
                    string[] w = pbw.Split(new[] { ',' });
                    string[] y = null;
                    if (w.Count() > 1) y = pby.Split(new[] { ',' });
                    for (int i = 0; i < w.Count() - 1; i++)
                    {
                        x += w[i] == "" ? 0 : float.Parse(w[i]);
                        pts.Add(new PitchPoint(x, y[i] == "" ? 0 : double.Parse(y[i])));
                    }
                    pts.Add(new PitchPoint(x + double.Parse(w[w.Count() - 1]), 0));
                }
            }
            return note;
        }

        static void VibratoFromUst(VibratoExpression vibrato, string ust)
        {
            var args = ust.Split(new[] { ',' }).Select(double.Parse).ToList();
            if (args.Count() >= 7)
            {
                vibrato.Length = args[0];
                vibrato.Period = args[1];
                vibrato.Depth = args[2];
                vibrato.In = args[3];
                vibrato.Out = args[4];
                vibrato.Shift = args[5];
                vibrato.Drift = args[6];
            }
        }

        static String VibratoToUst(VibratoExpression vibrato)
        {
            List<double> args = new List<double>()
            {
                vibrato.Length,
                vibrato.Period,
                vibrato.Depth,
                vibrato.In,
                vibrato.Out,
                vibrato.Shift,
                vibrato.Drift
            };
            return string.Join(",", args.ToArray());
        }
    }
}
