using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OpenUtau.App.Views {
    public partial class MessageBox : Window {
        public enum MessageBoxButtons { Ok, OkCancel, YesNo, YesNoCancel }
        public enum MessageBoxResult { Ok, Cancel, Yes, No }

        public MessageBox() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        public static Task<MessageBoxResult> Show(Window parent, string text, string title, MessageBoxButtons buttons) {
            var msgbox = new MessageBox() {
                Title = title
            };
            msgbox.FindControl<TextBlock>("Text").Text = text;
            var buttonPanel = msgbox.FindControl<StackPanel>("Buttons");

            var res = MessageBoxResult.Ok;

            void AddButton(string caption, MessageBoxResult r, bool def = false) {
                var btn = new Button { Content = caption };
                btn.Click += (_, __) => {
                    res = r;
                    msgbox.Close();
                };
                buttonPanel.Children.Add(btn);
                if (def)
                    res = r;
            }

            if (buttons == MessageBoxButtons.Ok || buttons == MessageBoxButtons.OkCancel)
                AddButton(ThemeManager.GetString("dialogs.messagebox.ok"), MessageBoxResult.Ok, true);
            if (buttons == MessageBoxButtons.YesNo || buttons == MessageBoxButtons.YesNoCancel) {
                AddButton(ThemeManager.GetString("dialogs.messagebox.yes"), MessageBoxResult.Yes);
                AddButton(ThemeManager.GetString("dialogs.messagebox.no"), MessageBoxResult.No, true);
            }

            if (buttons == MessageBoxButtons.OkCancel || buttons == MessageBoxButtons.YesNoCancel)
                AddButton(ThemeManager.GetString("dialogs.messagebox.cancel"), MessageBoxResult.Cancel, true);

            var tcs = new TaskCompletionSource<MessageBoxResult>();
            msgbox.Closed += delegate { tcs.TrySetResult(res); };
            if (parent != null)
                msgbox.ShowDialog(parent);
            else msgbox.Show();
            return tcs.Task;
        }
    }
}
