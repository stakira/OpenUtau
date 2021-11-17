using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    class LyricsViewModel : ViewModelBase {
        [Reactive] public string Text { get; set; }
        [Reactive] public int CurrentCount { get; set; }
        [Reactive] public int TotalCount { get; set; }
        [Reactive] public int MaxCount { get; set; }
        [Reactive] public bool SeparateBySpace { get; set; }
        [Reactive] public bool SeparateByComma { get; set; }
        [Reactive] public bool SeparateByQuote { get; set; }
        [Reactive] public bool LivePreview { get; set; }

        private UVoicePart? part;
        private UNote[]? notes;
        private string[]? startLyrics;

        public LyricsViewModel() {
            Text = string.Empty;
            LivePreview = true;
            this.WhenAnyValue(x => x.LivePreview,
                x => x.Text,
                x => x.SeparateBySpace,
                x => x.SeparateByComma,
                x => x.SeparateByQuote)
                .Subscribe(t => {
                    if (t.Item1) {
                        Preview();
                    }
                });
        }

        public void Start(UVoicePart part, UNote[] notes, string[] lyrics) {
            this.part = part;
            this.notes = notes;
            CurrentCount = TotalCount = lyrics.Length;
            MaxCount = notes.Length;
            SeparateBySpace = !lyrics.Any(l => l.Contains(' '));
            SeparateByComma = !lyrics.Any(l => l.Contains(','));
            SeparateByQuote = !lyrics.Any(l => l.Contains('"'));
            char sep = SeparateBySpace ? ' ' : SeparateByComma ? ',' : '\n';
            Text = string.Join(sep, lyrics);
            startLyrics = lyrics;
            DocManager.Inst.StartUndoGroup();
        }

        public void Preview() {
            if (startLyrics == null || notes == null || part == null) {
                return;
            }
            DocManager.Inst.RollBackUndoGroup();
            var lyrics = Split(Text);
            CurrentCount = lyrics.Count;
            for (int i = 0; i < lyrics.Count && i < notes.Length; ++i) {
                if (notes[i].lyric != lyrics[i]) {
                    DocManager.Inst.ExecuteCmd(new ChangeNoteLyricCommand(part, notes[i], lyrics[i]));
                }
            }
        }

        public void Reset() {
            if (startLyrics == null) {
                return;
            }
            DocManager.Inst.RollBackUndoGroup();
            char sep = SeparateBySpace ? ' ' : SeparateByComma ? ',' : '\n';
            Text = string.Join(sep, startLyrics);
        }

        public void Cancel() {
            DocManager.Inst.RollBackUndoGroup();
            DocManager.Inst.EndUndoGroup();
        }

        public void Finish() {
            Preview();
            DocManager.Inst.EndUndoGroup();
        }

        private List<string> Split(string text) {
            var lyrics = new List<string>();
            var builder = new StringBuilder();
            var etor = StringInfo.GetTextElementEnumerator(text);
            while (etor.MoveNext()) {
                string ele = etor.GetTextElement();
                if (ele == "\r" || ele == "\n" || ele == "\r\n" ||
                    ele == " " && SeparateBySpace ||
                    ele == "," && SeparateByComma) {
                    if (builder.Length > 0) {
                        lyrics.Add(builder.ToString());
                        builder.Clear();
                    }
                } else if (ele == "\"" && SeparateByQuote) {
                    while (etor.MoveNext()) {
                        string ele1 = etor.GetTextElement();
                        if (ele1 == "\"") {
                            if (builder.Length > 0) {
                                lyrics.Add(builder.ToString());
                                builder.Clear();
                            }
                            break;
                        } else {
                            builder.Append(ele1);
                        }
                    }
                } else {
                    builder.Append(ele);
                }
            }
            if (builder.Length > 0) {
                lyrics.Add(builder.ToString());
                builder.Clear();
            }
            return lyrics;
        }
    }
}
