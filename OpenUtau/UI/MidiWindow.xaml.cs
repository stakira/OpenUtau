using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;

using WinInterop = System.Windows.Interop;
using System.Runtime.InteropServices;

using OpenUtau.UI.Models;
using OpenUtau.UI.Controls;
using OpenUtau.Core;
using OpenUtau.Core.USTx;

namespace OpenUtau.UI
{
    /// <summary>
    /// Interaction logic for BorderlessWindow.xaml
    /// </summary>
    public partial class MidiWindow : BorderlessWindow
    {
        // Canvas states
        //NotesCanvasModel ncModel;
        KeyTrackBackground keyTrackBackground;
        TickBackground tickBackground;
        TimelineBackground timelineBackground;
        KeyboardBackground keyboardBackground;

        double lastNoteLength = 1;

        public MidiWindow()
        {
            InitializeComponent();

            //ncModel = new NotesCanvasModel();
            //ncModel.hScroll = this.horizontalScroll;
            //ncModel.notesVScroll = this.notesVerticalScroll;
            //ncModel.expVScroll = this.expVerticalScroll;
            //ncModel.notesCanvas = this.notesCanvas;
            //ncModel.expCanvas = this.expCanvas;
            //ncModel.keysCanvas = this.keysCanvas;
            //ncModel.timelineCanvas = this.timelineCanvas;

            //ncModel.initGraphics();
            //ncModel.updateGraphics();

            //CompositionTarget.Rendering += Window_PerFrameCallback;

            keyTrackBackground = new KeyTrackBackground();
            this.notesBackgroundGrid.Children.Add(keyTrackBackground);

            tickBackground = new TickBackground();
            this.notesBackgroundGrid.Children.Add(tickBackground);
            tickBackground.SnapsToDevicePixels = true;
            tickBackground.MinTickWidth = UIConstants.MidiTickMinWidth;

            timelineBackground = new TimelineBackground();
            this.timelineBackgroundGrid.Children.Add(timelineBackground);
            timelineBackground.SnapsToDevicePixels = true;

            keyboardBackground = new KeyboardBackground();
            this.keyboardBackgroundGrid.Children.Add(keyboardBackground);
            keyboardBackground.SnapsToDevicePixels = true;

            notesVerticalScroll.Minimum = 0;
            notesVerticalScroll.Maximum = UIConstants.MaxNoteNum * UIConstants.NoteDefaultHeight;
            notesVerticalScroll.Value = UIConstants.NoteDefaultHeight * UIConstants.MaxNoteNum / 2;

            horizontalScroll.Minimum = 0;
            horizontalScroll.Maximum = UIConstants.MaxNoteCount * UIConstants.MidiWNoteDefaultWidth;
            horizontalScroll.Value = 0;
            //navigateDrag.NavDrag += navigateDrag_NavDrag;

            this.CloseButtonClicked += (o, e) => { Hide(); };
        }

        public void LoadPart(UPart part)
        {
            //while (ncModel.trackPart.Notes.Count > 0)
            //{
            //    ncModel.trackPart.RemoveNote(ncModel.trackPart.Notes[ncModel.trackPart.Notes.Count - 1]);
            //}

            //if (part != null)
            //{
            //    ncModel.trackPart = part;
            //    ncModel.trackPart.ncModel = ncModel;
            //    foreach (UNote note in part.Notes)
            //    {
            //        ncModel.notesCanvas.Children.Add(note.noteControl);
            //    }
            //    ncModel.updateScroll();
            //    ncModel.updateGraphics();
            //}
        }

        # region Note Canvas

        UNote noteInDrag = null;
        double noteOffsetOfDrag;
        UNote leftMostNoteOfDrag, rightMostNoteOfDrag, maxNoteOfDrag, minNoteOfDrag;

        UNote noteInResize = null;
        UNote shortedNoteInResize;

        Nullable<Point> selectionStart = null; // Unit in offset/keyNo
        Rectangle selectionBox;

        private NoteControl getNoteVisualHit(HitTestResult result)
        {
            if (result == null) return null;
            var element = result.VisualHit;
            while (element != null && !(element is NoteControl))
                element = VisualTreeHelper.GetParent(element);
            return (NoteControl)element;
        }

