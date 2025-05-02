using System.Threading.Tasks;
using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using OpenUtau.App.ViewModels;
using OpenUtau.App.Views;
using OpenUtau.Core.Ustx;
using OpenUtau.Core;
using ReactiveUI;
using Serilog;
using System.Reactive;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia;
using OpenUtau.App.Controls;
using OpenUtau.App;
using OpenUtau.Classic;
using OpenUtau.Core.Analysis.Some;
using OpenUtau.Core.DiffSinger;
using OpenUtau.Core.Format;
using OpenUtau.Core.Util;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;

namespace OpenUtau.Views;

public partial class MainView : UserControl, ICmdSubscriber {
    private readonly MainViewModel viewModel;

    //private PianoRollWindow? pianoRollWindow;
    //private bool openPianoRollWindow;

    private PartEditState? partEditState;
    private readonly DispatcherTimer timer;
    private readonly DispatcherTimer autosaveTimer;
    private bool forceClose;

    private bool shouldOpenPartsContextMenu;

    private readonly ReactiveCommand<UPart, Unit> PartRenameCommand;
    private readonly ReactiveCommand<UPart, Unit> PartGotoFileCommand;
    private readonly ReactiveCommand<UPart, Unit> PartReplaceAudioCommand;
    private readonly ReactiveCommand<UPart, Unit> PartTranscribeCommand;

    public MainView() {
        Log.Information("Creating main window.");
        InitializeComponent();
        Log.Information("Initialized main window component.");
        DataContext = viewModel = new MainViewModel();

        viewModel.InitProject();

        timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(15),
            DispatcherPriority.Normal,
            (sender, args) => PlaybackManager.Inst.UpdatePlayPos());
        timer.Start();

        autosaveTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(30),
            DispatcherPriority.Normal,
            (sender, args) => DocManager.Inst.AutoSave());
        autosaveTimer.Start();

        DocManager.Inst.AddSubscriber(this);
    }

    void OnMenuNew(object sender, RoutedEventArgs args) => NewProject();
    async void NewProject() {
        viewModel.NewProject();
    }

    void OnMenuOpen(object sender, RoutedEventArgs args) => Open();
    async void Open() {
        //var files = await FilePicker.OpenFilesAboutProject(
        //    this, "menu.file.open",
        //    FilePicker.ProjectFiles,
        //    FilePicker.USTX,
        //    FilePicker.VSQX,
        //    FilePicker.UST,
        //    FilePicker.MIDI,
        //    FilePicker.UFDATA,
        //    FilePicker.MUSICXML);
        //if (files == null || files.Length == 0) {
        //    return;
        //}
        //try {
        //    viewModel.OpenProject(files);
        //} catch (Exception e) {
        //    Log.Error(e, $"Failed to open files {string.Join("\n", files)}");
        //    _ = await MessageBox.ShowError(this, new MessageCustomizableException($"Failed to open files {string.Join("\n", files)}", $"<translate:errors.failed.openfile>:\n{string.Join("\n", files)}", e));
        //}
    }

    void OnMainMenuOpened(object sender, RoutedEventArgs args) {
        viewModel.RefreshOpenRecent();
        viewModel.RefreshTemplates();
        viewModel.RefreshCacheSize();
    }

    void OnMainMenuClosed(object sender, RoutedEventArgs args) {
        Focus(); // Force unfocus menu for key down events.
    }

    void OnMainMenuPointerLeave(object sender, PointerEventArgs args) {
        Focus(); // Force unfocus menu for key down events.
    }

    async void OnMenuSave(object sender, RoutedEventArgs args) => await Save();
    public async Task Save() {
        if (!viewModel.ProjectSaved) {
            await SaveAs();
        } else {
            viewModel.SaveProject();
            string message = ThemeManager.GetString("progress.saved");
            message = string.Format(message, DateTime.Now);
            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, message));
        }
    }

    async void OnMenuSaveAs(object sender, RoutedEventArgs args) => await SaveAs();
    async Task SaveAs() {
        //var file = await FilePicker.SaveFileAboutProject(
        //    this, "menu.file.saveas", FilePicker.USTX);
        //if (!string.IsNullOrEmpty(file)) {
        //    viewModel.SaveProject(file);
        //}
    }

    void OnMenuSaveTemplate(object sender, RoutedEventArgs args) {
        //var project = DocManager.Inst.Project;
        //var dialog = new TypeInDialog();
        //dialog.Title = ThemeManager.GetString("menu.file.savetemplate");
        //dialog.SetText("default");
        //dialog.onFinish = file => {
        //    if (string.IsNullOrEmpty(file)) {
        //        return;
        //    }
        //    file = Path.GetFileNameWithoutExtension(file);
        //    file = $"{file}.ustx";
        //    file = Path.Combine(PathManager.Inst.TemplatesPath, file);
        //    Ustx.Save(file, project.CloneAsTemplate());
        //};
        //dialog.ShowDialog(this);
    }

    async void OnMenuImportTracks(object sender, RoutedEventArgs args) {
        //var files = await FilePicker.OpenFilesAboutProject(
        //    this, "menu.file.importtracks",
        //    FilePicker.ProjectFiles,
        //    FilePicker.USTX,
        //    FilePicker.VSQX,
        //    FilePicker.UST,
        //    FilePicker.MIDI,
        //    FilePicker.UFDATA,
        //    FilePicker.MUSICXML);
        //if (files == null || files.Length == 0) {
        //    return;
        //}
        //try {
        //    var loadedProjects = Formats.ReadProjects(files);
        //    if (loadedProjects == null || loadedProjects.Length == 0) {
        //        return;
        //    }
        //    bool importTempo = true;
        //    switch (Preferences.Default.ImportTempo) {
        //        case 1:
        //            importTempo = false;
        //            break;
        //        case 2:
        //            if (loadedProjects[0].tempos.Count == 0) {
        //                importTempo = false;
        //                break;
        //            }
        //            var tempoString = String.Join("\n",
        //                loadedProjects[0].tempos
        //                    .Select(tempo => $"position: {tempo.position}, tempo: {tempo.bpm}")
        //                );
        //            //ask the user
        //            var result = await MessageBox.Show(
        //                this,
        //                ThemeManager.GetString("dialogs.importtracks.importtempo") + "\n" + tempoString,
        //                ThemeManager.GetString("dialogs.importtracks.caption"),
        //                MessageBox.MessageBoxButtons.YesNo);
        //            if (result == MessageBox.MessageBoxResult.No) {
        //                importTempo = false;
        //            }
        //            break;
        //    }
        //    viewModel.ImportTracks(loadedProjects, importTempo);
        //} catch (Exception e) {
        //    Log.Error(e, $"Failed to import files");
        //    _ = await MessageBox.ShowError(this, new MessageCustomizableException("Failed to import files", "<translate:errors.failed.importfiles>", e));
        //}
        //ValidateTracksVoiceColor();
    }

    async void OnMenuImportAudio(object sender, RoutedEventArgs args) {
        //var file = await FilePicker.OpenFileAboutProject(
        //    this, "menu.file.importaudio", FilePicker.AudioFiles);
        //if (file == null) {
        //    return;
        //}
        //try {
        //    viewModel.ImportAudio(file);
        //} catch (Exception e) {
        //    Log.Error(e, "Failed to import audio");
        //    _ = await MessageBox.ShowError(this, new MessageCustomizableException("Failed to import audio", "<translate:errors.failed.importaudio>", e));
        //}
    }

    async void OnMenuImportMidi(object sender, RoutedEventArgs args) {
        //var file = await FilePicker.OpenFileAboutProject(
        //    this, "menu.file.importmidi", FilePicker.MIDI);
        //if (file == null) {
        //    return;
        //}
        //try {
        //    viewModel.ImportMidi(file);
        //} catch (Exception e) {
        //    Log.Error(e, "Failed to import midi");
        //    _ = await MessageBox.ShowError(this, new MessageCustomizableException("Failed to import midi", "<translate:errors.failed.importmidi>", e));
        //}
    }

    async void OnMenuExportMixdown(object sender, RoutedEventArgs args) {
        //var project = DocManager.Inst.Project;
        //var file = await FilePicker.SaveFileAboutProject(
        //    this, "menu.file.exportmixdown", FilePicker.WAV);
        //if (!string.IsNullOrEmpty(file)) {
        //    await PlaybackManager.Inst.RenderMixdown(project, file);
        //}
    }

    async void OnMenuExportWav(object sender, RoutedEventArgs args) {
        //var project = DocManager.Inst.Project;
        //if (await WarnToSave(project)) {
        //    var name = Path.GetFileNameWithoutExtension(project.FilePath);
        //    var path = Path.GetDirectoryName(project.FilePath);
        //    path = Path.Combine(path!, "Export", $"{name}.wav");
        //    await PlaybackManager.Inst.RenderToFiles(project, path);
        //}
    }

    async void OnMenuExportWavTo(object sender, RoutedEventArgs args) {
        //var project = DocManager.Inst.Project;
        //var file = await FilePicker.SaveFileAboutProject(
        //    this, "menu.file.exportwavto", FilePicker.WAV);
        //if (!string.IsNullOrEmpty(file)) {
        //    await PlaybackManager.Inst.RenderToFiles(project, file);
        //}
    }

    async void OnMenuExportUst(object sender, RoutedEventArgs e) {
        //var project = DocManager.Inst.Project;
        //if (await WarnToSave(project)) {
        //    var name = Path.GetFileNameWithoutExtension(project.FilePath);
        //    var path = Path.GetDirectoryName(project.FilePath);
        //    path = Path.Combine(path!, "Export", $"{name}.ust");
        //    for (var i = 0; i < project.parts.Count; i++) {
        //        var part = project.parts[i];
        //        if (part is UVoicePart voicePart) {
        //            var savePath = PathManager.Inst.GetPartSavePath(path, voicePart.DisplayName, i);
        //            Ust.SavePart(project, voicePart, savePath);
        //            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"{savePath}."));
        //        }
        //    }
        //}
    }

    async void OnMenuExportUstTo(object sender, RoutedEventArgs e) {
        //var project = DocManager.Inst.Project;
        //var file = await FilePicker.SaveFileAboutProject(
        //    this, "menu.file.exportustto", FilePicker.UST);
        //if (!string.IsNullOrEmpty(file)) {
        //    for (var i = 0; i < project.parts.Count; i++) {
        //        var part = project.parts[i];
        //        if (part is UVoicePart voicePart) {
        //            var savePath = PathManager.Inst.GetPartSavePath(file, voicePart.DisplayName, i);
        //            Ust.SavePart(project, voicePart, savePath);
        //            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"{savePath}."));
        //        }
        //    }
        //}
    }

    private async Task<bool> WarnToSave(UProject project) {
        if (string.IsNullOrEmpty(project.FilePath)) {
            //await MessageBox.Show(
            //    this,
            //    ThemeManager.GetString("dialogs.export.savefirst"),
            //    ThemeManager.GetString("dialogs.export.caption"),
            //    MessageBox.MessageBoxButtons.Ok);
            return false;
        }
        return true;
    }

    void OnMenuUndo(object sender, RoutedEventArgs args) => viewModel.Undo();
    void OnMenuRedo(object sender, RoutedEventArgs args) => viewModel.Redo();

    /// <summary>
    /// Check if a track has a singer and if it exists.
    /// If the user haven't selected a singer for the track, or the singer specified in ustx project doesn't exist, return null.
    /// Otherwise, return the singer.
    /// </summary>
    public USinger? TrackSingerIfFound(UTrack track) {
        if (track.Singer?.Found ?? false) {
            return track.Singer;
        }
        return null;
    }

    void OnKeyDown(object sender, KeyEventArgs args) {
        var tracksVm = viewModel.TracksViewModel;
        if (args.KeyModifiers == KeyModifiers.None) {
            args.Handled = true;
            switch (args.Key) {
                case Key.Delete: viewModel.TracksViewModel.DeleteSelectedParts(); break;
                case Key.Space: PlayOrPause(); break;
                case Key.Home: viewModel.PlaybackViewModel.MovePlayPos(0); break;
                case Key.End:
                    if (viewModel.TracksViewModel.Parts.Count > 0) {
                        int endTick = viewModel.TracksViewModel.Parts.Max(part => part.End);
                        viewModel.PlaybackViewModel.MovePlayPos(endTick);
                    }
                    break;
                default:
                    args.Handled = false;
                    break;
            }
        } else if (args.KeyModifiers == KeyModifiers.Alt) {
            args.Handled = true;
            switch (args.Key) {
                case Key.F4:
                    (Application.Current?.ApplicationLifetime as IControlledApplicationLifetime)?.Shutdown();
                    break;
                default:
                    args.Handled = false;
                    break;
            }
        } else if (args.KeyModifiers == KeyModifiers.Shift) {
            args.Handled = true;
            switch (args.Key) {
                // solo
                case Key.S:
                    if (viewModel.TracksViewModel.SelectedParts.Count > 0) {
                        var part = viewModel.TracksViewModel.SelectedParts.First();
                        var track = DocManager.Inst.Project.tracks[part.trackNo];
                        MessageBus.Current.SendMessage(new TracksSoloEvent(part.trackNo, !track.Solo, false));
                    }
                    break;
                // mute
                case Key.M:
                    if (viewModel.TracksViewModel.SelectedParts.Count > 0) {
                        var part = viewModel.TracksViewModel.SelectedParts.First();
                        MessageBus.Current.SendMessage(new TracksMuteEvent(part.trackNo, false));
                    }
                    break;
                default:
                    args.Handled = false;
                    break;
            }
        }
    }

    void OnPlayOrPause(object sender, RoutedEventArgs args) {
        PlayOrPause();
    }

    void PlayOrPause() {
        viewModel.PlaybackViewModel.PlayOrPause();
    }

    public void HScrollPointerWheelChanged(object sender, PointerWheelEventArgs args) {
        var scrollbar = (ScrollBar)sender;
        scrollbar.Value = Math.Max(scrollbar.Minimum, Math.Min(scrollbar.Maximum, scrollbar.Value - scrollbar.SmallChange * args.Delta.Y));
    }

    public void VScrollPointerWheelChanged(object sender, PointerWheelEventArgs args) {
        var scrollbar = (ScrollBar)sender;
        scrollbar.Value = Math.Max(scrollbar.Minimum, Math.Min(scrollbar.Maximum, scrollbar.Value - scrollbar.SmallChange * args.Delta.Y));
    }

    public void TimelinePointerWheelChanged(object sender, PointerWheelEventArgs args) {
        var control = (Control)sender;
        var position = args.GetCurrentPoint((Visual)sender).Position;
        var size = control.Bounds.Size;
        position = position.WithX(position.X / size.Width).WithY(position.Y / size.Height);
        viewModel.TracksViewModel.OnXZoomed(position, 0.1 * args.Delta.Y);
    }

    public void ViewScalerPointerWheelChanged(object sender, PointerWheelEventArgs args) {
        viewModel.TracksViewModel.OnYZoomed(new Point(0, 0.5), 0.1 * args.Delta.Y);
    }

    public void TimelinePointerPressed(object sender, PointerPressedEventArgs args) {
        var control = (Control)sender;
        var point = args.GetCurrentPoint(control);
        if (point.Properties.IsLeftButtonPressed) {
            args.Pointer.Capture(control);
            viewModel.TracksViewModel.PointToLineTick(point.Position, out int left, out int right);
            viewModel.PlaybackViewModel.MovePlayPos(left);
        } else if (point.Properties.IsRightButtonPressed) {
            int tick = viewModel.TracksViewModel.PointToTick(point.Position);
            viewModel.RefreshTimelineContextMenu(tick);
        }
    }

    public void TimelinePointerMoved(object sender, PointerEventArgs args) {
        var control = (Control)sender;
        var point = args.GetCurrentPoint(control);
        if (point.Properties.IsLeftButtonPressed) {
            viewModel.TracksViewModel.PointToLineTick(point.Position, out int left, out int right);
            viewModel.PlaybackViewModel.MovePlayPos(left);
        }
    }

    public void TimelinePointerReleased(object sender, PointerReleasedEventArgs args) {
        args.Pointer.Capture(null);
    }

    public void PartsCanvasPointerPressed(object sender, PointerPressedEventArgs args) {
        var control = (Control)sender;
        var point = args.GetCurrentPoint(control);
        var hitControl = control.InputHitTest(point.Position);
        if (partEditState != null) {
            return;
        }
        if (point.Properties.IsLeftButtonPressed) {
        //    if (args.KeyModifiers == cmdKey) {
        //        partEditState = new PartSelectionEditState(control, viewModel, SelectionBox);
        //        Cursor = ViewConstants.cursorCross;
        //    } else if (hitControl == control) {
        //        viewModel.TracksViewModel.DeselectParts();
        //        var part = viewModel.TracksViewModel.MaybeAddPart(point.Position);
        //        if (part != null) {
        //            // Start moving right away
        //            partEditState = new PartMoveEditState(control, viewModel, part);
        //            Cursor = ViewConstants.cursorSizeAll;
        //        }
        //    } else if (hitControl is PartControl partControl) {
        //        bool isVoice = partControl.part is UVoicePart;
        //        bool isWave = partControl.part is UWavePart;
        //        bool trim = point.Position.X > partControl.Bounds.Right - ViewConstants.ResizeMargin;
        //        bool skip = point.Position.X < partControl.Bounds.Left + ViewConstants.ResizeMargin;
        //        if (isVoice && trim) {
        //            partEditState = new PartResizeEditState(control, viewModel, partControl.part);
        //            Cursor = ViewConstants.cursorSizeWE;
        //        } else if (isWave && skip) {
        //            // TODO
        //        } else if (isWave && trim) {
        //            // TODO
        //        } else {
        //            partEditState = new PartMoveEditState(control, viewModel, partControl.part);
        //            Cursor = ViewConstants.cursorSizeAll;
        //        }
            //}
        } else if (point.Properties.IsRightButtonPressed) {
            //if (hitControl is PartControl partControl) {
            //    if (!viewModel.TracksViewModel.SelectedParts.Contains(partControl.part)) {
            //        viewModel.TracksViewModel.DeselectParts();
            //        viewModel.TracksViewModel.SelectPart(partControl.part);
            //    }
            //    if (PartsContextMenu != null && viewModel.TracksViewModel.SelectedParts.Count > 0) {
            //        PartsContextMenu.DataContext = new PartsContextMenuArgs {
            //            Part = partControl.part,
            //            PartDeleteCommand = viewModel.PartDeleteCommand,
            //            PartGotoFileCommand = PartGotoFileCommand,
            //            PartReplaceAudioCommand = PartReplaceAudioCommand,
            //            PartRenameCommand = PartRenameCommand,
            //            PartTranscribeCommand = PartTranscribeCommand,
            //        };
            //        shouldOpenPartsContextMenu = true;
            //    }
            //} else {
            //    viewModel.TracksViewModel.DeselectParts();
            //}
        } else if (point.Properties.IsMiddleButtonPressed) {
            //partEditState = new PartPanningState(control, viewModel);
            //Cursor = ViewConstants.cursorHand;
        }
        if (partEditState != null) {
            partEditState.Begin(point.Pointer, point.Position);
            partEditState.Update(point.Pointer, point.Position);
        }
    }

    public void PartsCanvasPointerMoved(object sender, PointerEventArgs args) {
        var control = (Control)sender;
        var point = args.GetCurrentPoint(control);
        if (partEditState != null) {
            partEditState.Update(point.Pointer, point.Position);
            return;
        }
        var hitControl = control.InputHitTest(point.Position);
        if (hitControl is PartControl partControl) {
            bool isVoice = partControl.part is UVoicePart;
            bool isWave = partControl.part is UWavePart;
            bool trim = point.Position.X > partControl.Bounds.Right - ViewConstants.ResizeMargin;
            bool skip = point.Position.X < partControl.Bounds.Left + ViewConstants.ResizeMargin;
            if (isVoice && trim) {
                Cursor = ViewConstants.cursorSizeWE;
            } else if (isWave && (skip || trim)) {
                Cursor = null; // TODO
            } else {
                Cursor = null;
            }
        } else {
            Cursor = null;
        }
    }

    public void PartsCanvasPointerReleased(object sender, PointerReleasedEventArgs args) {
        if (partEditState != null) {
            if (partEditState.MouseButton != args.InitialPressMouseButton) {
                return;
            }
            var control = (Control)sender;
            var point = args.GetCurrentPoint(control);
            partEditState.Update(point.Pointer, point.Position);
            partEditState.End(point.Pointer, point.Position);
            partEditState = null;
            Cursor = null;
        }
        //if (openPianoRollWindow) {
        //    pianoRollWindow?.Show();
        //    pianoRollWindow?.Activate();
        //    openPianoRollWindow = false;
        //}
    }

    public void PartsCanvasDoubleTapped(object sender, TappedEventArgs args) {
        if (!(sender is Canvas canvas)) {
            return;
        }
        var control = canvas.InputHitTest(args.GetPosition(canvas));
        if (control is PartControl partControl && partControl.part is UVoicePart) {
            //if (pianoRollWindow == null) {
            //    MessageBox.ShowLoading(this);
            //    pianoRollWindow = new PianoRollWindow() {
            //        MainWindow = this,
            //    };
            //    pianoRollWindow.ViewModel.PlaybackViewModel = viewModel.PlaybackViewModel;
            //    MessageBox.CloseLoading();
            //}
            //// Workaround for new window losing focus.
            //openPianoRollWindow = true;
            //int tick = viewModel.TracksViewModel.PointToTick(args.GetPosition(canvas));
            //DocManager.Inst.ExecuteCmd(new LoadPartNotification(partControl.part, DocManager.Inst.Project, tick));
            //pianoRollWindow.AttachExpressions();
        }
    }

    public void PartsCanvasPointerWheelChanged(object sender, PointerWheelEventArgs args) {
        var delta = args.Delta;
        //if (args.KeyModifiers == KeyModifiers.None || args.KeyModifiers == KeyModifiers.Shift) {
        //    if (args.KeyModifiers == KeyModifiers.Shift) {
        //        delta = new Vector(delta.Y, delta.X);
        //    }
        //    if (delta.X != 0) {
        //        HScrollBar.Value = Math.Max(HScrollBar.Minimum,
        //            Math.Min(HScrollBar.Maximum, HScrollBar.Value - HScrollBar.SmallChange * delta.X));
        //    }
        //    if (delta.Y != 0) {
        //        VScrollBar.Value = Math.Max(VScrollBar.Minimum,
        //            Math.Min(VScrollBar.Maximum, VScrollBar.Value - VScrollBar.SmallChange * delta.Y));
        //    }
        //} else if (args.KeyModifiers == KeyModifiers.Alt) {
        //    ViewScalerPointerWheelChanged(VScaler, args);
        //} else if (args.KeyModifiers == cmdKey) {
        //    TimelinePointerWheelChanged(TimelineCanvas, args);
        //}
        if (partEditState != null) {
            var point = args.GetCurrentPoint(partEditState.control);
            partEditState.Update(point.Pointer, point.Position);
        }
    }

    public void PartsContextMenuOpening(object sender, CancelEventArgs args) {
        if (shouldOpenPartsContextMenu) {
            shouldOpenPartsContextMenu = false;
        } else {
            args.Cancel = true;
        }
    }

    public void PartsContextMenuClosing(object sender, CancelEventArgs args) {
        if (PartsContextMenu != null) {
            PartsContextMenu.DataContext = null;
        }
    }

    public void OnNext(UCommand cmd, bool isUndo) {
        if (cmd is ErrorMessageNotification notif) {
            switch (notif.e) {
                case Core.Render.NoResamplerException:
                case Core.Render.NoWavtoolException:
                    //MessageBox.Show(
                    //   this,
                    //   ThemeManager.GetString("dialogs.noresampler.message"),
                    //   ThemeManager.GetString("dialogs.noresampler.caption"),
                    //   MessageBox.MessageBoxButtons.Ok);
                    break;
                default:
                    //MessageBox.ShowError(this, notif.e, notif.message, true);
                    break;
            }
        } else if (cmd is LoadingNotification loadingNotif && loadingNotif.window == typeof(MainWindow)) {
            if (loadingNotif.startLoading) {
                //MessageBox.ShowLoading(this);
            } else {
                //MessageBox.CloseLoading();
            }
        }
    }
}
