using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenUtau.Core.Ustx;
using OpenUtau.Core;
using Serilog;

namespace OpenUtau.Core.Format {
    public static class SVP {
        public static UProject Load(string svpFilePath) {
            try {
                var settings = new JsonSerializerSettings {
                    Converters = new List<JsonConverter> { new AttributeDictionaryConverter() },
                    Error = (sender, args) => {
                        args.ErrorContext.Handled = true;
                    }
                };

                var json = File.ReadAllText(svpFilePath);
                var svpProject = JsonConvert.DeserializeObject<SynthVProject>(json, settings);
                
                if (svpProject == null) {
                    throw new FileFormatException("Failed to parse SVP file");
                }

                return ConvertToUstx(svpProject, svpFilePath);
            } catch (Exception ex) {
                throw new FileFormatException($"Error loading SVP file: {ex.Message}", ex);
            }
        }

        private static UProject ConvertToUstx(SynthVProject svpProject, string filePath) {
            var project = new UProject {};
            Ustx.AddDefaultExpressions(project);

            // 1. Use SVP's time signature and tempo data directly
            project.timeSignatures = svpProject.time?.meter?.ConvertAll(m =>
                new UTimeSignature(m.index, m.numerator, m.denominator))
                ?? new List<UTimeSignature> { new UTimeSignature(0, 4, 4) };

            project.tempos = svpProject.time?.tempo?.Select(t =>
                new UTempo(
                    (int)Math.Round(t.position / 1_042_230.0), // convert to ms
                    t.bpm)
            ).ToList() ?? new List<UTempo>();

            // 2. Build final TimeAxis using actual tempo map
            var timeAxis = new TimeAxis();
            timeAxis.BuildSegments(project);

            // Convert tracks using final TimeAxis
            foreach (var svpTrack in svpProject.tracks ?? new List<SynthVTrack>()) {
                if (svpTrack.mainGroup?.notes == null || svpTrack.mainGroup.notes.Count == 0) {
                    continue;
                }

                var track = new UTrack(svpTrack.name ?? "Track") {
                    TrackNo = project.tracks.Count
                };

                var part = new UVoicePart {
                    name = svpTrack.name ?? "Part",
                    position = 0,
                    trackNo = track.TrackNo
                };

                foreach (var svpNote in svpTrack.mainGroup.notes) {
                    if (svpNote.musicalType != "singing") {
                        continue;
                    }
                    // nanoseconds to milliseconds
                    double onsetMs = svpNote.onset / 1_468_750.0;
                    double durationMs = svpNote.duration / 1_468_750.0;
                    Log.Error($"Note onset: {svpNote.onset} ticks ({onsetMs:F3} ms)");

                    int tickOn = timeAxis.MsPosToTickPos(Math.Round(onsetMs));
                    int tickOff = timeAxis.MsPosToTickPos(Math.Round(onsetMs + durationMs));
                    int duration = Math.Max(tickOff - tickOn, 1);
                    Log.Error($"Note tick: {tickOn}");

                    var note = project.CreateNote(
                        svpNote.pitch,
                        tickOn,
                        duration);
                    
                    note.lyric = string.IsNullOrEmpty(svpNote.lyrics) ? "a" : svpNote.lyrics;
                    if (note.lyric == "-") {
                        note.lyric = "+~";
                    }
                    
                    if (svpNote.attributes != null) {
                        note.vibrato.length = GetAttributeValue(svpNote.attributes, "tF0VbrLeft", 0.3f);
                        note.vibrato.depth = GetAttributeValue(svpNote.attributes, "dF0Vbr", 0f) / 100f;
                        note.vibrato.period = 1000f / GetAttributeValue(svpNote.attributes, "fF0Vbr", 5.7f);
                        note.vibrato.@in = GetAttributeValue(svpNote.attributes, "tF0VbrStart", 0.1f);
                    }

                    part.notes.Add(note);
                }

                if (part.notes.Count > 0) {
                    part.Duration = part.notes.Max(n => n.End);
                    project.tracks.Add(track);
                    project.parts.Add(part);
                }
            }

            project.ValidateFull();
            return project;
        }

        private static float GetAttributeValue(Dictionary<string, object> attributes, string key, float defaultValue) {
            if (attributes != null && attributes.TryGetValue(key, out var value)) {
                if (value is double d) return (float)d;
                if (value is float f) return f;
                if (value is int i) return i;
            }
            return defaultValue;
        }

        // Model classes
        private class SynthVProject {
            public int version { get; set; }
            public SynthVTime time { get; set; }
            public List<SynthVTrack> tracks { get; set; }
            public SynthVDatabase database { get; set; }
        }

        private class SynthVTime {
            public List<SynthVMeter> meter { get; set; }
            public List<SynthVTempo> tempo { get; set; }
        }

        private class SynthVMeter {
            public int index { get; set; }
            public int numerator { get; set; }
            public int denominator { get; set; }
        }

        private class SynthVTempo {
            public long position { get; set; }
            public double bpm { get; set; }
        }

        private class SynthVTrack {
            public string name { get; set; }
            public SynthVGroup mainGroup { get; set; }
        }

        private class SynthVGroup {
            public List<SynthVNote> notes { get; set; }
        }

        private class SynthVDatabase {
            public string name { get; set; }
        }

        private class SynthVNote {
            public string musicalType { get; set; }
            public long onset { get; set; }
            public long duration { get; set; }
            public string lyrics { get; set; }
            public int pitch { get; set; }

            [JsonConverter(typeof(AttributeDictionaryConverter))]
            public Dictionary<string, object> attributes { get; set; }

            [JsonProperty("pitchDelta")]
            public SynthVUcurve pitchDelta { get; set; }
        }

        public class SynthVUcurve {
            public string shape { get; set; }
            public List<float> x { get; set; }
            public List<float> y { get; set; }
        }

        private class AttributeDictionaryConverter : JsonConverter {
            public override bool CanConvert(Type objectType) {
                return objectType == typeof(Dictionary<string, object>);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
                var dictionary = new Dictionary<string, object>();
                JObject obj = JObject.Load(reader);

                foreach (var property in obj.Properties()) {
                    switch (property.Value.Type) {
                        case JTokenType.Boolean:
                            dictionary[property.Name] = property.Value.Value<bool>();
                            break;
                        case JTokenType.Float:
                            dictionary[property.Name] = property.Value.Value<double>();
                            break;
                        case JTokenType.Integer:
                            dictionary[property.Name] = property.Value.Value<double>();
                            break;
                        case JTokenType.String:
                            dictionary[property.Name] = property.Value.Value<string>();
                            break;
                        default:
                            dictionary[property.Name] = property.Value.ToString();
                            break;
                    }
                }

                return dictionary;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
                serializer.Serialize(writer, value);
            }
        }
    }
}
