using Avalonia.Controls;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class SearchNoteDialog : Window {
        public SearchNoteDialog() {
            InitializeComponent();
            Closing += (s, e) =>
            {
                (DataContext as SearchNoteViewModel)?.OnClose();
            };
        }
    }
}
