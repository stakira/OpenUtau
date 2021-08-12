using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public class UExpressionDescriptor {
        [JsonProperty] public readonly string name;
        [JsonProperty] public readonly string abbr;
        [JsonProperty] public readonly float min;
        [JsonProperty] public readonly float max;
        [JsonProperty] public readonly float defaultValue;

        public UExpressionDescriptor(string name, string abbr, float min, float max, float defaultValue) {
            this.name = name;
            this.abbr = abbr.ToLower();
            this.min = min;
            this.max = max;
            this.defaultValue = Math.Min(max, Math.Max(min, defaultValue));
        }

        public UExpression Create() {
            return new UExpression(this) {
                value = defaultValue,
            };
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class UExpression {
        public UExpressionDescriptor descriptor;

        float _value;

        [JsonProperty]
        public float value {
            get => _value;
            set => _value = descriptor == null
                ? value
                : Math.Min(descriptor.max, Math.Max(descriptor.min, value));
        }

        public UExpression(UExpressionDescriptor descriptor) {
            this.descriptor = descriptor;
        }
    }
}
