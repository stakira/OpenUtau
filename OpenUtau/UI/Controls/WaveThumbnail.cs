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
    class WaveThumbnail
    {
        public Path WavePath;
        StreamGeometry geometry;
        TransformGroup trans;
        TranslateTransform tTrans;
        ScaleTransform sTrans;

        public UWave Wave { set; get; }
        public Rectangle Box;

        public Brush Brush;

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

        public int TrackNo { get { return Wave.TrackNo; } }
        public int PosTick { get { return Wave.PosTick; } }
        public double DisplayWidth { get { return Wave.DurTick * ScaleX; } }

        //int _visualDurTick;
        double _height;

        //public bool Modified { get { return _visualDurTick != Wave.DurTick; } }

        bool _selected = false;
        bool _error = false;
        public bool Selected { set { _selected = value; Redraw(); } get { return _selected; } }
        public bool Error { set { _error = value; Redraw(); } get { return _error; } }

        public WaveThumbnail()
        {
            WavePath = new Path();

            sTrans = new ScaleTransform();
            tTrans = new TranslateTransform();
            trans = new TransformGroup();
            trans.Children.Add(sTrans);
            trans.Children.Add(tTrans);
            WavePath.RenderTransform = trans;
        }

        public void Redraw()
        {
            int i = 0;
            WavePath.Fill = Brush;
            geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(0, 0), true, true);
                foreach (float peak in Wave.LeftPeaks)
                {
                    ctx.LineTo(new Point(i + 1, peak), true, false);
                    i++;
                }
                ctx.LineTo(new Point(i, 0), true, false);
                while (i > 0)
                {
                    i--;
                    ctx.LineTo(new Point(i + 1, -Wave.LeftPeaks[i]), true, false);
                }
            }
            geometry.Freeze();
            WavePath.Data = geometry;
        }

        public void FitHeight(double height)
        {
            sTrans.ScaleY = -height / WavePath.ActualHeight;
            _height = height;
        }
    }
}
