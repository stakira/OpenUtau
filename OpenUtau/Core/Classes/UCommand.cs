using System.Collections.Generic;
using System.Linq;

namespace OpenUtau.Core {
    public abstract class UCommand {
        public abstract void Execute();
        public abstract void Unexecute();

        public override abstract string ToString();
    }

    public class UCommandGroup {
        public List<UCommand> Commands;
        public UCommandGroup() { Commands = new List<UCommand>(); }
        public override string ToString() { return Commands.Count == 0 ? "No op" : Commands.First().ToString(); }
    }

    public interface ICmdSubscriber {
        void OnNext(UCommand cmd, bool isUndo);
    }
}