        private void notesCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //FocusManager.SetFocusedElement(this, null);
            //Point mousePos = e.GetPosition((Canvas)sender);
            //NoteControl hitNoteControl = getNoteVisualHit(VisualTreeHelper.HitTest(notesCanvas, mousePos));

            //if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            //{
            //    selectionStart = new Point(ncModel.canvasToOffset(mousePos.X), ncModel.snapNoteKey(mousePos.Y));
            //    if (Keyboard.IsKeyUp(Key.LeftShift) && Keyboard.IsKeyUp(Key.RightShift))
            //        ncModel.trackPart.DeselectAll();
            //    if (selectionBox == null)
            //    {
            //        selectionBox = new Rectangle()
            //        {
            //            Stroke = Brushes.Black,
            //            StrokeThickness = 2,
            //            Fill = ThemeManager.getBarNumberBrush(),
            //            Width = 0,
            //            Height = 0,
            //            Opacity = 0.5,
            //            RadiusX = 8,
            //            RadiusY = 8
            //        };
            //        notesCanvas.Children.Add(selectionBox);
            //        Canvas.SetZIndex(selectionBox, 1000);
            //    } 
            //    else
            //    {
            //        selectionBox.Width = 0;
            //        selectionBox.Height = 0;
            //        Canvas.SetZIndex(selectionBox, 1000);
            //    }
            //    Mouse.OverrideCursor = Cursors.Cross;
            //}
            //else
            //{
            //    if (hitNoteControl != null)
            //    {
            //        UNote hitNote = ncModel.getNoteFromControl(hitNoteControl);
            //        if (e.GetPosition(hitNoteControl).X < hitNoteControl.ActualWidth - NotesCanvasModel.resizeMargin)
            //        {
            //            // Move note
            //            if (!ncModel.trackPart.selectedNotes.Contains(hitNote))
            //                ncModel.trackPart.DeselectAll();
            //            noteInDrag = hitNote;
            //            noteOffsetOfDrag = ncModel.snapNoteOffset(e.GetPosition((Canvas)sender).X) - noteInDrag.offset;
            //            lastNoteLength = noteInDrag.length;
            //            leftMostNoteOfDrag = rightMostNoteOfDrag = maxNoteOfDrag = minNoteOfDrag = noteInDrag;
            //            if (ncModel.trackPart.selectedNotes.Count != 0)
            //                foreach (UNote note in ncModel.trackPart.selectedNotes)
            //                {
            //                    if (note.offset < leftMostNoteOfDrag.offset)
            //                        leftMostNoteOfDrag = note;
            //                    if (note.offset > rightMostNoteOfDrag.offset)
            //                        rightMostNoteOfDrag = note;
            //                    if (note.keyNo > maxNoteOfDrag.keyNo)
            //                        maxNoteOfDrag = note;
            //                    if (note.keyNo < minNoteOfDrag.keyNo)
            //                        minNoteOfDrag = note;
            //                }
            //        }
            //        else if (!hitNoteControl.IsLyricBoxActive())
            //        {
            //            // Resize note
            //            if (!ncModel.trackPart.selectedNotes.Contains(hitNote))
            //                ncModel.trackPart.DeselectAll();
            //            noteInResize = hitNote;
            //            Mouse.OverrideCursor = Cursors.SizeWE;
            //            shortedNoteInResize = noteInResize;
            //            if (ncModel.trackPart.selectedNotes.Count != 0)
            //                foreach (UNote note in ncModel.trackPart.selectedNotes)
            //                    if (note.length < shortedNoteInResize.length)
            //                        shortedNoteInResize = note;
            //        }
            //    }
            //    else // Add note
            //    {
            //        UNote newNote = new UNote(ncModel.trackPart)
            //        {
            //            keyNo = ncModel.snapNoteKey(e.GetPosition((Canvas)sender).Y),
            //            offset = ncModel.snapNoteOffset(e.GetPosition((Canvas)sender).X),
            //            length = lastNoteLength
            //        };
            //        ncModel.trackPart.AddNote(newNote);
            //        // Enable drag
            //        noteInDrag = newNote;
            //        noteOffsetOfDrag = ncModel.snapNoteOffset(e.GetPosition((Canvas)sender).X) - noteInDrag.offset;
            //        leftMostNoteOfDrag = rightMostNoteOfDrag = maxNoteOfDrag = minNoteOfDrag = noteInDrag;
            //        ncModel.trackPart.DeselectAll();
            //    }
            //}
            //((UIElement)sender).CaptureMouse();
        }

