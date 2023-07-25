using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class EditSubbanksDialog : Window {
        internal readonly EditSubbanksViewModel ViewModel;

        internal Action RefreshSinger;

        private DataGrid dataGrid;

        public EditSubbanksDialog() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            DataContext = ViewModel = new EditSubbanksViewModel();
            dataGrid = this.FindControl<DataGrid>("SuffixGrid");
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        void OnAdd(object sender, RoutedEventArgs e) {
            var dialog = new TypeInDialog() {
                Title = ThemeManager.GetString("singers.subbanks.color.add"),
                onFinish = name => ViewModel.AddSubbank(name),
            };
            dialog.ShowDialog(this);
        }

        void OnRemove(object sender, RoutedEventArgs e) {
            ViewModel.RemoveSubbank();
        }

        void OnRename(object sender, RoutedEventArgs e) {
            if (ViewModel.SelectedColor == null || string.IsNullOrEmpty(ViewModel.SelectedColor.Name)) {
                return;
            }
            var dialog = new TypeInDialog() {
                Title = ThemeManager.GetString("singers.subbanks.color.rename"),
                onFinish = name => ViewModel.RenameSubbank(name),
            };
            dialog.ShowDialog(this);
        }

        void OnSave(object sender, RoutedEventArgs e) {
            ViewModel.SaveSubbanks();
            RefreshSinger?.Invoke();
            Close();
        }

        void OnCancel(object sender, RoutedEventArgs e) {
            Close();
        }

        void OnSelectAll(object sender, RoutedEventArgs e) {
            dataGrid.SelectAll();
        }

        void OnSet(object sender, RoutedEventArgs e) {
            ViewModel.Set(dataGrid.SelectedItems);
        }

        void OnClear(object sender, RoutedEventArgs e) {
            ViewModel.Clear(dataGrid.SelectedItems);
        }
    }
}
