using System.IO;
using System.Linq;
using System.Reactive;
using DynamicData.Binding;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class MainWindowViewModel : ViewModelBase, ICmdSubscriber {
        [Reactive] public PlaybackViewModel PlaybackViewModel { get; set; }
        [Reactive] public TracksViewModel TracksViewModel { get; set; }
        [Reactive] public ReactiveCommand<string, Unit>? OpenRecentCommand { get; private set; }
        public ObservableCollectionExtended<MenuItemViewModel> OpenRecent => openRecent;

        public bool ProjectSaved => !string.IsNullOrEmpty(DocManager.Inst.Project.FilePath) && DocManager.Inst.Project.Saved;
        public string AppVersion => $"OpenUtau v{System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version}";
        public double Progress {
            get => progress;
            set => this.RaiseAndSetIfChanged(ref progress, value);
        }
        public string ProgressText {
            get => progressText;
            set => this.RaiseAndSetIfChanged(ref progressText, value);
        }

        private double progress;
        private string progressText = string.Empty;
        private ObservableCollectionExtended<MenuItemViewModel> openRecent
            = new ObservableCollectionExtended<MenuItemViewModel>();

        public MainWindowViewModel() {
            PlaybackViewModel = new PlaybackViewModel();
            TracksViewModel = new TracksViewModel();
            OpenRecentCommand = ReactiveCommand.Create<string>(file => OpenProject(new[] { file }));
            DocManager.Inst.AddSubscriber(this);
        }

        public void Undo() {
            DocManager.Inst.Undo();
        }
        public void Redo() {
            DocManager.Inst.Redo();
        }

        public void NewProject() {
            DocManager.Inst.ExecuteCmd(new LoadProjectNotification(Core.Formats.Ustx.Create()));
        }

        public void OpenProject(string[] files) {
            if (files == null) {
                return;
            }
            Core.Formats.Formats.LoadProject(files);
        }

        public void SaveProject(string file = "") {
            if (file == null) {
                return;
            }
            DocManager.Inst.ExecuteCmd(new SaveProjectNotification(file));
        }

        public void ImportTracks(string[] files) {
            if (files == null) {
                return;
            }
            Core.Formats.Formats.ImportTracks(DocManager.Inst.Project, files);
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
            DocManager.Inst.ExecuteCmd(new AddTrackCommand(project, new UTrack() { TrackNo = trackNo }));
            DocManager.Inst.ExecuteCmd(new AddPartCommand(project, part));
            DocManager.Inst.EndUndoGroup();
        }

        public void ImportMidi(string file) {
            if (file == null) {
                return;
            }
            var project = DocManager.Inst.Project;
            var parts = Core.Formats.Midi.Load(file, project);
            DocManager.Inst.StartUndoGroup();
            foreach (var part in parts) {
                var track = new UTrack();
                track.TrackNo = project.tracks.Count;
                part.trackNo = track.TrackNo;
                part.AfterLoad(project, track);
                DocManager.Inst.ExecuteCmd(new AddTrackCommand(project, track));
                DocManager.Inst.ExecuteCmd(new AddPartCommand(project, part));
            }
            DocManager.Inst.EndUndoGroup();
        }

        public void InstallSinger(string file) {
            try {
                var installer = new Classic.VoicebankInstaller(PathManager.Inst.InstalledSingersPath, (progress, info) => {
                    DocManager.Inst.ExecuteCmd(new ProgressBarNotification(progress, info));
                });
                installer.LoadArchive(file);
            } finally {
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, ""));
                DocManager.Inst.ExecuteCmd(new SingersChangedNotification());
            }
        }

        public void RefreshOpenRecent() {
            openRecent.Clear();
            openRecent.AddRange(Core.Util.Preferences.Default.RecentFiles.Select(file => new MenuItemViewModel() {
                Header = file,
                Command = OpenRecentCommand,
                CommandParameter = file,
            }));
        }

        #region ICmdSubscriber

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is ProgressBarNotification progressBarNotification) {
                Progress = progressBarNotification.Progress;
                ProgressText = progressBarNotification.Info;
            } else if (cmd is LoadProjectNotification loadProject) {
                string filePath = loadProject.project.FilePath;
                if (string.IsNullOrEmpty(filePath)) {
                    return;
                }
                var recent = Core.Util.Preferences.Default.RecentFiles;
                recent.RemoveAll(f => f == filePath || string.IsNullOrEmpty(f) || !File.Exists(f));
                recent.Insert(0, filePath);
                if (recent.Count > 16) {
                    recent.RemoveRange(16, recent.Count - 16);
                }
                Core.Util.Preferences.Save();
            }
        }

        #endregion
    }
}
