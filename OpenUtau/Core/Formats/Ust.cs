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
            int currentNote = 0;
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
            uproject.RegisterExpression(new CCExpression(null, "velocity") { Data = 64f });
            uproject.RegisterExpression(new CCExpression(null, "opening") { Data = 127f });
            uproject.RegisterExpression(new FloatExpression(null, "accent") { Data = 50f, Min = 0f, Max = 100f });
            uproject.RegisterExpression(new FloatExpression(null, "decay") { Data = 50f, Min = 0f, Max = 100f });

            uproject.Tracks.Add(new UTrack());
            uproject.Tracks.First().TrackNo = 0;
            UVoicePart upart = new UVoicePart() { TrackNo = 0, PosTick = 0 };
            uproject.Parts.Add(upart);
            UNote currNote = null;
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
                        try { currentNote = int.Parse(line.Replace("[#", "").Replace("]", "")); }
                        catch { System.Windows.MessageBox.Show("Unknown ust format"); return null; }
                        currentBlock = UstBlock.Note;
                        if (currNote != null && !currNote.Lyric.Replace("R", "").Replace("r", "").Equals("")) upart.Notes.Add(currNote);
                        currNote = uproject.CreateNote();
                        currNote.Lyric = "R";
                        currNote.PosTick = currTick;
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
                        if (line.StartsWith("Lyric=")) currNote.Lyric = line.Trim().Replace("Lyric=", "");
                        if (line.StartsWith("Length=")) { currNote.DurTick = int.Parse(line.Trim().Replace("Length=", "")); currTick += currNote.DurTick; }
                        if (line.StartsWith("NoteNum=")) currNote.NoteNum = int.Parse(line.Trim().Replace("NoteNum=", ""));
                    }
                    else if (currentBlock == UstBlock.Trackend)
                    {
                        break;
                    }
                }
            }
            if (currentBlock != UstBlock.Trackend) System.Windows.MessageBox.Show("Unexpected ust file end");
            if (currNote != null && currNote.Lyric != "R") upart.Notes.Add(currNote);
            upart.DurTick = currTick;
            currNote = null;
            return uproject;
        }
    }
}
