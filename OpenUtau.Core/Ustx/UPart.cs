using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using Serilog;
using OpenUtau.Core.Render;

namespace OpenUtau.Core.Ustx {
    public abstract class UPart {
        public string name = "New Part";
        public string comment = string.Empty;
        public int trackNo;
        public int position = 0;

        [YamlIgnore] public virtual string DisplayName { get; }
        [YamlIgnore] public virtual int Duration { set; get; }
        [YamlIgnore] public int EndTick { get { return position + Duration; } }

        public UPart() { }

        public abstract int GetMinDurTick(UProject project);

        public virtual void BeforeSave(UProject project, UTrack track) { }
        public virtual void AfterLoad(UProject project, UTrack track) { }

        public virtual void Validate(ValidateOptions options, UProject project, UTrack track) { }

        public abstract UPart Clone();
    }

    public class UVoicePart : UPart {
        [YamlMember(Order = 100)]
        public SortedSet<UNote> notes = new SortedSet<UNote>();
        [YamlMember(Order = 101)]
        public List<UCurve> curves = new List<UCurve>();

        [YamlIgnore] public List<RenderPhrase> renderPhrases = new List<RenderPhrase>();

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
            foreach (var curve in curves) {
                curve.descriptor = project.expressions[curve.abbr];
            }
        }

        public override void Validate(ValidateOptions options, UProject project, UTrack track) {
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
            if (!options.SkipPhonemizer) {
                track.Phonemizer.SetTiming(project.bpm, project.beatUnit, project.resolution);
                track.Phonemizer.SetUp(notes
                    .Where(n => !n.OverlapError)
                    .Where(n => n.Extends == null)
                    .Select(n => new Api.Phonemizer.Note {
                        lyric = n.lyric.Trim(),
                        tone = n.tone,
                        position = n.position,
                        duration = n.ExtendedDuration,
                    }).ToArray());
                foreach (UNote note in notes.Reverse()) {
                    note.Phonemize(project, track);
                }
                track.Phonemizer.CleanUp();
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
            }
            foreach (UNote note in notes) {
                note.Validate(options, project, track, this);
            }
            renderPhrases.Clear();
            renderPhrases.AddRange(RenderPhrase.FromPart(project, track, this));
        }

        public override UPart Clone() {
            return new UVoicePart() {
                name = name,
                comment = comment,
                trackNo = trackNo,
                position = position,
                notes = new SortedSet<UNote>(notes.Select(note => note.Clone())),
                curves = curves.Select(c => c.Clone()).ToList(),
                Duration = Duration,
            };
        }
    }

    public class UWavePart : UPart {
        string _filePath;

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
                using (var waveStream = Format.Wave.OpenFile(FilePath)) {
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
                using (var waveStream = Format.Wave.OpenFile(FilePath)) {
                    var samples = Format.Wave.GetStereoSamples(waveStream);
                    lock (loadLockObj) {
                        Samples = samples;
                    }
                }
            });
        }

        public void BuildPeaks(IProgress<int> progress) {
            using (var waveStream = Format.Wave.OpenFile(FilePath)) {
                var peaks = Format.Wave.BuildPeaks(waveStream, progress);
                lock (loadLockObj) {
                    Peaks = peaks;
                }
            }
        }

        public override void Validate(ValidateOptions options, UProject project, UTrack track) {
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
