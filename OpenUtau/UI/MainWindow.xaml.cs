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
        MidiWindow midiWindow;
        UProject uproject;
        TracksViewModel trackVM;

        public MainWindow()
        {
            InitializeComponent();

            ThemeManager.LoadTheme(); // TODO : move to program entry point

            this.CloseButtonClicked += (o, e) => { CmdExit(); };
            CompositionTargetEx.FrameUpdating += RenderLoop;

            viewScaler.Max = UIConstants.TrackMaxHeight;
            viewScaler.Min = UIConstants.TrackMinHeight;
            viewScaler.Value = UIConstants.TrackDefaultHeight;

            trackVM = (TracksViewModel)this.Resources["tracksVM"];
            trackVM.TrackCanvas = this.trackCanvas;
        }

        void RenderLoop(object sender, EventArgs e)
        {
            tickBackground.RenderIfUpdated();
            timelineBackground.RenderIfUpdated();
            trackBackground.RenderIfUpdated();
            trackVM.RedrawIfUpdated();
        }

        # region Timeline Canvas

        private void timelineCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double zoomSpeed = 0.0012;
            Point mousePos = e.GetPosition((UIElement)sender);
            double zoomCenter;
            if (trackVM.OffsetX == 0 && mousePos.X < 128) zoomCenter = 0;
            else zoomCenter = (trackVM.OffsetX + mousePos.X) / trackVM.QuarterWidth;
            trackVM.QuarterWidth *= 1 + e.Delta * zoomSpeed;
            trackVM.OffsetX = Math.Max(0, Math.Min(trackVM.TotalWidth, zoomCenter * trackVM.QuarterWidth - mousePos.X));
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

        # region track canvas

        Rectangle selectionBox;
        Nullable<Point> selectionStart;

        bool _moveThumbnail = false;
        bool _resizeThumbnail = false;
        double _partMoveStartMouseQuater;
        int _partMoveStartTick;
        Point _mouseDownPos;
        PartThumbnail _hitThumbnail;
        List<PartThumbnail> selectedParts = new List<PartThumbnail>();

        private void trackCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition((UIElement)sender);

            var hit = VisualTreeHelper.HitTest(trackCanvas, mousePos).VisualHit;
            System.Diagnostics.Debug.WriteLine("Mouse hit " + hit.ToString());

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                selectionStart = new Point(trackVM.CanvasToQuarter(mousePos.X), trackVM.CanvasToTrack(mousePos.Y));
                if (Keyboard.IsKeyUp(Key.LeftShift) && Keyboard.IsKeyUp(Key.RightShift))
                {
                    trackVM.DeselectAll();
                }
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
                    trackCanvas.Children.Add(selectionBox);
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
            else if (hit is PartThumbnail)
            {
                PartThumbnail thumb = hit as PartThumbnail;
                _hitThumbnail = thumb;
                _mouseDownPos = mousePos;
                if (e.ClickCount == 2) // load part into midi window
                {
                    if (midiWindow == null) midiWindow = new MidiWindow();
                    midiWindow.Show();
                    midiWindow.LoadPart(thumb.Part, trackVM.Project);
                    midiWindow.Focus();
                }
                else if (mousePos.X > thumb.X + thumb.DisplayWidth - UIConstants.ResizeMargin) // resize
                {
                    _resizeThumbnail = true;
                    Mouse.OverrideCursor = Cursors.SizeWE;
                }
                else // move
                {
                    _moveThumbnail = true;
                    _partMoveStartMouseQuater = trackVM.CanvasToSnappedQuarter(mousePos.X);
                    _partMoveStartTick = thumb.Part.PosTick;
                    Mouse.OverrideCursor = Cursors.SizeAll;
                }
            }
            ((UIElement)sender).CaptureMouse();
        }

        private void trackCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _moveThumbnail = false;
            _resizeThumbnail = false;
            _hitThumbnail = null;
            selectionStart = null;
            if (selectionBox != null)
            {
                Canvas.SetZIndex(selectionBox, -100);
                selectionBox.Visibility = System.Windows.Visibility.Hidden;
            }
            ((UIElement)sender).ReleaseMouseCapture();
            Mouse.OverrideCursor = null;
            // TODO : Reload midiwindow if part moved
        }

        private void trackCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition((UIElement)sender);
            trackCanvas_MouseMove_Helper(mousePos);
        }

        private void trackCanvas_MouseMove_Helper(Point mousePos)
        {

            if (selectionStart != null) // Selection
            {
                double bottom = trackVM.TrackToCanvas(Math.Max(trackVM.CanvasToTrack(mousePos.Y), (int)selectionStart.Value.Y) + 1);
                double top = trackVM.TrackToCanvas(Math.Min(trackVM.CanvasToTrack(mousePos.Y), (int)selectionStart.Value.Y));
                double left = Math.Min(mousePos.X, trackVM.QuarterToCanvas(selectionStart.Value.X));
                selectionBox.Width = Math.Abs(mousePos.X - trackVM.QuarterToCanvas(selectionStart.Value.X));
                selectionBox.Height = bottom - top;
                Canvas.SetLeft(selectionBox, left);
                Canvas.SetTop(selectionBox, top);
                //ncModel.trackPart.SelectTempInBox(
                //    ncModel.canvasToOffset(mousePos.X),
                //    selectionStart.Value.X,
                //    ncModel.snapNoteKey(mousePos.Y),
                //    selectionStart.Value.Y);
            }
            else if (_moveThumbnail) // move
            {
                if (selectedParts.Count == 0)
                {
                    _hitThumbnail.Part.TrackNo = trackVM.CanvasToTrack(mousePos.Y);
                    _hitThumbnail.Part.PosTick = Math.Max(0, _partMoveStartTick +
                        (int)(trackVM.Project.Resolution * (trackVM.CanvasToSnappedQuarter(mousePos.X) - _partMoveStartMouseQuater)));
                    trackVM.MarkUpdate();
                }
                else
                {
                }
            }
            else if (_resizeThumbnail) // resize
            {
                if (selectedParts.Count == 0)
                {
                    int newDurTick = (int)(trackVM.Project.Resolution * trackVM.CanvasRoundToSnappedQuarter(mousePos.X)) - _hitThumbnail.Part.PosTick;
                    if (_hitThumbnail.Part.ValidateDurTick(newDurTick)) _hitThumbnail.Part.DurTick = newDurTick;
                    trackVM.MarkUpdate();
                }
                else
                {
                }
            }
            else
            {
                HitTestResult result = VisualTreeHelper.HitTest(trackCanvas, mousePos);
                if (result == null) return;
                var hit = result.VisualHit;
                if (hit is PartThumbnail)
                {
                    PartThumbnail thumb = hit as PartThumbnail;
                    if (mousePos.X > thumb.X + thumb.DisplayWidth - UIConstants.ResizeMargin) Mouse.OverrideCursor = Cursors.SizeWE;
                    else Mouse.OverrideCursor = null;
                }
                else
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }

        private void trackCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

            ((UIElement)sender).CaptureMouse();
        }

        private void trackCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

            ((UIElement)sender).ReleaseMouseCapture();
        }

        # endregion

        # region menu commands

        private void MenuOpen_Click(object sender, RoutedEventArgs e) { CmdOpenFile(); }
        private void MenuExit_Click(object sender, RoutedEventArgs e) { CmdExit(); }

        private void Menu_OpenMidiEditor(object sender, RoutedEventArgs e)
        {
            if (midiWindow == null) midiWindow = new MidiWindow();
            midiWindow.Show();
            midiWindow.Focus();
        }

        # endregion

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.F4) CmdExit();
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O) CmdOpenFile();
        }

        # region application commmands

        private void CmdOpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                uproject = OpenUtau.Core.Formats.VSQx.Load(openFileDialog.FileName);
                if (uproject != null) trackVM.LoadProject(uproject);
            }
        }

        private void CmdExit()
        {
            Application.Current.Shutdown();
        }

        # endregion

        private void navigateDrag_NavDrag(object sender, EventArgs e)
        {
            trackVM.OffsetX += ((NavDragEventArgs)e).X * trackVM.SmallChangeX * 5;
            trackVM.OffsetY += ((NavDragEventArgs)e).Y * trackVM.SmallChangeY * 5;
            trackVM.MarkUpdate();
        }
    }
}
