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

            this.Width = Properties.Settings.Default.MainWidth;
            this.Height = Properties.Settings.Default.MainHeight;
            this.WindowState = Properties.Settings.Default.MainMaximized ? WindowState.Maximized : WindowState.Normal;

            ThemeManager.LoadTheme(); // TODO : move to program entry point

            this.CloseButtonClicked += (o, e) => { CmdExit(); };
            CompositionTargetEx.FrameUpdating += RenderLoop;

            viewScaler.Max = UIConstants.TrackMaxHeight;
            viewScaler.Min = UIConstants.TrackMinHeight;
            viewScaler.Value = UIConstants.TrackDefaultHeight;

            trackVM = (TracksViewModel)this.Resources["tracksVM"];
            trackVM.TrackCanvas = this.trackCanvas;

            CmdNewFile();
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

        bool _movePartElement = false;
        bool _resizePartElement = false;
        PartElement _hitPartElement;
        //double _partMoveStartMouseQuater;
        int _partMoveRelativeTick;
        int _partMoveStartTick;
        int _resizeMinDurTick;
        UPart _partMovePartLeft;
        UPart _partMovePartMin;
        UPart _partResizeShortest;

        private void trackCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_uiLocked) return;
            Point mousePos = e.GetPosition((UIElement)sender);

            var hit = VisualTreeHelper.HitTest(trackCanvas, mousePos).VisualHit;
            System.Diagnostics.Debug.WriteLine("Mouse hit " + hit.ToString());

            if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                selectionStart = new Point(trackVM.CanvasToQuarter(mousePos.X), trackVM.CanvasToTrack(mousePos.Y));

                if (Keyboard.IsKeyUp(Key.LeftShift) && Keyboard.IsKeyUp(Key.RightShift)) trackVM.DeselectAll();
                
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
            else if (hit is DrawingVisual)
            {
                PartElement partEl = ((DrawingVisual)hit).Parent as PartElement;
                _hitPartElement = partEl;

                if (!trackVM.SelectedParts.Contains(_hitPartElement.Part)) trackVM.DeselectAll();

                if (e.ClickCount == 2 && partEl is VoicePartElement) // load part into midi window
                {
                    LockUI();
                    if (midiWindow == null) midiWindow = new MidiWindow(this);
                    midiWindow.LoadPart((UVoicePart)partEl.Part, trackVM.Project);
                    midiWindow.Show();
                    midiWindow.Focus();
                    UnlockUI();
                }
                else if (mousePos.X > partEl.X + partEl.VisualWidth - UIConstants.ResizeMargin && partEl is VoicePartElement) // resize
                {
                    _resizePartElement = true;
                    _resizeMinDurTick = trackVM.GetPartMinDurTick(_hitPartElement.Part);
                    Mouse.OverrideCursor = Cursors.SizeWE;
                    if (trackVM.SelectedParts.Count > 0)
                    {
                        _partResizeShortest = _hitPartElement.Part;
                        foreach (UPart part in trackVM.SelectedParts)
                        {
                            if (part.DurTick - part.GetMinDurTick(trackVM.Project) <
                                _partResizeShortest.DurTick - _partResizeShortest.GetMinDurTick(trackVM.Project))
                                _partResizeShortest = part;
                        }
                    }
                }
                else // move
                {
                    _movePartElement = true;
                    _partMoveRelativeTick = trackVM.CanvasToSnappedTick(mousePos.X) - _hitPartElement.Part.PosTick;
                    _partMoveStartTick = partEl.Part.PosTick;
                    Mouse.OverrideCursor = Cursors.SizeAll;
                    if (trackVM.SelectedParts.Count > 0)
                    {
                        _partMovePartLeft = _partMovePartMin = _hitPartElement.Part;
                        foreach (UPart part in trackVM.SelectedParts)
                        {
                            if (part.PosTick < _partMovePartLeft.PosTick) _partMovePartLeft = part;
                            if (part.TrackNo < _partMovePartMin.TrackNo) _partMovePartMin = part;
                        }
                    }
                }
            }
            else
            {
                UVoicePart part = new UVoicePart()
                {
                    PosTick = trackVM.CanvasToSnappedTick(mousePos.X),
                    TrackNo = trackVM.CanvasToTrack(mousePos.Y),
                    DurTick = trackVM.Project.Resolution * 4 / trackVM.Project.BeatUnit * trackVM.Project.BeatPerBar
                };
                trackVM.AddPart(part);
                trackVM.MarkUpdate();
                // Enable drag
                trackVM.DeselectAll();
                _movePartElement = true;
                _hitPartElement = trackVM.GetPartElement(part);
                _partMoveRelativeTick = 0;
                _partMoveStartTick = part.PosTick;
            }
            ((UIElement)sender).CaptureMouse();
        }

        private void trackCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _movePartElement = false;
            _resizePartElement = false;
            _hitPartElement = null;
            // End selection
            selectionStart = null;
            if (selectionBox != null)
            {
                Canvas.SetZIndex(selectionBox, -100);
                selectionBox.Visibility = System.Windows.Visibility.Hidden;
            }
            trackVM.DoneTempSelect();
            trackVM.UpdateViewSize();
            ((UIElement)sender).ReleaseMouseCapture();
            Mouse.OverrideCursor = null;
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
                trackVM.TempSelectInBox(selectionStart.Value.X, trackVM.CanvasToQuarter(mousePos.X), (int)selectionStart.Value.Y, trackVM.CanvasToTrack(mousePos.Y));
            }
            else if (_movePartElement) // move
            {
                if (trackVM.SelectedParts.Count == 0)
                {
                    _hitPartElement.Part.TrackNo = Math.Max(0, trackVM.CanvasToTrack(mousePos.Y));
                    _hitPartElement.Part.PosTick = Math.Max(0, (int)(trackVM.Project.Resolution * trackVM.CanvasToSnappedQuarter(mousePos.X)) - _partMoveRelativeTick);
                    trackVM.MarkUpdate();
                }
                else
                {
                    int deltaTrack = trackVM.CanvasToTrack(mousePos.Y) - _hitPartElement.Part.TrackNo;
                    int deltaPosTick = (int)(trackVM.Project.Resolution * trackVM.CanvasToSnappedQuarter(mousePos.X) - _partMoveRelativeTick) - _hitPartElement.Part.PosTick;

                    if (deltaTrack + _partMovePartMin.TrackNo >= 0)
                        foreach (UPart part in trackVM.SelectedParts) part.TrackNo += deltaTrack;

                    if (deltaPosTick + _partMovePartLeft.PosTick >= 0)
                        foreach (UPart part in trackVM.SelectedParts) part.PosTick += deltaPosTick;

                    trackVM.MarkUpdate();
                }
            }
            else if (_resizePartElement) // resize
            {
                if (trackVM.SelectedParts.Count == 0)
                {
                    int newDurTick = (int)(trackVM.Project.Resolution * trackVM.CanvasRoundToSnappedQuarter(mousePos.X)) - _hitPartElement.Part.PosTick;
                    if (newDurTick > _resizeMinDurTick)
                    {
                        _hitPartElement.Part.DurTick = newDurTick;
                        trackVM.MarkUpdate();
                    }
                }
                else
                {
                    int deltaDurTick = (int)(trackVM.CanvasRoundToSnappedQuarter(mousePos.X) * trackVM.Project.Resolution) - _hitPartElement.Part.EndTick;
                    if (_partResizeShortest.DurTick + deltaDurTick >= _partResizeShortest.GetMinDurTick(trackVM.Project))
                        foreach (UPart part in trackVM.SelectedParts) part.DurTick += deltaDurTick;
                    trackVM.MarkUpdate();
                }
            }
            else if (Mouse.RightButton == MouseButtonState.Pressed) // Remove Note
            {
                HitTestResult result = VisualTreeHelper.HitTest(trackCanvas, mousePos);
                if (result == null) return;
                var hit = result.VisualHit;
                if (hit is DrawingVisual)
                {
                    PartElement partEl = ((DrawingVisual)hit).Parent as PartElement;
                    if (partEl != null) trackVM.RemovePart(partEl);
                }
            }
            else if (Mouse.LeftButton == MouseButtonState.Released && Mouse.RightButton == MouseButtonState.Released)
            {
                HitTestResult result = VisualTreeHelper.HitTest(trackCanvas, mousePos);
                if (result == null) return;
                var hit = result.VisualHit;
                if (hit is DrawingVisual)
                {
                    PartElement partEl = ((DrawingVisual)hit).Parent as PartElement;
                    if (mousePos.X > partEl.X + partEl.VisualWidth - UIConstants.ResizeMargin && partEl is VoicePartElement) Mouse.OverrideCursor = Cursors.SizeWE;
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
            if (_uiLocked) return;
            FocusManager.SetFocusedElement(this, null);
            Point mousePos = e.GetPosition((Canvas)sender);
            HitTestResult result = VisualTreeHelper.HitTest(trackCanvas, mousePos);
            if (result == null) return;
            var hit = result.VisualHit;
            if (hit is DrawingVisual)
            {
                PartElement partEl = ((DrawingVisual)hit).Parent as PartElement;
                if (partEl != null && trackVM.SelectedParts.Contains(partEl.Part)) trackVM.RemovePart(partEl);
                else trackVM.DeselectAll();
            }
            else
            {
                trackVM.DeselectAll();
            }
            ((UIElement)sender).CaptureMouse();
            Mouse.OverrideCursor = Cursors.No;
        }

        private void trackCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            trackVM.UpdateViewSize();
            Mouse.OverrideCursor = null;
            ((UIElement)sender).ReleaseMouseCapture();
        }

        # endregion

        # region menu commands

        private void MenuNew_Click(object sender, RoutedEventArgs e) { CmdNewFile(); }
        private void MenuOpen_Click(object sender, RoutedEventArgs e) { CmdOpenFileDialog(); }
        private void MenuExit_Click(object sender, RoutedEventArgs e) { CmdExit(); }

        private void MenuImportAidio_Click(object sender, RoutedEventArgs e)
        {
            LockUI();
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "Audio Files|*.*",
                Multiselect = false,
                CheckFileExists = true
            };
            if (openFileDialog.ShowDialog() == true) CmdImportAudio(openFileDialog.FileName);
            UnlockUI();
        }

        private void Menu_OpenMidiEditor(object sender, RoutedEventArgs e)
        {
            if (midiWindow == null) midiWindow = new MidiWindow(this);
            midiWindow.Show();
            midiWindow.Focus();
        }

        # endregion

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (_uiLocked) return;
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.F4) CmdExit();
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O) CmdOpenFileDialog();
        }

        # region application commmands

        private void CmdNewFile()
        {
            CmdCloseFile();
            uproject = new UProject();
            trackVM.LoadProject(uproject);
        }

        private void CmdCloseFile()
        {
            if (midiWindow != null) midiWindow.UnloadPart();
            trackVM.UnloadProject();
        }

        private void CmdOpenFileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "Project Files|*.ustx; *.vsqx; *.ust|All Files|*.*",
                Multiselect = true,
                CheckFileExists = true
            };
            if (openFileDialog.ShowDialog() == true) CmdOpenFile(openFileDialog.FileNames);
        }

        private void CmdOpenFile(string[] files)
        {
            if (midiWindow != null) midiWindow.UnloadPart();
            LockUI();

            if (files.Length == 1)
            {
                uproject = OpenUtau.Core.Formats.Formats.LoadProject(files[0]);
            }
            else if (files.Length > 1)
            {
                uproject = OpenUtau.Core.Formats.Ust.Load(files);
            }

            if (uproject != null)
            {
                trackVM.LoadProject(uproject);
                Title = trackVM.Title;
            }

            UnlockUI();
        }

        private void CmdImportAudio(string file)
        {
            LockUI();
            UWavePart uwavepart = OpenUtau.Core.Formats.Sound.CreateUWavePart(file);
            if (uwavepart != null)
            {
                uproject.Tracks.Add(new UTrack());
                uwavepart.TrackNo = uproject.Tracks.Count - 1;
                uwavepart.DurTick = uwavepart.GetMinDurTick(uproject);
                uproject.Parts.Add(uwavepart);
                trackVM.AddPart(uwavepart);
            }
            UnlockUI();
        }

        private void CmdExit()
        {
            Properties.Settings.Default.MainMaximized = this.WindowState == System.Windows.WindowState.Maximized;
            Properties.Settings.Default.Save();
            Application.Current.Shutdown();
        }

        # endregion

        private void navigateDrag_NavDrag(object sender, EventArgs e)
        {
            trackVM.OffsetX += ((NavDragEventArgs)e).X * trackVM.SmallChangeX;
            trackVM.OffsetY += ((NavDragEventArgs)e).Y * trackVM.SmallChangeY * 0.2;
            trackVM.MarkUpdate();
        }

        public void UpdatePartElement(UPart part)
        {
            trackVM.UpdatePartElement(part);
        }

        private void trackCanvas_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
        }

        private void trackCanvas_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            CmdOpenFile(files);
        }

        bool _uiLocked = false;
        private void LockUI() { _uiLocked = true; Mouse.OverrideCursor = Cursors.AppStarting; }
        private void UnlockUI() { _uiLocked = false; Mouse.OverrideCursor = null; }

        private void trackCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                timelineCanvas_MouseWheel(sender, e);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                trackVM.OffsetX -= trackVM.ViewWidth * 0.001 * e.Delta;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
            }
            else
            {
                trackVM.OffsetY -= trackVM.ViewHeight * 0.001 * e.Delta;
            }
        }

    }
}
