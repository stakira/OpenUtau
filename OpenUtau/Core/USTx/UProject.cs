using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public class UProject
    {
        public double BPM = 120;
        public int BeatPerBar = 4;
        public int BeatUnit = 4;
        public int Resolution = 480;

        public string Name = "New Project";
        public string Comment = string.Empty;
        public string OutputDir = "Vocal";
        public string CacheDir = "UCache";
        public string FilePath;
        public bool Saved = false;

        public List<UTrack> Tracks = new List<UTrack>();
        public List<UPart> Parts = new List<UPart>();
        public List<USinger> Singers = new List<USinger>();

        public Dictionary<string, UExpression> ExpressionTable = new Dictionary<string, UExpression>();

        public void RegisterExpression(UExpression exp)
        {
            if (!ExpressionTable.ContainsKey(exp.Name))
                ExpressionTable.Add(exp.Name, exp);
        }

        public UNote CreateNote()
        {
            UNote note = UNote.Create();
            foreach (var pair in ExpressionTable) { note.Expressions.Add(pair.Key, pair.Value.Clone(note)); }
            note.PitchBend.Points[0].X = -25;
            note.PitchBend.Points[1].X = 25;
            return note;
        }

        public UNote CreateNote(int noteNum, int posTick, int durTick)
        {
            var note = CreateNote();
            note.NoteNum = noteNum;
            note.PosTick = posTick;
            note.DurTick = durTick;
            note.PitchBend.Points[1].X = Math.Min(25, DocManager.Inst.Project.TickToMillisecond(note.DurTick) / 2);
            return note;
        }

        public UProject() { }

        public int MillisecondToTick(double ms)
        {
            return MusicMath.MillisecondToTick(ms, BPM, BeatUnit, Resolution);
        }

        public double TickToMillisecond(double tick)
        {
            return MusicMath.TickToMillisecond(tick, BPM, BeatUnit, Resolution);
        }

    }
}
