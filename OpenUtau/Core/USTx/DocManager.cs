using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenUtau.Core.Lib;

namespace OpenUtau.Core.USTx
{
    class DocManager : ICmdPublisher
    {
        DocManager() { _project = new UProject(); }
        static DocManager _s;
        static DocManager GetInst() { if (_s == null) { _s = new DocManager(); } return _s; }
        public static DocManager Inst { get { return GetInst(); } }

        UProject _project;
        public UProject Project { get { return _project; } }

        # region Command Queue

        Deque<UCommandGroup> undoQueue = new Deque<UCommandGroup>();
        Deque<UCommandGroup> redoQueue = new Deque<UCommandGroup>();
        UCommandGroup undoGroup = null;
        UCommandGroup savedPoint = null;

        public void ExecuteCmd(UCommand cmd)
        {
            if (cmd is UNotification)
            {
                if (cmd is SaveProjectNotification && undoQueue.Count > 0) { savedPoint = undoQueue.Last(); }
                else if (cmd is LoadProjectNotification) { this._project = ((LoadProjectNotification)cmd).project; }
                Publish(cmd);
                System.Diagnostics.Debug.WriteLine("Publish notification " + cmd.ToString());
                return;
            }
            else if (undoGroup == null) { System.Diagnostics.Debug.WriteLine("Null undoGroup"); return; }
            else if (cmd is NoteCommand)
            {
                var _cmd = cmd as NoteCommand;
                lock (_cmd.Part)
                {
                    undoGroup.Commands.Add(cmd);
                    cmd.Execute();
                    Publish(cmd);
                }
            }
            else
            {
                undoGroup.Commands.Add(cmd);
                cmd.Execute();
                Publish(cmd);
            }
            System.Diagnostics.Debug.WriteLine("ExecuteCmd " + cmd.ToString());
        }

        public void StartUndoGroup()
        {
            if (undoGroup != null) { System.Diagnostics.Debug.WriteLine("undoGroup already started"); EndUndoGroup(); }
            undoGroup = new UCommandGroup();
            System.Diagnostics.Debug.WriteLine("undoGroup started");
        }

        public void EndUndoGroup()
        {
            if (undoGroup != null && undoGroup.Commands.Count > 0) { undoQueue.AddToBack(undoGroup); redoQueue.Clear(); }
            if (undoQueue.Count > Properties.Settings.Default.UndoLimit) undoQueue.RemoveFromFront();
            undoGroup = null;
            System.Diagnostics.Debug.WriteLine("undoGroup ended");
        }

        public void Undo()
        {
            if (undoQueue.Count == 0) return;
            var cmdg = undoQueue.RemoveFromBack();
            for (int i = cmdg.Commands.Count - 1; i >= 0; i--) { var cmd = cmdg.Commands[i]; cmd.Unexecute(); Publish(cmd, true); }
            redoQueue.AddToBack(cmdg);
        }

        public void Redo()
        {
            if (redoQueue.Count == 0) return;
            var cmdg = redoQueue.RemoveFromBack();
            foreach (var cmd in cmdg.Commands) { cmd.Execute(); Publish(cmd); }
            undoQueue.AddToBack(cmdg);
        }

        # endregion

        # region ICmdPublisher

        private List<ICmdSubscriber> subscribers = new List<ICmdSubscriber>();
        public void Subscribe(ICmdSubscriber sub) { if (!subscribers.Contains(sub)) subscribers.Add(sub); }
        public void Publish(UCommand cmd, bool isUndo = false) { foreach (var sub in subscribers) sub.OnNext(cmd, isUndo); }

        # endregion

        # region Command handeling

        # endregion
    }
}
