using System.IO;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Lib;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core {
    class DocManager {
        DocManager() {
            Project = new UProject();
        }

        static DocManager _s;
        static DocManager GetInst() { if (_s == null) { _s = new DocManager(); } return _s; }
        public static DocManager Inst { get { return GetInst(); } }

        public int playPosTick = 0;

        public Dictionary<string, USinger> Singers { get; private set; } = new Dictionary<string, USinger>();
        public UProject Project { get; private set; }
        public bool HasOpenUndoGroup => undoGroup != null;

        public void SearchAllSingers() {
            Singers = Formats.UtauSoundbank.FindAllSingers();
            Directory.CreateDirectory(PathManager.Inst.GetEngineSearchPath());
        }

        public USinger GetSinger(string name) {
            Log.Information(name);
            name = name.Replace(PathManager.UtauVoicePath, "");
            if (Singers.ContainsKey(name)) {
                return Singers[name];
            }
            return null;
        }

        #region Command Queue

        readonly Deque<UCommandGroup> undoQueue = new Deque<UCommandGroup>();
        readonly Deque<UCommandGroup> redoQueue = new Deque<UCommandGroup>();
        UCommandGroup undoGroup = null;
        UCommandGroup savedPoint = null;

        public bool ChangesSaved {
            get {
                return Project.Saved && (undoQueue.Count > 0 && savedPoint == undoQueue.Last() || undoQueue.Count == 0 && savedPoint == null);
            }
        }

        public void ExecuteCmd(UCommand cmd) {
            if (cmd is UNotification) {
                if (cmd is SaveProjectNotification) {
                    var _cmd = cmd as SaveProjectNotification;
                    if (undoQueue.Count > 0) {
                        savedPoint = undoQueue.Last();
                    }
                    if (string.IsNullOrEmpty(_cmd.Path)) {
                        Formats.Ustx.Save(Project.filePath, Project);
                    } else {
                        Formats.Ustx.Save(_cmd.Path, Project);
                    }
                } else if (cmd is LoadProjectNotification notification) {
                    undoQueue.Clear();
                    redoQueue.Clear();
                    undoGroup = null;
                    savedPoint = null;
                    Project = notification.project;
                    playPosTick = 0;
                } else if (cmd is SetPlayPosTickNotification) {
                    var _cmd = cmd as SetPlayPosTickNotification;
                    playPosTick = _cmd.playPosTick;
                } else if (cmd is SingersChangedNotification) {
                    SearchAllSingers();
                }
                Publish(cmd);
                if (!cmd.Silent) {
                    Log.Information($"Publish notification {cmd}");
                }
                return;
            }
            if (undoGroup == null) {
                Log.Error($"No active UndoGroup {cmd}");
                return;
            }
            undoGroup.Commands.Add(cmd);
            cmd.Execute();
            if (!cmd.Silent) {
                Log.Information($"ExecuteCmd {cmd}");
            }
            Publish(cmd);
            Project.Validate();
        }

        public void StartUndoGroup() {
            if (undoGroup != null) {
                Log.Error("undoGroup already started");
                EndUndoGroup();
            }
            undoGroup = new UCommandGroup();
            Log.Information("undoGroup started");
        }

        public void EndUndoGroup() {
            if (undoGroup == null) {
                Log.Error("No active undoGroup to end.");
                return;
            }
            if (undoGroup.Commands.Count > 0) {
                undoQueue.AddToBack(undoGroup);
                redoQueue.Clear();
            }
            while (undoQueue.Count > Util.Preferences.Default.UndoLimit) {
                undoQueue.RemoveFromFront();
            }
            undoGroup = null;
            Log.Information("undoGroup ended");
        }

        public void Undo() {
            if (undoQueue.Count == 0) {
                return;
            }
            var group = undoQueue.RemoveFromBack();
            for (int i = group.Commands.Count - 1; i >= 0; i--) {
                var cmd = group.Commands[i];
                cmd.Unexecute();
                Publish(cmd, true);
            }
            redoQueue.AddToBack(group);
            Project.Validate();
        }

        public void Redo() {
            if (redoQueue.Count == 0) {
                return;
            }
            var group = redoQueue.RemoveFromBack();
            foreach (var cmd in group.Commands) {
                cmd.Execute();
                Publish(cmd);
            }
            undoQueue.AddToBack(group);
            Project.Validate();
        }

        # endregion

        # region Command Subscribers

        private readonly List<ICmdSubscriber> subscribers = new List<ICmdSubscriber>();

        public void AddSubscriber(ICmdSubscriber sub) {
            if (!subscribers.Contains(sub)) {
                subscribers.Add(sub);
            }
        }

        public void RemoveSubscriber(ICmdSubscriber sub) {
            if (subscribers.Contains(sub)) {
                subscribers.Remove(sub);
            }
        }

        private void Publish(UCommand cmd, bool isUndo = false) {
            foreach (var sub in subscribers) {
                sub.OnNext(cmd, isUndo);
            }
        }

        #endregion
    }
}
