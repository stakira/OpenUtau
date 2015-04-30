using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;

namespace OpenUtau.Core.USTx
{
    public abstract class UPart
    {
        public string Name = "New Part";
        public string Comment = "";

        public int PosTick = 0;
        public int DurTick;
        public int TrackNo;
        public int EndTick { get { return PosTick + DurTick; } }

        public UPart() { }

        public abstract int GetMinDurTick(UProject project);
        public abstract void Dispose();
    }

    public class UVoicePart : UPart
    {
        public SortedSet<UNote> Notes = new SortedSet<UNote>();

        public override int GetMinDurTick(UProject project)
        {
            int durTick = 0;
            foreach (UNote note in Notes) durTick = Math.Max(durTick, note.PosTick + note.DurTick);
            return durTick;
        }

        public override void Dispose() { }
    }

    public class UWavePart : UPart
    {
        public WaveStream Stream;
        public WaveStream Peaks;

        public string FilePath;
        public string PeaksPath;

        public override int GetMinDurTick(UProject project)
        {
            return project.MillisecondToTick(Stream.TotalTime.TotalMilliseconds);
        }

        public override void Dispose()
        {
            if (Stream != null) Stream.Dispose();
            if (Peaks != null) Peaks.Dispose();
        }
    }
}
