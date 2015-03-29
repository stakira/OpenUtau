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

namespace OpenUtau.UI
{
    /// <summary>
    /// Interaction logic for BorderlessWindow.xaml
    /// </summary>
    public partial class MidiWindow : Window
    {
        // Window states
        WindowChrome _chrome;
        Thickness _activeBorderThickness;
        Thickness _inactiveBorderThickness;

        // Canvas states
        NotesCanvasModel ncModel;

        double lastNoteLength = 1;

        public MidiWindow()
        {
            InitializeComponent();
            _chrome = new WindowChrome();
            WindowChrome.SetWindowChrome(this, _chrome);
            _chrome.GlassFrameThickness = new Thickness(1);
            _chrome.CornerRadius = new CornerRadius(0);
            _chrome.CaptionHeight = 0;
            _activeBorderThickness = this.canvasBorder.BorderThickness;
            _inactiveBorderThickness = new Thickness(1);

            ncModel = new NotesCanvasModel();
            ncModel.hScroll = this.horizontalScroll;
            ncModel.notesVScroll = this.notesVerticalScroll;
            ncModel.expVScroll = this.expVerticalScroll;
            ncModel.notesCanvas = this.notesCanvas;
            ncModel.expCanvas = this.expCanvas;
            ncModel.keysCanvas = this.keysCanvas;
            ncModel.timelineCanvas = this.timelineCanvas;

            ncModel.initGraphics();
            ncModel.updateGraphics();

            CompositionTarget.Rendering += Window_PerFrameCallback;
        }

        #region Avoid hiding task bar upon maximalisation

        private static System.IntPtr WindowProc(
              System.IntPtr hwnd,
              int msg,
              System.IntPtr wParam,
              System.IntPtr lParam,
              ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:/* WM_GETMINMAXINFO */
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }

            return (System.IntPtr)0;
        }

        private static void WmGetMinMaxInfo(System.IntPtr hwnd, System.IntPtr lParam)
        {

            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            System.IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != System.IntPtr.Zero)
            {

                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
                mmi.ptMinTrackSize.x = 800;
                mmi.ptMinTrackSize.y = 600;
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        void win_SourceInitialized(object sender, EventArgs e)
        {
            System.IntPtr handle = (new WinInterop.WindowInteropHelper(this)).Handle;
            WinInterop.HwndSource.FromHwnd(handle).AddHook(new WinInterop.HwndSourceHook(WindowProc));
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            /// <summary>
            /// x coordinate of point.
            /// </summary>
            public int x;
            /// <summary>
            /// y coordinate of point.
            /// </summary>
            public int y;

            /// <summary>
            /// Construct a point of coordinates (x,y).
            /// </summary>
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor = new RECT();
            public RECT rcWork = new RECT();
            public int dwFlags = 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct RECT
        {
            /// <summary> Win32 </summary>
            public int left;
            /// <summary> Win32 </summary>
            public int top;
            /// <summary> Win32 </summary>
            public int right;
            /// <summary> Win32 </summary>
            public int bottom;

            /// <summary> Win32 </summary>
            public static readonly RECT Empty = new RECT();

            /// <summary> Win32 </summary>
            public int Width
            {
                get { return Math.Abs(right - left); }  // Abs needed for BIDI OS
            }
            /// <summary> Win32 </summary>
            public int Height
            {
                get { return bottom - top; }
            }

            /// <summary> Win32 </summary>
            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }


            /// <summary> Win32 </summary>
            public RECT(RECT rcSrc)
            {
                this.left = rcSrc.left;
                this.top = rcSrc.top;
                this.right = rcSrc.right;
                this.bottom = rcSrc.bottom;
            }

            /// <summary> Win32 </summary>
            public bool IsEmpty
            {
                get
                {
                    // BUGBUG : On Bidi OS (hebrew arabic) left > right
                    return left >= right || top >= bottom;
                }
            }
            /// <summary> Return a user friendly representation of this struct </summary>
            public override string ToString()
            {
                if (this == RECT.Empty) { return "RECT {Empty}"; }
                return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
            }

            /// <summary> Determine if 2 RECT are equal (deep compare) </summary>
            public override bool Equals(object obj)
            {
                if (!(obj is Rect)) { return false; }
                return (this == (RECT)obj);
            }

            /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
            public override int GetHashCode()
            {
                return left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
            }


            /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
            public static bool operator ==(RECT rect1, RECT rect2)
            {
                return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom);
            }

            /// <summary> Determine if 2 RECT are different(deep compare)</summary>
            public static bool operator !=(RECT rect1, RECT rect2)
            {
                return !(rect1 == rect2);
            }
        }

        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(ref Point lpPoint);

        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        #endregion

