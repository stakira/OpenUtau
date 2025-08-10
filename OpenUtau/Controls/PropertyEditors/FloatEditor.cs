using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using ReactiveUI;

namespace OpenUtau.Controls {
    public class FloatEditor : TextBox {
        protected override Type StyleKeyOverride => typeof(TextBox);
        public static readonly DirectProperty<FloatEditor, float> ValueProperty =
            AvaloniaProperty.RegisterDirect<FloatEditor, float>(
                nameof(Value),
                o => o.Value,
                (o, v) => o.Value = v,
                defaultBindingMode: BindingMode.TwoWay);
        private float _value = 0;

        public FloatEditor() {
            Text = "0";
            this.WhenAnyValue(x => x.Text)
                .Subscribe((text => {
                    OnTextChanged(text);
                }));
        }

        public float Value {
            get => _value;
            set {
                if (SetAndRaise(ValueProperty, ref _value, value)) {
                    Text = value.ToString();
                }
            }
        }

        protected void OnTextChanged(string? newText) {
            if (!IsKeyboardFocusWithin) {
                return;
            }

            if (newText != null && float.TryParse(newText, out float newValue)) {
                Value = newValue;
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e) {
            base.OnLostFocus(e);
            if (!float.TryParse(Text, out float newValue)) {
                Text = Value.ToString();
            }
        }
    }
}
