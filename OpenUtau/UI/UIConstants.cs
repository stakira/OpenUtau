using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.UI
{
    public static class UIConstants
    {
        public const int MaxNoteNum = 12 * 11;
        public const int HiddenNoteNum = 12 * 4;

        public const int KeyTrackZIndex = 0;
        public const int PosMarkerHightlighZIndex = 100;
        public const int NoteZIndex = 200;
        public const int NoteWithLyricBoxZIndex = 1000;

        public const int ResizeMargin = 8;

        public static System.Windows.Media.DoubleCollection DashLineArray = new System.Windows.Media.DoubleCollection() { 2, 4 };

        public const double NoteMaxHeight = 128;
        public const double NoteMinHeight = 8;
        public const double NoteDefaultHeight = 22;

        public const double MidiQuarterMaxWidth = 512;
        public const double MidiQuarterMinWidth = 8;
        public const double MidiQuarterDefaultWidth = 32;
        public const double MidiTickMinWidth = 16;

        public const double TrackMaxHeight = 256;
        public const double TrackMinHeight = 16;
        public const double TrackDefaultHeight = 64;

        public const double TrackQuarterMaxWidth = 256;
        public const double TrackQuarterMinWidth = 8;
        public const double TrackQuarterDefaultWidth = 16;
        public const double TrackTickMinWidth = 16;

        public const int DefaultTrackCount = 8;
        public const int DefaultQuarterCount = 256;

        public const double TickMinDisplayWidth = 6;
        public const double NoteMinDisplayWidth = 2;

        public const int PartRectangleZIndex = 100;
        public const int PartThumbnailZIndex = 200;
    }
}
