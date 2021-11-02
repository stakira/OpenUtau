using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    class LyricsViewModel : ViewModelBase {
        private readonly char[] Separators = new[] { ' ', ',', '\n' };
        private readonly char[] Separators1 = new[] { '\r', '\n', ',' };
        private readonly char[] Separators2 = new[] { '\r', '\n' };

        [Reactive] public string Text { get; set; }
        [Reactive] public int CurrentCount { get; set; }
        [Reactive] public int TotalCount { get; set; }
        [Reactive] public int MaxCount { get; set; }
        [Reactive] public int Separator { get; set; }

        [Reactive] public string[]? Lyrics { get; set; }
        public Action<string[]?>? OnApply { get; set; }

        private string[]? newLyrics;

        public LyricsViewModel() {
            Text = string.Empty;
            this.WhenAnyValue(x => x.Separator, x => x.Lyrics)
                .Subscribe(x => {
                    if (x.Item2 != null) {
                        Text = string.Join(Separators[x.Item1], x.Item2);
                    }
                });
            this.WhenAnyValue(x => x.Text)
                .Subscribe(x => {
                    if (Separator == 0) {
                        newLyrics = x.Split()
                            .Select(s => s.Trim()).ToArray();
                        CurrentCount = newLyrics.Length;
                    } else if (Separator == 1) {
                        newLyrics = x.Split(Separators1, StringSplitOptions.RemoveEmptyEntries)
                             .Select(s => s.Trim()).ToArray();
                        CurrentCount = newLyrics.Length;
                        if (!Preferences.Default.PreferCommaSeparator) {
                            Preferences.Default.PreferCommaSeparator = true;
                            Preferences.Save();
                        }
                    } else if (Separator == 2) {
                        newLyrics = x.Split(Separators2, StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => s.Trim()).ToArray();
                        CurrentCount = newLyrics.Length;
                        if (Preferences.Default.PreferCommaSeparator) {
                            Preferences.Default.PreferCommaSeparator = false;
                            Preferences.Save();
                        }
                    }
                });
        }

        public void Apply() {
            OnApply?.Invoke(newLyrics ?? Lyrics);
        }
    }
}
