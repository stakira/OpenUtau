using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace OpenUtau.App.Views {
    public partial class SingersDialog : Window {
        public SingersDialog() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        void OnSingerMenuButton(object sender, RoutedEventArgs args) {
            var menu = this.FindControl<ContextMenu>("SingerMenu");
            menu.PlacementTarget = sender as Button;
            menu.Open();
        }
    }
}
