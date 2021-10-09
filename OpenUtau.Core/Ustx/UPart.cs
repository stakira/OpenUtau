using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using Serilog;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class UPart {
        [JsonProperty] public string name = "New Part";
        [JsonProperty] public string comment = string.Empty;
        [JsonProperty] public int trackNo;
        [JsonProperty] public int position = 0;

        [YamlIgnore] public virtual int Duration { set; get; }
        [YamlIgnore] public int EndTick { get { return position + Duration; } }

        public UPart() { }

        public abstract int GetMinDurTick(UProject project);

        public virtual void BeforeSave(UProject project, UTrack track) { }
        public virtual void AfterLoad(UProject project, UTrack track) { }

        public virtual void Validate(UProject project, UTrack track) { }

        public abstract UPart Clone();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class UVoicePart : UPart {
        [JsonProperty]
        [YamlMember(Order = 100)]
        public SortedSet<UNote> notes = new SortedSet<UNote>();

        public override int GetMinDurTick(UProject project) {
            int durTick = 480;
            foreach (UNote note in notes)
                durTick = Math.Max(durTick, note.position + note.duration);
            return durTick;
        }

        public override void BeforeSave(UProject project, UTrack track) {
            foreach (var note in notes) {
                note.BeforeSave(project, track, this);
            }
        }

        public override void AfterLoad(UProject project, UTrack track) {
            foreach (var note in notes) {
                note.AfterLoad(project, track, this);
            }
        }

        public override void Validate(UProject project, UTrack track) {
            int barTicks = project.resolution * 4 / project.beatUnit * project.beatPerBar;
            Duration = (int)Math.Ceiling((double)GetMinDurTick(project) / barTicks) * barTicks;
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
                note.ExtendedDuration = note.duration;
                if (note.Prev != null && note.Prev.End == note.position && note.lyric.StartsWith("...")) {
                    note.Extends = note.Prev.Extends ?? note.Prev;
                    note.Extends.ExtendedDuration = note.End - note.Extends.position;
                } else {
                    note.Extends = null;
                }
            }
            foreach (UNote note in notes.Reverse()) {
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
        [YamlIgnore]
        public string FilePath {
            set {
                _filePath = value;
                name = Path.GetFileName(value);
            }
            get { return _filePath; }
        }

        [YamlMember(Order = 100)]
        public string relativePath;

        [YamlIgnore] public float[] Peaks { get; set; }
        [YamlIgnore] public float[] Samples { get; private set; }

        [YamlIgnore] public int Channels;
        [YamlIgnore] public int FileDurTick;
        [YamlIgnore] public int HeadTrimTick = 0;
        [YamlIgnore] public int TailTrimTick = 0;
        [YamlIgnore]
        public override int Duration {
            get { return FileDurTick - HeadTrimTick - TailTrimTick; }
            set { TailTrimTick = FileDurTick - HeadTrimTick - value; }
        }

        private TimeSpan duration;

        public override int GetMinDurTick(UProject project) { return project.MillisecondToTick(duration.TotalMilliseconds); }

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

        private readonly object loadLockObj = new object();
        public void Load(UProject project) {
            using (var waveStream = Formats.Wave.OpenFile(FilePath)) {
                duration = waveStream.TotalTime;
                FileDurTick = project.MillisecondToTick(duration.TotalMilliseconds);
                Channels = waveStream.WaveFormat.Channels;
            }
            lock (loadLockObj) {
                if (Samples != null) {
                    return;
                }
            }
            Task.Run(() => {
                using (var waveStream = Formats.Wave.OpenFile(FilePath)) {
                    var samples = Formats.Wave.GetSamples(waveStream);
                    lock (loadLockObj) {
                        Samples = samples;
                    }
                }
            });
        }

        public void BuildPeaks(IProgress<int> progress) {
            using (var waveStream = Formats.Wave.OpenFile(FilePath)) {
                var peaks = Formats.Wave.BuildPeaks(waveStream, progress);
                lock (loadLockObj) {
                    Peaks = peaks;
                }
            }
        }

        public override void Validate(UProject project, UTrack track) {
            FileDurTick = project.MillisecondToTick(duration.TotalMilliseconds);
        }

        public override void BeforeSave(UProject project, UTrack track) {
            relativePath = Path.GetRelativePath(Path.GetDirectoryName(project.FilePath), FilePath);
        }

        public override void AfterLoad(UProject project, UTrack track) {
            FilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project.FilePath), relativePath));
            Load(project);
        }
    }
}
