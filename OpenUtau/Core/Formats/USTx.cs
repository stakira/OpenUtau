using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Formats {
    class Ustx {
        public static readonly Version kUstxVersion = new Version(0, 1);

        public static UProject Create() {
            UProject project = new UProject() { Saved = false };
            project.RegisterExpression(new UExpressionDescriptor("velocity", "vel", 0, 200, 100));
            project.RegisterExpression(new UExpressionDescriptor("volume", "vol", 0, 200, 100));
            project.RegisterExpression(new UExpressionDescriptor("gender", "gen", -100, 100, 0));
            project.RegisterExpression(new UExpressionDescriptor("lowpass", "lpf", 0, 100, 0));
            project.RegisterExpression(new UExpressionDescriptor("highpass", "hpf", 0, 100, 0));
            project.RegisterExpression(new UExpressionDescriptor("accent", "acc", 0, 200, 100));
            project.RegisterExpression(new UExpressionDescriptor("decay", "dec", 0, 100, 0));
            return project;
        }

        public static void Save(string filePath, UProject project) {
            project.ustxVersion = kUstxVersion;
            project.BeforeSave();
            File.WriteAllText(filePath, JsonConvert.SerializeObject(
                project,
                Formatting.Indented,
                new VersionConverter(),
                new UPartConverter(),
                new UExpressionConverter()), Encoding.UTF8);
        }

        public static UProject Load(string filePath) {
            UProject project = JsonConvert.DeserializeObject<UProject>(
                File.ReadAllText(filePath, Encoding.UTF8),
                new VersionConverter(),
                new UPartConverter(),
                new UExpressionConverter());
            project.filePath = filePath;
            project.AfterLoad();
            project.Validate();
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

    public class UExpressionConverter : JsonConverter<UExpression> {
        public override UExpression ReadJson(JsonReader reader, Type objectType, UExpression existingValue, bool hasExistingValue, JsonSerializer serializer) {
            return new UExpression(null) {
                value = (float)(double)reader.Value,
            };
        }

        public override void WriteJson(JsonWriter writer, UExpression value, JsonSerializer serializer) {
            var exp = value;
            writer.WriteValue(exp.value);
        }
    }
}
