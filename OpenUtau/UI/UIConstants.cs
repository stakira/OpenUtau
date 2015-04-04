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

        public const double NoteMaxWidth = 256;
        public const double NoteMinWidth = 4;
        public const double NoteMinDisplayWidth = 6;
        public const double NoteMaxHeight = 128;
        public const double NoteMinHeight = 8;
        public const double NoteDefaultHeight = 22;

        public const double MidiWNoteMaxWidth = 1024;
        public const double MidiWNoteMinWidth = 16;
        public const double MidiWNoteDefaultWidth = 128;

        public static System.Windows.Media.DoubleCollection DashLineArray = new System.Windows.Media.DoubleCollection() { 2, 2 };

        public const double TrackMaxHeight = 256;
        public const double TrackMinHeight = 16;
        public const double TrackDefaultHeight = 64;

        public const double TrackWNoteMaxWidth = 1024;
        public const double TrackWNoteMinWidth = 16;
        public const double TrackWNoteDefaultWidth = 128;

        public const int MaxTrackCount = 8;

        public const double TickMinDisplayWidth = 6;

        public const int PartRectangleZIndex = 100;
        public const int PartThumbnailZIndex = 200;
    }
}
