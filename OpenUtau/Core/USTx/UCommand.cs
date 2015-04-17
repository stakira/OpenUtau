using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public abstract class UCommand
    {
        public abstract void Execute();
        public abstract void Unexecute();

        public override abstract string ToString();
    }

    public class UCommandGroup
    {
        public List<UCommand> Commands;
        public UCommandGroup() { Commands = new List<UCommand>(); }
        public override string ToString() { return Commands.Count == 0 ? "No op" : Commands.First().ToString(); }
    }

    public interface ICmdPublisher
    {
        void Subscribe(ICmdSubscriber subscriber);
        void Publish(UCommand cmd, bool isUndo);
    }

    public interface ICmdSubscriber
    {
        void Subscribe(ICmdPublisher publisher);
        void OnNext(UCommand cmd, bool isUndo);
    }
}
