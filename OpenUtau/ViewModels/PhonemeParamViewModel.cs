using System;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    class PhonemeParamViewModel : ViewModelBase {
        [Reactive] public string Offset { get; set; }
        [Reactive] public string Preutter { get; set; }
        [Reactive] public string Overlap { get; set; }

        private UVoicePart part;
        public UNote note;
        private int index;

        public PhonemeParamViewModel(UVoicePart part, UPhoneme phoneme) {
            this.part = part;
            this.note = phoneme.Parent;
            this.index = phoneme.index;
            Offset = (note.GetPhonemeOverride(index).offset ?? 0).ToString();
            Preutter = (note.GetPhonemeOverride(index).preutterDelta ?? 0).ToString();
            Overlap = (note.GetPhonemeOverride(index).overlapDelta ?? 0).ToString();

            this.WhenAnyValue(x => x.Offset)
                .Subscribe(value => {
                    if (!int.TryParse(value.ToString(), out int i)) {
                        Offset = (note.GetPhonemeOverride(index).offset ?? 0).ToString();
                        return;
                    }
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new PhonemeOffsetCommand(part, note, index, i));
                    DocManager.Inst.EndUndoGroup();
                }
            );
            this.WhenAnyValue(x => x.Preutter)
                .Subscribe(value => {
                    if (!float.TryParse(value.ToString(), out float f)) {
                        Preutter = (note.GetPhonemeOverride(index).preutterDelta ?? 0).ToString();
                        return;
                    }
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new PhonemePreutterCommand(part, note, index, f));
                    DocManager.Inst.EndUndoGroup();
                }
            );
            this.WhenAnyValue(x => x.Overlap)
                .Subscribe(value => {
                    if (!float.TryParse(value.ToString(), out float f)) {
                        Overlap = (note.GetPhonemeOverride(index).overlapDelta ?? 0).ToString();
                        return;
                    }
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new PhonemeOverlapCommand(part, note, index, f));
                    DocManager.Inst.EndUndoGroup();
                }
            );
        }
    }
}
