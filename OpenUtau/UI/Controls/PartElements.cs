using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Controls
{
    public class PartElement : FrameworkElement
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

        public UPart Part;
        public UProject Project;

        public double X
        {
            set { tTransPost.X = value; }
            get { return tTransPost.X; }
        }

        public double Y
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
            set { if (value != _height) { _height = value; sTransPost.ScaleY = value / partVisual.ContentBounds.Height; RedrawFrame(); } }
            get { return _height; }
        }

        public double VisualWidth { get { return Part.DurTick * ScaleX; } }

        protected int _channel = 0;
        public int Channel { set { if (_channel != value) { _channel = value; RedrawFrame(); } } get { return _channel; } }

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
            if (Selected) return OpenUtau.UI.Models.ThemeManager.NoteFillSelectedBrush;
            else return OpenUtau.UI.Models.ThemeManager.NoteFillBrushes[Channel];
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

    public class VoicePartElement : PartElement
    {
        public override void RedrawPart()
        {
            DrawingContext cxt = partVisual.RenderOpen();
            Pen pen = new Pen(Brushes.White, 3);
            pen.Freeze();
            cxt.DrawLine(pen, new Point(0, UIConstants.HiddenNoteNum), new Point(0, UIConstants.HiddenNoteNum));
            cxt.DrawLine(pen, new Point(Part.DurTick, UIConstants.MaxNoteNum - UIConstants.HiddenNoteNum),
                              new Point(Part.DurTick, UIConstants.MaxNoteNum - UIConstants.HiddenNoteNum));
            foreach (UNote note in ((UVoicePart)Part).Notes) cxt.DrawLine(pen, new Point(note.PosTick, UIConstants.MaxNoteNum - note.NoteNum),
                                                                 new Point(note.EndTick, UIConstants.MaxNoteNum - note.NoteNum));
            cxt.Close();
            tTransPre.Y = -partVisual.ContentBounds.Top;
        }
    }

    public class WavePartElement : PartElement
    {
        public override void RedrawPart()
        {
            Pen pen = new Pen(Brushes.White, 1);
            pen.Freeze();

            if (((UWavePart)Part).Peaks == null) return;
            byte[] peaksArray = ToArray(((UWavePart)Part).Peaks);

            List<double> peaks = new List<double>();

            int i = 0;
            double sum = 0;
            int srcratio = 64;
            while (i < peaksArray.Length)
            {
                int data = (int)peaksArray[i] - 128;
                sum += data * data;
                if (i % srcratio == 0)
                {
                    peaks.Add(Math.Sqrt(sum / srcratio));
                    sum = 0;
                }
                i++;
            }

            var geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                i = 0;
                ctx.BeginFigure(new Point(0, 0), true, true);
                foreach (float peak in peaks)
                {
                    ctx.LineTo(new Point(i + 1, peak), true, false);
                    i++;
                }
                ctx.LineTo(new Point(i, 0), true, false);
                while (i > 0)
                {
                    i--;
                    ctx.LineTo(new Point(i + 1, -peaks[i]), true, false);
                }
            }
            geometry.Freeze();

            DrawingContext cxt = partVisual.RenderOpen();
            cxt.DrawGeometry(Brushes.White, null, geometry);
            cxt.Close();
            sTransPre.ScaleX = Project.BPM / Project.BeatUnit * 4 * Project.Resolution / 60 / 2000 * srcratio;
            tTransPre.Y = partVisual.ContentBounds.Height / 2;
        }

        public static byte[] ToArray(NAudio.Wave.WaveStream stream)
        {
            byte[] buffer = new byte[4096];
            int reader = 0;
            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
            while ((reader = stream.Read(buffer, 0, buffer.Length)) != 0)
                memoryStream.Write(buffer, 0, reader);
            return memoryStream.ToArray();
        }
    }
}
