using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public class UNotification : UCommand
    {
        public UProject project;
        public UPart part;
        public override void Execute() { }
        public override void Unexecute() { }
        public override string ToString() { return "Notification"; }
    }

    public class LoadPartNotification : UNotification
    {
        public LoadPartNotification(UPart part, UProject project) { this.part = part; this.project = project; }
        public override string ToString() { return "Load part"; }
    }

    public class LoadProjectNotification : UNotification
    {
        public LoadProjectNotification(UProject project) { this.project = project; }
        public override string ToString() { return "Load project"; }
    }

    public class SaveProjectNotification : UNotification
    {
        public override string ToString() { return "Save project"; }
    }

    public class ChangeExpressionListNotification : UNotification
    {
        public override string ToString() { return "Change expression list"; }
    }

    public class SelectExpressionNotification : UNotification
    {
        public string ExpKey;
        public bool UpdateShadow;
        public int SelectorIndex;
        public SelectExpressionNotification(string expKey, int index, bool updateShadow) { ExpKey = expKey; SelectorIndex = index; UpdateShadow = updateShadow; }
        public override string ToString() { return "Select expression " + ExpKey; }
    }
}
