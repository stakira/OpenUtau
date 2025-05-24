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
        [Reactive] public string AttackTime { get; set; }
        [Reactive] public string ReleaseTime { get; set; }

        private string currentOfffset => (note.GetPhonemeOverride(index).offset ?? 0).ToString();
        private string currentPreutter => (note.GetPhonemeOverride(index).preutterDelta ?? 0).ToString();
        private string currentOverlap => (note.GetPhonemeOverride(index).overlapDelta ?? 0).ToString();
        private string currentAttackTime => (note.GetPhonemeOverride(index).attackTimeDelta ?? 0).ToString();
        private string currentReleaseTime => (note.GetPhonemeOverride(index).releaseTimeDelta ?? 0).ToString();

        private UVoicePart part;
        private UNote note;
        private int index;
        public bool loading = true;

        public PhonemeParamViewModel(UVoicePart part, UPhoneme phoneme) {
            this.part = part;
            this.note = phoneme.Parent;
            this.index = phoneme.index;
            Offset = currentOfffset;
            Preutter = currentPreutter;
            Overlap = currentOverlap;
            AttackTime = currentAttackTime;
            ReleaseTime = currentReleaseTime;

            this.WhenAnyValue(x => x.Offset)
                .Subscribe(value => {
                    if (loading) return;
                    if (!int.TryParse(value.ToString(), out int i)) {
                        Offset = currentOfffset;
                        return;
                    }
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new PhonemeOffsetCommand(part, note, index, i));
                    DocManager.Inst.EndUndoGroup();

                    loading = true;
                    Offset = currentOfffset;
                    loading = false;
                }
            );
            this.WhenAnyValue(x => x.Preutter)
                .Subscribe(value => {
                    if (loading) return;
                    if (!float.TryParse(value.ToString(), out float f)) {
                        Preutter = currentPreutter;
                        return;
                    }
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new PhonemePreutterCommand(part, note, index, phoneme, f));
                    DocManager.Inst.EndUndoGroup();

                    loading = true;
                    Preutter = currentPreutter;
                    loading = false;
                }
            );
            this.WhenAnyValue(x => x.Overlap)
                .Subscribe(value => {
                    if (loading) return;
                    if (!float.TryParse(value.ToString(), out float f)) {
                        Overlap = currentOverlap;
                        return;
                    }
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new PhonemeOverlapCommand(part, note, index, phoneme, f));
                    DocManager.Inst.EndUndoGroup();

                    loading = true;
                    Overlap = currentOverlap;
                    loading = false;
                }
            );
            this.WhenAnyValue(x => x.AttackTime)
                .Subscribe(value => {
                    if (loading) return;
                    if (!float.TryParse(value.ToString(), out float f)) {
                        AttackTime = currentAttackTime;
                        return;
                    }
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new PhonemeAttackTimeCommand(part, note, index, phoneme, f));
                    DocManager.Inst.EndUndoGroup();

                    loading = true;
                    AttackTime = currentAttackTime;
                    loading = false;
                }
            );
            this.WhenAnyValue(x => x.ReleaseTime)
                .Subscribe(value => {
                    if (loading) return;
                    if (!float.TryParse(value.ToString(), out float f)) {
                        ReleaseTime = currentReleaseTime;
                        return;
                    }
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new PhonemeReleaseTimeCommand(part, note, index, phoneme, f));
                    DocManager.Inst.EndUndoGroup();

                    loading = true;
                    ReleaseTime = currentReleaseTime;
                    loading = false;
                }
            );
        }
    }
}
