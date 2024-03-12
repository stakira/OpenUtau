using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using Serilog;

namespace OpenUtau.App.Controls {
    public partial class NotePropertyExpression : UserControl {

        public NotePropertyExpression() {
            InitializeComponent();
            
            slider.AddHandler(PointerPressedEvent, SliderPointerPressed, RoutingStrategies.Tunnel);
            slider.AddHandler(PointerReleasedEvent, SliderPointerReleased, RoutingStrategies.Tunnel);
            slider.AddHandler(PointerMovedEvent, SliderPointerMoved, RoutingStrategies.Tunnel);
        }

        private string textBoxValue = string.Empty;
        void OnTextBoxGotFocus(object? sender, GotFocusEventArgs args) {
            Log.Debug("Note property textbox got focus");
            if (sender is TextBox text) {
                textBoxValue = text.Text ?? string.Empty;
            }
        }
        void OnTextBoxLostFocus(object? sender, RoutedEventArgs args) {
            Log.Debug("Note property textbox lost focus");
            if (sender is TextBox textBox && textBoxValue != textBox.Text) {
                if (DataContext is NotePropertyExpViewModel ViewModel) {
                    DocManager.Inst.StartUndoGroup();
                    NotePropertiesViewModel.PanelControlPressed = true;
                    ViewModel.SetNumericalExpressions(textBox.Text);
                    NotePropertiesViewModel.PanelControlPressed = false;
                    DocManager.Inst.EndUndoGroup();
                }
            }
        }

        void SliderPointerPressed(object? sender, PointerPressedEventArgs args) {
            Log.Debug("Slider pressed");
            if (sender is Control control) {
                var point = args.GetCurrentPoint(control);
                if (point.Properties.IsLeftButtonPressed) {
                    DocManager.Inst.StartUndoGroup();
                    NotePropertiesViewModel.PanelControlPressed = true;
                } else if (point.Properties.IsRightButtonPressed) {
                    if (DataContext is NotePropertyExpViewModel ViewModel) {
                        DocManager.Inst.StartUndoGroup();
                        NotePropertiesViewModel.PanelControlPressed = true;
                        ViewModel.SetNumericalExpressions(null);
                        NotePropertiesViewModel.PanelControlPressed = false;
                        DocManager.Inst.EndUndoGroup();
                    }
                }
            }
        }
        void SliderPointerReleased(object? sender, PointerReleasedEventArgs args) {
            Log.Debug("Slider released");
            if (NotePropertiesViewModel.PanelControlPressed) {
                if (sender is Slider slider && DataContext is NotePropertyExpViewModel ViewModel) {
                    ViewModel.SetNumericalExpressions((float)slider.Value);
                }
                NotePropertiesViewModel.PanelControlPressed = false;
                DocManager.Inst.EndUndoGroup();
            }
        }
        void SliderPointerMoved(object? sender, PointerEventArgs args) {
            if (sender is Slider slider && DataContext is NotePropertyExpViewModel ViewModel) {
                ViewModel.SetNumericalExpressions((float)slider.Value);
            }
        }
    }
}
