using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Serilog;

namespace OpenUtau.App.Views {
    public partial class MessageBox : Window {
        public enum MessageBoxButtons { Ok, OkCancel, YesNo, YesNoCancel, OkCopy }
        public enum MessageBoxResult { Ok, Cancel, Yes, No }

        private static MessageBox? loadingDialog;

        public MessageBox() {
            InitializeComponent();
        }

        public void SetText(string text) {
            Dispatcher.UIThread.Post(() => {
                Text.Text = text;
            });
        }

        public static Task<MessageBoxResult> ShowError(Window parent, Exception? e) {
            return ShowError(parent, string.Empty, e);
        }

        public static Task<MessageBoxResult> ShowError(Window parent, string message, Exception? e) {
            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(message)) {
                builder.AppendLine(message);
            }
            if (e != null) {
                if (e is AggregateException ae) {
                    ae = ae.Flatten();
                    builder.AppendLine(ae.InnerExceptions.First().Message);
                    builder.AppendLine();
                    builder.Append(ae.ToString());
                } else {
                    builder.AppendLine(e.Message);
                    builder.AppendLine();
                    builder.Append(e.ToString());
                }
            }
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine(System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown Version");
            string title = ThemeManager.GetString("errors.caption");
            return Show(parent, builder.ToString(), title, MessageBoxButtons.OkCopy);
        }

        public static Task<MessageBoxResult> Show(Window parent, string text, string title, MessageBoxButtons buttons) {
            var msgbox = new MessageBox() {
                Title = title
            };
            msgbox.Text.IsVisible = false;
            msgbox.SetTextWithLink(text, msgbox.TextPanel);

            var res = MessageBoxResult.Ok;

            void AddButton(string caption, MessageBoxResult r, bool def = false) {
                var btn = new Button { Content = caption };
                btn.Click += (_, __) => {
                    res = r;
                    msgbox.Close();
                };
                msgbox.Buttons.Children.Add(btn);
                if (def)
                    res = r;
            }

            if (buttons == MessageBoxButtons.Ok || buttons == MessageBoxButtons.OkCancel || buttons == MessageBoxButtons.OkCopy)
                AddButton(ThemeManager.GetString("dialogs.messagebox.ok"), MessageBoxResult.Ok, true);
            if (buttons == MessageBoxButtons.YesNo || buttons == MessageBoxButtons.YesNoCancel) {
                AddButton(ThemeManager.GetString("dialogs.messagebox.yes"), MessageBoxResult.Yes);
                AddButton(ThemeManager.GetString("dialogs.messagebox.no"), MessageBoxResult.No, true);
            }

            if (buttons == MessageBoxButtons.OkCancel || buttons == MessageBoxButtons.YesNoCancel)
                AddButton(ThemeManager.GetString("dialogs.messagebox.cancel"), MessageBoxResult.Cancel, true);
            if (buttons == MessageBoxButtons.OkCopy) {
                var btn = new Button { Content = ThemeManager.GetString("dialogs.messagebox.copy") };
                btn.Click += (_, __) => {
                    try {
                        GetTopLevel(parent)?.Clipboard?.SetTextAsync(text);
                    } catch { }
                };
                msgbox.Buttons.Children.Add(btn);
            }

            var tcs = new TaskCompletionSource<MessageBoxResult>();
            msgbox.Closed += delegate { tcs.TrySetResult(res); };
            if (parent != null)
                msgbox.ShowDialog(parent);
            else msgbox.Show();
            return tcs.Task;
        }

        public static MessageBox ShowModal(Window parent, string text, string title) {
            var msgbox = new MessageBox() {
                Title = title
            };
            msgbox.Text.Text = text;
            msgbox.ShowDialog(parent);
            return msgbox;
        }

        public static void ShowLoading(Window parent) {
            loadingDialog = new MessageBox() {
                Title = "Loading"
            };
            loadingDialog.Text.Text = "Please wait...";
            loadingDialog.ShowDialog(parent);
        }

        public static void CloseLoading() {
            if (loadingDialog != null) {
                loadingDialog.Close();
            }
        }

        public static bool LoadingIsActive() {
            return loadingDialog != null && loadingDialog.IsActive;
        }

        private void SetTextWithLink(string text, StackPanel textPanel) {
            // @"http(s)?://([\w-]+\.)+[\w-]+(/[A-Z0-9-.,_/?%&=]*)?"
            var regex = new Regex(@"(\r\n|\n| )http(s)?://[^(\r\n|\n| )]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var match = regex.Match(text);
            if (match.Success) {
                textPanel.Children.Add(new TextBlock { Text = text.Substring(0, match.Index) });
                var hyperlink = new Button();
                hyperlink.Content = match.Value.Trim();
                hyperlink.Click += OnUrlClick;
                textPanel.Children.Add(hyperlink);

                SetTextWithLink(text.Substring(match.Index + match.Length), textPanel);
            } else {
                if (!string.IsNullOrEmpty(text)) {
                    textPanel.Children.Add(new TextBlock { Text = text });
                }
            }
        }
        private void OnUrlClick(object? sender, RoutedEventArgs e) {
            try {
                if (sender is Button button && button.Content is string url) {
                    OS.OpenWeb(url);
                }
            } catch (Exception ex) {
                Log.Error(ex, "Failed to open url");
            }
        }
    }
}
