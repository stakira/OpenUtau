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
        [JsonProperty] public readonly char flag;

        public UExpressionDescriptor(string name, string abbr, float min, float max, float defaultValue, char flag = '\0') {
            this.name = name;
            this.abbr = abbr.ToLower();
            this.min = min;
            this.max = max;
            this.defaultValue = Math.Min(max, Math.Max(min, defaultValue));
            this.flag = flag;
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

        private float _value;

        [JsonProperty]
        public bool overridden;
        [JsonProperty]
        public float value {
            get => overridden ? _value : descriptor.defaultValue;
            set {
                if (descriptor == null) {
                    _value = value;
                } else {
                    _value = Math.Min(descriptor.max, Math.Max(descriptor.min, value));
                    overridden = descriptor.defaultValue != value;
                }
            }
        }

        public UExpression(UExpressionDescriptor descriptor) {
            this.descriptor = descriptor;
        }
    }
}