        #region Window Buttons

        private void minButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void maxButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Maximized)
            {
                WindowState = System.Windows.WindowState.Normal;
                this.maxButton.Content = "1";
            }
            else
            {
                WindowState = System.Windows.WindowState.Maximized;
                this.maxButton.Content = "2";
            }

        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void dragMove_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (WindowState == System.Windows.WindowState.Maximized)
                {
                    WindowState = System.Windows.WindowState.Normal;
                }
                else
                {
                    WindowState = System.Windows.WindowState.Maximized;
                }
            }
            else if (WindowState != System.Windows.WindowState.Maximized)
            // TODO: drag to restore from maximized and move window to mouse position
            // http://stackoverflow.com/questions/11703833/dragmove-and-maximize
            {
                DragMove();
            }
        }

        #endregion

        #region Draw window border when losing focus

        private void Window_Activated(object sender, EventArgs e)
        {
            this.canvasBorder.BorderThickness = _activeBorderThickness;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (WindowState != System.Windows.WindowState.Maximized) this.canvasBorder.BorderThickness = _inactiveBorderThickness;
        }

        #endregion

        # region Note Canvas

        Note noteInDrag = null;
        double noteOffsetOfDrag;
        Note leftMostNoteOfDrag, rightMostNoteOfDrag, maxNoteOfDrag, minNoteOfDrag;

        Note noteInResize = null;
        Note shortedNoteInResize;

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
            Point mousePos = e.GetPosition((Canvas)sender);
            NoteControl hitNoteControl = getNoteVisualHit(VisualTreeHelper.HitTest(notesCanvas, mousePos));

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                selectionStart = new Point(ncModel.canvasToOffset(mousePos.X), ncModel.snapNoteKey(mousePos.Y));
                if (Keyboard.IsKeyUp(Key.LeftShift) && Keyboard.IsKeyUp(Key.RightShift))
                    ncModel.trackPart.DeselectAll();
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
                        RadiusY = 8
                    };
                    notesCanvas.Children.Add(selectionBox);
                    Canvas.SetZIndex(selectionBox, 1000);
                } 
                else
                {
                    selectionBox.Width = 0;
                    selectionBox.Height = 0;
                    Canvas.SetZIndex(selectionBox, 1000);
                }
                Mouse.OverrideCursor = Cursors.Cross;
            }
            else
            {
                if (hitNoteControl != null)
                {
                    Note hitNote = ncModel.getNoteFromControl(hitNoteControl);
                    if (e.GetPosition(hitNoteControl).X < hitNoteControl.ActualWidth - NotesCanvasModel.resizeMargin)
                    {
                        // Move note
                        noteInDrag = hitNote;
                        noteOffsetOfDrag = ncModel.snapNoteOffset(e.GetPosition((Canvas)sender).X) - noteInDrag.offset;
                        lastNoteLength = noteInDrag.length;
                        leftMostNoteOfDrag = rightMostNoteOfDrag = maxNoteOfDrag = minNoteOfDrag = noteInDrag;
                        if (ncModel.trackPart.selectedNotes.Count != 0)
                            foreach (Note note in ncModel.trackPart.selectedNotes)
                            {
                                if (note.offset < leftMostNoteOfDrag.offset)
                                    leftMostNoteOfDrag = note;
                                if (note.offset > rightMostNoteOfDrag.offset)
                                    rightMostNoteOfDrag = note;
                                if (note.keyNo > maxNoteOfDrag.keyNo)
                                    maxNoteOfDrag = note;
                                if (note.keyNo < minNoteOfDrag.keyNo)
                                    minNoteOfDrag = note;
                            }
                    }
                    else
                    {
                        // Resize note
                        noteInResize = hitNote;
                        Mouse.OverrideCursor = Cursors.SizeWE;
                        shortedNoteInResize = noteInResize;
                        if (ncModel.trackPart.selectedNotes.Count != 0)
                            foreach (Note note in ncModel.trackPart.selectedNotes)
                                if (note.length < shortedNoteInResize.length)
                                    shortedNoteInResize = note;
                    }
                }
                else // Add note
                {
                    Note newNote = new Note
                    {
                        keyNo = ncModel.snapNoteKey(e.GetPosition((Canvas)sender).Y),
                        offset = ncModel.snapNoteOffset(e.GetPosition((Canvas)sender).X),
                        length = lastNoteLength
                    };
                    ncModel.trackPart.AddNote(newNote);
                    // Enable drag
                    noteInDrag = newNote;
                    noteOffsetOfDrag = ncModel.snapNoteOffset(e.GetPosition((Canvas)sender).X) - noteInDrag.offset;
                    leftMostNoteOfDrag = rightMostNoteOfDrag = maxNoteOfDrag = minNoteOfDrag = noteInDrag;
                    ncModel.trackPart.DeselectAll();
                }
            }
            ((UIElement)sender).CaptureMouse();
        }

        private void notesCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            noteInDrag = null;
            noteInResize = null;
            selectionStart = null;
            if (selectionBox != null)
            {
                Canvas.SetZIndex(selectionBox, -100);
            }
            ncModel.trackPart.FinishSelectTemp();
            ((Canvas)sender).ReleaseMouseCapture();
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void notesCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition((Canvas)sender);
            notesCanvas_MouseMove_Helper(mousePos);
        }

        private void notesCanvas_MouseMove_Helper(Point mousePos)
        {

            if (selectionStart != null) // Selection
            {
                double top = ncModel.keyToCanvas(Math.Max(ncModel.snapNoteKey(mousePos.Y), (int)selectionStart.Value.Y));
                double bottom = ncModel.keyToCanvas(Math.Min(ncModel.snapNoteKey(mousePos.Y), (int)selectionStart.Value.Y) - 1);
                selectionBox.Width = Math.Abs(mousePos.X - ncModel.offsetToCanvas(selectionStart.Value.X));
                selectionBox.Height = bottom - top;
                Canvas.SetLeft(selectionBox, Math.Min(mousePos.X, ncModel.offsetToCanvas(selectionStart.Value.X)));
                Canvas.SetTop(selectionBox, top);
                ncModel.trackPart.SelectTempInBox(
                    ncModel.canvasToOffset(mousePos.X),
                    selectionStart.Value.X,
                    ncModel.snapNoteKey(mousePos.Y),
                    selectionStart.Value.Y);
            }
            else if (noteInDrag != null) // Drag Note
            {
                double movedOffset = ncModel.snapNoteOffset(mousePos.X) - noteOffsetOfDrag - noteInDrag.offset;
                if (leftMostNoteOfDrag.offset + movedOffset < 0) movedOffset = -leftMostNoteOfDrag.offset;
                int movedKeyNo = ncModel.snapNoteKey(mousePos.Y) - noteInDrag.keyNo;
                if (maxNoteOfDrag.keyNo + movedKeyNo > NotesCanvasModel.numNotesHeight - 1)
                    movedKeyNo = NotesCanvasModel.numNotesHeight - 1 - maxNoteOfDrag.keyNo;
                if (minNoteOfDrag.keyNo + movedKeyNo < 0)
                    movedKeyNo = -minNoteOfDrag.keyNo;
                if (ncModel.trackPart.selectedNotes.Count == 0)
                {
                    noteInDrag.keyNo += movedKeyNo;
                    noteInDrag.offset += movedOffset;
                    noteInDrag.updateGraphics(ncModel);
                }
                else
                {
                    foreach (Note note in ncModel.trackPart.selectedNotes)
                    {
                        note.keyNo += movedKeyNo;
                        note.offset += movedOffset;
                        note.updateGraphics(ncModel);
                    }
                }

                Mouse.OverrideCursor = Cursors.SizeAll;
            }
            else if (noteInResize != null) // Resize Note
            {
                double newLength = ncModel.snapLength ?
                    ncModel.getLengthSnapUnit() + Math.Max(0, ncModel.snapNoteLength(mousePos.X - ncModel.offsetToCanvas(noteInResize.offset) - ncModel.getViewOffsetX())) :
                    Math.Max(Note.minLength, ncModel.snapNoteLength(mousePos.X) - noteInResize.offset);
                double deltaLength = newLength - noteInResize.length;
                if (shortedNoteInResize.length + deltaLength < ncModel.getOffsetSnapUnit()) deltaLength = ncModel.getOffsetSnapUnit() - shortedNoteInResize.length;
                if (ncModel.trackPart.selectedNotes.Count == 0)
                {
                    noteInResize.length += deltaLength;
                    noteInResize.updateGraphics(ncModel);
                }
                else
                {
                    foreach (Note note in ncModel.trackPart.selectedNotes)
                    {
                        note.length += deltaLength;
                        note.updateGraphics(ncModel);
                    }
                }

                lastNoteLength = noteInResize.length;
            }
            else if (Mouse.RightButton == MouseButtonState.Pressed) // Remove Note
            {
                NoteControl hitNoteControl = getNoteVisualHit(VisualTreeHelper.HitTest(notesCanvas, mousePos));

                if (hitNoteControl != null)
                {
                    Note note = ncModel.getNoteFromControl(hitNoteControl);
                    ncModel.trackPart.RemoveNote(note);
                }
            }
            else if (Mouse.LeftButton == MouseButtonState.Released && Mouse.RightButton == MouseButtonState.Released)
            {
                NoteControl hitNoteControl = getNoteVisualHit(VisualTreeHelper.HitTest(notesCanvas, mousePos));

                if (hitNoteControl != null) // Change Cursor
                {
                    if (Mouse.GetPosition(hitNoteControl).X < hitNoteControl.ActualWidth - NotesCanvasModel.resizeMargin)
                    {
                        Mouse.OverrideCursor = Cursors.Arrow;
                    }
                    else
                    {
                        Mouse.OverrideCursor = Cursors.SizeWE;
                    }
                }
                else
                {
                    Mouse.OverrideCursor = Cursors.Arrow;
                }
            }
        }

        private void notesCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition((Canvas)sender);
            NoteControl hitNoteControl = getNoteVisualHit(VisualTreeHelper.HitTest(notesCanvas, mousePos));

            if (hitNoteControl != null)
            {
                Note note = ncModel.getNoteFromControl(hitNoteControl);
                notesCanvas.Children.Remove(note.noteControl);
                ncModel.trackPart.RemoveNote(note);
            }
            else
            {
                ncModel.trackPart.DeselectAll();
            }
            ((UIElement)sender).CaptureMouse();
            Mouse.OverrideCursor = Cursors.No;

            // Debug code start
            ncModel.trackPart.PrintNotes();
            // Fill notes for debug
            if (Mouse.LeftButton == MouseButtonState.Pressed && Mouse.RightButton == MouseButtonState.Pressed)
            {
                for (int i = 60; i < 80; i++)
                {
                    for (int j = 0; j < 20; j++)
                    {
                        ncModel.trackPart.AddNote(new Note()
                        {
                            keyNo = i,
                            offset = j
                        });
                    }
                }
            }
            // Debug code end
        }

        private void notesCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
            ((UIElement)sender).ReleaseMouseCapture();
        }

        // TODO : resize show same portion of view
        private void notesCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ncModel.vZoom(0, notesCanvas.ActualHeight / 2);
            ncModel.hZoom(0, notesCanvas.ActualWidth / 2);
            ncModel.updateScroll();
            ncModel.updateGraphics();
            updateVZoomControl();
        }

        private void notesCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                timelineCanvas_MouseWheel(sender, e);
            }
            else
            {
                notesVerticalScroll_MouseWheel(sender, e);
            }
        }

        # endregion

        #region Navigate Drag

        bool _navDrag = false;
        double _navDragLastX;
        double _navDragLastY;

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        private void setCursorPos(Point point)
        {
            SetCursorPos((int)(PointToScreen(point).X), (int)(PointToScreen(point).Y));
        }

        private void navigateDrag_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement el = (FrameworkElement)sender;
            el.CaptureMouse();

            _navDrag = true;
            _navDragLastX = e.GetPosition(el).X;
            _navDragLastY = e.GetPosition(el).Y;

            Mouse.OverrideCursor = Cursors.None;
        }

        private void navigateDrag_MouseMove(object sender, MouseEventArgs e)
        {
            if (_navDrag)
            {
                const double navigateSpeed = 0.001;

                bool cursorMoved = false;
                FrameworkElement el = (FrameworkElement)sender;

                this.notesVerticalScroll.Value += navigateSpeed * (e.GetPosition(el).Y - _navDragLastY);
                this.horizontalScroll.Value += navigateSpeed * (e.GetPosition(el).X - _navDragLastX);

                ncModel.updateGraphics();

                _navDragLastX = e.GetPosition(el).X;
                _navDragLastY = e.GetPosition(el).Y;

                // Restrict mouse position
                if (e.GetPosition(el).X < 0)
                {
                    cursorMoved = true;
                    _navDragLastX += el.ActualWidth;
                }
                else if (e.GetPosition(el).X > el.ActualWidth)
                {
                    cursorMoved = true;
                    _navDragLastX -= el.ActualWidth;
                }

                if (e.GetPosition(el).Y < 0)
                {
                    cursorMoved = true;
                    _navDragLastY += el.ActualHeight;
                }
                else if (e.GetPosition(el).Y > el.ActualHeight)
                {
                    cursorMoved = true;
                    _navDragLastY -= el.ActualHeight;
                }

                if (cursorMoved)
                {
                    setCursorPos(el.TransformToAncestor(this).Transform(new Point(_navDragLastX, _navDragLastY)));
                }
            }
        }

        private void navigateDrag_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_navDrag)
            {
                ((FrameworkElement)sender).ReleaseMouseCapture();
                Mouse.OverrideCursor = Cursors.Arrow;
                _navDrag = false;
            }
        }

        #endregion

        #region Vertical Zoom Control

        bool vZoomDrag = false;
        double vZoomDragLastX;
        double vZoomDragLastY;

        private void updateVZoomControl()
        {
            double offset = 7 * Math.Log(NotesCanvasModel.noteMaxHeight / ncModel.noteHeight, 2)
                / Math.Log(NotesCanvasModel.noteMaxHeight / NotesCanvasModel.noteMinHeight, 2);
            double size = offset < 4 ? 4 : 8 - offset;

            ((Path)this.vZoomControlStack.Children[0]).Data = Geometry.Parse(
                " M " + (8 - size) + " " + (offset + size) +
                " L 8 " + (offset).ToString() +
                " L " + (8 + size) + " " + (offset + size) +
                " M " + (8 - size) + " " + (16 - size - offset) +
                " L 8 " + (16 - offset) +
                " L " + (8 + size) + " " + (16 - size - offset));
        }

        private void vZoomControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double zoomSpeed = 0.001;
            ncModel.vZoom(zoomSpeed * e.Delta, notesCanvas.ActualHeight / 2);
            ncModel.updateScroll();
            ncModel.updateGraphics();
            updateVZoomControl();
        }

        private void vZoomControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement el = (FrameworkElement)sender;
            el.CaptureMouse();

            vZoomDrag = true;
            vZoomDragLastX = e.GetPosition(el).X;
            vZoomDragLastY = e.GetPosition(el).Y;

            Mouse.OverrideCursor = Cursors.None;
        }

        private void vZoomControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (vZoomDrag)
            {
                ((FrameworkElement)sender).ReleaseMouseCapture();
                Mouse.OverrideCursor = Cursors.Arrow;
                vZoomDrag = false;
            }
        }

        private void vZoomControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (vZoomDrag)
            {
                const double zoomSpeed = 0.002;

                bool cursorMoved = false;
                FrameworkElement el = (FrameworkElement)sender;

                ncModel.vZoom(zoomSpeed * (vZoomDragLastY - e.GetPosition(el).Y), notesCanvas.ActualHeight / 2);
                ncModel.updateScroll();
                ncModel.updateGraphics();
                updateVZoomControl();

                _navDragLastX = e.GetPosition(el).X;
                _navDragLastY = e.GetPosition(el).Y;

                // Restrict mouse position
                if (e.GetPosition(el).X < 0)
                {
                    cursorMoved = true;
                    vZoomDragLastX += el.ActualWidth;
                }
                else if (e.GetPosition(el).X > el.ActualWidth)
                {
                    cursorMoved = true;
                    vZoomDragLastX -= el.ActualWidth;
                }

                if (e.GetPosition(el).Y < 0)
                {
                    cursorMoved = true;
                    vZoomDragLastY += el.ActualHeight;
                }
                else if (e.GetPosition(el).Y > el.ActualHeight)
                {
                    cursorMoved = true;
                    vZoomDragLastY -= el.ActualHeight;
                }

                if (cursorMoved)
                {
                    setCursorPos(el.TransformToAncestor(this).Transform(new Point(vZoomDragLastX, vZoomDragLastY)));
                }
            }
        }

        private void vZoomControlStack_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Path)vZoomControlStack.Children[0]).Stroke = (SolidColorBrush)System.Windows.Application.Current.FindResource("ScrollBarBrushActive");
        }

        private void vZoomControlStack_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Path)vZoomControlStack.Children[0]).Stroke = (SolidColorBrush)System.Windows.Application.Current.FindResource("ScrollBarBrushNormal");
        }

        #endregion

        # region Timeline Canvas
        
        private void timelineCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double zoomSpeed = 0.0012;
            ncModel.hZoom(e.Delta * zoomSpeed, e.GetPosition((UIElement)sender).X);
            ncModel.updateScroll();
            ncModel.updateGraphics();
        }

        private void timelineCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ncModel.playPosMarkerOffset = ncModel.snapNoteOffset(e.GetPosition((UIElement)sender).X);
            ncModel.updatePlayPosMarker();
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
                ncModel.playPosMarkerOffset = ncModel.snapNoteOffset(mousePos.X);
                ncModel.updatePlayPosMarker();
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
            ncModel.updateGraphics();
        }

        private void notesVerticalScroll_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.notesVerticalScroll.Value = this.notesVerticalScroll.Value - 0.01 * notesVerticalScroll.SmallChange * e.Delta;
            ncModel.updateGraphics();
        }

        # endregion

        # region Horizontal Scrollbar

        private void horizontalScroll_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            ncModel.updateGraphics();
        }

        private void horizontalScroll_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.horizontalScroll.Value = this.horizontalScroll.Value - 0.01 * horizontalScroll.SmallChange * e.Delta;
            ncModel.updateGraphics();
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
            this.notesVerticalScroll.Value = this.notesVerticalScroll.Value - 0.01 * notesVerticalScroll.SmallChange * e.Delta;
            ncModel.updateGraphics();
        }

        # endregion

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                // Select all notes
                ncModel.trackPart.SelectAll();
            }
            else if (e.Key == Key.Delete)
            {
                // Delete notes
                ncModel.trackPart.RemoveSelectedNote();
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
                    ncModel.updateGraphics();
                    notesCanvas_MouseMove_Helper(mousePos);
                    if (Mouse.Captured == this.timelineCanvas) timelineCanvas_MouseMove_Helper(mousePos);
                }
            }

            lastFrame = nextFrame;
        }

        private void GridSplitter_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.SizeNS;
        }

        private void GridSplitter_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }
    }
}
