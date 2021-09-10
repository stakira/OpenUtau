using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OpenUtau.App.Controls {
    public partial class TrackHeader : UserControl {
        public TrackHeader() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
