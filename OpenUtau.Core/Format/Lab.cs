using System;
using System.IO;
using System.Linq;
using System.Text;
using Format;
using OpenUtau.Core.DiffSinger;
using OpenUtau.Core.Enunu;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;

namespace Format {
    public class Lab {

        public double offsetMs;
        public string[] ph_seq;
        public double[] phDurMs;
        public double frameMs;
        public double preMs =0;



        public Lab(RenderPhrase phrase) {

            var notes = phrase.notes;
            var phones = phrase.phones;
            double headMs = 0;
            double tailMs = 0;
            string silentstr = "pan";

            if (phrase.singer.SingerType.Equals(USingerType.Enunu)) {
                headMs = phrase.positionMs - phrase.timeAxis.TickPosToMsPos(phrase.position - EnunuUtils.headTicks);
                tailMs = phrase.timeAxis.TickPosToMsPos(phrase.end + EnunuUtils.tailTicks) - phrase.endMs;
                frameMs = 10;
            } else if (phrase.singer.SingerType.Equals(USingerType.DiffSinger)) {
                headMs = DiffSingerUtils.headMs;
                tailMs = DiffSingerUtils.tailMs;
                frameMs = 10;
            } else {
                return;
            }
            ph_seq = phones
                .Select(p => p.phoneme)
                .Prepend(silentstr)
                .Append(silentstr)
                .ToArray();
            phDurMs = phones
                .Select(p => p.durationMs)
                .Prepend(headMs)
                .Append(tailMs)
                .ToArray();

            int headFrames = (int)(headMs / frameMs);
            int tailFrames = (int)(tailMs / frameMs);
            var totalFrames = (int)(phDurMs.Sum() / frameMs);
            offsetMs = phrase.phones[0].positionMs - headMs;
        }

        public RawLab toRaw() {
            var labRow = new RawLab(this);
            preMs = labRow.ph_start + labRow.ph_end;
            return labRow;
        }

        static public void SavePart(UProject project, UVoicePart part, string filePath) {
            var LabList = RenderPhrase.FromPart(project, project.tracks[part.trackNo], part)
                .Select(x => new Lab(x))
                .ToArray();
            RawLab[] ScriptArray = new RawLab[LabList.Count()];
            for (int j = 1; j < LabList.Count(); j++) {
                ScriptArray = LabList.Select(x => x.toRaw()).ToArray();
            }
            File.WriteAllText(filePath, ScriptArray.Select(A => String.Join(A.ph_start.ToString(), " ", A.ph_end.ToString(), " ", A.ph_seq)).ToString(),
                new UTF8Encoding(false));
        }
    }
}
public class RawLab {
    public string ph_seq;
    public double ph_start;
    public double ph_end;

    public RawLab(Lab script) {
        ph_seq = script.ph_seq;
        ph_start = script.preMs;
        ph_end = double.Parse(script.phDurMs.Select(x => x).ToString());
    }
}
