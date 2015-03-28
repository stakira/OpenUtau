using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Shapes;

using OpenUtau.Core;

namespace OpenUtau.UI.Models
{
    public class NotesCanvasModel
    {
        public enum ZoomLevel { Bar, Beat, QuaterNote, HalfNote, EighthNote, SixteenthNote };

        public const double noteMaxWidth = 128;
        public const double noteMinWidth = 4;
        public const double noteMinWidthDisplay = 8;
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

        public double playPosMarkerOffset = 0;

        Path playPosMarker;
        Rectangle playPosMarkerHighlight;

        public bool snapOffset = true;
        public bool snapLength = true;

        public string[] noteStrings = new String[12] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        public TrackPart trackPart;
        public TrackPart shadowTrackPart;

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

            trackPart = new TrackPart(this);
            shadowTrackPart = null;
            // TODO : Load existing trackPart and shadowTrackPart

            ThemeManager.LoadTheme();
        }

        public void initGraphics()
        {
            initKeyCanvas();
            initNotesCanvasBackground();
            initPlayPosMarker();
        }

        public void updateGraphics()
        {
            updateKeyCanvas();
            updateNotesCanvasBackground();
            updateNotes();
            updatePlayPosMarker();
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
                Canvas.SetZIndex(keyShapes[i], -50);

                keyNames.Add(new TextBlock()
                {
                    Text = this.getKeyString(i),
                    Foreground = ThemeManager.getNoteBrush(i),
                    Width = 42,
                    TextAlignment = System.Windows.TextAlignment.Right,
                    IsHitTestVisible = false
                });
                keysCanvas.Children.Add(keyNames[i]);
                Canvas.SetZIndex(keyNames[i], 0);
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
                    if (noteHeight < 12) keyNames[i].Visibility = System.Windows.Visibility.Hidden;
                    else keyNames[i].Visibility = System.Windows.Visibility.Visible;

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
                Canvas.SetZIndex(keyTracks[i], -50);
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

            for (int i = 0; i < Math.Ceiling(notesCanvas.ActualWidth / displayLineWidth); i++)
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
                        X2 = 0
                    });
                    notesCanvas.Children.Add(verticalLines.Last());
                    Canvas.SetTop(verticalLines.Last(), 0);
                    Canvas.SetZIndex(verticalLines.Last(), -10);
                }
                verticalLines[i].Stroke = (firstLine + i) % (4 / getHZoomRatio()) == 0 ?
                    ThemeManager.TickLineBrushDark : ThemeManager.TickLineBrushLight;
                verticalLines[i].Y2 = notesCanvas.ActualHeight;
                Canvas.SetLeft(verticalLines[i], Math.Round(lineX) + 0.5);
                verticalLines[i].Visibility = System.Windows.Visibility.Visible;
            }

            for (int i = (int)Math.Ceiling(notesCanvas.ActualWidth / displayLineWidth); i < verticalLines.Count; i++)
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
                        Foreground = ThemeManager.BarNumberBrush,
                        IsHitTestVisible = false,
                        SnapsToDevicePixels = true
                    });
                    timelineCanvas.Children.Add(barNumbers.Last());
                    Canvas.SetTop(barNumbers.Last(), 3);
                    Canvas.SetZIndex(barNumbers.Last(), -10);
                    // Add line
                    barLines.Add(new Line
                    {
                        Stroke = ThemeManager.TickLineBrushDark,
                        StrokeThickness = 1,
                        X1 = 0,
                        Y1 = 0,
                        X2 = 0,
                        Y2 = timelineCanvas.ActualHeight
                    });
                    timelineCanvas.Children.Add(barLines.Last());
                    Canvas.SetZIndex(barLines.Last(), -10);
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

        public void initPlayPosMarker()
        {
            playPosMarker = new Path()
            {
                Fill = ThemeManager.getTickLineBrush(),
                Data = Geometry.Parse("M 0 0 L 13 0 L 13 3 L 6.5 9 L 0 3 Z")
            };
            timelineCanvas.Children.Add(playPosMarker);
            Canvas.SetZIndex(playPosMarker, 1000);

            playPosMarkerHighlight = new Rectangle()
            {
                Fill = ThemeManager.getTickLineBrush(),
                Opacity = 0.25,
                Width = 32
            };
            notesCanvas.Children.Add(playPosMarkerHighlight);
            Canvas.SetZIndex(playPosMarkerHighlight, 0);
        }

        public void updatePlayPosMarker()
        {
            playPosMarkerOffset = Math.Max(0, playPosMarkerOffset);
            Canvas.SetLeft(playPosMarker, Math.Round(offsetToCanvas(playPosMarkerOffset)) - 6);
            playPosMarkerHighlight.Height = notesCanvas.ActualHeight;
            double left = Math.Round(snapOffsetToLine(playPosMarkerOffset));
            playPosMarkerHighlight.Width = Math.Round(noteWidth * getHZoomRatio() + left) - left;
            Canvas.SetLeft(playPosMarkerHighlight, Math.Round(left + 0.5));
        }

        public double snapOffsetToLine(double offset)
        {
            return Math.Floor(offset / getHZoomRatio()) * getHZoomRatio() * noteWidth - getViewOffsetX();
        }

        public void hZoom(double delta, double centerX)
        {
            double offsetScrollCenter = (hScroll.Value == 0 || hScroll.ViewportSize == 10000) && centerX < 64 ? 0 : canvasToOffset(centerX);
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
                case ZoomLevel.HalfNote:
                    return 2;
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
            if (noteWidth < noteMinWidthDisplay) return ZoomLevel.Beat;
            else if (noteWidth < noteMinWidthDisplay * 2) return ZoomLevel.HalfNote;
            else if (noteWidth < noteMinWidthDisplay * 6) return ZoomLevel.QuaterNote;
            else if (noteWidth < noteMinWidthDisplay * 8) return ZoomLevel.EighthNote;
            else return ZoomLevel.SixteenthNote;
        }

        public Note getNoteFromControl(OpenUtau.UI.Controls.NoteControl control)
        {
            return control.note;
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

        public void updateNotes()
        {
            trackPart.UpdateGraphics();
        }
    }
}
