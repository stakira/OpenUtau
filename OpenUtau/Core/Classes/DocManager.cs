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

        public Dictionary<string, USinger> Singers { get; private set; }
        public UProject Project { get; private set; }

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

        public void ExecuteCmd(UCommand cmd, bool quiet = false) {
            if (cmd is UNotification) {
                if (cmd is SaveProjectNotification) {
                    var _cmd = cmd as SaveProjectNotification;
                    if (undoQueue.Count > 0) savedPoint = undoQueue.Last();
                    if (string.IsNullOrEmpty(_cmd.Path)) OpenUtau.Core.Formats.Ustx.Save(Project.filePath, Project);
                    else OpenUtau.Core.Formats.Ustx.Save(_cmd.Path, Project);
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
                if (!quiet) System.Diagnostics.Debug.WriteLine("Publish notification " + cmd.ToString());
                return;
            } else if (undoGroup == null) { System.Diagnostics.Debug.WriteLine("Null undoGroup"); return; } else {
                undoGroup.Commands.Add(cmd);
                cmd.Execute();
                Publish(cmd);
            }
            if (!quiet) System.Diagnostics.Debug.WriteLine("ExecuteCmd " + cmd.ToString());
        }

        public void StartUndoGroup() {
            if (undoGroup != null) { System.Diagnostics.Debug.WriteLine("undoGroup already started"); EndUndoGroup(); }
            undoGroup = new UCommandGroup();
            System.Diagnostics.Debug.WriteLine("undoGroup started");
        }

        public void EndUndoGroup() {
            if (undoGroup != null && undoGroup.Commands.Count > 0) { undoQueue.AddToBack(undoGroup); redoQueue.Clear(); }
            if (undoQueue.Count > Core.Util.Preferences.Default.UndoLimit) undoQueue.RemoveFromFront();
            undoGroup = null;
            System.Diagnostics.Debug.WriteLine("undoGroup ended");
        }

        public void Undo() {
            if (undoQueue.Count == 0) return;
            var cmdg = undoQueue.RemoveFromBack();
            for (int i = cmdg.Commands.Count - 1; i >= 0; i--) { var cmd = cmdg.Commands[i]; cmd.Unexecute(); if (!(cmd is NoteCommand)) Publish(cmd, true); }
            redoQueue.AddToBack(cmdg);
        }

        public void Redo() {
            if (redoQueue.Count == 0) return;
            var cmdg = redoQueue.RemoveFromBack();
            foreach (var cmd in cmdg.Commands) { cmd.Execute(); Publish(cmd); }
            undoQueue.AddToBack(cmdg);
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
