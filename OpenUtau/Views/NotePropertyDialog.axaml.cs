using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class NotePropertyDialog : Window {
        private readonly NotePropertyViewModel ViewModel;

        public NotePropertyDialog() {
            InitializeComponent();
            ViewModel = new NotePropertyViewModel();
        }
        public NotePropertyDialog(NotePropertyViewModel vm) {
            InitializeComponent();
            ViewModel = vm;
            DataContext = ViewModel;
        }

        void OnSavePortamentoPreset(object sender, RoutedEventArgs e) {
            var dialog = new TypeInDialog() {
                Title = ThemeManager.GetString("notedefaults.preset.namenew"),
                onFinish = name => ViewModel.SavePortamentoPreset(name),
            };
            dialog.ShowDialog(this);
        }

        void OnRemovePortamentoPreset(object sender, RoutedEventArgs e) {
            ViewModel.RemoveAppliedPortamentoPreset();
        }

        void OnSaveVibratoPreset(object sender, RoutedEventArgs e) {
            var dialog = new TypeInDialog() {
                Title = ThemeManager.GetString("notedefaults.preset.namenew"),
                onFinish = name => ViewModel.SaveVibratoPreset(name),
            };
            dialog.ShowDialog(this);
        }

        void OnRemoveVibratoPreset(object sender, RoutedEventArgs e) {
            ViewModel.RemoveAppliedVibratoPreset();
        }

        void OnCancel(object? sender, RoutedEventArgs e) {
            Close();
        }

        void OnFinish(object? sender, RoutedEventArgs e) {
            ViewModel.Finish();
            Close();
        }

        private void OnKeyDown(object? sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Enter:
                    OnFinish(sender, e);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    OnCancel(sender, e);
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }
    }
}
