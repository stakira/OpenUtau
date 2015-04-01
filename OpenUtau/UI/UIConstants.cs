using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.UI
{
    public static class UIConstants
    {
        public const int KeyTrackZIndex = 0;
        public const int PosMarkerHightlighZIndex = 100;
        public const int NoteZIndex = 200;
        public const int NoteWithLyricBoxZIndex = 1000;

        public static System.Windows.Media.DoubleCollection DashLineArray = new System.Windows.Media.DoubleCollection() { 2, 2 };
    }
}
