using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenUtau.Core.Ustx;

namespace OpenUtau.Core
{
    public abstract class PartCommand : UCommand
    {
        public UProject project;
        public UPart part;
    }

    public class AddPartCommand : PartCommand
    {
        public AddPartCommand(UProject project, UPart part) { this.project = project; this.part = part; }
        public override string ToString() { return "Add part"; }
        public override void Execute() { project.parts.Add(part); }
        public override void Unexecute() { project.parts.Remove(part); }
    }

    public class RemovePartCommand : PartCommand
    {
        public RemovePartCommand(UProject project, UPart part) { this.project = project; this.part = part; }
        public override string ToString() { return "Remove parts"; }
        public override void Execute() { project.parts.Remove(part); }
        public override void Unexecute() { project.parts.Add(part); }
    }

    public class MovePartCommand : PartCommand
    {
        readonly int newPos, oldPos, newTrackNo, oldTrackNo;
        public MovePartCommand(UProject project, UPart part, int newPos, int newTrackNo)
        {
            this.project = project;
            this.part = part;
            this.newPos = newPos;
            this.newTrackNo = newTrackNo;
            this.oldPos = part.PosTick;
            this.oldTrackNo = part.TrackNo;
        }
        public override string ToString() { return "Move parts"; }
        public override void Execute() { part.PosTick = newPos; part.TrackNo = newTrackNo; }
        public override void Unexecute() { part.PosTick = oldPos; part.TrackNo = oldTrackNo; }
    }

    public class ResizePartCommand : PartCommand
    {
        readonly int newDur, oldDur;
        public ResizePartCommand(UProject project, UPart part, int newDur) { this.project = project; this.part = part; this.newDur = newDur; this.oldDur = part.DurTick; }
        public override string ToString() { return "Change parts duration"; }
        public override void Execute() { part.DurTick = newDur; }
        public override void Unexecute() { part.DurTick = oldDur; }
    }
}
