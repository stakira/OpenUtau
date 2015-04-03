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

        public const double NoteMaxWidth = 256;
        public const double NoteMinWidth = 4;
        public const double NoteMinWidthDisplay = 6;
        public const double NoteMaxHeight = 128;
        public const double NoteMinHeight = 8;

        public static System.Windows.Media.DoubleCollection DashLineArray = new System.Windows.Media.DoubleCollection() { 2, 2 };
    }
}
