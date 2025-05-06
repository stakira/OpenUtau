using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Format {
    public static class SVP {
        public static UProject Load(string svpFilePath) {
            try {
                var settings = new JsonSerializerSettings {
                    Converters = new List<JsonConverter> { new AttributeDictionaryConverter() },
                    Error = (sender, args) => {
                        // Handle parsing errors
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
            var project = new UProject {
                name = Path.GetFileNameWithoutExtension(filePath),
                ustxVersion = new Version(0, 6),
                resolution = 480,
                FilePath = filePath,
                Saved = false
            };
            Ustx.AddDefaultExpressions(project);

            // Extract singer's name from the database section
            //string singerName = svpProject.database?.name ?? "Select Singer";

            // Convert time signatures
            project.timeSignatures = svpProject.time?.meter?.ConvertAll(m => 
                new UTimeSignature(m.index, m.numerator, m.denominator)) 
                ?? new List<UTimeSignature> { new UTimeSignature(0, 4, 4) };

            // Convert tempos
            project.tempos = svpProject.time?.tempo?.ConvertAll(t => 
                new UTempo(ConvertTimeToTicks(t.position, t.bpm), t.bpm))
                ?? new List<UTempo> { new UTempo(0, 120) };

            // Convert tracks
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
                    
                    int rawTickOn = ConvertTimeToTicks(svpNote.onset, GetBpmAtTime(svpProject, svpNote.onset));
                    int rawDuration = ConvertTimeToTicks(svpNote.duration, GetBpmAtTime(svpProject, svpNote.onset));

                    // Quantize to nearest 1/16th note (30 ticks)
                    int tickOn = Quantize(rawTickOn, 30);
                    int duration = Math.Max(Quantize(rawDuration, 30), 30);

                    var note = project.CreateNote(
                        svpNote.pitch,
                        tickOn,
                        duration);
                    
                    note.lyric = string.IsNullOrEmpty(svpNote.lyrics) ? "a" : svpNote.lyrics;

                    // Handle vibrato parameters
                    if (svpNote.attributes != null) {
                        note.vibrato.length = GetAttributeValue(svpNote.attributes, "tF0VbrLeft", 0.3f);
                        note.vibrato.depth = GetAttributeValue(svpNote.attributes, "dF0Vbr", 0f) / 100f;
                        note.vibrato.period = 1000f / GetAttributeValue(svpNote.attributes, "fF0Vbr", 5.7f);
                        note.vibrato.@in = GetAttributeValue(svpNote.attributes, "tF0VbrStart", 0.1f);
                    }

                    part.notes.Add(note);
                }

                if (part.notes.Count > 0) {
                    part.Duration = part.notes.Max.End;
                    project.tracks.Add(track);
                    project.parts.Add(part);
                }
            }

            project.ValidateFull();
            return project;
        }

        private static int Quantize(int ticks, int gridSize) {
            return (int)Math.Round(ticks / (double)gridSize) * gridSize;
        }

        private static float GetAttributeValue(Dictionary<string, object> attributes, string key, float defaultValue) {
            if (attributes != null && attributes.TryGetValue(key, out var value)) {
                if (value is double d) return (float)d;
                if (value is float f) return f;
                if (value is int i) return i;
            }
            return defaultValue;
        }

        private static int ConvertTimeToTicks(long nanoseconds, double bpm) {
            double quarterNotes = nanoseconds / (500000000.0 * (120.0 / bpm));
            return (int)Math.Round(quarterNotes * 480);
        }

        private static double GetBpmAtTime(SynthVProject project, long timeNs) {
            if (project.time?.tempo == null || project.time.tempo.Count == 0) {
                return 120.0;
            }

            var tempo = project.time.tempo
                .Where(t => t.position <= timeNs)
                .OrderByDescending(t => t.position)
                .FirstOrDefault();

            return tempo?.bpm ?? 120.0;
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
        }

        // Custom JSON converter for handling mixed attribute types
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