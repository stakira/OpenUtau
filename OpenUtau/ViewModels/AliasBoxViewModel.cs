using System;
using System.Linq;
using System.Threading.Tasks;
using DynamicData.Binding;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using WanaKanaNet;

namespace OpenUtau.App.ViewModels {
    class AliasBoxViewModel : ViewModelBase {
        public class SuggestionItem {
            public string Alias { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
        }

        [Reactive] public UVoicePart? Part { get; set; }
        [Reactive] public UPhoneme? Phoneme { get; set; }
        [Reactive] public bool IsVisible { get; set; }
        [Reactive] public string Text { get; set; }
        [Reactive] public SuggestionItem? SelectedSuggestion { get; set; }
        [Reactive] public ObservableCollectionExtended<SuggestionItem> Suggestions { get; set; }

        public AliasBoxViewModel() {
            Text = string.Empty;
            Suggestions = new ObservableCollectionExtended<SuggestionItem>();

            this.WhenAnyValue(x => x.Text, x => x.IsVisible)
                .Subscribe(_ => UpdateSuggestion());
            this.WhenAnyValue(x => x.SelectedSuggestion)
                .WhereNotNull()
                .Subscribe(ss => Serilog.Log.Information(ss.Alias));
        }

        private void UpdateSuggestion() {
            if (Part == null || Phoneme == null) {
                Suggestions.Clear();
                return;
            }
            var singer = DocManager.Inst.Project.tracks[Part.trackNo].Singer;
            if (singer == null || !singer.Found || !singer.Loaded) {
                Suggestions.Clear();
                Suggestions.Add(new SuggestionItem() {
                    Alias = "No Singer",
                });
                return;
            }
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Task.Run(() => singer.GetSuggestions(Text).Select(oto => new SuggestionItem() {
                Alias = oto.Alias,
                Source = string.IsNullOrEmpty(oto.Set) ? singer.Id : $"{singer.Id} / {oto.Set}",
            }).Take(32).ToList()).ContinueWith(task => {
                Suggestions.Clear();
                if (!string.IsNullOrEmpty(Text)) {
                    Suggestions.Add(new SuggestionItem() {
                        Alias = WanaKana.ToHiragana(Text),
                        Source = "a->あ",
                    });
                }
                if (!task.IsFaulted) {
                    Suggestions.AddRange(task.Result);
                }
            }, scheduler);
        }

        public void Commit() {
            if (Part == null || Phoneme == null || Text == Phoneme.phoneme) {
                return;
            }
            DocManager.Inst.StartUndoGroup();
            // TODO: Execute change alias command
            DocManager.Inst.EndUndoGroup();
        }
    }
}
