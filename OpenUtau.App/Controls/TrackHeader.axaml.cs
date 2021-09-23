using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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

        public double TrackHeight {
            get => trackHeight;
            set => SetAndRaise(TrackHeightProperty, ref trackHeight, value);
        }
        public Point Offset {
            get => offset;
            set => SetAndRaise(OffsetProperty, ref offset, value);
        }

        private double trackHeight;
        private Point offset;

        private List<IDisposable> unbinds = new List<IDisposable>();

        private UTrack track;
        private ContextMenu singerMenu;
        private ContextMenu phonemizerMenu;

        public TrackHeader() {
            InitializeComponent();
        }

        internal void Bind(UTrack track, TrackHeaderCanvas canvas) {
            this.track = track;
            unbinds.Add(this.Bind(TrackHeightProperty, canvas.GetObservable(TrackHeaderCanvas.TrackHeightProperty)));
            unbinds.Add(this.Bind(HeightProperty, canvas.GetObservable(TrackHeaderCanvas.TrackHeightProperty)));
            unbinds.Add(this.Bind(OffsetProperty, canvas.WhenAnyValue(x => x.TrackOffset, track => new Point(0, -track * TrackHeight))));
            SetPosition();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change) {
            base.OnPropertyChanged(change);
            if (!change.IsEffectiveValueChange) {
                return;
            }
            if (change.Property == OffsetProperty) {
                SetPosition();
            }
        }

        private void SetPosition() {
            Canvas.SetLeft(this, 0);
            Canvas.SetTop(this, Offset.Y + track.TrackNo * trackHeight);
        }

        void SingerButtonClicked(object sender, RoutedEventArgs args) {
            var singerMenu = this.FindControl<ContextMenu>("SingersMenu");
            if (DocManager.Inst.Singers.Count > 0) {
                singerMenu.Items = DocManager.Inst.Singers.Values.Select(singer => {
                    var item = new MenuItem() {
                        Header = singer.Name,
                        DataContext = singer,
                    };
                    item.Classes.Add("context");
                    item.Click += (o, e) => {
                        if (track.Singer != singer) {
                            DocManager.Inst.StartUndoGroup();
                            DocManager.Inst.ExecuteCmd(new TrackChangeSingerCommand(DocManager.Inst.Project, track, singer));
                            DocManager.Inst.EndUndoGroup();
                        }
                        (DataContext as TrackHeaderViewModel)?.ManuallyRaise();
                        singerMenu.Close();
                    };
                    return item;
                });
                singerMenu.Open();
            }
            args.Handled = true;
        }

        void PhonemizerButtonClicked(object sender, RoutedEventArgs args) {
            var phonemizerMenu = this.FindControl<ContextMenu>("PhonemizersMenu");
            if (DocManager.Inst.Phonemizers.Length > 0) {
                phonemizerMenu.Items = DocManager.Inst.Phonemizers.Select(phonemizer => {
                    var item = new MenuItem() {
                        Header = phonemizer.ToString(),
                        DataContext = phonemizer,
                    };
                    item.Classes.Add("context");
                    item.Click += (o, e) => {
                        if (track.Phonemizer.GetType() != phonemizer.GetType()) {
                            var newPhonemizer = Activator.CreateInstance(phonemizer.GetType()) as Phonemizer;
                            DocManager.Inst.StartUndoGroup();
                            DocManager.Inst.ExecuteCmd(new TrackChangePhonemizerCommand(DocManager.Inst.Project, track, newPhonemizer));
                            DocManager.Inst.EndUndoGroup();
                        }
                        (DataContext as TrackHeaderViewModel)?.ManuallyRaise();
                        phonemizerMenu.Close();
                    };
                    return item;
                });
                phonemizerMenu.Open();
            }
            args.Handled = true;
        }

        public void Dispose() {
            unbinds.ForEach(u => u.Dispose());
            unbinds.Clear();
        }
    }
}
