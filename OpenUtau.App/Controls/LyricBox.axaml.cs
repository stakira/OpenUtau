using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.App.Controls {
    public partial class LyricBox : UserControl {
        private LyricBoxViewModel viewModel;
        private TextBox box;
        private ListBox listBox;
        private DispatcherTimer? focusTimer;

        public LyricBox() {
            InitializeComponent();
            DataContext = viewModel = new LyricBoxViewModel();
            box = this.FindControl<TextBox>("PART_Box");
            listBox = this.FindControl<ListBox>("PART_Suggestions");
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            IsVisible = false;
        }

        private void Box_GotFocus(object? sender, GotFocusEventArgs e) {
            box.SelectAll();
        }

        private void Box_LostFocus(object? sender, RoutedEventArgs e) {
        }

        private void ListBox_KeyDown(object? sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Enter:
                    if (listBox.SelectedItem is LyricBoxViewModel.SuggestionItem item) {
                        box.Text = item.Alias;
                    }
                    EndEdit(true);
                    break;
                case Key.Escape:
                    EndEdit();
                    break;
                case Key.Tab:
                    if (listBox.SelectedItem is LyricBoxViewModel.SuggestionItem item1) {
                        box.Text = item1.Alias;
                    }
                    OnTab(e.KeyModifiers);
                    break;
                case Key.Up:
                    ListBoxSelect(listBox.SelectedIndex - 1);
                    break;
                case Key.Down:
                    ListBoxSelect(listBox.SelectedIndex + 1);
                    break;
                case Key.PageUp:
                    ListBoxSelect(listBox.SelectedIndex - 8);
                    break;
                case Key.PageDown:
                    ListBoxSelect(listBox.SelectedIndex + 8);
                    break;
                default:
                    break;
            }
            e.Handled = true;
        }

        private void ListBoxSelect(int index) {
            if (index < 0) {
                if (listBox.SelectedIndex == 0) {
                    index = listBox.ItemCount - 1;
                } else {
                    index = 0;
                }
            } else if (index >= listBox.ItemCount) {
                if (listBox.SelectedIndex == listBox.ItemCount - 1) {
                    index = 0;
                } else {
                    index = listBox.ItemCount - 1;
                }
            }
            listBox.SelectedIndex = index;
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
                    OnTab(e.KeyModifiers);
                    break;
                case Key.Up:
                case Key.Down:
                case Key.PageUp:
                case Key.PageDown:
                    listBox.Focus();
                    listBox.SelectedIndex = 0;
                    break;
                default:
                    break;
            }
            e.Handled = true;
        }

        private void OnTab(KeyModifiers keyModifiers) {
            UVoicePart? part = viewModel.Part;
            UNote? tabTo = null;
            if (keyModifiers == KeyModifiers.None) {
                tabTo = viewModel.Note?.Next;
            } else if (keyModifiers == KeyModifiers.Shift) {
                tabTo = viewModel.Note?.Prev;
            }
            EndEdit(true);
            if (tabTo != null && part != null) {
                DocManager.Inst.ExecuteCmd(new FocusNoteNotification(part, tabTo));
                Show(part, tabTo, tabTo.lyric);
            }
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
            focusTimer = new DispatcherTimer();
            focusTimer.Tick += FocusTimer_Tick;
            focusTimer.Start();
        }

        private void FocusTimer_Tick(object? sender, System.EventArgs e) {
            box.Focus();
            if (focusTimer != null) {
                focusTimer.Tick -= FocusTimer_Tick;
                focusTimer.Stop();
                focusTimer = null;
            }
        }

        public void EndEdit(bool commit = false) {
            if (commit) {
                viewModel.Commit();
            }
            viewModel.Part = null;
            viewModel.Note = null;
            viewModel.IsVisible = false;
            viewModel.Text = string.Empty;
            KeyboardDevice.Instance.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);
        }
    }
}
