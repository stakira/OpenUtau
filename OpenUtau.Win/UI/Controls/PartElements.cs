using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.UI.Controls {
    class PartElement : FrameworkElement {
        static Geometry pencilIcon = Geometry.Parse("M20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18,2.9 17.35,2.9 16.96,3.29L15.12,5.12L18.87,8.87M3,17.25V21H6.75L17.81,9.93L14.06,6.18L3,17.25Z");

        protected DrawingVisual frameVisual;
        protected DrawingVisual partVisual;
        protected DrawingVisual nameVisual;
        protected DrawingVisual commentVisual;

        protected TransformGroup trans;
        protected TranslateTransform tTransPre;
        protected TranslateTransform tTransPost;
        protected ScaleTransform sTransPre;
        protected ScaleTransform sTransPost;

        protected RectangleGeometry clip;

        protected Pen pen;

        public UPart Part;
        public UProject Project;

        public virtual double X {
            set { tTransPost.X = value; }
            get { return tTransPost.X; }
        }

        public virtual double Y {
            set { tTransPost.Y = value; }
            get { return tTransPost.Y; }
        }

        public virtual double ScaleX {
            set { sTransPost.ScaleX = value; RedrawFrame(); }
            get { return sTransPost.ScaleX; }
        }

        protected double _height;
        public double VisualHeight {
            set { if (value != _height) { _height = value; FitHeight(value); } }
            get { return _height; }
        }
        protected virtual void FitHeight(double height) { sTransPost.ScaleY = height / partVisual.ContentBounds.Height; RedrawFrame(); }
        public virtual double CanvasWidth { set; get; }

        public double VisualWidth { get { return Part.Duration * ScaleX; } }

        protected bool _selected = false;
        public bool Selected { set { if (_selected != value) { _selected = value; RedrawFrame(); } } get { return _selected; } }

        public PartElement() {
            sTransPre = new ScaleTransform();
            sTransPost = new ScaleTransform();
            tTransPre = new TranslateTransform();
            tTransPost = new TranslateTransform();
            trans = new TransformGroup();
            trans.Children.Add(sTransPre);
            trans.Children.Add(tTransPre);
            trans.Children.Add(sTransPost);
            trans.Children.Add(tTransPost);

            frameVisual = new DrawingVisual() { Transform = tTransPost };
            partVisual = new DrawingVisual() { Transform = trans };
            nameVisual = new DrawingVisual() { Transform = tTransPost };
            commentVisual = new DrawingVisual() { Transform = tTransPost };
            clip = new RectangleGeometry(new Rect(0, 0, 0, 0));

            RenderOptions.SetEdgeMode(partVisual, EdgeMode.Aliased);

            this.AddVisualChild(frameVisual);
            this.AddVisualChild(partVisual);
            this.AddVisualChild(nameVisual);
            this.AddVisualChild(commentVisual);
        }

        public virtual void Redraw() {
            RedrawPart();
            RedrawName();
            RedrawComment();
        }

        public virtual void RedrawFrame() {
            DrawingContext cxt = frameVisual.RenderOpen();
            cxt.DrawRoundedRectangle(GetFrameBrush(), null, new Rect(0, 0, Part.Duration * ScaleX, _height), 4, 4);
            clip.Rect = new Rect(0, 0, Part.Duration * ScaleX, _height);
            cxt.Close();
        }

        public virtual void RedrawPart() { }

        public virtual void RedrawName() {
            DrawingContext cxt = nameVisual.RenderOpen();
            FormattedText text = new FormattedText(
                Part.name,
                System.Threading.Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight,
                SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                12,
                Brushes.White, 1);
            text.SetFontWeight(FontWeights.Medium);
            cxt.PushClip(clip);
            cxt.DrawText(text, new Point(3, 2));
            if (Part is UVoicePart) {
                cxt.PushTransform(new TranslateTransform(5 + text.Width, 1));
                cxt.PushTransform(new ScaleTransform(.75, .75));
                cxt.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, 24, 24));
                cxt.DrawGeometry(Brushes.White, null, pencilIcon);
                cxt.Pop();
                cxt.Pop();
            }
            cxt.Pop();
            cxt.Close();
        }

        public virtual void RedrawComment() {
            DrawingContext cxt = commentVisual.RenderOpen();
            FormattedText text = new FormattedText(
                Part.comment,
                System.Threading.Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight,
                SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                12,
                Brushes.White, 1);
            text.SetFontWeight(FontWeights.Regular);
            cxt.DrawText(text, new Point(3, 18));
            cxt.Close();
        }

        public virtual Brush GetFrameBrush() {
            if (Selected) return ThemeManager.NoteFillSelectedBrush;
            else return ThemeManager.NoteFillBrushes[0];
        }

        protected bool _modified = false;
        public virtual bool Modified { set { _modified = value; } get { return _modified; } }

        protected override int VisualChildrenCount { get { return 4; } }

        protected override Visual GetVisualChild(int index) {
            switch (index) {
                case 0: return frameVisual;
                case 1: return partVisual;
                case 2: return nameVisual;
                case 3: return commentVisual;
                default: return null;
            }
        }

        public bool HitEditName(DrawingVisual hit) {
            return hit == nameVisual;
        }
    }

    class VoicePartElement : PartElement {
        public VoicePartElement() : base() { pen = new Pen(Brushes.White, 3); pen.Freeze(); }

        public override void RedrawPart() {
            DrawingContext cxt = partVisual.RenderOpen();
            cxt.DrawLine(pen, new Point(0, UIConstants.HiddenNoteNum), new Point(0, UIConstants.HiddenNoteNum));
            cxt.DrawLine(pen, new Point(Part.Duration, UIConstants.MaxNoteNum - UIConstants.HiddenNoteNum),
                              new Point(Part.Duration, UIConstants.MaxNoteNum - UIConstants.HiddenNoteNum));
            foreach (UNote note in ((UVoicePart)Part).notes) cxt.DrawLine(pen, new Point(note.position, UIConstants.MaxNoteNum - note.tone),
                                                                 new Point(note.End, UIConstants.MaxNoteNum - note.tone));
            cxt.Close();
            tTransPre.Y = -partVisual.ContentBounds.Top;
            FitHeight(VisualHeight);
        }
    }

    class WavePartElement : PartElement, IProgress<int> {
        class PartImage : Image {
            protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters) {
                return null;
            }
        }

        readonly PartImage partImage;
        readonly WriteableBitmap partBitmap;
        protected TranslateTransform partImageTrans;

        double _canvasWidth;
        public override double CanvasWidth {
            set { if (_canvasWidth != value) { _canvasWidth = value; RedrawPart(); } }
            get { return _canvasWidth; }
        }

        public override double Y {
            set { tTransPost.Y = value; partImageTrans.Y = value; }
            get { return tTransPost.Y; }
        }

        public override double X {
            set { tTransPost.X = value; RedrawPart(); }
            get { return tTransPost.X; }
        }

        public override double ScaleX {
            set { sTransPost.ScaleX = value; RedrawFrame(); RedrawPart(); }
            get { return sTransPost.ScaleX; }
        }

        protected override void FitHeight(double height) {
            base.FitHeight(height);
            RedrawPart();
        }

        public WavePartElement(UPart part) : base() {
            partImageTrans = new TranslateTransform();
            this.Part = part;

            partBitmap = BitmapFactory.New(
                (int)System.Windows.SystemParameters.VirtualScreenWidth,
                (int)UIConstants.TrackMaxHeight);
            partImage = new PartImage() { RenderTransform = partImageTrans, IsHitTestVisible = false };
            partImage.Arrange(new Rect(0, 0, partBitmap.PixelWidth, partBitmap.PixelHeight));
            partImage.Source = partBitmap;
            this.RemoveVisualChild(partVisual);
            this.AddVisualChild(partImage);

            if (((UWavePart)Part).Peaks == null) {
                var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
                Task.Run(() => {
                    ((UWavePart)Part).BuildPeaks(this);
                    Dispatcher.Invoke(() => {
                        RedrawPart();
                    });
                }).ContinueWith((task) => {
                    if (task.IsFaulted) {
                        Log.Information($"{task.Exception}");
                        throw task.Exception;
                    }
                }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, scheduler);
            }
        }

        protected override Visual GetVisualChild(int index) {
            switch (index) {
                case 0: return frameVisual;
                case 1: return partImage;
                case 2: return nameVisual;
                case 3: return commentVisual;
                default: return null;
            }
        }

        public override void RedrawPart() {
            if (((UWavePart)Part).Peaks == null) return;
            else DrawWaveform();
        }

        private void DrawWaveform() {
            float[] peaks = ((UWavePart)Part).Peaks;
            int x = 0;
            double width = Part.Duration * ScaleX;
            double height = _height;
            double samplesPerPixel = peaks.Length / width;
            using (BitmapContext cxt = partBitmap.GetBitmapContext()) {
                double monoChnlAmp = (height - 4) / 2;
                double stereoChnlAmp = (height - 6) / 4;

                int channels = ((UWavePart)Part).channels;
                partBitmap.Clear();
                float left, right, lmax, lmin, rmax, rmin;
                lmax = lmin = rmax = rmin = 0;
                double position = 0;

                int skippedPixels = (int)Math.Round(Math.Max(0, -this.X));
                if (skippedPixels > 0) { skippedPixels -= 1; x -= 1; } // draw 1 pixel out of view
                else if (this.X > 0) x = (int)Math.Round(this.X);
                position += skippedPixels * samplesPerPixel;

                for (int i = (int)(position / channels) * channels; i < peaks.Length; i += channels) {
                    left = peaks[i];
                    right = peaks[i + 1];
                    lmax = Math.Max(left, lmax);
                    lmin = Math.Min(left, lmin);
                    if (channels > 1) {
                        rmax = Math.Max(right, rmax);
                        rmin = Math.Min(right, rmin);
                    }
                    if (i > position) {
                        if (channels > 1) {
                            WriteableBitmapExtensions.DrawLine(
                                partBitmap,
                                x, (int)(stereoChnlAmp * (1 + lmin)) + 2,
                                x, (int)(stereoChnlAmp * (1 + lmax)) + 2,
                                Colors.White);
                            WriteableBitmapExtensions.DrawLine(
                                partBitmap,
                                x, (int)(stereoChnlAmp * (1 + rmin) + monoChnlAmp) + 3,
                                x, (int)(stereoChnlAmp * (1 + rmax) + monoChnlAmp) + 3,
                                Colors.White);
                        } else {
                            WriteableBitmapExtensions.DrawLine(
                                partBitmap,
                                x, (int)(monoChnlAmp * (1 + lmin)) + 2,
                                x, (int)(monoChnlAmp * (1 + lmax)) + 2,
                                Colors.White);
                        }
                        lmax = lmin = rmax = rmin = 0;
                        position += samplesPerPixel;
                        x++;
                        if (x > CanvasWidth) break;
                    }
                }
            }
        }

        public void Report(int value) {
            Dispatcher.Invoke(() => {
                using (BitmapContext cxt = partBitmap.GetBitmapContext()) {
                    partBitmap.Clear();
                    partBitmap.FillRectangle(
                        1 + (int)this.X, (int)(_height - 2),
                        17 + (int)this.X, (int)((_height - 4) * (1 - value / 100.0)) + 2,
                        Colors.White);
                }
            });
        }
    }
}
