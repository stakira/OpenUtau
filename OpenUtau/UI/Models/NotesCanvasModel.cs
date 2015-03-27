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
        public enum ZoomLevel { Bar, Beat, QuaterNote, EighthNote, SixteenthNote };

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

        public int bar = 4; // bar = number of beats
        public int beat = 4; // beat = number of quarter-notes
        public int bpm = 128000; // Beat per minute * 1000
        public int ppq = 960; // Pulse per quarter note

        public bool snapOffset = true;
        public bool snapLength = true;

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
        List<Line> verticalLines;
        List<Line> barLines;
        List<TextBlock> barNumbers;

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
            updateNotes();
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
                    Text = this.getKeyString(i),
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
            int max = snapNoteKey(0);
            int min = snapNoteKey(notesCanvas.ActualHeight);
            for (int i = 0; i < numNotesHeight; i++)
            {
                if (i < min || i > max)
                {
                    keyNames[i].Visibility = System.Windows.Visibility.Hidden;
                    keyShapes[i].Visibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    double notePosInView = keyToCanvas(i);
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

            verticalLines = new List<Line>();
            barNumbers = new List<TextBlock>();
            barLines = new List<Line>();
        }

        // Update notesCanvas background
        public void updateNotesCanvasBackground()
        {
            int max = snapNoteKey(0);
            int min = snapNoteKey(notesCanvas.ActualHeight);
            for (int i = 0; i < numNotesHeight; i++)
            {
                if (i < min || i > max)
                {
                    keyTracks[i].Visibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    double notePosInView = keyToCanvas(i);
                    keyTracks[i].Width = notesCanvas.ActualWidth;
                    keyTracks[i].Height = noteHeight;
                    Canvas.SetTop(keyTracks[i], notePosInView);
                    keyTracks[i].Visibility = System.Windows.Visibility.Visible;
                }
            }

            // Update vertical lines
            double displayLineWidth = noteWidth * getHZoomRatio();
            int firstLine = (int)(getViewOffsetX() / displayLineWidth ) + 1;
            double firstLineX = firstLine * displayLineWidth - getViewOffsetX();

            for (int i = 0; i < notesCanvas.ActualWidth / displayLineWidth; i++)
            {
                double lineX = firstLineX + displayLineWidth * i;
                if (verticalLines.Count <= i)
                {
                    // Add line
                    verticalLines.Add(new Line()
                    {
                        StrokeThickness = 1,
                        X1 = 0,
                        Y1 = 0,
                        X2 = 0,
                    });
                    notesCanvas.Children.Add(verticalLines.Last());
                    Canvas.SetTop(verticalLines.Last(), 0);
                }
                verticalLines[i].Stroke = (firstLine + i) % (4 / getHZoomRatio()) == 0 ?
                    Brushes.Black : ThemeManager.getTickLineBrush();
                verticalLines[i].Y2 = notesCanvas.ActualHeight;
                Canvas.SetLeft(verticalLines[i], Math.Round(lineX) + 0.5);
                verticalLines[i].Visibility = System.Windows.Visibility.Visible;
            }

            for (int i = (int)(notesCanvas.ActualWidth / displayLineWidth) + 1; i < verticalLines.Count; i++)
            {
                verticalLines[i].Visibility = System.Windows.Visibility.Hidden;
            }

            // Update bar number and line
            double displayBarWidth = noteWidth * beat * bar;
            int firstBar = (int)(getViewOffsetX() / displayBarWidth);
            double firstBarX = firstBar * displayBarWidth - getViewOffsetX();

            for (int i = 0; i < notesCanvas.ActualWidth / displayBarWidth + 1; i++)
            {
                double barX = firstBarX + displayBarWidth * i;
                if (barNumbers.Count <= i)
                {
                    // Add number
                    barNumbers.Add(new TextBlock
                    {
                        FontSize = 12,
                        Foreground = ThemeManager.getBarNumberBrush(),
                        IsHitTestVisible = false,
                        SnapsToDevicePixels = true
                    });
                    timelineCanvas.Children.Add(barNumbers.Last());
                    Canvas.SetTop(barNumbers.Last(), 3);
                    // Add line
                    barLines.Add(new Line
                    {
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        X1 = 0,
                        Y1 = 0,
                        X2 = 0,
                        Y2 = timelineCanvas.ActualHeight
                    });
                    timelineCanvas.Children.Add(barLines.Last());
                }
                Canvas.SetLeft(barNumbers[i], Math.Round(barX) + barNumberOffsetX);
                barNumbers[i].Text = (firstBar + i).ToString();
                barNumbers[i].Visibility = System.Windows.Visibility.Visible;
                Canvas.SetLeft(barLines[i], Math.Round(barX) + 0.5);
                barLines[i].Visibility = System.Windows.Visibility.Visible;
            }

            for (int i = (int)(notesCanvas.ActualWidth / displayBarWidth) + 2; i < barNumbers.Count; i++)
            {
                barNumbers[i].Visibility = System.Windows.Visibility.Hidden;
                barLines[i].Visibility = System.Windows.Visibility.Hidden;
            }
        }
        
        // TODO : Improve performance
        public void updateScroll()
        {
            if (notesCanvas.ActualHeight > numNotesHeight * noteHeight)
                noteHeight = notesCanvas.ActualHeight / numNotesHeight;

            if (notesCanvas.ActualWidth > numNotesWidthScroll * noteWidth)
                noteWidth = notesCanvas.ActualWidth / numNotesWidthScroll;

            notesVScroll.ViewportSize = getViewSizeY();
            notesVScroll.SmallChange = notesVScroll.ViewportSize / 10;
            notesVScroll.LargeChange = notesVScroll.ViewportSize;

            hScroll.ViewportSize = getViewSizeX();
            hScroll.SmallChange = hScroll.ViewportSize / 10;
            hScroll.LargeChange = hScroll.ViewportSize;
        }

        public void hZoom(double delta, double centerX)
        {
            double offsetScrollCenter = canvasToOffset(centerX);
            noteWidth = Math.Min(noteMaxWidth, Math.Max(noteMinWidth, noteWidth * (1.0 + delta)));
            setViewOffsetX(offsetScrollCenter * noteWidth - centerX);
        }

        public void vZoom(double delta, double centerY)
        {
            double keyScrollCenter = (centerY + getViewOffsetY()) / noteHeight;
            noteHeight = Math.Min(noteMaxHeight, Math.Max(noteMinHeight, noteHeight * (1.0 + delta)));
            setViewOffsetY(keyScrollCenter * noteHeight - centerY);
        }

        public double snapNoteOffset(double x)
        {
            return getOffsetSnapUnit() * (int)(canvasToOffset(x) / getOffsetSnapUnit());
        }

        public double snapNoteLength(double x)
        {
            return getLengthSnapUnit() * (int)((x + getViewOffsetX()) / noteWidth / getLengthSnapUnit());
        }

        public int snapNoteKey(double y)
        {
            return numNotesHeight - 1 - (int)((y + getViewOffsetY()) / noteHeight);
        }

        public double getOffsetSnapUnit() // Unit : quarter note
        {
            if (snapOffset) return getHZoomRatio();
            else return 1.0 / ppq;
        }

        public double getLengthSnapUnit() // Unit : quarter note
        {
            if (snapLength) return getHZoomRatio();
            else return 1.0 / ppq;
        }

        public double getHZoomRatio()
        {
            switch (getHZoomLevel())
            {
                case ZoomLevel.Bar:
                    return beat * bar;
                case ZoomLevel.Beat:
                    return beat;
                case ZoomLevel.QuaterNote:
                    return 1;
                case ZoomLevel.EighthNote:
                    return 0.5;
                default:
                    return 0.25;
            }
        }

        public ZoomLevel getHZoomLevel()
        {
            if (noteWidth < noteMinWidthDisplay / 4) return ZoomLevel.Bar;
            else if (noteWidth < noteMinWidthDisplay) return ZoomLevel.Beat;
            else if (noteWidth < noteMinWidthDisplay * 4) return ZoomLevel.QuaterNote;
            else if (noteWidth < noteMinWidthDisplay * 8) return ZoomLevel.EighthNote;
            else return ZoomLevel.SixteenthNote;
        }

        public Note shapeToNote(System.Windows.Shapes.Rectangle shape)
        {
            // TODO : Improve performance
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
                System.Diagnostics.Debug.WriteLine("Note : " + _note.offset.ToString() + " " + _note.keyNo.ToString());
            }
        }
            
        public double getViewSizeY()
        {
            if (numNotesHeight * noteHeight - notesCanvas.ActualHeight == 0) return 10000;
            return notesCanvas.ActualHeight / (numNotesHeight * noteHeight - notesCanvas.ActualHeight);
        }

        public double getViewSizeX()
        {
            if (numNotesWidthScroll * noteWidth - notesCanvas.ActualWidth == 0) return 10000;
            return notesCanvas.ActualWidth / (numNotesWidthScroll * noteWidth - notesCanvas.ActualWidth);
        }

        public double getViewOffsetY()
        {
            return notesVScroll.Value * (numNotesHeight * noteHeight - notesCanvas.ActualHeight);
        }

        public void setViewOffsetY(double y)
        {
            notesVScroll.Value = y / (numNotesHeight * noteHeight - notesCanvas.ActualHeight);
        }

        public double getViewOffsetX()
        {
            return hScroll.Value * (numNotesWidthScroll * noteWidth - notesCanvas.ActualWidth);
        }

        public void setViewOffsetX(double x)
        {
            hScroll.Value = x / (numNotesWidthScroll * noteWidth - notesCanvas.ActualWidth);
        }

        public double keyToCanvas(int noteNo)
        {
            return (numNotesHeight - noteNo - 1) * noteHeight - getViewOffsetY();
        }

        public double canvasToOffset(double x)
        {
            return (x + getViewOffsetX()) / noteWidth;
        }

        public double offsetToCanvas(double offset)
        {
            return offset * noteWidth - getViewOffsetX();
        }

        public String getKeyString(int keyNo)
        {
            int octave = keyNo / 12 - 2;
            return noteStrings[keyNo % 12] + octave;
        }

        // Note methods
        public void updateNote(Note note)
        {
            note.shape.Height = noteHeight - 2;
            note.shape.Width = Math.Max(2, Math.Round(note.length * noteWidth) - 3);
            Canvas.SetLeft(note.shape, Math.Round(offsetToCanvas(note.offset)) + 2);
            Canvas.SetTop(note.shape, Math.Round(keyToCanvas(note.keyNo)) + 1);
        }

        public void updateNotes()
        {
            foreach (Note note in notes) updateNote(note);
        }

        public void AddNote(Note note)
        {
            if (notes.Contains(note))
                throw new Exception("Note already exist, cannot be added again");
            notes.Add(note);
            notesCanvas.Children.Add(note.shape);
            updateNote(note);
        }

        public void RemoveNote(Note note)
        {
            if (!notes.Contains(note))
                throw new Exception("Note does not exist, cannot be removed");
            notesCanvas.Children.Remove(note.shape);
            notes.Remove(note);
        }

        public void SortNotes()
        {
            notes.Sort(delegate(Note lhs, Note rhs)
            {
                if (lhs.offset < rhs.offset) return -1;
                else if (lhs.offset > rhs.offset) return 1;
                else if (lhs.keyNo < rhs.keyNo) return -1;
                else if (lhs.keyNo > rhs.keyNo) return 1;
                else return 0;
            });
        }
    }
}
