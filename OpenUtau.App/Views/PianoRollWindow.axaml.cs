using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OpenUtau.App.Views {
    public partial class PianoRollWindow : Window {
        public PianoRollWindow() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
