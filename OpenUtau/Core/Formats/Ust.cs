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
                System.Windows.MessageBox.Show("Multiple files must be all Ust files");
                return;
            }

            List<UProject> projects = new List<UProject>();
            foreach (string file in files)
            {
                projects.Add(Load(file));
            }

            double bpm = projects.First().BPM;
            bool sameBpm = true;
            foreach (UProject project in projects)
            {
                if (bpm != project.BPM) { sameBpm = false; break; }
            }

            if (!sameBpm)
            {
                System.Windows.MessageBox.Show("Ust files BPM must match");
                return;
            }

            UProject uproject = new UProject() { BPM = bpm, Name = "Merged Project" };
            foreach (UProject p in projects)
            {
                uproject.Tracks.Add(new UTrack() { TrackNo = uproject.Tracks.Count });
                uproject.Parts.Add(p.Parts[0]);
                uproject.Parts.Last().TrackNo = uproject.Tracks.Count - 1;
            }

            if (uproject != null) DocManager.Inst.ExecuteCmd(new LoadProjectNotification(uproject));
        }

        static public UProject Load(string file, string encoding = "")
        {
            int currentNoteIndex = 0;
            UstBlock currentBlock = UstBlock.None;
            string[] lines;

            try
            {
                if (encoding == "") lines = File.ReadAllLines(file, EncodingUtil.DetectFileEncoding(file));
                else lines = File.ReadAllLines(file, Encoding.GetEncoding(encoding));
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.GetType().ToString() + "\n" + e.Message);
                return null;
            }

            UProject uproject = new UProject() { Resolution = 480 };
            uproject.RegisterExpression(new FloatExpression(null, "velocity") { Data = 100f, Min = 0f, Max = 200f });
            uproject.RegisterExpression(new FloatExpression(null, "volume") { Data = 100f, Min = 0f, Max = 200f });
            uproject.RegisterExpression(new SequenceExpression(null, "pitchbend"));

            uproject.Tracks.Add(new UTrack());
            uproject.Tracks.First().TrackNo = 0;
            UVoicePart upart = new UVoicePart() { TrackNo = 0, PosTick = 0 };
            uproject.Parts.Add(upart);
            UNote currentNote = null;
            string pbs = ""; string pbw = ""; string pby = ""; string pbm = "";
            int currTick = 0;

            foreach (string line in lines)
            {
                if (line.Trim().StartsWith(@"[#") && line.Trim().EndsWith(@"]"))
                {
                    if (line.Equals(versionTag)) currentBlock = UstBlock.Version;
                    else if (line.Equals(settingTag)) currentBlock = UstBlock.Setting;
                    else if (line.Equals(endTag)) currentBlock = UstBlock.Trackend;
                    else
                    {
                        try { currentNoteIndex = int.Parse(line.Replace("[#", "").Replace("]", "")); }
                        catch { System.Windows.MessageBox.Show("Unknown ust format"); return null; }
                        currentBlock = UstBlock.Note;
                        // Finalize note
                        if (currentNote != null && !currentNote.Lyric.Replace("R", "").Replace("r", "").Equals(""))
                        {
                            if (pbs != "")
                            {
                                var pts = currentNote.Expressions["pitchbend"].Data as List<ExpPoint>;
                                if (pbs.Contains(';')) pts.Add(new ExpPoint(float.Parse(pbs.Split(new[] { ';' })[0]), float.Parse(pbs.Split(new[] { ';' })[1])));
                                else pts.Add(new ExpPoint(float.Parse(pbs), 0));
                                if (pbw != "")
                                {
                                    string[] w = pbw.Split(new[] { ',' });
                                    string[] y = null;
                                    if (w.Count() > 1) y = pby.Split(new[] { ',' });
                                    for (int i = 0; i < w.Count() - 1; i++)
                                    {
                                        pts.Add(new ExpPoint(float.Parse(w[i]), float.Parse(y[i])));
                                    }
                                    pts.Add(new ExpPoint(float.Parse(w[w.Count() - 1]), 0));
                                }
                            }
                            upart.Notes.Add(currentNote);
                        }
                        // Clean up
                        pbs = pbw = pby = "";
                        // Next note
                        currentNote = uproject.CreateNote();
                        currentNote.Lyric = "R";
                        currentNote.PosTick = currTick;
                    }
                }
                else
                {
                    if (currentBlock == UstBlock.Setting)
                    {
                        if (line.StartsWith("Tempo=")) uproject.BPM = double.Parse(line.Trim().Replace("Tempo=", ""));
                        if (line.StartsWith("ProjectName=")) uproject.Name = line.Trim().Replace("ProjectName=", "");
                        if (line.StartsWith("VoiceDir="))
                        {
                            string singerpath = line.Trim().Replace("VoiceDir=", "").Replace("%VOICE%", "");
                            //if (singerpath.StartsWith("%VOICE%"))
                            //    singerpath = Path.Combine(@"E:\Utau\voice", singerpath.Replace("%VOICE%", ""));
                            uproject.Singers.Add(UtauSoundbank.LoadSinger(singerpath, EncodingUtil.DetectFileEncoding(file)));
                        }
                    }
                    else if (currentBlock == UstBlock.Note)
                    {
                        if (line.StartsWith("Lyric=")) currentNote.Lyric = line.Trim().Replace("Lyric=", "");
                        if (line.StartsWith("Length=")) { currentNote.DurTick = int.Parse(line.Trim().Replace("Length=", "")); currTick += currentNote.DurTick; }
                        if (line.StartsWith("NoteNum=")) currentNote.NoteNum = int.Parse(line.Trim().Replace("NoteNum=", ""));
                        if (line.StartsWith("Velocity=")) currentNote.Expressions["velocity"].Data = float.Parse(line.Trim().Replace("Velocity=", ""));
                        if (line.StartsWith("Intensity=")) currentNote.Expressions["volume"].Data = float.Parse(line.Trim().Replace("Intensity=", ""));
                        if (line.StartsWith("PBS=")) pbs = line.Trim().Replace("PBS=", "");
                        if (line.StartsWith("PBW=")) pbw = line.Trim().Replace("PBW=", "");
                        if (line.StartsWith("PBY=")) pby = line.Trim().Replace("PBY=", "");
                        if (line.StartsWith("PBM=")) pbm = line.Trim().Replace("PBM=", "");
                    }
                    else if (currentBlock == UstBlock.Trackend)
                    {
                        break;
                    }
                }
            }
            if (currentBlock != UstBlock.Trackend) System.Windows.MessageBox.Show("Unexpected ust file end");
            if (currentNote != null && currentNote.Lyric != "R") upart.Notes.Add(currentNote);
            upart.DurTick = currTick;
            currentNote = null;
            return uproject;
        }
    }
}
