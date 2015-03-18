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
        public const double noteMinWidthDisplay = 16;
        public const double noteMaxHeight = 128;
        public const double noteMinHeight = 8;

        public const double resizeMargin = 8;

        public const int numNotesHeight = 12 * 11;

        public double noteWidth { get; set; } // Actually a quater beat
        public double noteHeight { get; set; }
        public double verticalPosition { get; set; }
        public double horizontalPosition { get; set; }

        public const int numNotesWidthMin = 128; // 32 beats minimal
        public int numNotesWidthScroll;
        public int numNotesWidth;

        public int bar = 4; // beats per bar
        public int beat = 4; // quarter-notes per beat

        public string[] noteStrings;

        public List<Note> notes;

        public NotesCanvasModel()
        {
            noteHeight = 22;
            verticalPosition = 0.5;

            noteWidth = 32;
            numNotesWidth = numNotesWidthMin;
            numNotesWidthScroll = numNotesWidthMin;
            horizontalPosition = 0;

            noteStrings = new String[12] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

            notes = new List<Note>();
        }

        public Note shapeToNote(System.Windows.Shapes.Rectangle shape)
        {
            foreach (Note note in notes)
            {
                if (note.shape == shape) return note;
            }
            return null;
        }

        public void debugPrintNotes()
        {
            System.Diagnostics.Debug.WriteLine(notes.Count.ToString() + " Notes in Total");
            foreach (Note _note in notes)
            {
                System.Diagnostics.Debug.WriteLine("Note : " + _note.beat.ToString() + " " + _note.keyNo.ToString());
            }
        }
            
        public double getViewportSizeY(double viewHeight, double noteHeight = 0)
        {
            double _noteHeight = noteHeight == 0 ? this.noteHeight : noteHeight;
            if (numNotesHeight * _noteHeight - viewHeight == 0) return 10000;
            return viewHeight / (numNotesHeight * _noteHeight - viewHeight);
        }

        public double getViewportSizeX(double viewWidth, double noteWidth = 0)
        {
            double _noteWidth = noteWidth == 0 ? this.noteWidth : noteWidth;
            if (numNotesWidthScroll * _noteWidth - viewWidth == 0) return 10000;
            return viewWidth / (numNotesWidthScroll * _noteWidth - viewWidth);
        }

        public double getViewOffsetY(double scrollValue, double viewHeight)
        {
            return scrollValue * (numNotesHeight * noteHeight - viewHeight);
        }

        public double getViewOffsetX(double scrollValue, double viewWidth)
        {
            return scrollValue * (numNotesWidthScroll * noteWidth - viewWidth);
        }

        public double keyToCanvas(int noteNo, double scrollValue, double viewHeight)
        {
            return (numNotesHeight - noteNo - 1) * noteHeight - getViewOffsetY(scrollValue, viewHeight);
        }

        public int canvasToKey(double y, double scrollValue, double viewHeight)
        {
            return numNotesHeight - 1 - (int)((y + getViewOffsetY(scrollValue, viewHeight)) / noteHeight);
        }

        public double snapToKey(double y, double scrollValue, double viewHeight)
        {
            int noteNo = canvasToKey(y, scrollValue, viewHeight);
            return keyToCanvas(noteNo, scrollValue, viewHeight);
        }

        public double beatToCanvas(double beatNo, double scrollValue, double viewWidth)
        {
            return beatNo * noteWidth - getViewOffsetX(scrollValue, viewWidth);
        }

        public int canvasToBeat(double x, double scrollValue, double viewWidth)
        {
            return (int)((x + getViewOffsetX(scrollValue, viewWidth)) / noteWidth);
        }

        public double snapToBeat(double x, double scrollValue, double viewWidth)
        {
            int beatNo = canvasToBeat(x, scrollValue, viewWidth);
            return beatToCanvas(beatNo, scrollValue, viewWidth);
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

        static public System.Windows.Style getKeyStyle(int keyNo)
        {
            if (blackKeys.Contains(keyNo % 12)) return (System.Windows.Style)System.Windows.Application.Current.FindResource("BlackKeyStyle");
            else if (keyNo % 12 == 0) return (System.Windows.Style)System.Windows.Application.Current.FindResource("CenterKeyStyle");
            else return (System.Windows.Style)System.Windows.Application.Current.FindResource("WhiteKeyStyle");
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

        static public System.Windows.Media.Brush getTickLineBrush()
        {
            return (SolidColorBrush)System.Windows.Application.Current.FindResource("ScrollBarBrushNormal");
        }

        static public System.Windows.Media.Brush getBarNumberBrush()
        {
            return (SolidColorBrush)System.Windows.Application.Current.FindResource("ScrollBarBrushActive");
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
