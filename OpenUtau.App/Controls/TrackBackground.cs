using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using OpenUtau.Core;

namespace OpenUtau.App.Controls {
    class TrackBackground : TemplatedControl {
        public static readonly DirectProperty<TrackBackground, double> TrackHeightProperty =
            AvaloniaProperty.RegisterDirect<TrackBackground, double>(
                nameof(TrackHeight),
                o => o.TrackHeight,
                (o, v) => o.TrackHeight = v);
        public static readonly DirectProperty<TrackBackground, double> TrackProperty =
            AvaloniaProperty.RegisterDirect<TrackBackground, double>(
                nameof(Track),
                o => o.Track,
                (o, v) => o.Track = v);

        public double TrackHeight {
            get { return _trackHeight; }
            private set { SetAndRaise(TrackHeightProperty, ref _trackHeight, value); }
        }
        public double Track {
            get { return _track; }
            private set { SetAndRaise(TrackProperty, ref _track, value); }
        }

        private double _trackHeight = UIConstants.TrackDefaultHeight;
        private double _track;

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change) {
            base.OnPropertyChanged(change);
            if (!change.IsEffectiveValueChange) {
                return;
            }
            if (change.Property == TrackHeightProperty || change.Property == TrackProperty) {
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext context) {
            int track = (int)Track;
            double top = TrackHeight * (track - Track);
            while (top < Bounds.Height) {
                var brush = IsAltTrack(track) ? Foreground : Background;
                context.DrawRectangle(
                    brush,
                    null,
                    new Rect(0, (int)top, Bounds.Width, TrackHeight));
                track++;
                top += TrackHeight;
            }
        }

        protected virtual bool IsAltTrack(int track) {
            return track % 2 == 1;
        }
    }

    class KeyTrackBackground : TrackBackground {
        protected override bool IsAltTrack(int track) {
            int note = UIConstants.MaxNoteNum - 1 - track;
            return MusicMath.IsBlackKey(note);
        }
    }
}
