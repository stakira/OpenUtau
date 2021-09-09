using ReactiveUI;

namespace OpenUtau.App.ViewModels {
    public class TrackHeaderViewModel : ViewModelBase {
        public int TrackNo {
            get => trackNo;
            set => this.RaiseAndSetIfChanged(ref trackNo, value);
        }

        private int trackNo;
    }
}
