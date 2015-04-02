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

        public UPart UPart { set; get; }
        
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

        public PartThumbnail()
        {
            thumbnailGeometry = new GeometryGroup();
            pen = new Pen() { Thickness = 1 };
            this.Source = new DrawingImage(new GeometryDrawing(Brush, pen, thumbnailGeometry));

            sTrans = new ScaleTransform();
            tTrans = new TranslateTransform();
            trans = new TransformGroup();
            trans.Children.Add(sTrans);
            trans.Children.Add(tTrans);
            this.RenderTransform = trans;
        }

        public void Redraw()
        {
            if (UPart == null)
                throw new Exception("UPart cannot be null");
            thumbnailGeometry.Children.Clear();
            foreach (UNote unote in UPart.Notes){
                thumbnailGeometry.Children.Add(
                    new LineGeometry(new Point(unote.PosTick, unote.NoteNum),
                        new Point(unote.PosTick + unote.DurTick, unote.NoteNum)));
            }
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
