using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenUtau.App.ViewModels;

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

        async void OnEditSubbanksButton(object sender, RoutedEventArgs args) {
            var viewModel = (DataContext as SingersViewModel)!;
            if (viewModel.Singer == null) {
                return;
            }
            var dialog = new EditSubbanksDialog();
            dialog.ViewModel.SetSinger(viewModel.Singer!);
            dialog.RefreshSinger = () => viewModel.RefreshSinger();
            await dialog.ShowDialog(this);
        }

        void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            var viewModel = (DataContext as SingersViewModel)!;
            if (viewModel.Singer == null || e.AddedItems.Count < 1) {
                return;
            }
            var oto = (Core.Ustx.UOto?)e.AddedItems[0];
            if (oto == null || !File.Exists(oto.Value.File)) {
                return;
            }
        }
    }
}
