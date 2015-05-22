using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.ComponentModel;

using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Controls
{
    class PartElement : FrameworkElement
    {
        protected DrawingVisual frameVisual;
        protected DrawingVisual partVisual;
        protected DrawingVisual nameVisual;
        protected DrawingVisual commentVisual;

        protected TransformGroup trans;
        protected TranslateTransform tTransPre;
        protected TranslateTransform tTransPost;
        protected ScaleTransform sTransPre;
        protected ScaleTransform sTransPost;

        protected Pen pen;

        public UPart Part;
        public UProject Project;

        public virtual double X
        {
            set { tTransPost.X = value; }
            get { return tTransPost.X; }
        }

        public virtual double Y
        {
            set { tTransPost.Y = value; }
            get { return tTransPost.Y; }
        }

        public double ScaleX
        {
            set { sTransPost.ScaleX = value; RedrawFrame(); }
            get { return sTransPost.ScaleX; }
        }

        protected double _height;
        public double VisualHeight
        {
            set { if (value != _height) { _height = value; FitHeight(value); } }
            get { return _height; }
        }
        protected virtual void FitHeight(double height)
        { sTransPost.ScaleY = height / partVisual.ContentBounds.Height; RedrawFrame(); }
        public virtual double CanvasWidth { set; get; }

        public double VisualWidth { get { return Part.DurTick * ScaleX; } }

        protected bool _selected = false;
        public bool Selected { set { if (_selected != value) { _selected = value; RedrawFrame(); } } get { return _selected; } }

        public PartElement()
        {
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

            RenderOptions.SetEdgeMode(partVisual, EdgeMode.Aliased);

            this.AddVisualChild(frameVisual);
            this.AddVisualChild(partVisual);
            this.AddVisualChild(nameVisual);
            this.AddVisualChild(commentVisual);
        }

        public virtual void Redraw() { RedrawPart(); RedrawName(); RedrawComment(); }

        public virtual void RedrawFrame()
        {
            DrawingContext cxt = frameVisual.RenderOpen();
            cxt.DrawRoundedRectangle(GetFrameBrush(), null, new Rect(0, 0, Part.DurTick * ScaleX, _height), 4, 4);
            cxt.Close();
        }

        public virtual void RedrawPart() { }

        public virtual void RedrawName()
        {
            DrawingContext cxt = nameVisual.RenderOpen();
            FormattedText text = new FormattedText(
                Part.Name,
                System.Threading.Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight,
                SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                12,
                Brushes.White
            );
            text.SetFontWeight(FontWeights.Medium);
            cxt.DrawText(text, new Point(3, 2));
            cxt.Close();
        }

        public virtual void RedrawComment()
        {
            DrawingContext cxt = commentVisual.RenderOpen();
            FormattedText text = new FormattedText(
                Part.Comment,
                System.Threading.Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight,
                SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                12,
                Brushes.White
            );
            text.SetFontWeight(FontWeights.Regular);
            cxt.DrawText(text, new Point(3, 18));
            cxt.Close();
        }

        public virtual Brush GetFrameBrush()
        {
            if (Selected) return ThemeManager.NoteFillSelectedBrush;
            else return ThemeManager.NoteFillBrushes[0];
        }

        protected bool _modified = false;
        public virtual bool Modified { set { _modified = value; } get { return _modified; } }

        protected override int VisualChildrenCount { get { return 4; } }

        protected override Visual GetVisualChild(int index)
        {
            switch (index)
            {
                case 0: return frameVisual;
                case 1: return partVisual;
                case 2: return nameVisual;
                case 3: return commentVisual;
                default: return null;
            }
        }
    }

    class VoicePartElement : PartElement
    {
        public VoicePartElement() : base() { pen = new Pen(Brushes.White, 3); pen.Freeze(); }

        public override void RedrawPart()
        {
            DrawingContext cxt = partVisual.RenderOpen();
            cxt.DrawLine(pen, new Point(0, UIConstants.HiddenNoteNum), new Point(0, UIConstants.HiddenNoteNum));
            cxt.DrawLine(pen, new Point(Part.DurTick, UIConstants.MaxNoteNum - UIConstants.HiddenNoteNum),
                              new Point(Part.DurTick, UIConstants.MaxNoteNum - UIConstants.HiddenNoteNum));
            foreach (UNote note in ((UVoicePart)Part).Notes) cxt.DrawLine(pen, new Point(note.PosTick, UIConstants.MaxNoteNum - note.NoteNum),
                                                                 new Point(note.EndTick, UIConstants.MaxNoteNum - note.NoteNum));
            cxt.Close();
            tTransPre.Y = -partVisual.ContentBounds.Top;
            FitHeight(VisualHeight);
        }
    }

    class WavePartElement : PartElement
    {
        class PartImage : Image
        {
            protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
            {
                return null;
            }
        }

        BackgroundWorker worker;
        float[] peaks;

        PartImage partImage;
        WriteableBitmap partBitmap;
        protected TranslateTransform partImageTrans;

        double _canvasWidth;
        public override double CanvasWidth
        {
            set { if (_canvasWidth != value) { _canvasWidth = value; RedrawPart(); } }
            get { return _canvasWidth; }
        }

        public override double Y
        {
            set { tTransPost.Y = value; partImageTrans.Y = value; }
            get { return tTransPost.Y; }
        }

        public override double X
        {
            set { tTransPost.X = value; RedrawPart(); }
            get { return tTransPost.X; }
        }

        protected override void FitHeight(double height)
        {
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

            worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += BuildPeaksAsync;
            worker.ProgressChanged += BuildPeaksProgressChanged;
            worker.RunWorkerCompleted += BuildPeaksCompleted;
            worker.RunWorkerAsync((UWavePart)Part);
        }

        protected override Visual GetVisualChild(int index)
        {
            switch (index)
            {
                case 0: return frameVisual;
                case 1: return partImage;
                case 2: return nameVisual;
                case 3: return commentVisual;
                default: return null;
            }
        }

        public override void RedrawPart()
        {
            if (peaks == null) return;
            else DrawWaveform();
        }

        private void DrawWaveform()
        {
            int x = 0;
            double width = Part.DurTick * ScaleX;
            double height = _height;
            double samplesPerPixel = peaks.Length / width;
            using (BitmapContext cxt = partBitmap.GetBitmapContext())
            {
                double monoChnlAmp = (height - 4) / 2;
                double stereoChnlAmp = (height - 6) / 4;

                int channels = ((UWavePart)Part).Channels;
                partBitmap.Clear();
                float left, right, lmax, lmin, rmax, rmin;
                lmax = lmin = rmax = rmin = 0;
                double position = 0;

                int skippedPixels = (int)Math.Max(0, -this.X);
                if (skippedPixels > 0) { skippedPixels -= 1; x -= 1; } // draw 1 pixel out of view
                else if (this.X > 0) x = (int)Math.Round(this.X);
                position += skippedPixels * samplesPerPixel;

                for (int i = (int)(position / channels) * channels; i < peaks.Length; i += channels)
                {
                    left = peaks[i];
                    right = peaks[i + 1];
                    lmax = Math.Max(left, lmax);
                    lmin = Math.Min(left, lmin);
                    if (channels > 1)
                    {
                        rmax = Math.Max(right, rmax);
                        rmin = Math.Min(right, rmin);
                    }
                    if (i > position)
                    {
                        if (channels > 1)
                        {
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
                        }
                        else
                        {
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

        private void BuildPeaksProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            using (BitmapContext cxt = partBitmap.GetBitmapContext())
            {
                WriteableBitmapExtensions.FillRectangle(
                    partBitmap,
                    1, (int)(_height - 2),
                    17, (int)((_height - 4) * (1 - e.ProgressPercentage / 100.0)) + 2,
                    Colors.White);
            }
        }

        private void BuildPeaksAsync(object sender, DoWorkEventArgs e)
        {
            var _part = e.Argument as UWavePart;
            float[] peaks = Core.Formats.Wave.BuildPeaks(_part, sender as BackgroundWorker);
            e.Result = peaks;
        }

        private void BuildPeaksCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            peaks = e.Result as float[];
            RedrawPart();
        }
    }
}
