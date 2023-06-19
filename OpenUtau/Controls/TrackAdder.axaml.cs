using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using ReactiveUI;

namespace OpenUtau.App.Controls {
    public partial class TrackAdder : UserControl {
        public static readonly DirectProperty<TrackAdder, double> TrackHeightProperty =
            AvaloniaProperty.RegisterDirect<TrackAdder, double>(
                nameof(TrackHeight),
                o => o.TrackHeight,
                (o, v) => o.TrackHeight = v);
        public static readonly DirectProperty<TrackAdder, Point> OffsetProperty =
            AvaloniaProperty.RegisterDirect<TrackAdder, Point>(
                nameof(Offset),
                o => o.Offset,
                (o, v) => o.Offset = v);
        public static readonly DirectProperty<TrackAdder, int> TrackNoProperty =
            AvaloniaProperty.RegisterDirect<TrackAdder, int>(
                nameof(TrackNo),
                o => o.TrackNo,
                (o, v) => o.TrackNo = v);

        public double TrackHeight {
            get => trackHeight;
            set => SetAndRaise(TrackHeightProperty, ref trackHeight, value);
        }
        public Point Offset {
            get => offset;
            set => SetAndRaise(OffsetProperty, ref offset, value);
        }
        public int TrackNo {
            get => trackNo;
            set => SetAndRaise(TrackNoProperty, ref trackNo, value);
        }

        private double trackHeight;
        private Point offset;
        private int trackNo;

        public TrackAdder() {
            InitializeComponent();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);
            if (change.Property == OffsetProperty ||
                change.Property == TrackNoProperty ||
                change.Property == TrackHeightProperty) {
                SetPosition();
            }
        }

        internal void Bind(TrackHeaderCanvas canvas) {
            this.Bind(TrackHeightProperty, canvas.GetObservable(TrackHeaderCanvas.TrackHeightProperty));
            this.Bind(HeightProperty, canvas.GetObservable(TrackHeaderCanvas.TrackHeightProperty));
            this.Bind(OffsetProperty, canvas.WhenAnyValue(x => x.TrackOffset, trackOffset => new Point(0, -trackOffset * TrackHeight)));
            SetPosition();
        }

        private void SetPosition() {
            Canvas.SetLeft(this, 0);
            Canvas.SetTop(this, Offset.Y + TrackNo * trackHeight);
        }

        private void ButtonClicked(object sender, RoutedEventArgs e) {
            (DataContext as TracksViewModel)?.AddTrack();
        }
    }
}
