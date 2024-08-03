using System;
using System.Diagnostics;
using System.Linq;
using YamlDotNet.Serialization;

namespace OpenUtau.Core.Ustx {
    public enum UExpressionType : int {
        Numerical = 0,
        Options = 1,
        Curve = 2,
    }

    public class UExpressionDescriptor : IEquatable<UExpressionDescriptor> {
        public string name;
        public string abbr;
        public UExpressionType type;
        public float min;
        public float max;
        public float defaultValue;
        public bool isFlag;
        public string flag;
        public string[] options;

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

        public override string ToString() => $"{abbr.ToUpper()}: {name}";

        public bool Equals(UExpressionDescriptor other) {
            return this.name == other.name &&
                this.abbr == other.abbr &&
                this.type == other.type &&
                this.min == other.min &&
                this.max == other.max &&
                this.defaultValue == other.defaultValue &&
                this.isFlag == other.isFlag &&
                this.flag == other.flag &&
                ((this.options == null && other.options == null) || this.options.SequenceEqual(other.options));
        }
    }

    public class UExpression {
        [YamlIgnore] public UExpressionDescriptor descriptor;

        private float _value;

        public int? index;
        public string abbr;
        public float value {
            get => _value;
            set => _value = descriptor == null ? value
                : abbr == Format.Ustx.CLR ? value
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

        public override string ToString() => $"{abbr.ToUpper()}: {value}";
    }
}
