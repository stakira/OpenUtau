using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using Avalonia.Media.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Serilog;
using System.Threading;

namespace OpenUtau.App.Controls {
    class PartControl : TemplatedControl, IDisposable, IProgress<int> {
        public static readonly DirectProperty<PartControl, double> TickWidthProperty =
            AvaloniaProperty.RegisterDirect<PartControl, double>(
                nameof(TickWidth),
                o => o.TickWidth,
                (o, v) => o.TickWidth = v);
        public static readonly DirectProperty<PartControl, double> TrackHeightProperty =
            AvaloniaProperty.RegisterDirect<PartControl, double>(
                nameof(TrackHeight),
                o => o.TrackHeight,
                (o, v) => o.TrackHeight = v);
        public static readonly DirectProperty<PartControl, double> ViewWidthProperty =
            AvaloniaProperty.RegisterDirect<PartControl, double>(
                nameof(ViewWidth),
                o => o.ViewWidth,
                (o, v) => o.ViewWidth = v);
        public static readonly DirectProperty<PartControl, double> TickOffsetProperty =
            AvaloniaProperty.RegisterDirect<PartControl, double>(
                nameof(TickOffset),
                o => o.TickOffset,
                (o, v) => o.TickOffset = v);
        public static readonly DirectProperty<PartControl, Point> OffsetProperty =
            AvaloniaProperty.RegisterDirect<PartControl, Point>(
                nameof(Offset),
                o => o.Offset,
                (o, v) => o.Offset = v);
        public static readonly DirectProperty<PartControl, string> TextProperty =
            AvaloniaProperty.RegisterDirect<PartControl, string>(
                nameof(Text),
                o => o.Text,
                (o, v) => o.Text = v);
        public static readonly DirectProperty<PartControl, bool> SelectedProperty =
            AvaloniaProperty.RegisterDirect<PartControl, bool>(
                nameof(Selected),
                o => o.Selected,
                (o, v) => o.Selected = v);

        // Tick width in pixel.
        public double TickWidth {
            get => tickWidth;
            set => SetAndRaise(TickWidthProperty, ref tickWidth, value);
        }
        public double TrackHeight {
            get => trackHeight;
            set => SetAndRaise(TrackHeightProperty, ref trackHeight, value);
        }
        public double ViewWidth {
            get { return viewWidth; }
            set { SetAndRaise(ViewWidthProperty, ref viewWidth, value); }
        }
        public double TickOffset {
            get { return tickOffset; }
            set { SetAndRaise(TickOffsetProperty, ref tickOffset, value); }
        }
        public Point Offset {
            get { return offset; }
            set { SetAndRaise(OffsetProperty, ref offset, value); }
        }
        public string Text {
            get { return text; }
            set { SetAndRaise(TextProperty, ref text, value); }
        }
        public bool Selected {
            get { return selected; }
            set { SetAndRaise(SelectedProperty, ref selected, value); }
        }

        private double tickWidth;
        private double trackHeight;
        private double viewWidth;
        private double tickOffset;
        private Point offset;
        private string text = string.Empty;
        private bool selected;

        public readonly UPart part;
        private readonly Pen notePen = new Pen(Brushes.White, 3);
        private List<IDisposable> unbinds = new List<IDisposable>();
        public readonly Image image;
        private WriteableBitmap? bitmap;
        private int[] bitmapData;

