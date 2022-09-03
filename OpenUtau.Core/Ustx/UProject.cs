using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Util;
using YamlDotNet.Serialization;

namespace OpenUtau.Core.Ustx {
    public class UTempo {
        public int position;
        public double bpm;

        public UTempo() { }
        public UTempo(int position, double bpm) {
            this.position = position;
            this.bpm = bpm;
        }
        public override string ToString() => $"{bpm}@{position}";
    }

    public class UTimeSignature {
        public int barPosition;
        public int beatPerBar;
        public int beatUnit;

        public UTimeSignature() { }
        public UTimeSignature(int barPosition, int beatPerBar, int beatUnit) {
            this.barPosition = barPosition;
            this.beatPerBar = beatPerBar;
            this.beatUnit = beatUnit;
        }
        public override string ToString() => $"{beatPerBar}/{beatUnit}@bar{barPosition}";
    }

    public class UProject {
        public string name = "New Project";
        public string comment = string.Empty;
        public string outputDir = "Vocal";
        public string cacheDir = "UCache";
        [YamlMember(SerializeAs = typeof(string))]
        public Version ustxVersion;
        public int resolution = 480;

        [Obsolete("Since ustx v0.6")] public double bpm = 120;
        [Obsolete("Since ustx v0.6")] public int beatPerBar = 4;
        [Obsolete("Since ustx v0.6")] public int beatUnit = 4;

        public Dictionary<string, UExpressionDescriptor> expressions = new Dictionary<string, UExpressionDescriptor>();
        public List<UTimeSignature> timeSignatures;
        public List<UTempo> tempos;
        public List<UTrack> tracks;
        [YamlIgnore] public List<UPart> parts;

        /// <summary>
        /// Transient field used for serialization.
        /// </summary>
        public List<UVoicePart> voiceParts;
        /// <summary>
        /// Transient field used for serialization.
        /// </summary>
        public List<UWavePart> waveParts;

        [YamlIgnore] public string FilePath { get; set; }
        [YamlIgnore] public bool Saved { get; set; } = false;
        [YamlIgnore] public int EndTick => parts.Count == 0 ? 0 : parts.Max(p => p.End);

        [YamlIgnore] public readonly TimeAxis timeAxis = new TimeAxis();

        public UProject() {
            timeSignatures = new List<UTimeSignature> { new UTimeSignature(0, 4, 4) };
            tempos = new List<UTempo> { new UTempo(0, 120) };
            tracks = new List<UTrack>();
            parts = new List<UPart>();
            timeAxis.BuildSegments(this);
        }

        public void RegisterExpression(UExpressionDescriptor descriptor) {
            if (!expressions.ContainsKey(descriptor.abbr)) {
                expressions.Add(descriptor.abbr, descriptor);
            }
        }

        public UNote CreateNote() {
            UNote note = UNote.Create();
            int start = NotePresets.Default.DefaultPortamento.PortamentoStart;
            int length = NotePresets.Default.DefaultPortamento.PortamentoLength;
            note.pitch.AddPoint(new PitchPoint(start, 0));
            note.pitch.AddPoint(new PitchPoint(start + length, 0));
            return note;
        }

        public UNote CreateNote(int noteNum, int posTick, int durTick) {
            var note = CreateNote();
            note.tone = noteNum;
            note.position = posTick;
            note.duration = durTick;
            return note;
        }

        public void BeforeSave() {
            foreach (var track in tracks) {
                track.BeforeSave();
            }
            foreach (var part in parts) {
                part.BeforeSave(this, tracks[part.trackNo]);
            }
            voiceParts = parts
                .Where(part => part is UVoicePart)
                .Select(part => part as UVoicePart)
                .OrderBy(part => part.trackNo)
                .ThenBy(part => part.position)
                .ToList();
            waveParts = parts
                .Where(part => part is UWavePart)
                .Select(part => part as UWavePart)
                .OrderBy(part => part.trackNo)
                .ThenBy(part => part.position)
                .ToList();
        }

        public UProject CloneAsTemplate() {
            var project = new UProject() {
                ustxVersion = ustxVersion,
            };
            foreach (var kv in expressions) {
                project.expressions.Add(kv.Key, kv.Value.Clone());
            }
            return project;
        }

        public void AfterSave() {
            voiceParts = null;
            waveParts = null;
        }

        public void AfterLoad() {
            foreach (var track in tracks) {
                track.AfterLoad(this);
            }
            if (voiceParts != null) {
                parts.AddRange(voiceParts);
                voiceParts = null;
            }
            if (waveParts != null) {
                parts.AddRange(waveParts);
                waveParts = null;
            }
            foreach (var part in parts) {
                part.AfterLoad(this, tracks[part.trackNo]);
            }
        }

        public void Validate(ValidateOptions options) {
            if (!options.SkipTiming) {
                timeSignatures.Sort((lhs, rhs) => lhs.barPosition.CompareTo(rhs.barPosition));
                tempos.Sort((lhs, rhs) => lhs.position.CompareTo(rhs.position));
                timeAxis.BuildSegments(this);
            }
            if (options.Part == null) {
                foreach (var track in tracks) {
                    track.Validate(options, this);
                }
            }
            foreach (var part in parts) {
                if (options.Part == null || options.Part == part) {
                    part.Validate(options, this, tracks[part.trackNo]);
                }
            }
        }

        public void ValidateFull() {
            Validate(new ValidateOptions());
        }
    }
}
