using System;
using System.Diagnostics;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public class UExpressionDescriptor {
        [JsonProperty] public string name;
        [JsonProperty] public string abbr;
        [JsonProperty] public float min;
        [JsonProperty] public float max;
        [JsonProperty] public float defaultValue;
        [JsonProperty] public string flag;
        [JsonProperty] public bool isNoteExpression;

        /// <summary>
        /// Constructor for Yaml deserialization
        /// </summary>
        public UExpressionDescriptor() { }

        public UExpressionDescriptor(string name, string abbr, float min, float max, float defaultValue, string flag = "", bool isNoteExpression = false) {
            this.name = name;
            this.abbr = abbr.ToLower();
            this.min = min;
            this.max = max;
            this.defaultValue = Math.Min(max, Math.Max(min, defaultValue));
            this.flag = flag;
            this.isNoteExpression = isNoteExpression;
        }

        public UExpression Create() {
            return new UExpression(this) {
                value = defaultValue,
            };
        }

        public override string ToString() => name;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class UExpression {
        [YamlIgnore] public UExpressionDescriptor descriptor;

        private float _value;

        [JsonProperty]
        public int? index;
        [JsonProperty]
        public string abbr;
        [JsonProperty]
        public float value {
            get => _value;
            set => _value = descriptor == null
                ? value
                : Math.Min(descriptor.max, Math.Max(descriptor.min, value));
        }

        /// <summary>
        /// Constructor for Yaml deserialization
        /// </summary>
        public UExpression() { }

        public UExpression(UExpressionDescriptor descriptor) {
            Trace.Assert(descriptor != null);
            this.descriptor = descriptor;
            abbr = descriptor.abbr;
        }

        public UExpression(string abbr) {
            this.abbr = abbr;
        }

        public UExpression Clone() {
            return new UExpression(descriptor) {
                index = index,
                value = value,
            };
        }
    }
}
