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
        }

        void OnGotFocus(object sender, GotFocusEventArgs e) {
            Log.Information("Note property panel got focus");
            DocManager.Inst.StartUndoGroup();
            NotePropertiesViewModel.PanelControlPressed = true;
        }
        void OnLostFocus(object sender, RoutedEventArgs e) {
            Log.Information("Note property panel lost focus");
            NotePropertiesViewModel.PanelControlPressed = false;
            DocManager.Inst.EndUndoGroup();
        }

    }
}
