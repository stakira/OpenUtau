using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenUtau.App.ViewModels;
using OpenUtau.Core.Util;

namespace OpenUtau.App.Views {
    public partial class NoteDefaultsDialog : Window {
        internal readonly NoteDefaultsViewModel ViewModel;
        public NoteDefaultsDialog() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            DataContext = ViewModel = new NoteDefaultsViewModel();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
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

        void OnReset(object sender, RoutedEventArgs e) {
            NotePresets.Reset();
            ViewModel.ResetSettings();
        }
    }
}
