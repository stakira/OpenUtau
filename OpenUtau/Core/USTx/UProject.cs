using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public class UProject {
        [JsonProperty] public string name = "New Project";
        [JsonProperty] public string comment = string.Empty;
        public string filePath;
        [JsonProperty] public string outputDir = "Vocal";
        [JsonProperty] public string cacheDir = "UCache";
        [JsonProperty] public Version ustxVersion;

        [JsonProperty] public double bpm = 120;
        [JsonProperty] public int beatPerBar = 4;
        [JsonProperty] public int beatUnit = 4;
        [JsonProperty] public int resolution = 480;

        [JsonProperty] public Dictionary<string, UExpressionDescriptor> expressions = new Dictionary<string, UExpressionDescriptor>();
        [JsonProperty] public List<UTrack> tracks = new List<UTrack>();
        [JsonProperty] public List<UPart> parts = new List<UPart>();

        public List<USinger> singers = new List<USinger>();
        public bool Saved { get; set; } = false;

        public void RegisterExpression(UExpressionDescriptor def) {
            expressions.Add(def.abbr, def);
        }

        public UNote CreateNote() {
            UNote note = UNote.Create();
            foreach (var pair in expressions) {
                note.expressions.Add(pair.Key, pair.Value.Create());
            }
            note.pitch.AddPoint(new PitchPoint(-25, 0));
            note.pitch.AddPoint(new PitchPoint(25, 0));
            return note;
        }

        public UNote CreateNote(int noteNum, int posTick, int durTick) {
            var note = CreateNote();
            note.noteNum = noteNum;
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

        public int EndTick {
            get {
                int lastTick = 0;
                foreach (var part in parts) {
                    lastTick = Math.Max(lastTick, part.EndTick);
                }
                return lastTick;
            }
        }

        public void Validate() {
            foreach (var part in parts) {
                part.Validate(this);
            }
        }
    }
}
