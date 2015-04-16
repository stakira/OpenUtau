using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public enum UCommandTypes
    {
        //AddNote,
        //RemoveNote,
        //ChangeNotePos,
        //ChangeNoteDur,
        //ChangeNoteNum,
        ChangeNoteChannel,
        AddVoicePart,
        RemoveVoicePart,
        ChangeVoicePartPos,
        ChangeVoicePartDur,
        ChangeVoicePartTrack,
        AddWavePart,
        RemoveWavePart,
        ChangeWavePartPos,
        ChangeWavePartDur,
        ChangeWavePartTrack,
        //AddTrack,
        //RemoveTrack,
        NewProject,
        LoadProject,
        CloseProject,
        SaveProject
    };

    public abstract class UCommand
    {
        public abstract void Execute();
        public abstract void Unexecute();

        public override abstract string ToString();
    }

    public class UCommandGroup : ICollection<UCommand>
    {
        List<UCommand> commands;

        // Constructor
        public UCommandGroup() { commands = new List<UCommand>(); }

        // ICollection Methods
        public void Add(UCommand item) { commands.Add(item); }
        public void Clear() { commands.Clear(); }
        public bool Contains(UCommand item) { return commands.Contains(item); }
        public void CopyTo(UCommand[] array, int arrayIndex) { commands.CopyTo(array, arrayIndex); }
        public int Count { get { return commands.Count; } }
        public bool IsReadOnly { get { return false; } }
        public bool Remove(UCommand item) { return commands.Remove(item); }
        public IEnumerator<UCommand> GetEnumerator() { return commands.GetEnumerator(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return commands.GetEnumerator(); }
        public UCommand First() { return commands.First(); }
        public UCommand Last() { return commands.Last(); }

        // Command Methods
        public void Execute() { foreach (UCommand cmd in commands) cmd.Execute(); }
        public void Unexecute() { foreach (UCommand cmd in commands) cmd.Unexecute(); }

        public override string ToString() { return base.ToString(); } // TODO
    }
}
