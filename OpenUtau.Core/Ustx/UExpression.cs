using System;
using System.Diagnostics;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace OpenUtau.Core.Ustx {
    public enum UExpressionType : int {
        Numerical = 0,
        Options = 1,
        Curve = 2,
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class UExpressionDescriptor {
        [JsonProperty] public string name;
        [JsonProperty] public string abbr;
        [JsonProperty] public UExpressionType type;
        [JsonProperty] public float min;
        [JsonProperty] public float max;
        [JsonProperty] public float defaultValue;
        [JsonProperty] public bool isFlag;
        [JsonProperty] public string flag;
        [JsonProperty] public string[] options;


        /// <summary>
        /// Constructor for Yaml deserialization
        /// </summary>
        public UExpressionDescriptor() { }

        public UExpressionDescriptor(string name, string abbr, float min, float max, float defaultValue, string flag = "") {
            this.name = name;
            this.abbr = abbr.ToLower();
            this.min = min;
            this.max = max;
            this.defaultValue = Math.Min(max, Math.Max(min, defaultValue));
            isFlag = !string.IsNullOrEmpty(flag);
            this.flag = flag;
        }

        public UExpressionDescriptor(string name, string abbr, bool isFlag, string[] options) {
            this.name = name;
            this.abbr = abbr.ToLower();
            type = UExpressionType.Options;
            min = 0;
            max = options.Length - 1;
            this.isFlag = isFlag;
            this.options = options;
        }

        public UExpression Create() {
            return new UExpression(this) {
                value = defaultValue,
            };
        }

        public UExpressionDescriptor Clone() {
            return new UExpressionDescriptor() {
                name = name,
                abbr = abbr,
                type = type,
                min = min,
                max = max,
                defaultValue = defaultValue,
                isFlag = isFlag,
                flag = flag,
                options = (string[])options?.Clone(),
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
