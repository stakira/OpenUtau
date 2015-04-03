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
using Microsoft.Win32;

using OpenUtau.UI.Models;
using OpenUtau.UI.Controls;
using OpenUtau.Core;
using OpenUtau.Core.USTx;

namespace OpenUtau.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : BorderlessWindow
    {
        // Window states
        WindowChrome _chrome;
        Thickness _activeBorderThickness;
        Thickness _inactiveBorderThickness;

        MidiWindow _midiWindow;

        UProject uproject;

        PartThumbnail thumb;

        public MainWindow()
        {
            InitializeComponent();
            _chrome = new WindowChrome();
            WindowChrome.SetWindowChrome(this, _chrome);
            _chrome.GlassFrameThickness = new Thickness(1);
            _chrome.CornerRadius = new CornerRadius(0);
            _chrome.CaptionHeight = 0;
            //_activeBorderThickness = this.canvasBorder.BorderThickness;
            _inactiveBorderThickness = new Thickness(1);

            ThemeManager.LoadTheme();

            thumb = new PartThumbnail();
            trackCanvas.Children.Add(thumb);

            this.CloseButtonClicked += delegate(object sender, EventArgs e) { Exit(); };
        }

        # region Splitter

        private void GridSplitter_MouseEnter(object sender, MouseEventArgs e)
        {
            //Mouse.OverrideCursor = Cursors.SizeNS;
        }

        private void GridSplitter_MouseLeave(object sender, MouseEventArgs e)
        {
            //Mouse.OverrideCursor = null;
        }

        # endregion

        #region Vertical Zoom Control

        #endregion

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

                this.verticalScroll.Value += navigateSpeed * (e.GetPosition(el).Y - _navDragLastY);
                this.horizontalScroll.Value += navigateSpeed * (e.GetPosition(el).X - _navDragLastX);

                //ncModel.updateGraphics();

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

        # region Vertical Scrollbar

        private void verticalScroll_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            //ncModel.updateGraphics();
        }

        private void verticalScroll_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.verticalScroll.Value = this.verticalScroll.Value - 0.01 * verticalScroll.SmallChange * e.Delta;
            //ncModel.updateGraphics();
        }

        # endregion

        # region Timeline Canvas

        double scale = 1;
        double move = 0;

        private void timelineCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double zoomSpeed = 0.0012;
            scale += scale * zoomSpeed * e.Delta;
            move += 0.1 * e.Delta;
            //titleLabel.Text = move.ToString();
            thumb.ScaleX = scale;
        }

        private void timelineCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                uproject = OpenUtau.Core.Formats.VSQx.Load(openFileDialog.FileName);
                if (uproject != null)
                {
                    if (_midiWindow == null) _midiWindow = new MidiWindow();
                    _midiWindow.Show();
                    _midiWindow.LoadPart(uproject.Tracks[0].Parts[0]);
                    thumb.Brush = ThemeManager.NoteFillBrushes[0];
                    thumb.UPart = uproject.Tracks[0].Parts[0];
                    thumb.Redraw();
                }
            }
        }
        
        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Exit();
        }

        private void Exit()
        {
            if (_midiWindow != null) 
                if(_midiWindow.IsLoaded)
                    _midiWindow.Close();
            Close();
        }

        private void trackCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition((UIElement)sender);
            var hit = VisualTreeHelper.HitTest(trackCanvas, mousePos).VisualHit;
            //titleLabel.Text = hit.ToString();
        }
    }
}