        public PartControl(UPart part, PartsCanvas canvas) {
            image = new Image() {
                IsHitTestVisible = false,
            };
            this.part = part;
            Foreground = Brushes.White;
            Text = part.DisplayName;
            bitmapData = new int[0];

            unbinds.Add(this.Bind(TickWidthProperty, canvas.GetObservable(PartsCanvas.TickWidthProperty)));
            unbinds.Add(this.Bind(TrackHeightProperty, canvas.GetObservable(PartsCanvas.TrackHeightProperty)));
            unbinds.Add(this.Bind(WidthProperty, canvas.GetObservable(PartsCanvas.TickWidthProperty).Select(tickWidth => tickWidth * part.Duration)));
            unbinds.Add(this.Bind(HeightProperty, canvas.GetObservable(PartsCanvas.TrackHeightProperty)));
            unbinds.Add(this.Bind(OffsetProperty, canvas.WhenAnyValue(x => x.TickOffset, x => x.TrackOffset,
                (tick, track) => new Point(-tick * TickWidth, -track * TrackHeight))));
            unbinds.Add(this.Bind(ViewWidthProperty, canvas.WhenAnyValue(x => x.Bounds).Select(bounds => bounds.Width)));
            unbinds.Add(this.Bind(TickOffsetProperty, canvas.WhenAnyValue(x => x.TickOffset).Select(tickOffset => tickOffset)));

            SetPosition();

            if (part is UWavePart wavePart) {
                var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
                Task.Run(() => {
                    wavePart.BuildPeaks(this);
                }).ContinueWith((task) => {
                    if (task.IsFaulted) {
                        Log.Error(task.Exception, "Failed to build peaks");
                    } else {
                        InvalidateVisual();
                    }
                }, CancellationToken.None, TaskContinuationOptions.None, scheduler);
            }
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change) {
            base.OnPropertyChanged(change);
            if (!change.IsEffectiveValueChange) {
                return;
            }
            if (change.Property == OffsetProperty ||
                change.Property == TrackHeightProperty ||
                change.Property == TickWidthProperty) {
                SetPosition();
            }
            if (change.Property == SelectedProperty ||
                change.Property == TextProperty) {
                InvalidateVisual();
            }
        }

        public void SetPosition() {
            Canvas.SetLeft(this, Offset.X + part.position * tickWidth);
            Canvas.SetTop(this, Offset.Y + part.trackNo * trackHeight);
            Canvas.SetLeft(image, 0);
            Canvas.SetTop(image, Offset.Y + part.trackNo * trackHeight);
        }

        public void SetSize() {
            Width = TickWidth * part.Duration;
            Height = trackHeight;
        }

        public void Refersh() {
            Text = part.name;
        }

        public override void Render(DrawingContext context) {
            var backgroundBrush = Selected ? ThemeManager.AccentBrush2 : ThemeManager.AccentBrush1;
            // Background
            context.DrawRectangle(backgroundBrush, null, new Rect(1, 0, Width - 1, Height - 1), 4, 4);

            // Text
            var textLayout = TextLayoutCache.Get(Text, Foreground!, 12);
            using (var state = context.PushPreTransform(Matrix.CreateTranslation(3, 2))) {
                context.DrawRectangle(backgroundBrush, null, new Rect(new Point(0, 0), textLayout.Size));
                textLayout.Draw(context);
            }

            if (part == null) {
                return;
            }
            if (part is UVoicePart voicePart && voicePart.notes.Count > 0) {
                // Notes
                int maxTone = voicePart.notes.Max(note => note.tone);
                int minTone = voicePart.notes.Min(note => note.tone);
                if (maxTone - minTone < 36) {
                    int additional = (36 - (maxTone - minTone)) / 2;
                    minTone -= additional;
                    maxTone += additional;
                }
                using var pushedState = context.PushPreTransform(Matrix.CreateScale(1, trackHeight / (maxTone - minTone)));
                foreach (var note in voicePart.notes) {
                    var start = new Point((int)(note.position * tickWidth), maxTone - note.tone);
                    var end = new Point((int)(note.End * tickWidth), maxTone - note.tone);
                    context.DrawLine(notePen, start, end);
                }
            } else if (part is UWavePart wavePart) {
                // Waveform
                if (wavePart.Peaks == null) {
                    return;
                }
                try {
                    DrawWaveform(wavePart, GetBitmap(ViewWidth));
                } catch (Exception e) {
                    Log.Error(e, "failed to draw bitmap");
                }
            }
        }

