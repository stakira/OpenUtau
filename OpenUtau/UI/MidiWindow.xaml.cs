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
        MidiViewModel midiVM;

        public MidiWindow()
        {
            InitializeComponent();

            this.CloseButtonClicked += (o, e) => { Hide(); };
            CompositionTargetEx.FrameUpdating += RenderLoop;

            viewScaler.Max = UIConstants.NoteMaxHeight;
            viewScaler.Min = UIConstants.NoteMinHeight;
            viewScaler.Value = UIConstants.NoteDefaultHeight;
            viewScaler.ViewScaled += viewScaler_ViewScaled;

            midiVM = (MidiViewModel)this.Resources["midiVM"];
            midiVM.MidiCanvas = this.notesCanvas;
            midiVM.ExpCanvas = this.expCanvas;
            midiVM.Subscribe(DocManager.Inst);
        }

        void viewScaler_ViewScaled(object sender, EventArgs e)
        {
            double zoomCenter = (midiVM.OffsetY + midiVM.ViewHeight / 2) / midiVM.TrackHeight;
            midiVM.TrackHeight = ((ViewScaledEventArgs)e).Value;
            midiVM.OffsetY = midiVM.TrackHeight * zoomCenter - midiVM.ViewHeight / 2;
            midiVM.MarkUpdate();
        }

        private TimeSpan lastFrame = TimeSpan.Zero;

        void RenderLoop(object sender, EventArgs e)
        {
            if (midiVM.Part == null || midiVM.Project == null) return;

            TimeSpan nextFrame = ((RenderingEventArgs)e).RenderingTime;
            double deltaTime = (nextFrame - lastFrame).TotalMilliseconds;
            lastFrame = nextFrame;

            DragScroll(deltaTime);
            keyboardBackground.RenderIfUpdated();
            tickBackground.RenderIfUpdated();
            timelineBackground.RenderIfUpdated();
            keyTrackBackground.RenderIfUpdated();
            expTickBackground.RenderIfUpdated();
            midiVM.RedrawIfUpdated();
        }

        public void DragScroll(double deltaTime)
        {
            if ((Mouse.Captured == this.notesCanvas || Mouse.Captured == this.timelineCanvas)
                && Mouse.LeftButton == MouseButtonState.Pressed)
            {

                const double scrollSpeed = 0.015;
                Point mousePos = Mouse.GetPosition(notesCanvas);
                bool needUdpate = false;
                double delta = scrollSpeed * deltaTime;
                if (mousePos.X < 0)
                {
                    this.horizontalScroll.Value = this.horizontalScroll.Value - this.horizontalScroll.SmallChange * delta;
                    needUdpate = true;
                }
                else if (mousePos.X > notesCanvas.ActualWidth)
                {
                    this.horizontalScroll.Value = this.horizontalScroll.Value + this.horizontalScroll.SmallChange * delta;
                    needUdpate = true;
                }

                if (mousePos.Y < 0 && Mouse.Captured == this.notesCanvas)
                {
                    this.verticalScroll.Value = this.verticalScroll.Value - this.verticalScroll.SmallChange * delta;
                    needUdpate = true;
                }
                else if (mousePos.Y > notesCanvas.ActualHeight && Mouse.Captured == this.notesCanvas)
                {
                    this.verticalScroll.Value = this.verticalScroll.Value + this.verticalScroll.SmallChange * delta;
                    needUdpate = true;
                }

                if (needUdpate)
                {
                    notesCanvas_MouseMove_Helper(mousePos);
                    if (Mouse.Captured == this.timelineCanvas) timelineCanvas_MouseMove_Helper(mousePos);
                }
            }
        }

        # region Note Canvas

        Rectangle selectionBox;
        Nullable<Point> selectionStart;
        int _lastNoteLength = 480;

        bool _moveNote = false;
        bool _resizeNote = false;
        NoteControl _hitNoteControl;
        int _noteMoveRelativeTick;
        int _noteMoveStartTick;
        UNote _noteMoveNoteLeft;
        UNote _noteMoveNoteRight;
        UNote _noteMoveNoteMin;
        UNote _noteMoveNoteMax;
        UNote _noteResizeShortest;

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
            if (midiVM.Part == null) return;
            FocusManager.SetFocusedElement(this, null);
            Point mousePos = e.GetPosition((Canvas)sender);

            var hit = VisualTreeHelper.HitTest(notesCanvas, mousePos).VisualHit;
            System.Diagnostics.Debug.WriteLine("Mouse hit " + hit.ToString());

            NoteControl hitNoteControl = getNoteVisualHit(VisualTreeHelper.HitTest(notesCanvas, mousePos));
            if (hitNoteControl != null) System.Diagnostics.Debug.WriteLine("Mouse hit" + hitNoteControl.ToString());

            if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                selectionStart = new Point(midiVM.CanvasToQuarter(mousePos.X), midiVM.CanvasToNoteNum(mousePos.Y));
                
                if (Keyboard.IsKeyUp(Key.LeftShift) && Keyboard.IsKeyUp(Key.RightShift)) midiVM.DeselectAll();

                if (selectionBox == null)
                {
                    selectionBox = new Rectangle()
                    {
                        Stroke = Brushes.Black,
                        StrokeThickness = 2,
                        Fill = ThemeManager.getBarNumberBrush(),
                        Width = 0,
                        Height = 0,
                        Opacity = 0.5,
                        RadiusX = 8,
                        RadiusY = 8,
                        IsHitTestVisible = false
                    };
                    notesCanvas.Children.Add(selectionBox);
                    Canvas.SetZIndex(selectionBox, 1000);
                    selectionBox.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    selectionBox.Width = 0;
                    selectionBox.Height = 0;
                    Canvas.SetZIndex(selectionBox, 1000);
                    selectionBox.Visibility = System.Windows.Visibility.Visible;
                }
                Mouse.OverrideCursor = Cursors.Cross;
            }
            else
            {
                if (hitNoteControl != null)
                {
                    _hitNoteControl = hitNoteControl;
                    UNote hitNote = hitNoteControl.Note;
                    if (!midiVM.SelectedNotes.Contains(hitNote)) midiVM.DeselectAll();

                    if (e.GetPosition(hitNoteControl).X < hitNoteControl.ActualWidth - UIConstants.ResizeMargin)
                    {
                        // Move note
                        _moveNote = true;
                        _noteMoveRelativeTick = midiVM.CanvasToSnappedTick(mousePos.X) - hitNote.PosTick;
                        _noteMoveStartTick = hitNote.PosTick;
                        _lastNoteLength = hitNote.DurTick;
                        if (midiVM.SelectedNotes.Count != 0)
                        {
                            _noteMoveNoteMax = _noteMoveNoteMin = hitNote;
                            _noteMoveNoteLeft = _noteMoveNoteRight = hitNote;
                            foreach (UNote note in midiVM.SelectedNotes)
                            {
                                if (note.PosTick < _noteMoveNoteLeft.PosTick) _noteMoveNoteLeft = note;
                                if (note.EndTick > _noteMoveNoteRight.EndTick) _noteMoveNoteRight = note;
                                if (note.NoteNum < _noteMoveNoteMin.NoteNum) _noteMoveNoteMin = note;
                                if (note.NoteNum > _noteMoveNoteMax.NoteNum) _noteMoveNoteMax = note;
                            }
                        }
                        DocManager.Inst.StartUndoGroup();
                    }
                    else if (!hitNoteControl.IsLyricBoxActive())
                    {
                        // Resize note
                        _resizeNote = true;
                        Mouse.OverrideCursor = Cursors.SizeWE;
                        if (midiVM.SelectedNotes.Count != 0)
                        {
                            _noteResizeShortest = hitNote;
                            foreach (UNote note in midiVM.SelectedNotes)
                                if (note.DurTick < _noteResizeShortest.DurTick) _noteResizeShortest = note;
                        }
                        DocManager.Inst.StartUndoGroup();
                    }
                }
                else // Add note
                {
                    UNote newNote = new UNote()
                    {
                        Lyric = "a",
                        NoteNum = midiVM.CanvasToNoteNum(mousePos.Y),
                        PosTick = midiVM.CanvasToSnappedTick(mousePos.X),
                        DurTick = _lastNoteLength
                    };
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new AddNoteCommand(midiVM.Part, newNote));
                    DocManager.Inst.EndUndoGroup();
                    midiVM.MarkUpdate();
                    // Enable drag
                    midiVM.DeselectAll();
                    _moveNote = true;
                    _hitNoteControl = midiVM.GetNoteControl(newNote);
                    _noteMoveRelativeTick = 0;
                    _noteMoveStartTick = newNote.PosTick;
                    DocManager.Inst.StartUndoGroup();
                }
            }
            ((UIElement)sender).CaptureMouse();
        }

        private void notesCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (midiVM.Part == null) return;
            _moveNote = false;
            _resizeNote = false;
            _hitNoteControl = null;
            DocManager.Inst.EndUndoGroup();
            // End selection
            selectionStart = null;
            if (selectionBox != null)
            {
                Canvas.SetZIndex(selectionBox, -100);
                selectionBox.Visibility = System.Windows.Visibility.Hidden;
            }
            midiVM.DoneTempSelect();
            ((Canvas)sender).ReleaseMouseCapture();
            Mouse.OverrideCursor = null;
        }

        private void notesCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition((Canvas)sender);
            notesCanvas_MouseMove_Helper(mousePos);
        }

        private void notesCanvas_MouseMove_Helper(Point mousePos)
        {
            if (midiVM.Part == null) return;
            if (selectionStart != null) // Selection
            {
                double top = midiVM.NoteNumToCanvas(Math.Max(midiVM.CanvasToNoteNum(mousePos.Y), (int)selectionStart.Value.Y));
                double bottom = midiVM.NoteNumToCanvas(Math.Min(midiVM.CanvasToNoteNum(mousePos.Y), (int)selectionStart.Value.Y) - 1);
                double left = Math.Min(mousePos.X, midiVM.QuarterToCanvas(selectionStart.Value.X));
                selectionBox.Width = Math.Abs(mousePos.X - midiVM.QuarterToCanvas(selectionStart.Value.X));
                selectionBox.Height = bottom - top;
                Canvas.SetLeft(selectionBox, left);
                Canvas.SetTop(selectionBox, top);
                midiVM.TempSelectInBox(selectionStart.Value.X, midiVM.CanvasToQuarter(mousePos.X), (int)selectionStart.Value.Y, midiVM.CanvasToNoteNum(mousePos.Y));
            }
            else if (_moveNote) // Move Note
            {
                if (midiVM.SelectedNotes.Count == 0)
                {
                    int newNoteNum = Math.Max(0, Math.Min(UIConstants.MaxNoteNum - 1, midiVM.CanvasToNoteNum(mousePos.Y)));
                    int newPosTick = Math.Max(0, Math.Min((int)(midiVM.QuarterCount * midiVM.Project.Resolution) - _hitNoteControl.Note.DurTick,
                        (int)(midiVM.Project.Resolution * midiVM.CanvasToSnappedQuarter(mousePos.X)) - _noteMoveRelativeTick));
                    if (newNoteNum != _hitNoteControl.Note.NoteNum || newPosTick != _hitNoteControl.Note.PosTick)
                        DocManager.Inst.ExecuteCmd(new MoveNoteCommand(midiVM.Part, _hitNoteControl.Note, newPosTick, newNoteNum));
                }
                else
                {
                    int deltaNoteNum = midiVM.CanvasToNoteNum(mousePos.Y) - _hitNoteControl.Note.NoteNum;
                    int deltaPosTick = ((int)(midiVM.Project.Resolution * midiVM.CanvasToSnappedQuarter(mousePos.X)) - _noteMoveRelativeTick) - _hitNoteControl.Note.PosTick;

                    if (deltaNoteNum != 0 || deltaPosTick != 0)
                    {
                        bool changeNoteNum = deltaNoteNum + _noteMoveNoteMin.NoteNum >= 0 && deltaNoteNum + _noteMoveNoteMax.NoteNum < UIConstants.MaxNoteNum;
                        bool changePosTick = deltaPosTick + _noteMoveNoteLeft.PosTick >= 0 && deltaPosTick + _noteMoveNoteRight.EndTick <= midiVM.QuarterCount * midiVM.Project.Resolution;
                        if (changeNoteNum || changePosTick)
                            foreach (UNote note in midiVM.SelectedNotes)
                                DocManager.Inst.ExecuteCmd(new MoveNoteCommand(midiVM.Part, note,
                                    changePosTick ? note.PosTick + deltaPosTick : note.PosTick,
                                    changeNoteNum ? note.NoteNum + deltaNoteNum : note.NoteNum));
                    }
                }
                Mouse.OverrideCursor = Cursors.SizeAll;
            }
            else if (_resizeNote) // resize
            {
                if (midiVM.SelectedNotes.Count == 0)
                {
                    int newDurTick = (int)(midiVM.CanvasRoundToSnappedQuarter(mousePos.X) * midiVM.Project.Resolution) - _hitNoteControl.Note.PosTick;
                    if (newDurTick != _hitNoteControl.Note.DurTick && newDurTick >= midiVM.GetSnapUnit() * midiVM.Project.Resolution)
                    {
                        DocManager.Inst.ExecuteCmd(new ResizeNoteCommand(midiVM.Part, _hitNoteControl.Note, newDurTick));
                        _lastNoteLength = newDurTick;
                    }
                }
                else
                {
                    int deltaDurTick = (int)(midiVM.CanvasRoundToSnappedQuarter(mousePos.X) * midiVM.Project.Resolution) - _hitNoteControl.Note.EndTick;
                    if (deltaDurTick != 0 && deltaDurTick + _noteResizeShortest.DurTick > midiVM.GetSnapUnit())
                    {
                        foreach (UNote note in midiVM.SelectedNotes) DocManager.Inst.ExecuteCmd(new ResizeNoteCommand(midiVM.Part, note, note.DurTick + deltaDurTick));
                        _lastNoteLength = _hitNoteControl.Note.DurTick;
                    }
                }
            }
            else if (Mouse.RightButton == MouseButtonState.Pressed) // Remove Note
            {
                NoteControl hitNoteControl = getNoteVisualHit(VisualTreeHelper.HitTest(notesCanvas, mousePos));
                if (hitNoteControl != null) DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(midiVM.Part, hitNoteControl.Note));
            }
            else if (Mouse.LeftButton == MouseButtonState.Released && Mouse.RightButton == MouseButtonState.Released)
            {
                NoteControl hitNoteControl = getNoteVisualHit(VisualTreeHelper.HitTest(notesCanvas, mousePos));
                if (hitNoteControl != null) // Change Cursor
                {
                    if (Mouse.GetPosition(hitNoteControl).X > hitNoteControl.ActualWidth - UIConstants.ResizeMargin) Mouse.OverrideCursor = Cursors.SizeWE;
                    else Mouse.OverrideCursor = null;
                }
                else
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }

        private void notesCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (midiVM.Part == null) return;
            FocusManager.SetFocusedElement(this, null);
            Point mousePos = e.GetPosition((Canvas)sender);
            NoteControl hitNoteControl = getNoteVisualHit(VisualTreeHelper.HitTest(notesCanvas, mousePos));
            DocManager.Inst.StartUndoGroup();
            if (hitNoteControl != null && midiVM.SelectedNotes.Contains(hitNoteControl.Note))
                DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(midiVM.Part, hitNoteControl.Note));
            else midiVM.DeselectAll();

            ((UIElement)sender).CaptureMouse();
            Mouse.OverrideCursor = Cursors.No;
            System.Diagnostics.Debug.WriteLine("Total notes: " + midiVM.Part.Notes.Count +
                " controls: " + midiVM.NoteControls.Count + " selected: " + midiVM.SelectedNotes.Count);
        }

        private void notesCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (midiVM.Part == null) return;
            Mouse.OverrideCursor = null;
            ((UIElement)sender).ReleaseMouseCapture();
            DocManager.Inst.EndUndoGroup();
        }

        private void notesCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                timelineCanvas_MouseWheel(sender, e);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                midiVM.OffsetX -= midiVM.ViewWidth * 0.001 * e.Delta;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
            }
            else
            {
                midiVM.OffsetY -= midiVM.ViewHeight * 0.001 * e.Delta;
            }
        }

        # endregion

        #region Navigate Drag

        private void navigateDrag_NavDrag(object sender, EventArgs e)
        {
            midiVM.OffsetX += ((NavDragEventArgs)e).X * midiVM.SmallChangeX;
            midiVM.OffsetY += ((NavDragEventArgs)e).Y * midiVM.SmallChangeY * 0.5;
            midiVM.MarkUpdate();
        }

        #endregion

        # region Timeline Canvas
        
        private void timelineCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double zoomSpeed = 0.0012;
            Point mousePos = e.GetPosition((UIElement)sender);
            double zoomCenter;
            if (midiVM.OffsetX == 0 && mousePos.X < 128) zoomCenter = 0;
            else zoomCenter = (midiVM.OffsetX + mousePos.X) / midiVM.QuarterWidth;
            midiVM.QuarterWidth *= 1 + e.Delta * zoomSpeed;
            midiVM.OffsetX = Math.Max(0, Math.Min(midiVM.TotalWidth, zoomCenter * midiVM.QuarterWidth - mousePos.X));
        }

        private void timelineCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (midiVM.Part == null) return;
            //ncModel.playPosMarkerOffset = ncModel.snapNoteOffset(e.GetPosition((UIElement)sender).X);
            //ncModel.updatePlayPosMarker();
            ((Canvas)sender).CaptureMouse();
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
            if (midiVM.Part == null) return;
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.A)
            {
                midiVM.SelectAll();
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
            {
                midiVM.DeselectAll();
                DocManager.Inst.Undo();
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Y)
            {
                midiVM.DeselectAll();
                DocManager.Inst.Redo();
            }
            else if (Keyboard.Modifiers == 0 && e.Key == Key.Delete)
            {
                DocManager.Inst.StartUndoGroup();
                while (midiVM.SelectedNotes.Count > 0) DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(midiVM.Part, midiVM.SelectedNotes.Last()));
                DocManager.Inst.EndUndoGroup();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void expCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition((UIElement)sender);
            expCanvas_SetExpHelper(mousePos);
            ((Canvas)sender).CaptureMouse();
        }

        private void expCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ((Canvas)sender).ReleaseMouseCapture();
        }

        private void expCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition((UIElement)sender);
                expCanvas_SetExpHelper(mousePos);
            }
        }

        private void expCanvas_SetExpHelper(Point mousePos)
        {
            int newValue;
            if (Keyboard.Modifiers == ModifierKeys.Alt) newValue = 64;
            else newValue = (int)Math.Max(0, Math.Min(127, (1 - mousePos.Y / expCanvas.ActualHeight) * 127));
            UNote note = midiVM.CanvasXToNote(mousePos.X);
            if (midiVM.SelectedNotes.Count == 0 || midiVM.SelectedNotes.Contains(note))
            if (note != null) note.styles[midiVM.expElement.Key] = (object)newValue;
            midiVM.expElement.Redraw();
        }
    }
}
