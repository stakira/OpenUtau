using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.Lib;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core {
    public class DocManager {
        DocManager() {
            Project = new UProject();
        }

        static DocManager _s;
        static DocManager GetInst() { if (_s == null) { _s = new DocManager(); } return _s; }
        public static DocManager Inst { get { return GetInst(); } }

        public int playPosTick = 0;

        public Dictionary<string, USinger> Singers { get; private set; } = new Dictionary<string, USinger>();
        public List<USinger> SingersOrdered { get; private set; } = new List<USinger>();
        public Plugin[] Plugins { get; private set; }
        public PhonemizerFactory[] PhonemizerFactories { get; private set; }
        public UProject Project { get; private set; }
        public bool HasOpenUndoGroup => undoGroup != null;
        public List<UPart> PartsClipboard { get; set; }
        public List<UNote> NotesClipboard { get; set; }

        public void Initialize() {
            SearchAllSingers();
            SearchAllPlugins();
            SearchAllLegacyPlugins();
        }

        public void SearchAllSingers() {
            try {
                Directory.CreateDirectory(PathManager.Inst.SingersPath);
                var stopWatch = Stopwatch.StartNew();
                Singers = Format.UtauSoundbank.FindAllSingers();
                SingersOrdered = Singers.Values.OrderBy(singer => singer.Name).ToList();
                stopWatch.Stop();
                Log.Information($"Search all singers: {stopWatch.Elapsed}");
            } catch (Exception e) {
                Log.Error(e, "Failed to search singers.");
                Singers = new Dictionary<string, USinger>();
            }
        }

        public USinger GetSinger(string name) {
            Log.Information(name);
            name = name.Replace("%VOICE%", "");
            if (Singers.ContainsKey(name)) {
                return Singers[name];
            }
            return null;
        }

        public void SearchAllLegacyPlugins() {
            try {
                var stopWatch = Stopwatch.StartNew();
                Plugins = PluginLoader.LoadAll(PathManager.Inst.PluginsPath);
                stopWatch.Stop();
                Log.Information($"Search all legacy plugins: {stopWatch.Elapsed}");
            } catch (Exception e) {
                Log.Error(e, "Failed to search legacy plugins.");
                Plugins = new Plugin[0];
            }
        }

        public void SearchAllPlugins() {
            const string kBuiltin = "OpenUtau.Plugin.Builtin.dll";
            var stopWatch = Stopwatch.StartNew();
            var phonemizerFactories = new List<PhonemizerFactory>();
            phonemizerFactories.Add(PhonemizerFactory.Get(typeof(DefaultPhonemizer)));
            var files = new List<string>();
            try {
                files.Add(Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), kBuiltin));
                Directory.CreateDirectory(PathManager.Inst.PluginsPath);
                string oldBuiltin = Path.Combine(PathManager.Inst.PluginsPath, kBuiltin);
                if (File.Exists(oldBuiltin)) {
                    File.Delete(oldBuiltin);
                }
                files.AddRange(Directory.EnumerateFiles(PathManager.Inst.PluginsPath, "*.dll", SearchOption.AllDirectories));
            } catch (Exception e) {
                Log.Error(e, "Failed to search plugins.");
            }
            foreach (var file in files) {
                Assembly assembly;
                try {
                    if (!LibraryLoader.IsManagedAssembly(file)) {
                        Log.Information($"Skipping {file}");
                        continue;
                    }
                    assembly = Assembly.LoadFile(file);
                    foreach (var type in assembly.GetExportedTypes()) {
                        if (type.IsAbstract) {
                            continue;
                        }
                        if (type.IsSubclassOf(typeof(Phonemizer))) {
                            phonemizerFactories.Add(PhonemizerFactory.Get(type));
                        }
                    }
                } catch (Exception e) {
                    Log.Warning(e, $"Failed to load {file}.");
                    continue;
                }
            }
            PhonemizerFactories = phonemizerFactories.OrderBy(factory => factory.tag).ToArray();
            stopWatch.Stop();
            Log.Information($"Search all plugins: {stopWatch.Elapsed}");
        }

        #region Command Queue

        readonly Deque<UCommandGroup> undoQueue = new Deque<UCommandGroup>();
        readonly Deque<UCommandGroup> redoQueue = new Deque<UCommandGroup>();
        UCommandGroup undoGroup = null;
        UCommandGroup savedPoint = null;

        public bool ChangesSaved {
            get {
                return (Project.Saved || Project.tracks.Count == 0) &&
                    (undoQueue.Count > 0 && savedPoint == undoQueue.Last() || undoQueue.Count == 0 && savedPoint == null);
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
                        Format.Ustx.Save(Project.FilePath, Project);
                    } else {
                        Format.Ustx.Save(_cmd.Path, Project);
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
                } else if (cmd is ValidateProjectNotification) {
                    Project.Validate();
                } else if (cmd is SingersRefreshedNotification) {
                    foreach (var track in Project.tracks) {
                        track.OnSingerRefreshed();
                    }
                    Project.Validate();
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
            lock (Project) {
                cmd.Execute();
            }
            if (!cmd.Silent) {
                Log.Information($"ExecuteCmd {cmd}");
            }
            Publish(cmd);
            if (cmd.ValidatePart == null) {
                Project.Validate();
            } else {
                cmd.ValidatePart.Validate(Project, Project.tracks[cmd.ValidatePart.trackNo]);
            }
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
            undoGroup.Merge();
            undoGroup = null;
            Log.Information("undoGroup ended");
            ExecuteCmd(new PreRenderNotification());
        }

        public void RollBackUndoGroup() {
            if (undoGroup == null) {
                Log.Error("No active undoGroup to rollback.");
                return;
            }
            for (int i = undoGroup.Commands.Count - 1; i >= 0; i--) {
                var cmd = undoGroup.Commands[i];
                cmd.Unexecute();
                if (i == 0) {
                    Project.Validate();
                }
                Publish(cmd, true);
            }
            undoGroup.Commands.Clear();
        }

        public void Undo() {
            if (undoQueue.Count == 0) {
                return;
            }
            var group = undoQueue.RemoveFromBack();
            for (int i = group.Commands.Count - 1; i >= 0; i--) {
                var cmd = group.Commands[i];
                cmd.Unexecute();
                if (i == 0) {
                    Project.Validate();
                }
                Publish(cmd, true);
            }
            redoQueue.AddToBack(group);
            ExecuteCmd(new PreRenderNotification());
        }

        public void Redo() {
            if (redoQueue.Count == 0) {
                return;
            }
            var group = redoQueue.RemoveFromBack();
            for (var i = 0; i < group.Commands.Count; i++) {
                var cmd = group.Commands[i];
                cmd.Execute();
                if (i == group.Commands.Count - 1) {
                    Project.Validate();
                }
                Publish(cmd);
            }
            undoQueue.AddToBack(group);
            ExecuteCmd(new PreRenderNotification());
        }

        # endregion

        # region Command Subscribers

        private readonly object lockObj = new object();
        private readonly List<ICmdSubscriber> subscribers = new List<ICmdSubscriber>();

        public void AddSubscriber(ICmdSubscriber sub) {
            lock (lockObj) {
                if (!subscribers.Contains(sub)) {
                    subscribers.Add(sub);
                }
            }
        }

        public void RemoveSubscriber(ICmdSubscriber sub) {
            lock (lockObj) {
                if (subscribers.Contains(sub)) {
                    subscribers.Remove(sub);
                }
            }
        }

        private void Publish(UCommand cmd, bool isUndo = false) {
            lock (lockObj) {
                foreach (var sub in subscribers) {
                    sub.OnNext(cmd, isUndo);
                }
            }
        }

        #endregion
    }
}
