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
        public const double noteMaxWidth = 100;
        public const double noteMinWidth = 10;
        public const double noteMaxHeight = 128;
        public const double noteMinHeight = 8;

        public const int numNotesHeight = 12 * 11;

        public double noteWidth { get; set; } // Actually a quater beat
        public double noteHeight { get; set; }
        public double verticalPosition { get; set; }
        public double horizontalPosition { get; set; }

        public double viewPortWidth { get; set; }
        public double viewPortHeight { get; set; }

        public double numNotesWidth { get; set; }

        public string[] noteStrings;

        public NotesCanvasModel()
        {
            noteWidth = 32;
            noteHeight = 22;
            verticalPosition = 0.5;
            horizontalPosition = 0;
            noteStrings = new String[12] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        }

        public double getViewportSize(double viewHeight, double noteHeight=0)
        {
            if (noteHeight == 0)
                return viewHeight / (numNotesHeight * this.noteHeight - viewHeight);
            else
                return viewHeight / (numNotesHeight * noteHeight - viewHeight);
        }

        public double getViewOffset(double scrollValue, double viewHeight)
        {
            return scrollValue * (numNotesHeight * noteHeight - viewHeight);
        }

        public double noteToCanvas(int noteNo, double scrollValue, double viewHeight)
        {
            return (numNotesHeight - noteNo - 1) * noteHeight - getViewOffset(scrollValue, viewHeight);
        }

        public int canvasToNote(double y, double scrollValue, double viewHeight)
        {
            return numNotesHeight - 1 - (int)((y + getViewOffset(scrollValue, viewHeight)) / noteHeight);
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
