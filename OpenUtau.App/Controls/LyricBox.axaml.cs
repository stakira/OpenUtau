using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenUtau.Core.Ustx;
using Serilog;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Controls {
    public partial class LyricBox : UserControl {
        private TextBox box;
        private LyricBoxViewModel viewModel;

        public LyricBox() {
            InitializeComponent();
            DataContext = viewModel = new LyricBoxViewModel();
            box = this.FindControl<TextBox>("PART_Box");
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            IsVisible = false;
        }

        private void Box_GotFocus(object? sender, GotFocusEventArgs e) {
            box.SelectAll();
            Log.Information($"Box_GotFocus");
        }

        private void Box_LostFocus(object? sender, RoutedEventArgs e) {
            Log.Information($"Box_LostFocus");
        }

        private void Box_KeyDown(object? sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Enter:
                    EndEdit(true);
                    break;
                case Key.Escape:
                    EndEdit();
                    break;
                case Key.Tab:
                    if (e.KeyModifiers == KeyModifiers.None) {
                    } else if (e.KeyModifiers == KeyModifiers.Shift) {
                    }
                    break;
                default:
                    break;
            }
            e.Handled = true;
        }

        public void ListBox_PointerPressed(object sender, PointerPressedEventArgs args) {
            if (sender is Grid grid &&
                grid.DataContext is LyricBoxViewModel.SuggestionItem item) {
                box.Text = item.Alias;
            }
            EndEdit(true);
        }

        public void Show(UVoicePart part, UNote note, string text) {
            viewModel.Part = part;
            viewModel.Note = note;
            viewModel.Text = text;
            viewModel.IsVisible = true;
            box.SelectAll();
            box.Focus();
        }

        protected override void OnGotFocus(GotFocusEventArgs e) {
            base.OnGotFocus(e);
            Log.Information($"OnGotFocus");
        }

        protected override void OnLostFocus(RoutedEventArgs e) {
            base.OnLostFocus(e);
            Log.Information($"OnLostFocus");
        }

        public void EndEdit(bool commit = false) {
            if (commit) {
                viewModel.Commit();
            }
            viewModel.Part = null;
            viewModel.Note = null;
            viewModel.IsVisible = false;
            viewModel.Text = string.Empty;
        }
    }
}
