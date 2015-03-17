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

        Nullable<Point> _dragStart = null;
        Rectangle _dragShape = null;

        // Canvas states
        NotesCanvasModel _nCM;

        List<TextBlock> _keyNames;
        List<Rectangle> _keys;
        List<Rectangle> _keyTracks;

        bool _snapPosition = true;
        bool _snapLength = true;

        List<Line> _tickLines;

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
            notesCanvas.DataContext = _nCM;

            _keyNames = new List<TextBlock>();
            _keys = new List<Rectangle>();
            _keyTracks = new List<Rectangle>();
            _tickLines = new List<Line>();

            for (int i = 0; i < NotesCanvasModel.numNotesHeight; i++)
            {
                _keys.Add(new Rectangle() { Fill = NotesCanvasModel.getNoteBackgroundBrush(i), Width = 48, Height = _nCM.noteHeight });
                keysCanvas.Children.Add(_keys[i]);
                _keyNames.Add(new TextBlock() { Text = _nCM.getNoteString(i), Foreground = NotesCanvasModel.getNoteBrush(i), Width = 42, TextAlignment = TextAlignment.Right, IsHitTestVisible = false});
                keysCanvas.Children.Add(_keyNames[i]);
                _keyTracks.Add(new Rectangle() { Fill = NotesCanvasModel.getNoteTrackBrush(i), Width = notesCanvas.ActualWidth, Height = _nCM.noteHeight, IsHitTestVisible = false});
                notesCanvas.Children.Add(_keyTracks[i]);
            }

            for (int i = 0; i < 64; i++)
            {
                _tickLines.Add(new Line() { Stroke = Brushes.Gray, StrokeThickness = 0.75, X1 = 0, Y1 = 0, X2 = 0, Y2 = 400, SnapsToDevicePixels = true });
                notesCanvas.Children.Add(_tickLines[i]);
            }

            updateZoomControl();
            updateCanvas();
        }

        private void updateCanvas()
        {
            notesVerticalScroll.ViewportSize = _nCM.getViewportSizeY(notesCanvas.ActualHeight);
            notesVerticalScroll.SmallChange = notesVerticalScroll.ViewportSize / 10;
            notesVerticalScroll.LargeChange = notesVerticalScroll.ViewportSize;

            //horizontalScroll.ViewportSize = _nCM.getViewportSize(notesCanvas.ActualHeight);
            horizontalScroll.SmallChange = horizontalScroll.ViewportSize / 10;
            horizontalScroll.LargeChange = horizontalScroll.ViewportSize;

            // TODO : Improve performance (Maybe?)
            for (int i = 0; i < _keyNames.Count; i++)
            {
                double notePosInView = _nCM.keyToCanvas(i, notesVerticalScroll.Value, notesCanvas.ActualHeight);
                Canvas.SetLeft(_keyNames[i], 0);
                Canvas.SetTop(_keyNames[i], notePosInView + (_nCM.noteHeight - 16) / 2);
                if (_nCM.noteHeight > 12) _keyNames[i].Visibility = System.Windows.Visibility.Visible;
                else _keyNames[i].Visibility = System.Windows.Visibility.Hidden;

                _keys[i].Height = _nCM.noteHeight;
                Canvas.SetLeft(_keys[i], 0);
                Canvas.SetTop(_keys[i], notePosInView);

                _keyTracks[i].Width = notesCanvas.ActualWidth;
                _keyTracks[i].Height = _nCM.noteHeight;
                Canvas.SetTop(_keyTracks[i], notePosInView);
            }

            for (int i = 0; i < 64; i++)
            {
                _tickLines[i].Y2 = notesCanvas.ActualHeight;
                Canvas.SetTop(_tickLines[i], 0);
                Canvas.SetLeft(_tickLines[i], (i + 1) * _nCM.noteWidth);
            }
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
            else {
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

        # region Notes Vertical Scrollbar

        private void notesVerticalScroll_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            updateCanvas();
        }

        private void notesVerticalScroll_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.notesVerticalScroll.Value = this.notesVerticalScroll.Value - 0.0002 * e.Delta;
            updateCanvas();
        }

        # endregion

        # region Horizontal Scrollbar

        private void horizontalScroll_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {

        }

        private void horizontalScroll_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        # endregion

        # region Note Canvas

        private void notesCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition((Canvas)sender);
            HitTestResult result = VisualTreeHelper.HitTest(notesCanvas, pt);

            if (result.VisualHit.GetType() == typeof(Rectangle) && ((UIElement)result.VisualHit).IsHitTestVisible)
            {
                _dragShape = (Rectangle)result.VisualHit;
                _dragStart = e.GetPosition(_dragShape);
            }
            else
            {
                var Rendershape = new Rectangle() { Fill = Brushes.Blue, Height = 45, Width = 45, RadiusX = 12, RadiusY = 12 };
                Canvas.SetLeft(Rendershape, e.GetPosition((Canvas)sender).X);
                Canvas.SetTop(Rendershape, e.GetPosition((Canvas)sender).Y);

                notesCanvas.Children.Add(Rendershape);
            }
        }

        private void notesCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition((Canvas)sender);
            HitTestResult result = VisualTreeHelper.HitTest(notesCanvas, pt);

            if (result != null && ((UIElement)result.VisualHit).IsHitTestVisible)
            {
                notesCanvas.Children.Remove(result.VisualHit as Shape);
            }
        }

        private void notesCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _dragStart = null;
            _dragShape = null;
        }

        private void notesCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragStart != null && e.LeftButton == MouseButtonState.Pressed && VisualTreeHelper.HitTest(notesCanvas, e.GetPosition((Canvas)sender)) != null)
            {
                var p2 = e.GetPosition(notesCanvas);
                Canvas.SetLeft(_dragShape, p2.X - _dragStart.Value.X);
                Canvas.SetTop(_dragShape, p2.Y - _dragStart.Value.Y);
            }
        }

        private void notesCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _nCM.viewPortHeight = notesCanvas.ActualHeight;
            _nCM.viewPortWidth = notesCanvas.ActualWidth;
            updateCanvas();
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
                // this.horizontalScroll.Value += navigateSpeed * (e.GetPosition(el).X - _navDragLastX);

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

            // Keep the center of viewport

            _nCM.noteHeight = newNoteHeight;

            updateZoomControl();
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

        private void timelineCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        # endregion

        private void keysCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            titleLabel.Content = _nCM.getNoteString(_nCM.canvasToKey(e.GetPosition(keysCanvas).Y, notesVerticalScroll.Value, keysCanvas.ActualHeight));
        }
    }
}
