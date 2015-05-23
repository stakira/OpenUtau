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

        public int TrackNo;
        public int PosTick = 0;
        public virtual int DurTick { set; get; }
        public int EndTick { get { return PosTick + DurTick; } }

        public UPart() { }

        public abstract int GetMinDurTick(UProject project);
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
    }

    public class UWavePart : UPart
    {
        string _filePath;
        public string FilePath
        {
            set { _filePath = value; Name = System.IO.Path.GetFileName(value); }
            get { return _filePath; }
        }
        public float[] Peaks;

        public int Channels;
        public int FileDurTick;
        public int HeadTrimTick = 0;
        public int TailTrimTick = 0;
        public override int DurTick
        {
            get { return FileDurTick - HeadTrimTick - TailTrimTick; }
            set { TailTrimTick = FileDurTick - HeadTrimTick - value; }
        }
        public override int GetMinDurTick(UProject project) { return 60; }
    }
}
