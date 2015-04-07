using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Models
{
    class PartThumbnail : System.Windows.Controls.Image
    {
        GeometryGroup thumbnailGeometry;
        Pen pen;
        TransformGroup trans;
        TranslateTransform tTrans;
        ScaleTransform sTrans;

        public UPart Part { set; get; }
        
        public Brush Brush
        {
            set { pen.Brush = value; }
            get { return pen.Brush; }
        }

        public double ScaleX
        {
            set { sTrans.ScaleX = value; }
            get { return sTrans.ScaleX; }
        }
        
        public double ScaleY
        {
            set { sTrans.ScaleY = value; }
            get { return sTrans.ScaleY; }
        }

        public double X
        {
            set { tTrans.X = value; }
            get { return tTrans.X; }
        }

        public double Y
        {
            set { tTrans.Y = value; }
            get { return tTrans.Y; }
        }

        public int TrackNo { get { return Part.TrackNo; } }
        public int PosTick { get { return Part.PosTick; } }
        public double DisplayWidth { get { return Part.DurTick * ScaleX; } }
        public int VisualDurTick;

        bool _selected = false;
        bool _error = false;
        public bool Selected { set { _selected = value; Redraw(); } get { return _selected; } }
        public bool Error { set { _error = value; Redraw(); } get { return _error; } }

        public PartThumbnail()
        {
            thumbnailGeometry = new GeometryGroup();
            pen = new Pen() { Thickness = 3 };
            this.Source = new DrawingImage(new GeometryDrawing(null, pen, thumbnailGeometry));

            sTrans = new ScaleTransform();
            tTrans = new TranslateTransform();
            trans = new TransformGroup();
            trans.Children.Add(sTrans);
            trans.Children.Add(tTrans);
            this.RenderTransform = trans;
            RenderOptions.SetEdgeMode((DependencyObject)this, EdgeMode.Aliased);
        }

        public void Redraw()
        {
            if (Part == null)
                throw new Exception("UPart cannot be null");
            thumbnailGeometry.Children.Clear();
            // Topleft
            thumbnailGeometry.Children.Add(
                new LineGeometry(new Point(0, UIConstants.HiddenNoteNum), new Point(0, UIConstants.HiddenNoteNum)));
            // Bottomright
            thumbnailGeometry.Children.Add(
                new LineGeometry(
                    new Point(Part.DurTick, UIConstants.MaxNoteNum - UIConstants.HiddenNoteNum),
                    new Point(Part.DurTick, UIConstants.MaxNoteNum - UIConstants.HiddenNoteNum)));
            foreach (UNote unote in Part.Notes){
                thumbnailGeometry.Children.Add(
                    new LineGeometry(new Point(unote.PosTick, UIConstants.MaxNoteNum - unote.NoteNum),
                        new Point(unote.PosTick + unote.DurTick, UIConstants.MaxNoteNum - unote.NoteNum)));
            }
            VisualDurTick = Part.DurTick;
        }

        public void FitHeight(double height)
        {
            sTrans.ScaleY = height / Source.Height;
        }

        public void FitWidth(double width)
        {
            sTrans.ScaleX = width / Source.Width;
        }
    }
}
