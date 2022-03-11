using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using Serilog;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class UPart {
        [JsonProperty] public string name = "New Part";
        [JsonProperty] public string comment = string.Empty;
        [JsonProperty] public int trackNo;
        [JsonProperty] public int position = 0;

        [YamlIgnore] public virtual string DisplayName { get; }
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

        public override string DisplayName => name;

        public override int GetMinDurTick(UProject project) {
            return notes.Count > 0
                ? Math.Max(project.BarTicks, notes.Last().End)
                : project.BarTicks;
        }

        public int GetBarDurTick(UProject project) {
            int barTicks = project.BarTicks;
            return (int)Math.Ceiling((double)GetMinDurTick(project) / barTicks) * barTicks;
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
            Duration = GetBarDurTick(project);
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
                note.ExtendedDuration = note.duration;
                if (note.Prev != null && note.Prev.End == note.position && note.lyric.StartsWith("+")) {
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
                name = name,
                comment = comment,
                trackNo = trackNo,
                position = position,
                notes = new SortedSet<UNote>(notes.Select(note => note.Clone())),
                Duration = Duration,
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

        [YamlMember(Order = 100)] public string relativePath;
        [YamlMember(Order = 101)] public double fileDurationMs;
        [YamlMember(Order = 102)] public double skipMs;
        [YamlMember(Order = 103)] public double TrimMs;

        [YamlIgnore]
        public override string DisplayName => Missing ? $"[Missing] {name}" : name;
        [YamlIgnore]
        public override int Duration {
            get => fileDurTick;
            set { }
        }
        [YamlIgnore] bool Missing { get; set; }
        [YamlIgnore] public float[] Peaks { get; set; }
        [YamlIgnore] public float[] Samples { get; private set; }

        [YamlIgnore] public int channels;
        [YamlIgnore] public int fileDurTick;

        private TimeSpan duration;

        public override int GetMinDurTick(UProject project) { return project.MillisecondToTick(duration.TotalMilliseconds); }

        public override UPart Clone() {
            return new UWavePart() {
                _filePath = _filePath,
                Peaks = Peaks,
                channels = channels,
                fileDurTick = fileDurTick,
            };
        }

        private readonly object loadLockObj = new object();
        public void Load(UProject project) {
            try {
                using (var waveStream = Formats.Wave.OpenFile(FilePath)) {
                    duration = waveStream.TotalTime;
                    fileDurationMs = duration.TotalMilliseconds;
                    channels = waveStream.WaveFormat.Channels;
                }
            } catch (Exception e) {
                Log.Error(e, $"Failed to load wave part {FilePath}");
                Missing = true;
                if (fileDurationMs == 0) {
                    fileDurationMs = 10000;
                }
                duration = TimeSpan.FromMilliseconds(fileDurationMs);
            }
            fileDurTick = project.MillisecondToTick(fileDurationMs);
            lock (loadLockObj) {
                if (Samples != null || Missing) {
                    return;
                }
            }
            Task.Run(() => {
                using (var waveStream = Formats.Wave.OpenFile(FilePath)) {
                    var samples = Formats.Wave.GetStereoSamples(waveStream);
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
            fileDurTick = project.MillisecondToTick(duration.TotalMilliseconds);
        }

        public override void BeforeSave(UProject project, UTrack track) {
            relativePath = Path.GetRelativePath(Path.GetDirectoryName(project.FilePath), FilePath);
        }

        public override void AfterLoad(UProject project, UTrack track) {
            FilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project.FilePath), relativePath ?? ""));
            Load(project);
        }
    }
}
