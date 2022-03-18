using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core {
    public abstract class UCommand {
        public virtual bool Silent => false;
        public virtual UPart ValidatePart => null;
        public abstract void Execute();
        public abstract void Unexecute();
        public virtual bool Mergeable => false;
        public virtual UCommand Merge(IList<UCommand> commands) => null;
        public abstract override string ToString();
    }

    public class UCommandGroup {
        public List<UCommand> Commands;
        public UCommandGroup() { Commands = new List<UCommand>(); }
        public void Merge() {
            if (Commands.Count > 0 && Commands.Last().Mergeable) {
                var merged = Commands.Last().Merge(Commands);
                Commands.Clear();
                Commands.Add(merged);
            }
        }
        public override string ToString() { return Commands.Count == 0 ? "No op" : Commands.First().ToString(); }
    }

    public interface ICmdSubscriber {
        void OnNext(UCommand cmd, bool isUndo);
    }
}
