using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class UPart {
        [JsonProperty] public string Name = "New Part";
        [JsonProperty] public string Comment = string.Empty;

        [JsonProperty] public int TrackNo;
        [JsonProperty] public int PosTick = 0;
        public virtual int DurTick { set; get; }
        public int EndTick { get { return PosTick + DurTick; } }

        public UPart() { }

        public abstract int GetMinDurTick(UProject project);

        public virtual void Validate(UProject project) { }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class UVoicePart : UPart {
        [JsonProperty] public SortedSet<UNote> notes = new SortedSet<UNote>();
        public override int GetMinDurTick(UProject project) {
            int durTick = 0;
            foreach (UNote note in notes)
                durTick = Math.Max(durTick, note.position + note.duration);
            return durTick;
        }

        public override void Validate(UProject project) {
            foreach (var note in notes) {
                note.Validate(project);
            }
            DurTick = GetMinDurTick(project) + project.resolution;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class UWavePart : UPart {
        string _filePath;

        [JsonProperty]
        public string FilePath {
            set { _filePath = value; Name = System.IO.Path.GetFileName(value); }
            get { return _filePath; }
        }
        public float[] Peaks;

        public int Channels;
        public int FileDurTick;
        public int HeadTrimTick = 0;
        public int TailTrimTick = 0;
        public override int DurTick {
            get { return FileDurTick - HeadTrimTick - TailTrimTick; }
            set { TailTrimTick = FileDurTick - HeadTrimTick - value; }
        }
        public override int GetMinDurTick(UProject project) { return 60; }
    }
}
