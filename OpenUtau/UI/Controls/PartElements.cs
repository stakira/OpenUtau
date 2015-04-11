using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Controls
{
    class PartElement : FrameworkElement
    {
        Visual frameVisual;
        Visual partVisual;
        Visual nameVisual;
        Visual commentVisual;

        TransformGroup trans;
        TranslateTransform tTrans;
        ScaleTransform sTrans;

        int _trackNo;

        public UPart Part { set; get; }

        public double X;
        public double Y;
        public double ScaleX;

        public PartElement()
        {
            sTrans = new ScaleTransform();
            tTrans = new TranslateTransform();
            trans = new TransformGroup();
            trans.Children.Add(sTrans);
            trans.Children.Add(tTrans);
            this.RenderTransform = trans;
        }

        public void FitHeight(double height) { }
        
        public void Redraw() { }

        private bool CheckModified() { return false; }

    }

    class WavePartElement : PartElement
    {

    }

    class VoicePartElement : PartElement
    {

    }
}
