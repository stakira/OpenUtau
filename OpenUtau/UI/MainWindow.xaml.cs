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
        TracksViewModel trackVM;
        ProgressBarViewModel progVM;

        public MainWindow()
        {
            InitializeComponent();

            this.Width = Properties.Settings.Default.MainWidth;
            this.Height = Properties.Settings.Default.MainHeight;
            this.WindowState = Properties.Settings.Default.MainMaximized ? WindowState.Maximized : WindowState.Normal;

            ThemeManager.LoadTheme(); // TODO : move to program entry point

            progVM = this.Resources["progVM"] as ProgressBarViewModel;
            progVM.Subscribe(DocManager.Inst);
            progVM.Foreground = ThemeManager.NoteFillBrushes[0];

            this.CloseButtonClicked += (o, e) => { CmdExit(); };
            CompositionTargetEx.FrameUpdating += RenderLoop;

            viewScaler.Max = UIConstants.TrackMaxHeight;
            viewScaler.Min = UIConstants.TrackMinHeight;
            viewScaler.Value = UIConstants.TrackDefaultHeight;

            trackVM = this.Resources["tracksVM"] as TracksViewModel;
            trackVM.TimelineCanvas = this.timelineCanvas;
            trackVM.TrackCanvas = this.trackCanvas;
            trackVM.HeaderCanvas = this.headerCanvas;
            trackVM.Subscribe(DocManager.Inst);

            CmdNewFile();

            if (UpdateChecker.Check())
                this.mainMenu.Items.Add(new MenuItem()
                {
                    Header = @"Update available",
                    Foreground = ThemeManager.WhiteKeyNameBrushNormal
                });
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
            Point mousePos = e.GetPosition((UIElement)sender);
            int tick = (int)(trackVM.CanvasToSnappedQuarter(mousePos.X) * trackVM.Project.Resolution);
            DocManager.Inst.ExecuteCmd(new SetPlayPosTickNotification(Math.Max(0, tick)));
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
                int tick = (int)(trackVM.CanvasToSnappedQuarter(mousePos.X) * trackVM.Project.Resolution);
                if (trackVM.playPosTick != tick)
                    DocManager.Inst.ExecuteCmd(new SetPlayPosTickNotification(Math.Max(0, tick)));
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
        int _partMoveRelativeTick;
        int _partMoveStartTick;
        int _resizeMinDurTick;
        UPart _partMovePartLeft;
        UPart _partMovePartMin;
        UPart _partResizeShortest;

        private void trackCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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
                        Fill = ThemeManager.BarNumberBrush,
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

                if (e.ClickCount == 2)
                {
                    if (partEl is VoicePartElement) // load part into midi window
                    {
                        if (midiWindow == null) midiWindow = new MidiWindow();
                        DocManager.Inst.ExecuteCmd(new LoadPartNotification(partEl.Part, trackVM.Project));
                        midiWindow.Show();
                        midiWindow.Focus();
                    }
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
                        _resizeMinDurTick = _partResizeShortest.GetMinDurTick(trackVM.Project);
                    }
                    DocManager.Inst.StartUndoGroup();
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
                    DocManager.Inst.StartUndoGroup();
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
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new AddPartCommand(trackVM.Project, part));
                DocManager.Inst.EndUndoGroup();
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
            DocManager.Inst.EndUndoGroup();
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
                    int newTrackNo = Math.Max(0, trackVM.CanvasToTrack(mousePos.Y));
                    int newPosTick = Math.Max(0, (int)(trackVM.Project.Resolution * trackVM.CanvasToSnappedQuarter(mousePos.X)) - _partMoveRelativeTick);
                    if (newTrackNo != _hitPartElement.Part.TrackNo || newPosTick != _hitPartElement.Part.PosTick)
                        DocManager.Inst.ExecuteCmd(new MovePartCommand(trackVM.Project, _hitPartElement.Part, newPosTick, newTrackNo));
                }
                else
                {
                    int deltaTrackNo = trackVM.CanvasToTrack(mousePos.Y) - _hitPartElement.Part.TrackNo;
                    int deltaPosTick = (int)(trackVM.Project.Resolution * trackVM.CanvasToSnappedQuarter(mousePos.X) - _partMoveRelativeTick) - _hitPartElement.Part.PosTick;
                    bool changeTrackNo = deltaTrackNo + _partMovePartMin.TrackNo >= 0;
                    bool changePosTick = deltaPosTick + _partMovePartLeft.PosTick >= 0;
                    if (changeTrackNo || changePosTick)
                        foreach (UPart part in trackVM.SelectedParts)
                            DocManager.Inst.ExecuteCmd(new MovePartCommand(trackVM.Project, part,
                                changePosTick ? part.PosTick + deltaPosTick : part.PosTick,
                                changeTrackNo ? part.TrackNo + deltaTrackNo : part.TrackNo));
                }
            }
            else if (_resizePartElement) // resize
            {
                if (trackVM.SelectedParts.Count == 0)
                {
                    int newDurTick = (int)(trackVM.Project.Resolution * trackVM.CanvasRoundToSnappedQuarter(mousePos.X)) - _hitPartElement.Part.PosTick;
                    if (newDurTick > _resizeMinDurTick && newDurTick != _hitPartElement.Part.DurTick)
                        DocManager.Inst.ExecuteCmd(new ResizePartCommand(trackVM.Project, _hitPartElement.Part, newDurTick));
                }
                else
                {
                    int deltaDurTick = (int)(trackVM.CanvasRoundToSnappedQuarter(mousePos.X) * trackVM.Project.Resolution) - _hitPartElement.Part.EndTick;
                    if (deltaDurTick != 0 && _partResizeShortest.DurTick + deltaDurTick > _resizeMinDurTick)
                        foreach (UPart part in trackVM.SelectedParts)
                            DocManager.Inst.ExecuteCmd(new ResizePartCommand(trackVM.Project, part, part.DurTick + deltaDurTick));
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
                    if (partEl != null) DocManager.Inst.ExecuteCmd(new RemovePartCommand(trackVM.Project, partEl.Part));
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
            FocusManager.SetFocusedElement(this, null);
            DocManager.Inst.StartUndoGroup();
            Point mousePos = e.GetPosition((Canvas)sender);
            HitTestResult result = VisualTreeHelper.HitTest(trackCanvas, mousePos);
            if (result == null) return;
            var hit = result.VisualHit;
            if (hit is DrawingVisual)
            {
                PartElement partEl = ((DrawingVisual)hit).Parent as PartElement;
                if (partEl != null && trackVM.SelectedParts.Contains(partEl.Part))
                    DocManager.Inst.ExecuteCmd(new RemovePartCommand(trackVM.Project, partEl.Part));
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
            DocManager.Inst.EndUndoGroup();
        }

        # endregion

        # region menu commands

        private void MenuNew_Click(object sender, RoutedEventArgs e) { CmdNewFile(); }
        private void MenuOpen_Click(object sender, RoutedEventArgs e) { CmdOpenFileDialog(); }
        private void MenuSave_Click(object sender, RoutedEventArgs e) { CmdSaveFile(); }
        private void MenuExit_Click(object sender, RoutedEventArgs e) { CmdExit(); }
        private void MenuUndo_Click(object sender, RoutedEventArgs e) { DocManager.Inst.Undo(); }
        private void MenuRedo_Click(object sender, RoutedEventArgs e) { DocManager.Inst.Redo(); }

        private void MenuImportAidio_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "Audio Files|*.*",
                Multiselect = false,
                CheckFileExists = true
            };
            if (openFileDialog.ShowDialog() == true) CmdImportAudio(openFileDialog.FileName);
        }

        private void MenuSingers_Click(object sender, RoutedEventArgs e)
        {
            var w = new Dialogs.SingerViewDialog();
            w.ShowDialog();
        }

        private void MenuRenderAll_Click(object sender, RoutedEventArgs e)
        {
            var ri = new OpenUtau.Core.Render.ResamplerInterface();
            ri.RenderAll(DocManager.Inst.Project);
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            string text = "OpenUtau is a free singing software synthesizer workstation, "
                        + "designed and developed by Sugita Akira, "
                        + "aiming to bring modern user experience, "
                        + "including smooth editing and intellegent phonology support "
                        + "to singing software synthesizer community."
                        + "\n\nOpenUtau is an open source software under the MIT Licence. Visit us on GitHub.";
            MessageBox.Show(text, "About OpenUtau", MessageBoxButton.OK, MessageBoxImage.None);
        }

        private void MenuPrefs_Click(object sender, RoutedEventArgs e)
        {
            var w = new Dialogs.PreferencesDialog();
            w.ShowDialog();
        }

        # endregion

        // Disable system menu and main menu
        protected override void OnKeyDown(KeyEventArgs e)
        {
            Window_KeyDown(this, e);
            e.Handled = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == 0 && e.Key == Key.Delete)
            {
                DocManager.Inst.StartUndoGroup();
                while (trackVM.SelectedParts.Count > 0) DocManager.Inst.ExecuteCmd(new RemovePartCommand(trackVM.Project, trackVM.SelectedParts.Last()));
                DocManager.Inst.EndUndoGroup();
            }
            else if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.F4) CmdExit();
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O) CmdOpenFileDialog();
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
            {
                trackVM.DeselectAll();
                DocManager.Inst.Undo();
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Y)
            {
                trackVM.DeselectAll();
                DocManager.Inst.Redo();
            }
        }

        # region application commmands

        private void CmdNewFile()
        {
            DocManager.Inst.ExecuteCmd(new LoadProjectNotification(OpenUtau.Core.Formats.USTx.Create()));
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
            if (files.Length == 1)
            {
                OpenUtau.Core.Formats.Formats.LoadProject(files[0]);
            }
            else if (files.Length > 1)
            {
                OpenUtau.Core.Formats.Ust.Load(files);
            }
        }

        private void CmdSaveFile()
        {
            if (DocManager.Inst.Project.Saved == false)
            {
                SaveFileDialog dialog = new SaveFileDialog() { DefaultExt = "ustx", Filter = "Project Files|*.ustx", Title = "Save File" };
                if (dialog.ShowDialog() == true)
                {
                    DocManager.Inst.ExecuteCmd(new SaveProjectNotification(dialog.FileName));
                }
            }
            else
            {
                DocManager.Inst.ExecuteCmd(new SaveProjectNotification(""));
            }
        }

        private void CmdImportAudio(string file)
        {
            UWavePart uwavepart = OpenUtau.Core.Formats.Sound.CreateUWavePart(file, (part) =>
            {
                trackVM.UpdatePartElement(part);
                trackVM.GetPartElement(part).VisualHeight = 1; // for force redraw
                trackVM.MarkUpdate();
            });
            if (uwavepart != null)
            {
                int _trackNo = trackVM.Project.Tracks.Count;
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new AddTrackCommand(trackVM.Project, new UTrack() { TrackNo = _trackNo }));
                DocManager.Inst.EndUndoGroup();
                uwavepart.TrackNo = _trackNo;
                uwavepart.DurTick = uwavepart.GetMinDurTick(trackVM.Project);
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new AddPartCommand(trackVM.Project, uwavepart));
                DocManager.Inst.EndUndoGroup();
            }
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

        private void trackCanvas_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
        }

        private void trackCanvas_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            CmdOpenFile(files);
        }

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
                verticalScroll.Value -= verticalScroll.SmallChange * e.Delta / 100;
                verticalScroll.Value = Math.Min(verticalScroll.Maximum, Math.Max(verticalScroll.Minimum, verticalScroll.Value));
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (trackVM != null) trackVM.MarkUpdate();
        }
    }
}
