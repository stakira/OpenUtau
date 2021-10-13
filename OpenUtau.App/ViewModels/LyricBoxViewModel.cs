using System;
using DynamicData.Binding;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using WanaKanaNet;

namespace OpenUtau.App.ViewModels {
    class LyricBoxViewModel : ViewModelBase {
        public class SuggestionItem {
            public string Alias { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
        }

        [Reactive] public UVoicePart? Part { get; set; }
        [Reactive] public UNote? Note { get; set; }
        [Reactive] public bool IsVisible { get; set; }
        [Reactive] public string Text { get; set; }
        [Reactive] public SuggestionItem? SelectedSuggestion { get; set; }
        [Reactive] public ObservableCollectionExtended<SuggestionItem> Suggestions { get; set; }

        public LyricBoxViewModel() {
            Text = string.Empty;
            Suggestions = new ObservableCollectionExtended<SuggestionItem>();

            this.WhenAnyValue(x => x.Text, x => x.IsVisible)
                .Subscribe(_ => UpdateSuggestion());
            this.WhenAnyValue(x => x.SelectedSuggestion)
                .WhereNotNull()
                .Subscribe(ss => Serilog.Log.Information(ss.Alias));
        }

        private void UpdateSuggestion() {
            if (Part == null || Note == null) {
                Suggestions.Clear();
                return;
            }
            Suggestions.Clear();
            var singer = DocManager.Inst.Project.tracks[Part.trackNo].Singer;
            if (singer == null || !singer.Loaded) {
                Suggestions.Add(new SuggestionItem() {
                    Alias = "No Singer",
                });
                return;
            }
            if (!string.IsNullOrEmpty(Text)) {
                Suggestions.Add(new SuggestionItem() {
                    Alias = WanaKana.ToHiragana(Text),
                    Source = "a->あ",
                });
            }
            singer.GetSuggestions(Text, oto => Suggestions.Add(new SuggestionItem() {
                Alias = oto.Alias,
                Source = string.IsNullOrEmpty(oto.Set) ? singer.Id : $"{singer.Id} / {oto.Set}",
            }));
        }

        public void Commit() {
            if (Part == null || Note == null || Text == Note.lyric) {
                return;
            }
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new ChangeNoteLyricCommand(Part, Note, Text));
            DocManager.Inst.EndUndoGroup();
        }
    }
}
