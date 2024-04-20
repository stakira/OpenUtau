using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using OpenUtau.Core;
using Serilog;
using SharpCompress;

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
            string text = message;
            string title = ThemeManager.GetString("errors.caption");

            if (e is MessageCustomizableException mce) {
                if (!string.IsNullOrEmpty(mce.TranslatableMessage)) {
                    var matches = Regex.Matches(mce.TranslatableMessage, "<translate:(.*?)>");
                    matches.ForEach(m => mce.TranslatableMessage = mce.TranslatableMessage.Replace(m.Value, ThemeManager.GetString(m.Groups[1].Value)));
                    text = mce.TranslatableMessage;
                    e = mce.SubstanceException;
                }

                if (!mce.ShowStackTrace) {
                    return Show(parent, text, null, title, MessageBoxButtons.Ok);
                }
            }

            var builder = new StringBuilder();
            if (e != null) {
                if (e is AggregateException ae) {
                    ae = ae.Flatten();
                    builder.AppendLine(ae.InnerExceptions.First().Message);
                    builder.AppendLine();
                    builder.Append(ae.ToString());

                    if (!string.IsNullOrWhiteSpace(text)) {
                        text += "\n";
                    }
                    if (ae.InnerExceptions.First() is MessageCustomizableException innnerMce) {
                        if (!string.IsNullOrEmpty(innnerMce.TranslatableMessage)) {
                            var matches = Regex.Matches(innnerMce.TranslatableMessage, "<translate:(.*?)>");
                            matches.ForEach(m => innnerMce.TranslatableMessage = innnerMce.TranslatableMessage.Replace(m.Value, ThemeManager.GetString(m.Groups[1].Value)));
                            text += innnerMce.TranslatableMessage;
                        } else {
                            text += ae.InnerExceptions.First().Message;
                        }
                    }
                } else {
                    builder.AppendLine(e.Message);
                    builder.AppendLine();
                    builder.Append(e.ToString());
                    if (string.IsNullOrEmpty(text)) {
                        text = e.Message;
                    }
                }
            }
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine(System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown Version");

            return Show(parent, text, builder.ToString(), title, MessageBoxButtons.OkCopy);
        }

        public static Task<MessageBoxResult> Show(Window parent, string text, string title, MessageBoxButtons buttons) {
            return Show(parent, text, null, title, buttons);
        }
        public static Task<MessageBoxResult> Show(Window parent, string text, string? stackTrace, string title, MessageBoxButtons buttons) {
            var msgbox = new MessageBox() {
                Title = title
            };
            msgbox.Text.IsVisible = false;
            msgbox.SetTextWithLink(text, msgbox.TextPanel);
            if (stackTrace != null) {
                var stackTracePanel = new StackPanel();
                var expander = new Expander() { Header = ThemeManager.GetString("errors.details"), Content = stackTracePanel };
                msgbox.TextPanel.Children.Add(expander);
                msgbox.SetTextWithLink(stackTrace, stackTracePanel);
            }

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
                        GetTopLevel(parent)?.Clipboard?.SetTextAsync(text + "\n" + stackTrace);
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
