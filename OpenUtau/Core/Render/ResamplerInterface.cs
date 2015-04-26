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
            for (int i = 0; i < part.Notes.Count; i++)
            {
                UNote note = part.Notes[i];
                UNote nextNote = null;
                if (i != part.Notes.Count - 1 && part.Notes[i].EndTick == part.Notes[i + 1].PosTick)
                    nextNote = part.Notes[i + 1];
                System.Diagnostics.Debug.WriteLine(BuildArgs(note, part, project, nextNote));
            }
        }

        private string BuildArgs(UNote note, UPart part, UProject project, UNote nextNote)
        {
            USinger singer = project.Tracks[part.TrackNo].Singer;
            string noteName = MusicMath.GetNoteString(note.NoteNum);
            string lyric = note.Lyric;
            if (lyric.StartsWith("?")) lyric = lyric.Substring(1);
            else if (singer.PitchMap.ContainsKey(noteName)) lyric += singer.PitchMap[noteName];
            UOto oto = singer.AliasMap[lyric];
            string inputfile;
            if (singer.PitchMap.ContainsKey(noteName))
                inputfile = Lib.EncodingUtil.ConvertEncoding(singer.FileEncoding, singer.PathEncoding,
                    System.IO.Path.Combine(singer.PitchMap[noteName], oto.File));
            else
                inputfile = Lib.EncodingUtil.ConvertEncoding(singer.FileEncoding, singer.PathEncoding, oto.File);
            inputfile = System.IO.Path.Combine(singer.ActualPath, inputfile);

            double strechRatio = Math.Pow(2, 1.0 - ((float)note.Expressions["velocity"].Data / 100));
            double length = 60.0 * 1000.0 * note.DurTick / project.BPM / 480;
            length += (double)oto.Preutter * strechRatio;
            if (nextNote != null)
            {
                var nextOto = GetOto(nextNote, part, project);
                double nextStrechRatio = Math.Pow(2, 1.0 - ((float)nextNote.Expressions["velocity"].Data / 100));
                length += nextOto.Overlap * nextStrechRatio - nextOto.Preutter * nextStrechRatio;
                length = Math.Max(length, oto.Consonant);
            }
            double requiredLength = Math.Ceiling(length / 50) * 50;
            // fresamp.exe <infile> <outfile> <tone> <velocity> <flags> <offset> <length_req>
            // <fixed_length> <endblank> <volume> <modulation> <pitch>
            string args = string.Format(
                "{0} {1:D} {2} {3:D} {4:D} {5:D} {6:D} {7:D} {8:D} {9}",
                noteName,
                (int)(float)note.Expressions["velocity"].Data,
                note.GetResamplerFlags(),
                oto.Offset,
                (int)requiredLength,
                oto.Consonant,
                oto.Cutoff,
                (int)(float)note.Expressions["volume"].Data,
                0,
                "pitches"
                );
            string outputfile = string.Format("{0:x}_{1:x}.wav", note.GetHashCode(), Lib.xxHash.CalcStringHash(inputfile + " " + args));
            args = string.Format("{0} {1} {2}", inputfile, outputfile, args);
            return args;
        }

        private UOto GetOto(UNote note, UPart part, UProject project)
        {
            USinger singer = project.Tracks[part.TrackNo].Singer;
            string noteName = MusicMath.GetNoteString(note.NoteNum);
            string lyric = note.Lyric;
            if (lyric.StartsWith("?")) lyric = lyric.Substring(1);
            else if (singer.PitchMap.ContainsKey(noteName)) lyric += singer.PitchMap[noteName];
            return singer.AliasMap[lyric];
        }
    }
}
