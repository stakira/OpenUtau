using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using OpenUtau.Core;
using OpenUtau.Core.USTx;

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

        public void EndNoteEditing(bool cancel) {
            string finalText = Text;
            if (!cancel && Note != null && finalText != Note.Lyric) {
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
                    EndNoteEditing(false);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    EndNoteEditing(true);
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
                var singer = DocManager.Inst.Project.Tracks[Part.TrackNo].Singer;
                if (singer == null) {
                    e.Handled = true;
                    return;
                }
                if (singer.AliasMap.ContainsKey(textBlock.Text)) {
                    Text = textBlock.Text;
                    EndNoteEditing(false);
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
            var singer = DocManager.Inst.Project.Tracks[Part.TrackNo].Singer;
            if (singer == null) {
                itemList.Items.Add("No Singer");
                return;
            }
            string text = Text;
            if (string.IsNullOrEmpty(text)) {
                foreach (var key in singer.AliasMap.Keys) {
                    itemList.Items.Add(key);
                }
            } else if (singer != null && singer.AliasMap != null) {
                foreach (var key in singer.AliasMap.Keys) {
                    if (key.Contains(text)) {
                        itemList.Items.Add(key);
                    }
                }
            }
        }
    }
}
