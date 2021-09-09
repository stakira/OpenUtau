using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OpenUtau.App.Controls {
    public partial class HeadTracker : UserControl {
        public HeadTracker() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
