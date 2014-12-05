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

        Nullable<Point> _navDragStart = null;

        // Canvas states
        NotesCanvasModel _nCM;

        List<TextBlock> _keyNames;
        List<Rectangle> _keys;
        List<Rectangle> _keyTracks;

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
            for (int i = 0; i < NotesCanvasModel.numNotesHeight; i++)
            {
                _keys.Add(new Rectangle() { Fill = NotesCanvasModel.getNoteBackgroundBrush(i), Width = 48, Height = _nCM.noteHeight });
                keysCanvas.Children.Add(_keys[i]);
                _keyNames.Add(new TextBlock() { Text = _nCM.getNoteString(i), Foreground = NotesCanvasModel.getNoteBrush(i), Width = 42, TextAlignment = TextAlignment.Right, IsHitTestVisible = false});
                keysCanvas.Children.Add(_keyNames[i]);
                _keyTracks.Add(new Rectangle() { Fill = NotesCanvasModel.getNoteTrackBrush(i), Width = notesCanvas.ActualWidth, Height = _nCM.noteHeight, IsHitTestVisible = false});
                notesCanvas.Children.Add(_keyTracks[i]);
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

        #region Window button event handlers

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

        private void notesVerticalScroll_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            double current_note_size = 24;
            double current_note_window_height = this.notesCanvas.ActualHeight;
            double current_position = (1.0 - notesVerticalScroll.Value) * (12.0 * 11.0 * current_note_size - current_note_window_height);
            UpdateCanvas();
        }

        private void notesVerticalScroll_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.notesVerticalScroll.Value = this.notesVerticalScroll.Value - 0.0002 * e.Delta;
            UpdateCanvas();
        }

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
            _navDragStart = null;
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
            UpdateCanvas();
        }

        private void UpdateCanvas()
        {
            for (int i = 0; i < _keyNames.Count; i++)
            {
                double notePosInView = _nCM.getNotePosInView(i, _nCM.verticalValToPos(notesVerticalScroll.Value));
                Canvas.SetLeft(_keyNames[i], 0);
                Canvas.SetTop(_keyNames[i], notePosInView + 3);

                _keys[i].Height = _nCM.noteHeight;
                Canvas.SetLeft(_keys[i], 0);
                Canvas.SetTop(_keys[i], notePosInView);

                _keyTracks[i].Width = notesCanvas.ActualWidth;
                _keyTracks[i].Height = _nCM.noteHeight;
                Canvas.SetTop(_keyTracks[i], notePosInView);
            }
        }

        private void navigateDrag_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _navDragStart = e.GetPosition(this);
            titleLabel.Content = e.GetPosition(this).ToString();
        }

        private void zoomDrag_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            titleLabel.Content = e.GetPosition(this).ToString();
        }
    }
}