        private WriteableBitmap GetBitmap(double width) {
            int w = 128 * (int)(width / 128 + 1);
            if (bitmap == null || bitmap.Size.Width < w) {
                image.Source = null;
                bitmap?.Dispose();
                var size = new PixelSize(w, (int)ViewConstants.TrackHeightMax);
                Log.Information($"created bitmap {size}");
                bitmap = new WriteableBitmap(
                    size, new Vector(96, 96),
                    Avalonia.Platform.PixelFormat.Rgba8888,
                    Avalonia.Platform.AlphaFormat.Unpremul);
                bitmapData = new int[size.Width * size.Height];
                image.Source = bitmap;
                image.Width = bitmap.Size.Width;
                image.Height = bitmap.Size.Height;
            }
            return bitmap;
        }

        private void DrawWaveform(UWavePart wavePart, WriteableBitmap bitmap) {
            double width = wavePart.Duration * TickWidth;
            double height = TrackHeight;
            double offset = TickWidth * (TickOffset - wavePart.position);
            using (var frameBuffer = bitmap.Lock()) {
                Array.Clear(bitmapData, 0, bitmapData.Length);

                double monoChnlAmp = (height - 4.0) / 2;
                double stereoChnlAmp = (height - 6.0) / 4;
                float[] peaks = wavePart.Peaks;
                double samplesPerPixel = peaks.Length / width;

                int channels = wavePart.channels;
                float left, right, lmax, lmin, rmax, rmin;
                lmax = lmin = rmax = rmin = 0;
                double position = (int)Math.Round(offset) * samplesPerPixel;
                int x = 0;
                if (offset < 0) {
                    position = 0;
                    x = (int)-offset;
                } else {
                    position = (int)Math.Round(offset) * samplesPerPixel;
                    x = 0;
                }

                for (int i = (int)(position / channels) * channels; i < peaks.Length; i += channels) {
                    left = peaks[i];
                    right = channels > 1 ? peaks[i + 1] : left;
                    lmax = Math.Max(left, lmax);
                    lmin = Math.Min(left, lmin);
                    if (channels > 1) {
                        rmax = Math.Max(right, rmax);
                        rmin = Math.Min(right, rmin);
                    }
                    if (i > position) {
                        if (channels > 1) {
                            DrawPeak(
                                bitmapData, frameBuffer.Size.Width, x,
                                (int)(stereoChnlAmp * (1 + lmin)) + 1,
                                (int)(stereoChnlAmp * (1 + lmax)) + 1);
                            DrawPeak(
                                bitmapData, frameBuffer.Size.Width, x,
                                (int)(stereoChnlAmp * (1 + rmin) + monoChnlAmp) + 2,
                                (int)(stereoChnlAmp * (1 + rmax) + monoChnlAmp) + 2);
                        } else {
                            DrawPeak(
                                bitmapData, frameBuffer.Size.Width, x,
                                (int)(monoChnlAmp * (1 + lmin)) + 2,
                                (int)(monoChnlAmp * (1 + lmax)) + 2);
                        }
                        lmax = lmin = rmax = rmin = 0;
                        position += samplesPerPixel;
                        x++;
                        if (x >= bitmap.Size.Width) {
                            break;
                        }
                    }
                }
                Marshal.Copy(bitmapData, 0, frameBuffer.Address, bitmapData.Length);
            }
        }

        private void DrawPeak(int[] data, int width, int x, int y1, int y2) {
            const int white = unchecked((int)0xFFFFFFFF);
            if (y1 > y2) {
                int temp = y2;
                y2 = y1;
                y1 = temp;
            }
            for (var y = y1; y <= y2; ++y) {
                data[x + width * y] = white;
            }
        }

        public void Report(int value) {
        }

        public void Dispose() {
            bitmap?.Dispose();
            unbinds.ForEach(u => u.Dispose());
            unbinds.Clear();
        }
    }
}
