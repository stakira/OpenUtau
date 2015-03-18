using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.UI.Models
{
    class Note
    {
        public int keyNo;
        public double beat;
        public double length = 1;
        public string lyric = "a";
        public System.Windows.Shapes.Rectangle shape;
    }
}
