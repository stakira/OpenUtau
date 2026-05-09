using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class TimelineSelectionViewModel : ViewModelBase {
        [Reactive] public int SelectionAnchorTick { get; set; }
        [Reactive] public int SelectionActiveTick { get; set; }
        [Reactive] public bool HasSelectionRange { get; set; }
        [Reactive] public bool IsSelectingRange { get; set; }

        public int SelectionStartTick => SelectionAnchorTick <= SelectionActiveTick
            ? SelectionAnchorTick
            : SelectionActiveTick;
        public int SelectionEndTick => SelectionAnchorTick >= SelectionActiveTick
            ? SelectionAnchorTick
            : SelectionActiveTick;
        public int SelectionDurationTicks => SelectionEndTick - SelectionStartTick;

        /// <summary>
        /// Starts a new timeline selection at the specified project tick.
        /// </summary>
        public void BeginSelectionRange(int tick) {
            SelectionAnchorTick = tick;
            SelectionActiveTick = tick;
            HasSelectionRange = true;
            IsSelectingRange = true;
            RaiseSelectionRangeProperties();
        }

        /// <summary>
        /// Updates the active end of the timeline selection using a project tick.
        /// </summary>
        public void UpdateSelectionRange(int tick) {
            if (!HasSelectionRange) {
                return;
            }
            SelectionActiveTick = tick;
            RaiseSelectionRangeProperties();
        }

        /// <summary>
        /// Commits the current timeline selection.
        /// </summary>
        public void CommitSelectionRange() {
            IsSelectingRange = false;
        }

        /// <summary>
        /// Sets the timeline selection using project-relative start and end ticks.
        /// </summary>
        public void SetSelectionRange(int startTick, int endTick) {
            SelectionAnchorTick = startTick;
            SelectionActiveTick = endTick;
            HasSelectionRange = true;
            RaiseSelectionRangeProperties();
        }

        /// <summary>
        /// Clears the current timeline selection.
        /// </summary>
        public void ClearSelectionRange() {
            SelectionAnchorTick = 0;
            SelectionActiveTick = 0;
            HasSelectionRange = false;
            IsSelectingRange = false;
            RaiseSelectionRangeProperties();
        }

        private void RaiseSelectionRangeProperties() {
            this.RaisePropertyChanged(nameof(SelectionStartTick));
            this.RaisePropertyChanged(nameof(SelectionEndTick));
            this.RaisePropertyChanged(nameof(SelectionDurationTicks));
        }
    }
}
