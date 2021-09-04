using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class UPart {
        [JsonProperty] public string name = "New Part";
        [JsonProperty] public string comment = string.Empty;
        [JsonProperty] public int trackNo;
        [JsonProperty] public int position = 0;

        public virtual int Duration { set; get; }
        public int EndTick { get { return position + Duration; } }

        public UPart() { }

        public abstract int GetMinDurTick(UProject project);

        public virtual void AfterLoad(UProject project, UTrack track) { }

        public virtual void Validate(UProject project, UTrack track) { }

        public abstract UPart Clone();
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

        public override void AfterLoad(UProject project, UTrack track) {
            foreach (var note in notes) {
                note.AfterLoad(project, track, this);
            }
            Duration = GetMinDurTick(project) + project.resolution;
        }

        public override void Validate(UProject project, UTrack track) {
            UNote lastNote = null;
            foreach (UNote note in notes) {
                note.Prev = lastNote;
                note.Next = null;
                if (lastNote != null) {
                    lastNote.Next = note;
                }
                lastNote = note;
            }
            foreach (UNote note in notes) {
                if (note.Prev != null && note.Prev.End == note.position && note.lyric.StartsWith("...")) {
                    note.Extends = note.Prev.Extends ?? note.Prev;
                } else {
                    note.Extends = null;
                }
            }
            foreach (UNote note in notes) {
                note.Phonemize(project, track);
            }
            UPhoneme lastPhoneme = null;
            foreach (UNote note in notes) {
                foreach (var phoneme in note.phonemes) {
                    phoneme.Parent = note;
                    phoneme.Prev = lastPhoneme;
                    phoneme.Next = null;
                    if (lastPhoneme != null) {
                        lastPhoneme.Next = phoneme;
                    }
                    lastPhoneme = phoneme;
                }
            }
            foreach (UNote note in notes) {
                note.Validate(project, track, this);
            }
        }

        public override UPart Clone() {
            return new UVoicePart() {
                notes = new SortedSet<UNote>(notes.Select(note => note.Clone())),
            };
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class UWavePart : UPart {
        string _filePath;

        [JsonProperty]
        public string FilePath {
            set { _filePath = value; name = System.IO.Path.GetFileName(value); }
            get { return _filePath; }
        }
        public float[] Peaks;

        public int Channels;
        public int FileDurTick;
        public int HeadTrimTick = 0;
        public int TailTrimTick = 0;
        public override int Duration {
            get { return FileDurTick - HeadTrimTick - TailTrimTick; }
            set { TailTrimTick = FileDurTick - HeadTrimTick - value; }
        }
        public override int GetMinDurTick(UProject project) { return 60; }

        public override UPart Clone() {
            return new UWavePart() {
                _filePath = _filePath,
                Peaks = Peaks,
                Channels = Channels,
                FileDurTick = FileDurTick,
                HeadTrimTick = HeadTrimTick,
                TailTrimTick = TailTrimTick,
            };
        }
    }
}
