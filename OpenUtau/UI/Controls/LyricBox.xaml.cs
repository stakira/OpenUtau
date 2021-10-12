using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using WanaKanaNet;

namespace OpenUtau.UI.Controls {
    /// <summary>
    /// Interaction logic for LyricBox.xaml
    /// </summary>
    public partial class LyricBox : TextBox {
        UVoicePart Part;
        UNote Note;

        Popup popup;
        ListBox itemList;
        ScrollViewer host;

        public LyricBox() {
            InitializeComponent();
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            popup = Template.FindName("PART_Popup", this) as Popup;
            itemList = Template.FindName("PART_ItemList", this) as ListBox;
            itemList.PreviewMouseDown += ItemList_PreviewMouseDown;
            host = Template.FindName("PART_ContentHost", this) as ScrollViewer;
        }

        public void Show(UVoicePart part, UNote note, string text) {
            Part = part;
            Note = note;
            Visibility = Visibility.Visible;
            Text = text;
            Focus();
            SelectAll();
            UpdateSuggestion();
        }

        public void EndNoteEditing(bool edit = false) {
            string finalText = Text;
            if (edit && Note != null && finalText != Note.lyric) {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new ChangeNoteLyricCommand(Part, Note, finalText));
                DocManager.Inst.EndUndoGroup();
            }
            Part = null;
            Note = null;
            Visibility = Visibility.Hidden;
            Clear();
            UpdateSuggestion();
        }

        public void TextBox_KeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Enter:
                    EndNoteEditing(true);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    EndNoteEditing();
                    e.Handled = true;
                    break;
                case Key.Tab:
                    if (itemList.SelectedItem != null) {
                        Text = (string)itemList.SelectedItem;
                    }
                    var part = Part;
                    var note = Keyboard.Modifiers == ModifierKeys.Shift ? Note.Prev : Note.Next;
                    EndNoteEditing(!string.IsNullOrEmpty(Text));
                    e.Handled = true;
                    if (note == null) {
                        break;
                    }
                    Show(part, note, note.lyric);
                    DocManager.Inst.ExecuteCmd(new FocusNoteNotification(part, note));
                    break;
                default:
                    break;
            }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Up:
                case Key.Down:
                    int move = e.Key == Key.Up ? -1 : 1;
                    int newIndex = (itemList.SelectedIndex + itemList.Items.Count + move) % itemList.Items.Count;
                    itemList.SelectedIndex = newIndex;
                    itemList.ScrollIntoView(itemList.Items[newIndex]);
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
            UpdateSuggestion();
        }

        private void ItemList_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            TextBlock textBlock = e.OriginalSource as TextBlock;
            if (textBlock != null) {
                var singer = DocManager.Inst.Project.tracks[Part.trackNo].Singer;
                if (singer == null) {
                    e.Handled = true;
                    return;
                }
                if (textBlock.Text != "No Singer") {
                    Text = textBlock.Text;
                    EndNoteEditing(true);
                    e.Handled = true;
                }
            }
        }

        private void UpdateSuggestion() {
            if (Part == null || Note == null) {
                itemList.Items.Clear();
                popup.IsOpen = false;
                return;
            }
            itemList.Items.Clear();
            popup.IsOpen = true;
            var singer = DocManager.Inst.Project.tracks[Part.trackNo].Singer;
            if (singer == null) {
                itemList.Items.Add("No Singer");
                return;
            }
            if (!string.IsNullOrEmpty(Text)) {
                itemList.Items.Add(WanaKana.ToHiragana(Text));
            }
            singer.GetSuggestions(Text, oto => itemList.Items.Add(oto.Alias));
        }
    }
}
