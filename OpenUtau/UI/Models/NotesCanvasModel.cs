using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace OpenUtau.UI.Models
{
    class NotesCanvasModel
    {
        public enum ZoomLevel { Bar, Beat, QuaterNote, SixteenthNote };

        public const double noteMaxWidth = 256;
        public const double noteMinWidth = 4;
        public const double noteMinWidthDisplay = 16;
        public const double noteMaxHeight = 128;
        public const double noteMinHeight = 8;

        public const double resizeMargin = 8;
        public const double barNumberOffsetX = 3;
        
        public const int numNotesHeight = 12 * 11;

        public double noteWidth { get; set; } // Actually a quater note
        public double noteHeight { get; set; }
        public double verticalPosition { get; set; }
        public double horizontalPosition { get; set; }

        public const int numNotesWidthMin = 128; // 32 beats minimal
        public int numNotesWidthScroll;
        public int numNotesWidth;

        public int bar = 4; // beats per bar
        public int beat = 4; // quarter-notes per beat

        public string[] noteStrings = new String[12] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        public List<Note> notes;

        // Handle to UI elements
        public System.Windows.Controls.Primitives.ScrollBar hScroll;
        public System.Windows.Controls.Primitives.ScrollBar notesVScroll;
        public System.Windows.Controls.Primitives.ScrollBar expVScroll;
        public System.Windows.Controls.Canvas notesCanvas;
        public System.Windows.Controls.Canvas expCanvas;
        public System.Windows.Controls.Canvas keysCanvas;
        public System.Windows.Controls.Canvas timelineCanvas;

        // keysCanvas elements
        List<TextBlock> keyNames;
        List<Rectangle> keyShapes;

        //notesCanvas elements
        List<Rectangle> keyTracks;
        List<Line> _qnoteLines;
        List<Line> _beatLines;
        List<TextBlock> _barNumbers;

        public NotesCanvasModel()
        {
            noteHeight = 22;
            verticalPosition = 0.5;

            noteWidth = 32;
            numNotesWidth = numNotesWidthMin;
            numNotesWidthScroll = numNotesWidthMin;
            horizontalPosition = 0;

            notes = new List<Note>();
        }

        public void initGraphics()
        {
            initKeyCanvas();
            initNotesCanvasBackground();
        }

        public void updateGraphics()
        {
            updateKeyCanvas();
            updateNotesCanvasBackground();
        }

        // Create key shapes and text
        public void initKeyCanvas()
        {
            keyNames  = new List<TextBlock>();
            keyShapes = new List<Rectangle>();
            for (int i = 0; i < numNotesHeight; i++)
            {
                keyShapes.Add(new Rectangle()
                {
                    Width = 48,
                    Height = this.noteHeight
                });
                keyShapes[i].Style = ThemeManager.getKeyStyle(i);
                keysCanvas.Children.Add(keyShapes[i]);

                keyNames.Add(new TextBlock()
                {
                    Text = this.getNoteString(i),
                    Foreground = ThemeManager.getNoteBrush(i),
                    Width = 42,
                    TextAlignment = System.Windows.TextAlignment.Right,
                    IsHitTestVisible = false
                });
                keysCanvas.Children.Add(keyNames[i]);
            }
        }

        // Update keys size and position
        public void updateKeyCanvas()
        {
            int max = canvasToKey(0, 0, 0);
            int min = canvasToKey(notesCanvas.ActualHeight, 0, 0);
            for (int i = 0; i < numNotesHeight; i++)
            {
                if (i < min || i > max)
                {
                    keyNames[i].Visibility = System.Windows.Visibility.Hidden;
                    keyShapes[i].Visibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    double notePosInView = keyToCanvas(i, notesVScroll.Value, notesCanvas.ActualHeight);
                    Canvas.SetLeft(keyNames[i], 0);
                    Canvas.SetTop(keyNames[i], notePosInView + (noteHeight - 16) / 2);
                    if (noteHeight > 12) keyNames[i].Visibility = System.Windows.Visibility.Visible;
                    else keyNames[i].Visibility = System.Windows.Visibility.Hidden;

                    keyShapes[i].Height = noteHeight;
                    Canvas.SetLeft(keyShapes[i], 0);
                    Canvas.SetTop(keyShapes[i], notePosInView);
                    keyShapes[i].Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        // Create notesCanvas background
        public void initNotesCanvasBackground()
        {
            keyTracks = new List<Rectangle>();
            for (int i = 0; i < numNotesHeight; i++)
            {
                keyTracks.Add(new Rectangle()
                {
                    Fill = ThemeManager.getNoteTrackBrush(i),
                    Width = notesCanvas.ActualWidth,
                    Height = noteHeight,
                    IsHitTestVisible = false
                });
                notesCanvas.Children.Add(keyTracks[i]);
            }

            _qnoteLines = new List<Line>();
            _beatLines = new List<Line>();
            _barNumbers = new List<TextBlock>();
        }

        private void expandLines(int numNotesLines, int numBarLines)
        {
            while (_qnoteLines.Count < numNotesLines + 1)
            {
                _qnoteLines.Add(new Line() { Stroke = ThemeManager.getTickLineBrush(), StrokeThickness = .75, X1 = 0, Y1 = 0, X2 = 0, Y2 = 400, SnapsToDevicePixels = true });
                notesCanvas.Children.Add(_qnoteLines.Last());
                Canvas.SetTop(_qnoteLines.Last(), 0);
            }
            while (_beatLines.Count < numBarLines + 1)
            {
                _beatLines.Add(new Line() { Stroke = ThemeManager.getTickLineBrush(), StrokeThickness = .75, X1 = 0, Y1 = 0, X2 = 0, Y2 = 400, SnapsToDevicePixels = true });
                timelineCanvas.Children.Add(_beatLines.Last());
                Canvas.SetTop(_beatLines.Last(), 0);
            }
            while (_barNumbers.Count < numBarLines + 3)
            {
                _barNumbers.Add(new TextBlock { FontSize = 12, Foreground = ThemeManager.getBarNumberBrush(), IsHitTestVisible = false, SnapsToDevicePixels = true });
                timelineCanvas.Children.Add(_barNumbers.Last());
                Canvas.SetTop(_barNumbers.Last(), 3);
            }
        }

        // Update notesCanvas background
        public void updateNotesCanvasBackground()
        {
            int max = canvasToKey(0);
            int min = canvasToKey(notesCanvas.ActualHeight);
            for (int i = 0; i < numNotesHeight; i++)
            {
                if (i < min || i > max)
                {
                    keyTracks[i].Visibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    double notePosInView = keyToCanvas(i, notesVScroll.Value, notesCanvas.ActualHeight);
                    keyTracks[i].Width = notesCanvas.ActualWidth;
                    keyTracks[i].Height = noteHeight;
                    Canvas.SetTop(keyTracks[i], notePosInView);
                    keyTracks[i].Visibility = System.Windows.Visibility.Visible;
                }
            }

            // Update vertical lines
            double displayLineWidth = noteWidth * getZoomRatio();
            int firstLine = (int)(getViewOffsetX() / displayLineWidth ) + 1;
            double firstLineX = firstLine * displayLineWidth - getViewOffsetX();

            for (int i = 0; i < notesCanvas.ActualWidth / displayLineWidth; i++)
            {
                double lineX = firstLineX + displayLineWidth * i;
                if (_qnoteLines.Count <= i)
                {
                    // Add line
                    _qnoteLines.Add(new Line()
                    {
                        Stroke = ThemeManager.getTickLineBrush(),
                        StrokeThickness = .75,
                        X1 = 0,
                        Y1 = 0,
                        X2 = 0,
                        Y2 = 400,
                        SnapsToDevicePixels = true
                    });
                    notesCanvas.Children.Add(_qnoteLines.Last());
                    Canvas.SetTop(_qnoteLines.Last(), 0);
                }
                _qnoteLines[i].Y2 = notesCanvas.ActualHeight;
                Canvas.SetLeft(_qnoteLines[i], (int)lineX);
                _qnoteLines[i].Visibility = System.Windows.Visibility.Visible;
            }

            for (int i = (int)(notesCanvas.ActualWidth / displayLineWidth) + 1; i < _qnoteLines.Count; i++)
            {
                _qnoteLines[i].Visibility = System.Windows.Visibility.Hidden;
            }

            // Update bar number
            double displayBarWidth = noteWidth * beat * bar;
            int firstBar = (int)(getViewOffsetX() / displayBarWidth);
            double firstBarX = firstBar * displayBarWidth - getViewOffsetX();

            for (int i = 0; i < notesCanvas.ActualWidth / displayBarWidth + 1; i++)
            {
                double barX = firstBarX + displayBarWidth * i;
                if (_barNumbers.Count <= i)
                {
                    // Add number
                    _barNumbers.Add(new TextBlock
                    {
                        FontSize = 12,
                        Foreground = ThemeManager.getBarNumberBrush(),
                        IsHitTestVisible = false,
                        SnapsToDevicePixels = true
                    });
                    timelineCanvas.Children.Add(_barNumbers.Last());
                    Canvas.SetTop(_barNumbers.Last(), 3);
                }
                Canvas.SetLeft(_barNumbers[i], (int)barX + barNumberOffsetX);
                _barNumbers[i].Text = (firstBar + i).ToString();
                _barNumbers[i].Visibility = System.Windows.Visibility.Visible;
            }

            for (int i = (int)(notesCanvas.ActualWidth / displayBarWidth) + 2; i < _barNumbers.Count; i++)
            {
                _barNumbers[i].Visibility = System.Windows.Visibility.Hidden;
            }
        }

        public double getZoomRatio()
        {
            switch (getZoomLevel())
            {
                case ZoomLevel.Bar:
                    return beat * bar;
                case ZoomLevel.Beat:
                    return beat;
                case ZoomLevel.QuaterNote:
                    return 1;
                default:
                    return 0.25; // FIXME
            }
        }

        public ZoomLevel getZoomLevel()
        {
            if (noteWidth < noteMinWidthDisplay / 4) return ZoomLevel.Bar;
            else if (noteWidth < noteMinWidthDisplay) return ZoomLevel.Beat;
            else if (noteWidth < noteMinWidthDisplay * 4) return ZoomLevel.QuaterNote;
            else return ZoomLevel.SixteenthNote;
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

        public double getViewOffsetY(double scrollValue=0, double viewHeight=0)
        {
            return notesVScroll.Value * (numNotesHeight * noteHeight - notesCanvas.ActualHeight);
        }

        public double getViewOffsetX(double scrollValue=0, double viewWidth=0)
        {
            return hScroll.Value * (numNotesWidthScroll * noteWidth - notesCanvas.ActualWidth);
        }

        public double keyToCanvas(int noteNo, double scrollValue, double viewHeight)
        {
            return (numNotesHeight - noteNo - 1) * noteHeight - getViewOffsetY(scrollValue, viewHeight);
        }

        public int canvasToKey(double y, double scrollValue=0, double viewHeight=0)
        {
            return numNotesHeight - 1 - (int)((y + getViewOffsetY(notesVScroll.Value, notesCanvas.ActualHeight)) / noteHeight);
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
    }
}
