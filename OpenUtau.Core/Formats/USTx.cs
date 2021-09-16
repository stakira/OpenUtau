using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core.Formats {
    public class Ustx {
        public static readonly Version kUstxVersion = new Version(0, 2);

        public static void AddBuiltInExpressions(UProject project) {
            project.RegisterExpression(new UExpressionDescriptor("velocity", "vel", 0, 200, 100));
            project.RegisterExpression(new UExpressionDescriptor("volume", "vol", 0, 200, 100));
            project.RegisterExpression(new UExpressionDescriptor("accent", "acc", 0, 200, 100));
            project.RegisterExpression(new UExpressionDescriptor("decay", "dec", 0, 100, 0));
        }

        public static void AddDefaultExpressions(UProject project) {
            AddBuiltInExpressions(project);
            project.RegisterExpression(new UExpressionDescriptor("gender", "gen", -100, 100, 0, "g"));
            project.RegisterExpression(new UExpressionDescriptor("breath", "bre", 0, 100, 0, "B"));
            project.RegisterExpression(new UExpressionDescriptor("lowpass", "lpf", 0, 100, 0, "H"));
            project.RegisterExpression(new UExpressionDescriptor("modulation", "mod", 0, 100, 0));
        }

        public static UProject Create() {
            UProject project = new UProject() { Saved = false };
            AddDefaultExpressions(project);
            return project;
        }

        public static void Save(string filePath, UProject project) {
            project.ustxVersion = kUstxVersion;
            project.BeforeSave();
            File.WriteAllText(filePath, JsonConvert.SerializeObject(
                project,
                Formatting.Indented,
                new VersionConverter(),
                new UPartConverter()), Encoding.UTF8);
            project.FilePath = filePath;
            project.Saved = true;
        }

        public static UProject Load(string filePath) {
            UProject project = JsonConvert.DeserializeObject<UProject>(
                File.ReadAllText(filePath, Encoding.UTF8),
                new VersionConverter(),
                new UPartConverter());
            AddDefaultExpressions(project);
            project.FilePath = filePath;
            project.Saved = true;
            project.AfterLoad();
            project.Validate();
            if (project.ustxVersion < kUstxVersion) {
                Log.Information($"Upgrading project from {project.ustxVersion} to {kUstxVersion}");
            }
            if (project.ustxVersion == new Version(0, 1)) {
                project.parts
                    .Where(part => part is UVoicePart)
                    .Select(part => part as UVoicePart)
                    .SelectMany(part => part.notes)
                    .ToList()
                    .ForEach(note => {
                        foreach (var kv in note.expressions) {
                            if (kv.Value != null) {
                                foreach (var phoneme in note.phonemes) {
                                    phoneme.SetExpression(project, kv.Key, (float)kv.Value.Value);
                                }
                            }
                        }
                        note.expressions = null;
                    });
                project.ustxVersion = new Version(0, 2);
            }
            return project;
        }
    }

    public class VersionConverter : JsonConverter<Version> {
        public override void WriteJson(JsonWriter writer, Version value, JsonSerializer serializer) {
            writer.WriteValue(value.ToString());
        }

        public override Version ReadJson(JsonReader reader, Type objectType, Version existingValue, bool hasExistingValue, JsonSerializer serializer) {
            return new Version((string)reader.Value);
        }
    }

    public class UPartConverter : JsonConverter<UPart> {
        public override UPart ReadJson(JsonReader reader, Type objectType, UPart existingValue, bool hasExistingValue, JsonSerializer serializer) {
            JObject jObj = JObject.Load(reader);
            UPart part;
            if (jObj.Property("notes") != null) {
                part = new UVoicePart();
            } else {
                part = new UWavePart();
            }
            serializer.Populate(jObj.CreateReader(), part);
            return part;
        }

        public override bool CanWrite => false;
        public override void WriteJson(JsonWriter writer, UPart value, JsonSerializer serializer) { }
    }
}
