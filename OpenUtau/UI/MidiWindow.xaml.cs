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
        NotesCanvasModel _nCM;


        // TODO : support unsnapped move / add / resize
        bool _snapPosition = true;
        bool _snapLength = true;

        double _lastNoteLength = 1;


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

            _nCM = new NotesCanvasModel();
            _nCM.hScroll = this.horizontalScroll;
            _nCM.notesVScroll = this.notesVerticalScroll;
            _nCM.expVScroll = this.expVerticalScroll;
            _nCM.notesCanvas = this.notesCanvas;
            _nCM.expCanvas = this.expCanvas;
            _nCM.keysCanvas = this.keysCanvas;
            _nCM.timelineCanvas = this.timelineCanvas;

            _nCM.initGraphics();

            updateCanvas();
        }

        private void updateCanvas()
        {
            // TODO : Improve performance
            // Update canvas
            if (notesCanvas.ActualHeight > NotesCanvasModel.numNotesHeight * _nCM.noteHeight)
                _nCM.noteHeight = notesCanvas.ActualHeight / NotesCanvasModel.numNotesHeight;

            if (notesCanvas.ActualWidth > _nCM.numNotesWidthScroll * _nCM.noteWidth)
                _nCM.noteWidth = notesCanvas.ActualWidth / _nCM.numNotesWidthScroll;

            notesVerticalScroll.ViewportSize = _nCM.getViewportSizeY(notesCanvas.ActualHeight);
            notesVerticalScroll.SmallChange = notesVerticalScroll.ViewportSize / 10;
            notesVerticalScroll.LargeChange = notesVerticalScroll.ViewportSize;

            horizontalScroll.ViewportSize = _nCM.getViewportSizeX(notesCanvas.ActualWidth);
            horizontalScroll.SmallChange = horizontalScroll.ViewportSize / 10;
            horizontalScroll.LargeChange = horizontalScroll.ViewportSize;

            _nCM.updateGraphics();

            // Update components
            updateNotes();
            updateZoomControl();
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

        Note _dragNote = null;
        Nullable<Point> _dragPosOfNote = null;
        Note _resizeNote = null;

        private void updateNotes()
        {
            foreach (Note note in _nCM.notes)
            {
                note.shape.Height = _nCM.noteHeight - 2;
                note.shape.Width = note.length * _nCM.noteWidth - 3;
                Canvas.SetLeft(note.shape, _nCM.beatToCanvas(note.beat, horizontalScroll.Value, notesCanvas.ActualWidth) + 1);
                Canvas.SetTop(note.shape, _nCM.keyToCanvas(note.keyNo, notesVerticalScroll.Value, notesCanvas.ActualHeight) + 1);
            }
        }

        private void notesCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition((Canvas)sender);
            HitTestResult result = VisualTreeHelper.HitTest(notesCanvas, pt);

            if (result.VisualHit.GetType() == typeof(Rectangle) && ((UIElement)result.VisualHit).IsHitTestVisible)
            {
                Note hitNote = _nCM.shapeToNote((Rectangle)result.VisualHit);
                if (e.GetPosition((UIElement)result.VisualHit).X < hitNote.shape.Width - NotesCanvasModel.resizeMargin) // Move note
                {
                    _dragNote = hitNote;
                    Point mousePos = e.GetPosition((UIElement)result.VisualHit);
                    _dragPosOfNote = new Point(_nCM.snapToBeat(mousePos.X, horizontalScroll.Value, notesCanvas.ActualWidth),
                        _nCM.snapToKey(mousePos.Y, notesVerticalScroll.Value, notesCanvas.ActualHeight));
                    _lastNoteLength = _dragNote.length;
                    
                }
                else // Resize note
                {
                    _resizeNote = hitNote;
                    ((Canvas)sender).CaptureMouse();
                    Mouse.OverrideCursor = Cursors.SizeWE;
                }
            }
            else // Add note
            {
                var shape = new Rectangle { Fill = Brushes.Gray, RadiusX = 4, RadiusY = 4, Opacity = 0.75 };
                Note newNote = new Note
                {
                    keyNo = _nCM.canvasToKey(e.GetPosition((Canvas)sender).Y, notesVerticalScroll.Value, notesCanvas.ActualHeight),
                    beat = _nCM.canvasToBeat(e.GetPosition((Canvas)sender).X, horizontalScroll.Value, notesCanvas.ActualWidth),
                    shape = shape,
                    length = _lastNoteLength
                };
                _nCM.notes.Add(newNote);
                notesCanvas.Children.Add(shape);
                updateCanvas();
                // TODO : Enable Drag
            }
        }

        private void notesCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition((Canvas)sender);
            HitTestResult result = VisualTreeHelper.HitTest(notesCanvas, pt);

            if (result.VisualHit.GetType() == typeof(Rectangle) && ((UIElement)result.VisualHit).IsHitTestVisible)
            {
                Note note = _nCM.shapeToNote((Rectangle)result.VisualHit);
                notesCanvas.Children.Remove(note.shape);
                _nCM.notes.Remove(note);
            }

            _nCM.debugPrintNotes();
        }

        private void notesCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _dragNote = null;
            _dragPosOfNote = null;
            _resizeNote = null;
            ((Canvas)sender).ReleaseMouseCapture();
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void notesCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragNote != null && e.LeftButton == MouseButtonState.Pressed) // Drag Note
            {
                var mousePos = e.GetPosition(notesCanvas);
                _dragNote.keyNo = _nCM.canvasToKey(mousePos.Y, notesVerticalScroll.Value, notesCanvas.ActualHeight);
                _dragNote.beat = _nCM.canvasToBeat(mousePos.X - _dragPosOfNote.Value.X, horizontalScroll.Value, notesCanvas.ActualWidth);
                _dragNote.beat = Math.Max(0, _dragNote.beat);
                Canvas.SetLeft(_dragNote.shape, _nCM.beatToCanvas(_dragNote.beat, horizontalScroll.Value, notesCanvas.ActualWidth) + 1);
                Canvas.SetTop(_dragNote.shape, _nCM.keyToCanvas(_dragNote.keyNo, notesVerticalScroll.Value, notesCanvas.ActualHeight) + 1);
                // TODO : Drag scroll
            }
            else if (_resizeNote != null) // Resize Note
            {
                var mousePos = e.GetPosition(notesCanvas);
                int beat = _nCM.canvasToBeat(mousePos.X, horizontalScroll.Value, notesCanvas.ActualWidth) + 1;
                double newLength = 1;
                if (beat > _resizeNote.beat)
                {
                    newLength = beat - _resizeNote.beat;
                }
                _resizeNote.length = newLength;
                _resizeNote.shape.Width = _resizeNote.length * _nCM.noteWidth - 3;
                _lastNoteLength = newLength;
            }
            else if (e.RightButton == MouseButtonState.Pressed) // Remove Notes
            {
                Point pt = e.GetPosition((Canvas)sender);
                HitTestResult result = VisualTreeHelper.HitTest(notesCanvas, pt);

                if (result.VisualHit.GetType() == typeof(Rectangle) && ((UIElement)result.VisualHit).IsHitTestVisible)
                {
                    Note note = _nCM.shapeToNote((Rectangle)result.VisualHit);
                    notesCanvas.Children.Remove(note.shape);
                    _nCM.notes.Remove(note);
                    _nCM.notes.Sort(delegate (Note lhs, Note rhs)
                    {
                        if (lhs.beat < rhs.beat) return -1;
                        else if (lhs.beat > rhs.beat) return 1;
                        else if (lhs.keyNo < rhs.keyNo) return -1;
                        else if (lhs.keyNo > rhs.keyNo) return 1;
                        else return 0;
                    });
                }
            }
            else if(e.LeftButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Released)
            {
                Point pt = e.GetPosition((Canvas)sender);
                HitTestResult result = VisualTreeHelper.HitTest(notesCanvas, pt);

                if (result.VisualHit.GetType() == typeof(Rectangle) && ((UIElement)result.VisualHit).IsHitTestVisible) // Change Cursor
                {
                    Note hitNote = _nCM.shapeToNote((Rectangle)result.VisualHit);
                    if (e.GetPosition((UIElement)result.VisualHit).X > hitNote.shape.Width - NotesCanvasModel.resizeMargin)
                    {
                        Mouse.OverrideCursor = Cursors.SizeWE;
                    }
                    else
                    {
                        Mouse.OverrideCursor = Cursors.Arrow;
                    }
                }
                else
                {
                    Mouse.OverrideCursor = Cursors.Arrow;
                }
            }
        }

        private void notesCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            zoomControl_Zoom(0);
            horizontalZoom(0);
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

                updateCanvas();

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

        bool _zoomDrag = false;
        double _zoomDragLastX;
        double _zoomDragLastY;

        private void updateZoomControl()
        {
            double offset = 7 * Math.Log(NotesCanvasModel.noteMaxHeight / _nCM.noteHeight, 2)
                / Math.Log(NotesCanvasModel.noteMaxHeight / NotesCanvasModel.noteMinHeight, 2);
            double size = offset < 4 ? 4 : 8 - offset;

            ((Path)this.zoomControlStack.Children[0]).Data = Geometry.Parse(
                " M " + (8 - size) + " " + (offset + size) +
                " L 8 " + (offset).ToString() +
                " L " + (8 + size) + " " + (offset + size) +
                " M " + (8 - size) + " " + (16 - size - offset) +
                " L 8 " + (16 - offset) +
                " L " + (8 + size) + " " + (16 - size - offset));
        }

        private void zoomControl_Zoom(double delta)
        {
            double newNoteHeight = _nCM.noteHeight * (1.0 + delta);

            if (newNoteHeight < NotesCanvasModel.noteMinHeight)
                newNoteHeight = NotesCanvasModel.noteMinHeight;
            else if (newNoteHeight > NotesCanvasModel.noteMaxHeight)
                newNoteHeight = NotesCanvasModel.noteMaxHeight;

            _nCM.noteHeight = newNoteHeight;

            updateCanvas();
        }

        private void zoomControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double zoomSpeed = 0.001;
            zoomControl_Zoom(zoomSpeed * e.Delta);
        }

        private void zoomControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement el = (FrameworkElement)sender;
            el.CaptureMouse();

            _zoomDrag = true;
            _zoomDragLastX = e.GetPosition(el).X;
            _zoomDragLastY = e.GetPosition(el).Y;

            Mouse.OverrideCursor = Cursors.None;
        }

        private void zoomControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_zoomDrag)
            {
                ((FrameworkElement)sender).ReleaseMouseCapture();
                Mouse.OverrideCursor = Cursors.Arrow;
                _zoomDrag = false;
            }
        }

        private void zoomControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (_zoomDrag)
            {
                const double zoomSpeed = 0.002;

                bool cursorMoved = false;
                FrameworkElement el = (FrameworkElement)sender;

                zoomControl_Zoom(zoomSpeed * (_zoomDragLastY - e.GetPosition(el).Y));

                _navDragLastX = e.GetPosition(el).X;
                _navDragLastY = e.GetPosition(el).Y;

                // Restrict mouse position
                if (e.GetPosition(el).X < 0)
                {
                    cursorMoved = true;
                    _zoomDragLastX += el.ActualWidth;
                }
                else if (e.GetPosition(el).X > el.ActualWidth)
                {
                    cursorMoved = true;
                    _zoomDragLastX -= el.ActualWidth;
                }

                if (e.GetPosition(el).Y < 0)
                {
                    cursorMoved = true;
                    _zoomDragLastY += el.ActualHeight;
                }
                else if (e.GetPosition(el).Y > el.ActualHeight)
                {
                    cursorMoved = true;
                    _zoomDragLastY -= el.ActualHeight;
                }

                if (cursorMoved)
                {
                    setCursorPos(el.TransformToAncestor(this).Transform(new Point(_zoomDragLastX, _zoomDragLastY)));
                }
            }
        }

        private void zoomControlStack_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Path)zoomControlStack.Children[0]).Stroke = (SolidColorBrush)System.Windows.Application.Current.FindResource("ScrollBarBrushActive");
        }

        private void zoomControlStack_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Path)zoomControlStack.Children[0]).Stroke = (SolidColorBrush)System.Windows.Application.Current.FindResource("ScrollBarBrushNormal");
        }

        #endregion

        # region Horizontal Zoom Control

        private void horizontalZoom(double delta)
        {
            // TODO : Use mouse position as zoom center
            double newNoteWidth = _nCM.noteWidth * (1.0 + delta);

            if (newNoteWidth < NotesCanvasModel.noteMinWidth)
                newNoteWidth = NotesCanvasModel.noteMinWidth;
            else if (newNoteWidth > NotesCanvasModel.noteMaxWidth)
                newNoteWidth = NotesCanvasModel.noteMaxWidth;

            _nCM.noteWidth = newNoteWidth;

            updateCanvas();
        }

        private void timelineCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double zoomSpeed = 0.001;
            horizontalZoom(e.Delta * zoomSpeed);
        }

        # endregion

        # region Notes Vertical Scrollbar

        private void notesVerticalScroll_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            updateCanvas();
        }

        private void notesVerticalScroll_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.notesVerticalScroll.Value = this.notesVerticalScroll.Value - 0.01 * notesVerticalScroll.SmallChange * e.Delta;
            updateCanvas();
        }

        # endregion

        # region Horizontal Scrollbar

        private void horizontalScroll_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            updateCanvas();
        }

        private void horizontalScroll_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.horizontalScroll.Value = this.horizontalScroll.Value - 0.01 * horizontalScroll.SmallChange * e.Delta;
            updateCanvas();
        }

        # endregion

        #region Keys Action

        // TODO : keys mouse over, clicke, release, click and move

        private void keysCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void keysCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void keysCanvas_MouseMove(object sender, MouseEventArgs e)
        {
        }

        # endregion
    }
}
