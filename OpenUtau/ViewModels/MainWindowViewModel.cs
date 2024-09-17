using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Threading;
using DynamicData.Binding;
using OpenUtau.App.Views;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class PartsContextMenuArgs {
        public UPart? Part { get; set; }
        public bool IsVoicePart => Part is UVoicePart;
        public bool IsWavePart => Part is UWavePart;
        public ReactiveCommand<UPart, Unit>? PartDeleteCommand { get; set; }
        public ReactiveCommand<UPart, Unit>? PartRenameCommand { get; set; }
        public ReactiveCommand<UPart, Unit>? PartGotoFileCommand { get; set; }
        public ReactiveCommand<UPart, Unit>? PartReplaceAudioCommand { get; set; }
        public ReactiveCommand<UPart, Unit>? PartTranscribeCommand { get; set; }
    }

    public class MainWindowViewModel : ViewModelBase, ICmdSubscriber {
        public bool ExtendToFrame => OS.IsMacOS();
        public string Title => !ProjectSaved
            ? $"{AppVersion}"
            : $"{(DocManager.Inst.ChangesSaved ? "" : "*")}{AppVersion} [{DocManager.Inst.Project.FilePath}]";
        [Reactive] public PlaybackViewModel PlaybackViewModel { get; set; }
        [Reactive] public TracksViewModel TracksViewModel { get; set; }
        [Reactive] public ReactiveCommand<string, Unit>? OpenRecentCommand { get; private set; }
        [Reactive] public ReactiveCommand<string, Unit>? OpenTemplateCommand { get; private set; }
        public ObservableCollectionExtended<MenuItemViewModel> OpenRecent => openRecent;
        public ObservableCollectionExtended<MenuItemViewModel> OpenTemplates => openTemplates;
        public ObservableCollectionExtended<MenuItemViewModel> TimelineContextMenuItems { get; }
            = new ObservableCollectionExtended<MenuItemViewModel>();

        [Reactive] public string ClearCacheHeader { get; set; }
        public bool ProjectSaved => !string.IsNullOrEmpty(DocManager.Inst.Project.FilePath) && DocManager.Inst.Project.Saved;
        public string AppVersion => $"OpenUtau v{System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version}";
        [Reactive] public double Progress { get; set; }
        [Reactive] public string ProgressText { get; set; }
        public ReactiveCommand<UPart, Unit> PartDeleteCommand { get; set; }
        public ReactiveCommand<int, Unit>? AddTempoChangeCmd { get; set; }
        public ReactiveCommand<int, Unit>? DelTempoChangeCmd { get; set; }
        public ReactiveCommand<int, Unit>? AddTimeSigChangeCmd { get; set; }
        public ReactiveCommand<int, Unit>? DelTimeSigChangeCmd { get; set; }

        private ObservableCollectionExtended<MenuItemViewModel> openRecent
            = new ObservableCollectionExtended<MenuItemViewModel>();
        private ObservableCollectionExtended<MenuItemViewModel> openTemplates
            = new ObservableCollectionExtended<MenuItemViewModel>();

        public MainWindowViewModel() {
            PlaybackViewModel = new PlaybackViewModel();
            TracksViewModel = new TracksViewModel();
            ClearCacheHeader = string.Empty;
            ProgressText = string.Empty;
            OpenRecentCommand = ReactiveCommand.Create<string>(file => {
                try {
                    OpenProject(new[] { file });
                } catch (Exception e) {
                    var customEx = new MessageCustomizableException("Failed to open recent", "<translate:errors.failed.openfile>: recent project", e);
                    DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(customEx));
                }
            });
            OpenTemplateCommand = ReactiveCommand.Create<string>(file => {
                try {
                    OpenProject(new[] { file });
                    DocManager.Inst.Project.Saved = false;
                    DocManager.Inst.Project.FilePath = string.Empty;
                } catch (Exception e) {
                    var customEx = new MessageCustomizableException("Failed to open template", "<translate:errors.failed.openfile>: project template", e);
                    DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(customEx));
                }
            });
            PartDeleteCommand = ReactiveCommand.Create<UPart>(part => {
                TracksViewModel.DeleteSelectedParts();
            });
            DocManager.Inst.AddSubscriber(this);
        }

        public void Undo() {
            DocManager.Inst.Undo();
        }
        public void Redo() {
            DocManager.Inst.Redo();
        }

        public Task? GetInitSingerTask() {
            return SingerManager.Inst.InitializationTask;
        }

        public void InitProject() {
            var args = Environment.GetCommandLineArgs();
            if (args.Length == 2 && File.Exists(args[1])) {
                try {
                    Core.Format.Formats.LoadProject(new string[] { args[1] });
                    DocManager.Inst.ExecuteCmd(new VoiceColorRemappingNotification(-1, true));
                    return;
                } catch (Exception e) {
                    var customEx = new MessageCustomizableException($"Failed to open file {args[1]}", $"<translate:errors.failed.openfile>: {args[1]}", e);
                    DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(customEx));
                }
            }
            NewProject();
        }

        public void NewProject() {
            var defaultTemplate = Path.Combine(PathManager.Inst.TemplatesPath, "default.ustx");
            if (File.Exists(defaultTemplate)) {
                try {
                    OpenProject(new[] { defaultTemplate });
                    DocManager.Inst.Project.Saved = false;
                    DocManager.Inst.Project.FilePath = string.Empty;
                    return;
                } catch (Exception e) {
                    var customEx = new MessageCustomizableException("Failed to load default template", "<translate:errors.failed.load>: default template", e);
                    DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(customEx));
                }
            }
            DocManager.Inst.ExecuteCmd(new LoadProjectNotification(Core.Format.Ustx.Create()));
        }

        public void OpenProject(string[] files) {
            if (files == null) {
                return;
            }
            DocManager.Inst.ExecuteCmd(new LoadingNotification(typeof(MainWindow), true, "project"));
            try {
                Core.Format.Formats.LoadProject(files);
                DocManager.Inst.ExecuteCmd(new VoiceColorRemappingNotification(-1, true));
                this.RaisePropertyChanged(nameof(Title));
            } finally {
                DocManager.Inst.ExecuteCmd(new LoadingNotification(typeof(MainWindow), false, "project"));
            }
        }

        public void SaveProject(string file = "") {
            if (file == null) {
                return;
            }
            DocManager.Inst.ExecuteCmd(new SaveProjectNotification(file));
            this.RaisePropertyChanged(nameof(Title));
        }
        
        public void ImportTracks(UProject[] loadedProjects, bool importTempo){
            if (loadedProjects == null || loadedProjects.Length < 1) {
                return;
            }
            Core.Format.Formats.ImportTracks(DocManager.Inst.Project, loadedProjects, importTempo);
        }

        public void ImportTracks(string[] files, bool importTempo) {
            if (files == null) {
                return;
            }
            Core.Format.Formats.ImportTracks(DocManager.Inst.Project, files, importTempo);
        }

        public void ImportAudio(string file) {
            if (file == null) {
                return;
            }
            var project = DocManager.Inst.Project;
            UWavePart part = new UWavePart() {
                FilePath = file,
            };
            part.Load(project);
            if (part == null) {
                return;
            }
            int trackNo = project.tracks.Count;
            part.trackNo = trackNo;
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new AddTrackCommand(project, new UTrack(project) { TrackNo = trackNo }));
            DocManager.Inst.ExecuteCmd(new AddPartCommand(project, part));
            DocManager.Inst.EndUndoGroup();
        }

        public void ImportMidi(string file) {
            if (file == null) {
                return;
            }
            var project = DocManager.Inst.Project;
            var parts = Core.Format.MidiWriter.Load(file, project);
            DocManager.Inst.StartUndoGroup();
            foreach (var part in parts) {
                var track = new UTrack(project);
                track.TrackNo = project.tracks.Count;
                part.trackNo = track.TrackNo;
                if(part.name != "New Part"){
                    track.TrackName = part.name;
                }
                part.AfterLoad(project, track);
                DocManager.Inst.ExecuteCmd(new AddTrackCommand(project, track));
                DocManager.Inst.ExecuteCmd(new AddPartCommand(project, part));
            }
            DocManager.Inst.EndUndoGroup();
        }

        public void RefreshOpenRecent() {
            openRecent.Clear();
            openRecent.AddRange(Core.Util.Preferences.Default.RecentFiles.Select(file => new MenuItemViewModel() {
                Header = file,
                Command = OpenRecentCommand,
                CommandParameter = file,
            }));
        }

        public void RefreshTemplates() {
            Directory.CreateDirectory(PathManager.Inst.TemplatesPath);
            var templates = Directory.GetFiles(PathManager.Inst.TemplatesPath, "*.ustx");
            openTemplates.Clear();
            openTemplates.AddRange(templates.Select(file => new MenuItemViewModel() {
                Header = Path.GetRelativePath(PathManager.Inst.TemplatesPath, file),
                Command = OpenTemplateCommand,
                CommandParameter = file,
            }));
        }

        public void RefreshCacheSize() {
            string header = ThemeManager.GetString("menu.tools.clearcache") ?? "";
            ClearCacheHeader = header;
            Task.Run(async () => {
                var cacheSize = PathManager.Inst.GetCacheSize();
                await Dispatcher.UIThread.InvokeAsync(() => {
                    ClearCacheHeader = $"{header} ({cacheSize})";
                });
            });
        }

        public void RefreshTimelineContextMenu(int tick) {
            TimelineContextMenuItems.Clear();
            var project = TracksViewModel.Project;
            var timeAxis = project.timeAxis;
            timeAxis.TickPosToBarBeat(tick, out int bar, out int beat, out int _);
            var timeSig = timeAxis.TimeSignatureAtBar(bar);
            if (bar == 0) {
                // Do nothing
            } else if (timeSig.barPosition != bar) {
                TimelineContextMenuItems.Add(new MenuItemViewModel {
                    Header = ThemeManager.GetString("context.timeline.addtimesig"),
                    Command = AddTimeSigChangeCmd,
                    CommandParameter = bar,
                });
            } else {
                TimelineContextMenuItems.Add(new MenuItemViewModel {
                    Header = ThemeManager.GetString("context.timeline.deltimesig"),
                    Command = DelTimeSigChangeCmd,
                    CommandParameter = bar,
                });
            }
            var tempo = project.tempos.LastOrDefault(t => t.position < tick);
            if (tempo != null && tempo.position > 0 && (tick - tempo.position) * TracksViewModel.TickWidth < 40) {
                string template = ThemeManager.GetString("context.timeline.deltempo");
                TimelineContextMenuItems.Add(new MenuItemViewModel {
                    Header = string.Format(template, tempo.position),
                    Command = DelTempoChangeCmd,
                    CommandParameter = tempo.position,
                });
            }
            TracksViewModel.TickToLineTick(tick, out int left, out int right);
            if (tempo == null || tempo.position != left) {
                string template = ThemeManager.GetString("context.timeline.addtempo");
                TimelineContextMenuItems.Add(new MenuItemViewModel {
                    Header = string.Format(template, left),
                    Command = AddTempoChangeCmd,
                    CommandParameter = left,
                });
            }
        }

        /// <summary>
        /// Remap a tick position from the old time axis to the new time axis without changing its absolute position (in ms).
        /// Note that this can only be used on positions, not durations.
        /// </summary>
        private int RemapTickPos(int tickPos, TimeAxis oldTimeAxis, TimeAxis newTimeAxis){
            double msPos = oldTimeAxis.TickPosToMsPos(tickPos);
            return newTimeAxis.MsPosToTickPos(msPos);
        }

        /// <summary>
        /// Remap the starting and ending positions of all the notes and parts in the whole project 
        /// from the old time axis to the new time axis, without changing their absolute positions in ms.
        /// </summary>
        public void RemapTimeAxis(TimeAxis oldTimeAxis, TimeAxis newTimeAxis){
            var project = DocManager.Inst.Project;
            foreach(var part in project.parts){
                var partOldStartTick = part.position;
                var partNewStartTick = RemapTickPos(part.position, oldTimeAxis, newTimeAxis);
                if(partNewStartTick != partOldStartTick){
                    DocManager.Inst.ExecuteCmd(new MovePartCommand(
                        project, part, partNewStartTick, part.trackNo));
                }
                if(part is UVoicePart voicePart){
                    var partOldEndTick = voicePart.End;
                    var partNewEndTick = RemapTickPos(voicePart.End, oldTimeAxis, newTimeAxis);
                    if(partNewEndTick - partNewStartTick != voicePart.Duration){
                        DocManager.Inst.ExecuteCmd(new ResizePartCommand(
                            project, voicePart, partNewEndTick - partNewStartTick));
                    }
                    var noteCommands = new List<UCommand>();
                    foreach(var note in voicePart.notes){
                        var noteOldStartTick = note.position + partOldStartTick;
                        var noteOldEndTick = note.End + partOldStartTick;
                        var noteOldDuration = note.duration;
                        var noteNewStartTick = RemapTickPos(noteOldStartTick, oldTimeAxis, newTimeAxis);
                        var noteNewEndTick = RemapTickPos(noteOldEndTick, oldTimeAxis, newTimeAxis);
                        var deltaPosTickInPart = (noteNewStartTick - partNewStartTick) - (noteOldStartTick - partOldStartTick);
                        if(deltaPosTickInPart != 0){
                            noteCommands.Add(new MoveNoteCommand(voicePart, note, deltaPosTickInPart, 0));
                        }
                        var noteNewDuration = noteNewEndTick - noteNewStartTick;
                        var deltaDur = noteNewDuration - noteOldDuration;
                        if(deltaDur != 0){
                            noteCommands.Add(new ResizeNoteCommand(voicePart, note, deltaDur));
                        }
                        //TODO: expression curve remapping, phoneme timing remapping
                    }
                    foreach(var command in noteCommands){
                        DocManager.Inst.ExecuteCmd(command);
                    }
                }
            }
        }

        #region ICmdSubscriber

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is ProgressBarNotification progressBarNotification) {
                Dispatcher.UIThread.InvokeAsync(() => {
                    Progress = progressBarNotification.Progress;
                    ProgressText = progressBarNotification.Info;
                });
            } else if (cmd is LoadProjectNotification loadProject) {
                Core.Util.Preferences.AddRecentFileIfEnabled(loadProject.project.FilePath);
            } else if (cmd is SaveProjectNotification saveProject) {
                Core.Util.Preferences.AddRecentFileIfEnabled(saveProject.Path);
            }
            this.RaisePropertyChanged(nameof(Title));
        }

        #endregion
    }
}
