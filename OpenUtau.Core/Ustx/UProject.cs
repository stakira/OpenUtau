using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace OpenUtau.Core.Ustx {
    public class UProject {
        public string name = "New Project";
        public string comment = string.Empty;
        public string outputDir = "Vocal";
        public string cacheDir = "UCache";
        [YamlMember(SerializeAs = typeof(string))]
        public Version ustxVersion;

        public double bpm = 120;
        public int beatPerBar = 4;
        public int beatUnit = 4;
        public int resolution = 480;

        public Dictionary<string, UExpressionDescriptor> expressions = new Dictionary<string, UExpressionDescriptor>();
        public List<UTrack> tracks = new List<UTrack>();
        [YamlIgnore] public List<UPart> parts = new List<UPart>();

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
        [YamlIgnore] public int EndTick => parts.Count == 0 ? 0 : parts.Max(p => p.EndTick);
        [YamlIgnore] public int BarTicks => resolution * 4 * beatPerBar / beatUnit;

        public void RegisterExpression(UExpressionDescriptor descriptor) {
            if (!expressions.ContainsKey(descriptor.abbr)) {
                expressions.Add(descriptor.abbr, descriptor);
            }
        }

        public UNote CreateNote() {
            UNote note = UNote.Create();
            note.pitch.AddPoint(new PitchPoint(-40, 0));
            note.pitch.AddPoint(new PitchPoint(40, 0));
            return note;
        }

        public UNote CreateNote(int noteNum, int posTick, int durTick) {
            var note = CreateNote();
            note.tone = noteNum;
            note.position = posTick;
            note.duration = durTick;
            return note;
        }

        public int MillisecondToTick(double ms) {
            return MusicMath.MillisecondToTick(ms, bpm, beatUnit, resolution);
        }

        public double TickToMillisecond(double tick) {
            return MusicMath.TickToMillisecond(tick, bpm, beatUnit, resolution);
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
