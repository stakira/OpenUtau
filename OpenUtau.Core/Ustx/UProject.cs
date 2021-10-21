using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public class UProject {
        [JsonProperty] public string name = "New Project";
        [JsonProperty] public string comment = string.Empty;
        [JsonProperty] public string outputDir = "Vocal";
        [JsonProperty] public string cacheDir = "UCache";
        [JsonProperty]
        [YamlMember(SerializeAs = typeof(string))]
        public Version ustxVersion;

        [JsonProperty] public double bpm = 120;
        [JsonProperty] public int beatPerBar = 4;
        [JsonProperty] public int beatUnit = 4;
        [JsonProperty] public int resolution = 480;

        [JsonProperty] public Dictionary<string, UExpressionDescriptor> expressions = new Dictionary<string, UExpressionDescriptor>();
        [JsonProperty] public List<UTrack> tracks = new List<UTrack>();
        [JsonProperty] [YamlIgnore] public List<UPart> parts = new List<UPart>();

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

        [YamlIgnore]
        public int EndTick {
            get {
                int lastTick = 0;
                foreach (var part in parts) {
                    lastTick = Math.Max(lastTick, part.EndTick);
                }
                return lastTick;
            }
        }
        [YamlIgnore] public int BarTicks => resolution * 4 * beatPerBar / beatUnit;

        public void RegisterExpression(UExpressionDescriptor descriptor) {
            if (!expressions.ContainsKey(descriptor.abbr)) {
                expressions.Add(descriptor.abbr, descriptor);
            }
        }

        public UNote CreateNote() {
            UNote note = UNote.Create();
            note.pitch.AddPoint(new PitchPoint(-25, 0));
            note.pitch.AddPoint(new PitchPoint(25, 0));
            return note;
        }

        public UNote CreateNote(int noteNum, int posTick, int durTick) {
            var note = CreateNote();
            note.tone = noteNum;
            note.position = posTick;
            note.duration = durTick;
            note.pitch.data[1].X = (float)Math.Min(25, DocManager.Inst.Project.TickToMillisecond(note.duration) / 2);
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

        public void Validate() {
            foreach (var part in parts) {
                part.Validate(this, tracks[part.trackNo]);
            }
        }
    }
}
