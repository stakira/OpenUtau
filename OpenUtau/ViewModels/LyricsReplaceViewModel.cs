using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    class LyricsReplaceViewModel : ViewModelBase {
        [Reactive] public string OldValue { get; set; } = "";
        [Reactive] public string NewValue { get; set; } = "";
        [Reactive] public string Preview { get; private set; } = "";
        public List<ReplacePreset> PresetList { get; } = new List<ReplacePreset>() { //Increase!
            new ReplacePreset(ThemeManager.GetString("lyricsreplace.preset.rmvalphabet"), @"[a-zA-Z]", ""),
            new ReplacePreset(ThemeManager.GetString("lyricsreplace.preset.rmvnonhiragana"), @"[^\p{IsHiragana}ヴ]+", ""),
            new ReplacePreset(ThemeManager.GetString("lyricsreplace.preset.rmvphonetichint"), @"\[.*\]", ""),
            new ReplacePreset(ThemeManager.GetString("lyricsreplace.preset.rmvtone"), @"_?[A-G](#|b)?[1-7]", ""),
            new ReplacePreset(ThemeManager.GetString("lyricsreplace.preset.rmvspace"), ".* ", "")
        };
        [Reactive] public ReplacePreset? SelectedPreset { get; set; }
        public string[] Lyrics { get; private set; }

        private string startLyrics;

        public LyricsReplaceViewModel(string[] lyrics) {
            startLyrics = SplitLyrics.Join(lyrics);
            Preview = SplitLyrics.Join(lyrics);
            Lyrics = lyrics;
            this.WhenAnyValue(x => x.OldValue, x => x.NewValue)
                .Subscribe(t =>
                {
                    Preview = Regex.Replace(startLyrics, t.Item1, t.Item2);
                });
            this.WhenAnyValue(x => SelectedPreset)
                .Subscribe(t => {
                    if (t != null) {
                        OldValue = t.OldValue;
                        NewValue = t.NewValue;
                    }
                });
        }

        public void Cancel() {

        }

        public void Finish() {
            Lyrics = SplitLyrics.Split(Preview).ToArray();
        }
    }

    class ReplacePreset {
        public string Name { get; private set; }
        public string OldValue { get; private set; }
        public string NewValue { get; private set; }

        public ReplacePreset(string name, string oldValue, string newValue) {
            Name = name;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public override string ToString() {
            return Name;
        }
    }
}
