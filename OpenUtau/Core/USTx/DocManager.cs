using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    class DocManager
    {
        static DocManager _s;
        private DocManager() { project = new UProject(); }
        public static DocManager GetInst() { if (_s == null) { _s = new DocManager(); } return _s; }

        public UProject project;

        # region Command Queue

        public Deque<UCommandGroup> undoQueue = new Deque<UCommandGroup>();
        public Deque<UCommandGroup> redoQueue = new Deque<UCommandGroup>();
        public UCommandGroup undoGroup = null;

        public void ExecuteCmd(UCommand cmd)
        {
            if (undoGroup == null) { System.Diagnostics.Debug.WriteLine("Null undoGroup"); return; }
            undoGroup.Add(cmd);
            cmd.Execute();
        }

        public void StartUndoGroup()
        {
            redoQueue.Clear();
            if (undoGroup != null) throw new Exception("undoGroup already started");
            undoGroup = new UCommandGroup();
        }

        public void EndUndoGroup()
        {
            undoQueue.AddToBack(undoGroup);
            undoGroup = null;
            if (undoQueue.Count > Properties.Settings.Default.UndoLimit) undoQueue.RemoveFromFront();
        }

        public void Undo() { var cmdg = undoQueue.RemoveFromBack(); cmdg.Unexecute(); redoQueue.AddToBack(cmdg); }
        public void Redo() { var cmdg = redoQueue.RemoveFromBack(); cmdg.Execute(); undoQueue.AddToBack(cmdg); }

        # endregion
    }
}
