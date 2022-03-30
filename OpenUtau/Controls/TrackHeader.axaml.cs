using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.Controls {
    public partial class TrackHeader : UserControl, IDisposable {
        public static readonly DirectProperty<TrackHeader, double> TrackHeightProperty =
            AvaloniaProperty.RegisterDirect<TrackHeader, double>(
                nameof(TrackHeight),
                o => o.TrackHeight,
                (o, v) => o.TrackHeight = v);
        public static readonly DirectProperty<TrackHeader, Point> OffsetProperty =
            AvaloniaProperty.RegisterDirect<TrackHeader, Point>(
                nameof(Offset),
                o => o.Offset,
                (o, v) => o.Offset = v);
        public static readonly DirectProperty<TrackHeader, int> TrackNoProperty =
            AvaloniaProperty.RegisterDirect<TrackHeader, int>(
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

        public TrackHeaderViewModel? ViewModel;

        private List<IDisposable> unbinds = new List<IDisposable>();

        private UTrack? track;

        public TrackHeader() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change) {
            base.OnPropertyChanged(change);
            if (!change.IsEffectiveValueChange) {
                return;
            }
            if (change.Property == OffsetProperty ||
                change.Property == TrackNoProperty ||
                change.Property == TrackHeightProperty) {
                SetPosition();
            }
        }

        internal void Bind(UTrack track, TrackHeaderCanvas canvas) {
            this.track = track;
            unbinds.Add(this.Bind(TrackHeightProperty, canvas.GetObservable(TrackHeaderCanvas.TrackHeightProperty)));
            unbinds.Add(this.Bind(HeightProperty, canvas.GetObservable(TrackHeaderCanvas.TrackHeightProperty)));
            unbinds.Add(this.Bind(OffsetProperty, canvas.WhenAnyValue(x => x.TrackOffset, trackOffset => new Point(0, -trackOffset * TrackHeight))));
            SetPosition();
        }

        private void SetPosition() {
            Canvas.SetLeft(this, 0);
            Canvas.SetTop(this, Offset.Y + (track?.TrackNo ?? 0) * trackHeight);
        }

        void SingerButtonClicked(object sender, RoutedEventArgs args) {
            var singerMenu = this.FindControl<ContextMenu>("SingersMenu");
            if (DocManager.Inst.Singers.Count > 0) {
                ViewModel?.RefreshSingers();
                singerMenu.Open();
            }
            args.Handled = true;
        }

        void SingerButtonContextRequested(object sender, ContextRequestedEventArgs args) {
            args.Handled = true;
        }

        void PhonemizerButtonClicked(object sender, RoutedEventArgs args) {
            var phonemizerMenu = this.FindControl<ContextMenu>("PhonemizersMenu");
            if (DocManager.Inst.PhonemizerFactories.Length > 0) {
                ViewModel?.RefreshPhonemizers();
                phonemizerMenu.Open();
            }
            args.Handled = true;
        }

        void PhonemizerButtonContextRequested(object sender, ContextRequestedEventArgs args) {
            args.Handled = true;
        }

        void FaderPointerPressed(object sender, PointerPressedEventArgs args) {
            if (args.GetCurrentPoint((IVisual?)sender).Properties.IsRightButtonPressed && ViewModel != null) {
                ViewModel.Volume = 0;
                args.Handled = true;
            }
        }

        void FaderContextRequested(object sender, ContextRequestedEventArgs args) {
            if (ViewModel != null) {
                ViewModel.Volume = 0;
            }
            args.Handled = true;
        }

        public void Dispose() {
            unbinds.ForEach(u => u.Dispose());
            unbinds.Clear();
        }
    }
}
