using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core.Formats {
    public class Ustx {
        public static readonly Version kUstxVersion = new Version(0, 5);

        public static void AddBuiltInExpressions(UProject project) {
            project.RegisterExpression(new UExpressionDescriptor("dynamics", "dyn", 0, 200, 100) { type = UExpressionType.Curve });
            project.RegisterExpression(new UExpressionDescriptor("pitch deviation", "pit", -300, 300, 0) { type = UExpressionType.Curve });
            project.RegisterExpression(new UExpressionDescriptor("voice color", "clr", false, new string[0]));
            project.RegisterExpression(new UExpressionDescriptor("resampler engine", "eng", false, new string[] { "", WorldlineResampler.name }));
            project.RegisterExpression(new UExpressionDescriptor("velocity", "vel", 0, 200, 100));
            project.RegisterExpression(new UExpressionDescriptor("volume", "vol", 0, 200, 100));
            project.RegisterExpression(new UExpressionDescriptor("attack", "atk", 0, 200, 100));
            project.RegisterExpression(new UExpressionDescriptor("decay", "dec", 0, 100, 0));
        }

        public static void AddDefaultExpressions(UProject project) {
            AddBuiltInExpressions(project);
            project.RegisterExpression(new UExpressionDescriptor("gender", "gen", -100, 100, 0, "g"));
            project.RegisterExpression(new UExpressionDescriptor("breath", "bre", 0, 100, 0, "B"));
            project.RegisterExpression(new UExpressionDescriptor("lowpass", "lpf", 0, 100, 0, "H"));
            project.RegisterExpression(new UExpressionDescriptor("modulation", "mod", 0, 100, 0));
            project.RegisterExpression(new UExpressionDescriptor("alternate", "alt", 0, 16, 0));
            project.RegisterExpression(new UExpressionDescriptor("tone shift", "shft", -36, 36, 0));
        }

        public static UProject Create() {
            UProject project = new UProject() { Saved = false };
            AddDefaultExpressions(project);
            return project;
        }

        public static void Save(string filePath, UProject project) {
            project.ustxVersion = kUstxVersion;
            project.FilePath = filePath;
            project.BeforeSave();
            File.WriteAllText(filePath, Yaml.DefaultSerializer.Serialize(project), Encoding.UTF8);
            project.Saved = true;
            project.AfterSave();
        }

        public static UProject Load(string filePath) {
            string text = File.ReadAllText(filePath, Encoding.UTF8);
            UProject project = text.StartsWith("{")
                ? JsonConvert.DeserializeObject<UProject>(text, new VersionConverter(), new UPartConverter())
                : Yaml.DefaultDeserializer.Deserialize<UProject>(text);
            AddDefaultExpressions(project);
            project.FilePath = filePath;
            project.Saved = true;
            project.AfterLoad();
            project.Validate();
            if (project.ustxVersion < kUstxVersion) {
                Log.Information($"Upgrading project from {project.ustxVersion} to {kUstxVersion}");
            }
            if (project.ustxVersion < new Version(0, 4)) {
                if (project.expressions.TryGetValue("acc", out var exp) && exp.name == "accent") {
                    project.expressions.Remove("acc");
                    exp.abbr = "atk";
                    exp.name = "attack";
                    project.expressions["atk"] = exp;
                    project.parts
                        .Where(part => part is UVoicePart)
                        .Select(part => part as UVoicePart)
                        .SelectMany(part => part.notes)
                        .SelectMany(note => note.phonemeExpressions)
                        .Where(exp => exp.abbr == "acc")
                        .ToList()
                        .ForEach(exp => exp.abbr = "atk");
                    project.Validate();
                }
            }
            if (project.ustxVersion < new Version(0, 5)) {
                project.parts
                    .Where(part => part is UVoicePart)
                    .Select(part => part as UVoicePart)
                    .SelectMany(part => part.notes)
                    .Where(note => note.lyric.StartsWith("..."))
                    .ToList()
                    .ForEach(note => note.lyric = note.lyric.Replace("...", "+"));
                project.Validate();
            }
            project.ustxVersion = kUstxVersion;
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
