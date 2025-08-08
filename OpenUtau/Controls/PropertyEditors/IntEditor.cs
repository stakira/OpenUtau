using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using ReactiveUI;

namespace OpenUtau.Controls{
    public class IntEditor : TextBox
    {
        protected override Type StyleKeyOverride => typeof(TextBox);
        public static readonly DirectProperty<IntEditor, int> ValueProperty =
            AvaloniaProperty.RegisterDirect<IntEditor, int>(
                nameof(Value),
                o => o.Value,
                (o, v) => o.Value = v,
                defaultBindingMode: BindingMode.TwoWay);
        private int _value = 0;

        public IntEditor()
        {
            Text = "0";
            this.WhenAnyValue(x => x.Text)
                .Subscribe((text => { 
                    OnTextChanged(text);
                }));
        }

        public int Value
        {
            get => _value;
            set
            {
                if (SetAndRaise(ValueProperty, ref _value, value))
                {
                    Text = value.ToString() ?? "";
                }
            }
        }

        protected void OnTextChanged(string? newText)
        {
            if (!IsKeyboardFocusWithin){
                return;
            }

            if( newText != null && int.TryParse(newText, out int newValue))
            {
                Value = newValue;  
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e) {
            base.OnLostFocus(e);
            if (!int.TryParse(Text, out int newValue))
            {
                Text = Value.ToString();
            }
        }
    }
}