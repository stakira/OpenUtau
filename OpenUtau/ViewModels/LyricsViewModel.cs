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
        [Reactive] public string? Text { get; set; }
        [Reactive] public int CurrentCount { get; set; }
        [Reactive] public int TotalCount { get; set; }
        [Reactive] public int MaxCount { get; set; }
        [Reactive] public bool LivePreview { get; set; }

        private UVoicePart? part;
        private UNote[]? notes;
        private string[]? startLyrics;

        public LyricsViewModel() {
            Text = string.Empty;
            LivePreview = true;
            this.WhenAnyValue(x => x.LivePreview,
                x => x.Text)
                .Subscribe(t => {
                    Preview(t.Item1);
                });
        }

        public void Start(UVoicePart part, UNote[] notes, string[] lyrics) {
            this.part = part;
            this.notes = notes;
            CurrentCount = TotalCount = lyrics.Length;
            MaxCount = notes.Length;
            Text = SplitLyrics.Join(lyrics);
            startLyrics = lyrics;
            DocManager.Inst.StartUndoGroup();
        }

        public void Preview(bool update) {
            if (startLyrics == null || notes == null || part == null) {
                return;
            }
            DocManager.Inst.RollBackUndoGroup();
            var lyrics = SplitLyrics.Split(Text);
            CurrentCount = lyrics.Count;
            if (update) {
                for (int i = 0; i < lyrics.Count && i < notes.Length; ++i) {
                    if (notes[i].lyric != lyrics[i]) {
                        DocManager.Inst.ExecuteCmd(new ChangeNoteLyricCommand(part, notes[i], lyrics[i]));
                    }
                }
            }
        }

        public void Reset() {
            if (startLyrics == null) {
                return;
            }
            DocManager.Inst.RollBackUndoGroup();
            Text = SplitLyrics.Join(startLyrics);
        }

        public void Cancel() {
            DocManager.Inst.RollBackUndoGroup();
            DocManager.Inst.EndUndoGroup();
        }

        public void Finish() {
            Preview(true);
            DocManager.Inst.EndUndoGroup();
        }
    }
}
