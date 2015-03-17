using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.ComponentModel;

namespace OpenUtau.UI.Models
{
    class NotesCanvasModel : INotifyPropertyChanged
    {
        public const double noteMaxWidth = 256;
        public const double noteMinWidth = 8;
        public const double noteMaxHeight = 128;
        public const double noteMinHeight = 8;

        public const int numNotesHeight = 12 * 11;

        public double noteWidth { get; set; } // Actually a quater beat
        public double noteHeight { get; set; }
        public double verticalPosition { get; set; }
        public double horizontalPosition { get; set; }

        public double viewPortWidth { get; set; }
        public double viewPortHeight { get; set; }

        public const int numNotesWidthMin = 128; // 32 beats minimal
        public int numNotesWidthScroll;
        public int numNotesWidth;

        public string[] noteStrings;

        public NotesCanvasModel()
        {
            noteHeight = 22;
            verticalPosition = 0.5;

            noteWidth = 32;
            numNotesWidth = numNotesWidthMin;
            numNotesWidthScroll = numNotesWidthMin;
            horizontalPosition = 0;

            noteStrings = new String[12] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        }

        public double getViewportSizeY(double viewHeight, double noteHeight=0)
        {
            if (noteHeight == 0)
                return viewHeight / (numNotesHeight * this.noteHeight - viewHeight);
            else
                return viewHeight / (numNotesHeight * noteHeight - viewHeight);
        }

        public double getViewOffsetY(double scrollValue, double viewHeight)
        {
            return scrollValue * (numNotesHeight * noteHeight - viewHeight);
        }

        public double keyToCanvas(int noteNo, double scrollValue, double viewHeight)
        {
            return (numNotesHeight - noteNo - 1) * noteHeight - getViewOffsetY(scrollValue, viewHeight);
        }

        public int canvasToKey(double y, double scrollValue, double viewHeight)
        {
            return numNotesHeight - 1 - (int)((y + getViewOffsetY(scrollValue, viewHeight)) / noteHeight);
        }

        public String getNoteString(int noteNo)
        {
            int octave = noteNo / 12 - 2;
            return noteStrings[noteNo % 12] + octave;
        }

        static int[] blackKeys = { 1, 3, 6, 8, 10 };

        static public System.Windows.Media.Brush getNoteBackgroundBrush(int noteNo)
        {
            if (blackKeys.Contains(noteNo % 12)) return (LinearGradientBrush)System.Windows.Application.Current.FindResource("BlackKeyBrushNormal");
            else if (noteNo % 12 == 0) return (LinearGradientBrush)System.Windows.Application.Current.FindResource("CenterKeyBrushNormal");
            else return (LinearGradientBrush)System.Windows.Application.Current.FindResource("WhiteKeyBrushNormal");
        }

        static public System.Windows.Media.Brush getNoteBrush(int noteNo)
        {
            if (blackKeys.Contains(noteNo % 12)) return (SolidColorBrush)System.Windows.Application.Current.FindResource("BlackKeyNoteBrushNormal");
            else if (noteNo % 12 == 0) return (SolidColorBrush)System.Windows.Application.Current.FindResource("CenterKeyNoteBrushNormal");
            else return (SolidColorBrush)System.Windows.Application.Current.FindResource("WhiteKeyNoteBrushNormal");
        }

        static public System.Windows.Media.Brush getNoteTrackBrush(int noteNo)
        {
            if (blackKeys.Contains(noteNo % 12)) return (SolidColorBrush)System.Windows.Application.Current.FindResource("BlackKeyTrackBrushNormal");
            else return (SolidColorBrush)System.Windows.Application.Current.FindResource("WhiteKeyTrackBrushNormal");
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
