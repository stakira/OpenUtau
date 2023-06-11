using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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

        private TextBlock volumeTextBlock;
        private TextBox volumeTextBox;
        private Slider volumeSlider;
        private TextBlock panTextBlock;
        private TextBox panTextBox;
        private Slider panSlider;

        public TrackHeader() {
            InitializeComponent();
            volumeTextBlock = this.FindControl<TextBlock>("VolumeTextBlock");
            volumeTextBox = this.FindControl<TextBox>("VolumeTextBox");
            volumeSlider = this.FindControl<Slider>("VolumeSlider");
            panTextBlock = this.FindControl<TextBlock>("PanTextBlock");
            panTextBox = this.FindControl<TextBox>("PanTextBox");
            panSlider = this.FindControl<Slider>("PanSlider");
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

        void TrackNameButtonClicked(object sender, RoutedEventArgs args) {
            ViewModel?.Rename();
            args.Handled = true;
        }

        void SingerButtonClicked(object sender, RoutedEventArgs args) {
            var singerMenu = this.FindControl<ContextMenu>("SingersMenu");
            if (SingerManager.Inst.Singers.Count > 0) {
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

        void RendererButtonClicked(object sender, RoutedEventArgs args) {
            var rendererMenu = this.FindControl<ContextMenu>("RenderersMenu");
            ViewModel?.RefreshRenderers();
            if (ViewModel?.RenderersMenuItems?.Count > 0) {
                rendererMenu.Open();
            }
            args.Handled = true;
        }

        void RendererButtonContextRequested(object sender, ContextRequestedEventArgs args) {
            args.Handled = true;
        }

        void VolumeFaderPointerPressed(object sender, PointerPressedEventArgs args) {
            if (args.GetCurrentPoint((IVisual?)sender).Properties.IsRightButtonPressed && ViewModel != null) {
                ViewModel.Volume = 0;
                args.Handled = true;
            }
        }

        void PanFaderPointerPressed(object sender, PointerPressedEventArgs args) {
            if (args.GetCurrentPoint((IVisual?)sender).Properties.IsRightButtonPressed && ViewModel != null) {
                ViewModel.Pan = 0;
                args.Handled = true;
            }
        }

        void VolumeFaderContextRequested(object sender, ContextRequestedEventArgs args) {
            if (ViewModel != null) {
                ViewModel.Volume = 0;
            }
            args.Handled = true;
        }

        void PanFaderContextRequested(object sender, ContextRequestedEventArgs args) {
            if (ViewModel != null) {
                ViewModel.Pan = 0;
            }
            args.Handled = true;
        }

        void TrackSettingsButtonClicked(object sender, RoutedEventArgs args) {
            if (track?.Singer != null && track.Singer.Found) {
                var dialog = new Views.TrackSettingsDialog(track);
                var window = (Application.Current?.ApplicationLifetime
                    as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                dialog.ShowDialog(window);
            }
        }

        void TextBlockClicked(object sender, RoutedEventArgs args) {
            var textBlock = (TextBlock)sender;
            if (textBlock.Name.Equals("VolumeTextBlock") && ViewModel != null) {
                volumeTextBox.Text = ViewModel.Volume.ToString();
                volumeTextBlock.IsVisible = false;
                volumeTextBox.IsVisible = true;
                args.Handled = true;
            }
            else if (textBlock.Name.Equals("PanTextBlock") && ViewModel != null) {
                panTextBox.Text = ViewModel.Pan.ToString();
                panTextBlock.IsVisible = false;
                panTextBox.IsVisible = true;
                args.Handled = true;
            }
        }
        void TextBoxEnter(object sender, KeyEventArgs args) {
            var textBlock = (TextBlock)sender;
            if (args.Key == Key.Enter) {
                if (textBlock.Name.Equals("VolumeTextBlock") && ViewModel != null) {
                    if (double.TryParse(volumeTextBox.Text, out double number)) {
                        number = number > volumeSlider.Minimum ? number < volumeSlider.Maximum ? number : volumeSlider.Maximum : volumeSlider.Minimum;
                        ViewModel.Volume = number;
                    }
                    volumeTextBlock.IsVisible = true;
                    volumeTextBox.IsVisible = false;
                    args.Handled = true;
                } else if (textBlock.Name.Equals("PanTextBlock") && ViewModel != null) {
                    if (int.TryParse(panTextBox.Text, out int number)) {
                        number = (int)(number > panSlider.Minimum ? number < panSlider.Maximum ? number : panSlider.Maximum : panSlider.Minimum);
                        ViewModel.Pan = number;
                    }
                    panTextBlock.IsVisible = true;
                    panTextBox.IsVisible = false;
                    args.Handled = true;
                }
            }
        }

        void TextBoxLeave(object sender, PointerEventArgs args) {
            var textBox = (TextBox)sender;
            textBox.IsVisible = true;
            volumeTextBox.IsVisible = false;
            panTextBox.IsVisible = false;
            args.Handled = true;
        }

        public void Dispose() {
            unbinds.ForEach(u => u.Dispose());
            unbinds.Clear();
        }
    }
}
