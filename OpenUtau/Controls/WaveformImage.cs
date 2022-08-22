using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using OpenUtau.App.ViewModels;
using ReactiveUI;
using Serilog;

namespace OpenUtau.App.Controls {
    class WaveformImage : Image {
        public static readonly DirectProperty<WaveformImage, double> TickWidthProperty =
            AvaloniaProperty.RegisterDirect<WaveformImage, double>(
                nameof(TickWidth),
                o => o.TickWidth,
                (o, v) => o.TickWidth = v);
        public static readonly DirectProperty<WaveformImage, double> TickOffsetProperty =
            AvaloniaProperty.RegisterDirect<WaveformImage, double>(
                nameof(TickOffset),
                o => o.TickOffset,
                (o, v) => o.TickOffset = v);
        public static readonly DirectProperty<WaveformImage, bool> ShowWaveformProperty =
            AvaloniaProperty.RegisterDirect<WaveformImage, bool>(
                nameof(ShowWaveform),
                o => o.ShowWaveform,
                (o, v) => o.ShowWaveform = v);

        public double TickWidth {
            get => tickWidth;
            set => SetAndRaise(TickWidthProperty, ref tickWidth, value);
        }
        public double TickOffset {
            get { return tickOffset; }
            set { SetAndRaise(TickOffsetProperty, ref tickOffset, value); }
        }
        public bool ShowWaveform {
            get { return showWaveform; }
            set { SetAndRaise(ShowWaveformProperty, ref showWaveform, value); }
        }

        private double tickWidth;
        private double tickOffset;
        private bool showWaveform;

        private WriteableBitmap? bitmap;
        private float[] sampleData = new float[0];
        private int sampleCount;
        private int[] bitmapData = new int[0];

        public WaveformImage() {
            MessageBus.Current.Listen<WaveformRefreshEvent>()
                .Subscribe(e => {
                    InvalidateVisual();
                });
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change) {
            base.OnPropertyChanged(change);
            if (!change.IsEffectiveValueChange) {
                return;
            }
            if (change.Property == DataContextProperty ||
                change.Property == TickWidthProperty ||
                change.Property == TickOffsetProperty ||
                change.Property == ShowWaveformProperty) {
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext context) {
            var bitmap = GetBitmap();
            if (bitmap != null) {
                Array.Clear(bitmapData, 0, bitmapData.Length);
                var viewModel = (NotesViewModel?)DataContext;
                if (viewModel != null && ShowWaveform &&
                    viewModel.TickWidth > ViewConstants.PianoRollTickWidthShowDetails) {
                    var project = viewModel.Project;
                    var part = viewModel.Part;
                    if (project != null && part != null && part.Mix != null) {
                        double leftMs = project.timeAxis.TickPosToMsPos(viewModel.TickOrigin + viewModel.TickOffset);
                        double rightMs = project.timeAxis.TickPosToMsPos(viewModel.TickOrigin + viewModel.TickOffset + viewModel.ViewportTicks);
                        int samplePos = (int)(leftMs * 44100 / 1000) * 2;
                        sampleCount = (int)((rightMs - leftMs) * 44100 / 1000) * 2;
                        if (sampleData.Length < sampleCount) {
                            Array.Resize(ref sampleData, sampleCount);
                        }
                        Array.Clear(sampleData, 0, sampleData.Length);
                        part.Mix.Mix(samplePos, sampleData, 0, sampleCount);

                        double samplesPerPixel = (double)sampleCount / bitmap.PixelSize.Width;
                        int startSample = 0;
                        for (int i = 0; i < bitmap.PixelSize.Width; ++i) {
                            int endSample = Math.Min(sampleCount, (int)(i * samplesPerPixel));
                            float max = 0;
                            float min = 0;
                            for (int j = startSample; j < endSample; ++j) {
                                max = Math.Max(max, sampleData[j]);
                                min = Math.Min(min, sampleData[j]);
                            }
                            float yMax = Math.Clamp(-max + 1f, 0, 2) * 0.5f * (bitmap.PixelSize.Height - 2) + 1;
                            float yMin = Math.Clamp(-min + 1f, 0, 2) * 0.5f * (bitmap.PixelSize.Height - 2) + 1;
                            DrawPeak(bitmapData, bitmap.PixelSize.Width, i, (int)yMax, (int)yMin);
                            startSample = endSample;
                        }
                    }
                }
                using (var frameBuffer = bitmap.Lock()) {
                    Marshal.Copy(bitmapData, 0, frameBuffer.Address, bitmapData.Length);
                }
            }
            base.Render(context);
        }

        private WriteableBitmap? GetBitmap() {
            if (Parent == null) {
                return null;
            }
            int desiredWidth = (int)Parent.Bounds.Width;
            int desiredHeight = (int)Parent.Bounds.Height;
            if (bitmap == null || bitmap.Size.Width != desiredWidth) {
                Source = null;
                bitmap?.Dispose();
                var size = new PixelSize(desiredWidth, desiredHeight);
                bitmap = new WriteableBitmap(
                    size, new Vector(96, 96),
                    Avalonia.Platform.PixelFormat.Rgba8888,
                    Avalonia.Platform.AlphaFormat.Unpremul);
                Log.Information($"Created bitmap {size}");
                bitmapData = new int[size.Width * size.Height];
                Source = bitmap;
                Width = bitmap.Size.Width;
                Height = bitmap.Size.Height;
            }
            return bitmap;
        }

        private void DrawPeak(int[] data, int width, int x, int y1, int y2) {
            const int color = 0x7F7F7F7F;
            if (y1 > y2) {
                int temp = y2;
                y2 = y1;
                y1 = temp;
            }
            for (var y = y1; y <= y2; ++y) {
                data[x + width * y] = color;
            }
        }
    }
}
