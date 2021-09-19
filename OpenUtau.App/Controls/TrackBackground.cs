using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using OpenUtau.Core;

namespace OpenUtau.App.Controls {
    class TrackBackground : TemplatedControl {
        public static readonly DirectProperty<TrackBackground, double> MaxTrackHeightProperty =
            AvaloniaProperty.RegisterDirect<TrackBackground, double>(
                nameof(MaxTrackHeight),
                o => o.MaxTrackHeight,
                (o, v) => o.MaxTrackHeight = v);
        public static readonly DirectProperty<TrackBackground, double> MinTrackHeightProperty =
            AvaloniaProperty.RegisterDirect<TrackBackground, double>(
                nameof(MinTrackHeight),
                o => o.MinTrackHeight,
                (o, v) => o.MinTrackHeight = v);
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

        public double MaxTrackHeight {
            get { return _maxTrackHeight; }
            set {
                value = Math.Max(value, MinTrackHeight);
                SetAndRaise(MaxTrackHeightProperty, ref _maxTrackHeight, value);
            }
        }
        public double MinTrackHeight {
            get { return _minTrackHeight; }
            set {
                SetAndRaise(MinTrackHeightProperty, ref _minTrackHeight, value);
                MaxTrackHeight = Math.Max(MaxTrackHeight, MinTrackHeight);
            }
        }
        public double TrackHeight {
            get { return _trackHeight; }
            private set {
                value = Math.Clamp(value, MinTrackHeight, MaxTrackHeight);
                SetAndRaise(TrackHeightProperty, ref _trackHeight, value);
            }
        }
        public double Track {
            get { return _track; }
            private set { SetAndRaise(TrackProperty, ref _track, value); }
        }

        private double _maxTrackHeight;
        private double _minTrackHeight;
        private double _trackHeight;
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
            if (TrackHeight == 0) {
                return;
            }
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
        public const int MaxNoteNum = 12 * 11;
        protected override bool IsAltTrack(int track) {
            int note = MaxNoteNum - 1 - track;
            return MusicMath.IsBlackKey(note);
        }
    }
}