        private void notesCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //noteInDrag = null;
            //noteInResize = null;
            //selectionStart = null;
            //if (selectionBox != null)
            //{
            //    Canvas.SetZIndex(selectionBox, -100);
            //}
            //ncModel.trackPart.FinishSelectTemp();
            //((Canvas)sender).ReleaseMouseCapture();
            //Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void notesCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition((Canvas)sender);
            notesCanvas_MouseMove_Helper(mousePos);
        }

        private void notesCanvas_MouseMove_Helper(Point mousePos)
        {

            //if (selectionStart != null) // Selection
            //{
            //    double top = ncModel.keyToCanvas(Math.Max(ncModel.snapNoteKey(mousePos.Y), (int)selectionStart.Value.Y));
            //    double bottom = ncModel.keyToCanvas(Math.Min(ncModel.snapNoteKey(mousePos.Y), (int)selectionStart.Value.Y) - 1);
            //    selectionBox.Width = Math.Abs(mousePos.X - ncModel.offsetToCanvas(selectionStart.Value.X));
            //    selectionBox.Height = bottom - top;
            //    Canvas.SetLeft(selectionBox, Math.Min(mousePos.X, ncModel.offsetToCanvas(selectionStart.Value.X)));
            //    Canvas.SetTop(selectionBox, top);
            //    ncModel.trackPart.SelectTempInBox(
            //        ncModel.canvasToOffset(mousePos.X),
            //        selectionStart.Value.X,
            //        ncModel.snapNoteKey(mousePos.Y),
            //        selectionStart.Value.Y);
            //}
            //else if (noteInDrag != null) // Drag Note
            //{
            //    double movedOffset = ncModel.snapNoteOffset(mousePos.X) - noteOffsetOfDrag - noteInDrag.offset;
            //    if (leftMostNoteOfDrag.offset + movedOffset < 0) movedOffset = -leftMostNoteOfDrag.offset;
            //    int movedKeyNo = ncModel.snapNoteKey(mousePos.Y) - noteInDrag.keyNo;
            //    if (maxNoteOfDrag.keyNo + movedKeyNo > NotesCanvasModel.numNotesHeight - 1)
            //        movedKeyNo = NotesCanvasModel.numNotesHeight - 1 - maxNoteOfDrag.keyNo;
            //    if (minNoteOfDrag.keyNo + movedKeyNo < 0)
            //        movedKeyNo = -minNoteOfDrag.keyNo;
            //    if (ncModel.trackPart.selectedNotes.Count == 0)
            //    {
            //        noteInDrag.keyNo += movedKeyNo;
            //        noteInDrag.offset += movedOffset;
            //        noteInDrag.updateGraphics(ncModel);
            //    }
            //    else
            //    {
            //        foreach (UNote note in ncModel.trackPart.selectedNotes)
            //        {
            //            note.keyNo += movedKeyNo;
            //            note.offset += movedOffset;
            //            note.updateGraphics(ncModel);
            //        }
            //    }

            //    ncModel.trackPart.Notes.Sort();
            //    Mouse.OverrideCursor = Cursors.SizeAll;
            //}
            //else if (noteInResize != null) // Resize Note
            //{
            //    double newLength = ncModel.snapLength ?
            //        ncModel.getLengthSnapUnit() + Math.Max(0, ncModel.snapNoteLength(mousePos.X - ncModel.offsetToCanvas(noteInResize.offset) - ncModel.getViewOffsetX())) :
            //        Math.Max(UNote.minLength, ncModel.snapNoteLength(mousePos.X) - noteInResize.offset);
            //    double deltaLength = newLength - noteInResize.length;
            //    if (shortedNoteInResize.length + deltaLength < ncModel.getOffsetSnapUnit()) deltaLength = ncModel.getOffsetSnapUnit() - shortedNoteInResize.length;
            //    if (ncModel.trackPart.selectedNotes.Count == 0)
            //    {
            //        noteInResize.length += deltaLength;
            //        noteInResize.updateGraphics(ncModel);
            //    }
            //    else
            //    {
            //        foreach (UNote note in ncModel.trackPart.selectedNotes)
            //        {
            //            note.length += deltaLength;
            //            note.updateGraphics(ncModel);
            //        }
            //    }

            //    lastNoteLength = noteInResize.length;
            //}
            //else if (Mouse.RightButton == MouseButtonState.Pressed) // Remove Note
            //{
            //    NoteControl hitNoteControl = getNoteVisualHit(VisualTreeHelper.HitTest(notesCanvas, mousePos));

            //    if (hitNoteControl != null)
            //    {
            //        UNote note = ncModel.getNoteFromControl(hitNoteControl);
            //        ncModel.trackPart.RemoveNote(note);
            //    }
            //}
            //else if (Mouse.LeftButton == MouseButtonState.Released && Mouse.RightButton == MouseButtonState.Released)
            //{
            //    NoteControl hitNoteControl = getNoteVisualHit(VisualTreeHelper.HitTest(notesCanvas, mousePos));

            //    if (hitNoteControl != null) // Change Cursor
            //    {
            //        if (Mouse.GetPosition(hitNoteControl).X < hitNoteControl.ActualWidth - NotesCanvasModel.resizeMargin)
            //        {
            //            Mouse.OverrideCursor = Cursors.Arrow;
            //        }
            //        else
            //        {
            //            if (!hitNoteControl.IsLyricBoxActive())
            //                Mouse.OverrideCursor = Cursors.SizeWE;
            //        }
            //    }
            //    else
            //    {
            //        Mouse.OverrideCursor = Cursors.Arrow;
            //    }
            //}
        }

        private void notesCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //FocusManager.SetFocusedElement(this, null);
            //Point mousePos = e.GetPosition((Canvas)sender);
            //NoteControl hitNoteControl = getNoteVisualHit(VisualTreeHelper.HitTest(notesCanvas, mousePos));

            //if (hitNoteControl != null)
            //{
            //    UNote note = ncModel.getNoteFromControl(hitNoteControl);
            //    notesCanvas.Children.Remove(note.noteControl);
            //    ncModel.trackPart.RemoveNote(note);
            //}
            //else
            //{
            //    ncModel.trackPart.DeselectAll();
            //}
            //((UIElement)sender).CaptureMouse();
            //Mouse.OverrideCursor = Cursors.No;
        }

        private void notesCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Mouse.OverrideCursor = Cursors.Arrow;
            //((UIElement)sender).ReleaseMouseCapture();
        }

        // TODO : resize show same portion of view
        private void notesCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //ncModel.vZoom(0, notesCanvas.ActualHeight / 2);
            //ncModel.hZoom(0, notesCanvas.ActualWidth / 2);
            //ncModel.updateScroll();
            //ncModel.updateGraphics();
        }

        private void notesCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                timelineCanvas_MouseWheel(sender, e);
            }
            else if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                horizontalScroll_MouseWheel(sender, e);
            }
            else if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {

            }
            else
            {
                notesVerticalScroll_MouseWheel(sender, e);
            }
        }

        # endregion

        #region Navigate Drag

        private void navigateDrag_NavDrag(object sender, EventArgs e)
        {
            //this.notesVerticalScroll.Value += ((NavDragEventArgs)e).Y;
            //this.horizontalScroll.Value += ((NavDragEventArgs)e).X;
            //ncModel.updateGraphics();
        }

        #endregion

        #region Vertical Zoom Control

        #endregion

        # region Timeline Canvas
        
        private void timelineCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //const double zoomSpeed = 0.0012;
            //ncModel.hZoom(e.Delta * zoomSpeed, e.GetPosition((UIElement)sender).X);
            //ncModel.updateScroll();
            //ncModel.updateGraphics();
        }

        private void timelineCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //ncModel.playPosMarkerOffset = ncModel.snapNoteOffset(e.GetPosition((UIElement)sender).X);
            //ncModel.updatePlayPosMarker();
            //((Canvas)sender).CaptureMouse();
        }

        private void timelineCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition((UIElement)sender);
            timelineCanvas_MouseMove_Helper(mousePos);
        }

        private void timelineCanvas_MouseMove_Helper(Point mousePos)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed && Mouse.Captured == timelineCanvas)
            {
                //ncModel.playPosMarkerOffset = ncModel.snapNoteOffset(mousePos.X);
                //ncModel.updatePlayPosMarker();
            }
        }

        private void timelineCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ((Canvas)sender).ReleaseMouseCapture();
        }

        # endregion

        # region Notes Vertical Scrollbar

        private void notesVerticalScroll_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            keyTrackBackground.VerticalOffset = notesVerticalScroll.Value;
            keyboardBackground.VerticalOffset = notesVerticalScroll.Value;
        }

        private void notesVerticalScroll_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.notesVerticalScroll.Value = this.notesVerticalScroll.Value - 0.01 * notesVerticalScroll.SmallChange * e.Delta;
        }

        # endregion

        # region Horizontal Scrollbar

        private void horizontalScroll_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            //ncModel.updateGraphics();
        }

        private void horizontalScroll_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.horizontalScroll.Value = this.horizontalScroll.Value - 0.01 * horizontalScroll.SmallChange * e.Delta;
            //ncModel.updateGraphics();
        }

        # endregion

        #region Keys Action

        // TODO : keys mouse over, click, release, click and move

        private void keysCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void keysCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void keysCanvas_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void keysCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //this.notesVerticalScroll.Value = this.notesVerticalScroll.Value - 0.01 * notesVerticalScroll.SmallChange * e.Delta;
            //ncModel.updateGraphics();
        }

        # endregion

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                // Select all notes
                //ncModel.trackPart.SelectAll();
            }
            else if (e.Key == Key.Delete)
            {
                // Delete notes
                //ncModel.trackPart.RemoveSelectedNote();
            }
        }

        private TimeSpan lastFrame = TimeSpan.Zero;

        private void Window_PerFrameCallback(object sender, EventArgs e)
        {
            TimeSpan nextFrame = ((RenderingEventArgs)e).RenderingTime;
            if (lastFrame == nextFrame) return; // Skip redundant call
            double deltaTime = (nextFrame - lastFrame).TotalMilliseconds;

            if (Mouse.Captured == this.notesCanvas || Mouse.Captured == this.timelineCanvas
                && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                const double scrollSpeed = 2.5;
                Point mousePos = Mouse.GetPosition(notesCanvas);
                bool needUdpate = false;
                if (mousePos.X < 0)
                {
                    this.horizontalScroll.Value = this.horizontalScroll.Value - 0.01 * horizontalScroll.SmallChange * scrollSpeed * deltaTime;
                    needUdpate = true;
                }
                else if (mousePos.X > notesCanvas.ActualWidth)
                {
                    this.horizontalScroll.Value = this.horizontalScroll.Value + 0.01 * horizontalScroll.SmallChange * scrollSpeed * deltaTime;
                    needUdpate = true;
                }

                if (mousePos.Y < 0 && Mouse.Captured == this.notesCanvas)
                {
                    this.notesVerticalScroll.Value = this.notesVerticalScroll.Value - 0.01 * notesVerticalScroll.SmallChange * scrollSpeed * deltaTime;
                    needUdpate = true;
                }
                else if (mousePos.Y > notesCanvas.ActualHeight && Mouse.Captured == this.notesCanvas)
                {
                    this.notesVerticalScroll.Value = this.notesVerticalScroll.Value + 0.01 * notesVerticalScroll.SmallChange * scrollSpeed * deltaTime;
                    needUdpate = true;
                }

                if (needUdpate)
                {
                    //ncModel.updateGraphics();
                    notesCanvas_MouseMove_Helper(mousePos);
                    if (Mouse.Captured == this.timelineCanvas) timelineCanvas_MouseMove_Helper(mousePos);
                }
            }

            //ncModel.trackPart.CheckOverlap();

            lastFrame = nextFrame;
        }

        # region Splitter

        private void GridSplitter_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.SizeNS;
        }

        private void GridSplitter_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        # endregion

    }
}
