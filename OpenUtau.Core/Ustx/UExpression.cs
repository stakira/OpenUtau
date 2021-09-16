using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public class UExpressionDescriptor {
        [JsonProperty] public readonly string name;
        [JsonProperty] public readonly string abbr;
        [JsonProperty] public readonly float min;
        [JsonProperty] public readonly float max;
        [JsonProperty] public readonly float defaultValue;
        [JsonProperty] public readonly string flag;
        [JsonProperty] public readonly bool isNoteExpression;

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
        public UExpressionDescriptor descriptor;

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
