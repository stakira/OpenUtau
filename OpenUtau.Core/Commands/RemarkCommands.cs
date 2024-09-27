using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NAudio.CoreAudioApi;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using UtfUnknown.Core.Models.SingleByte.Vietnamese;
using YamlDotNet.Core;

namespace OpenUtau.Core {
    public abstract class RemarkCommands : UCommand {
        public UPart Part;
        public URemark Remark;
        public override ValidateOptions ValidateOptions
            => new ValidateOptions {
                SkipTiming = true,
                Part = Part,
                SkipPhonemizer = true,
            };
        public RemarkCommands(UPart part, URemark remark) {
            Part = part;
            Remark = remark;
        }
    }
    public class AddRemarkCommand : RemarkCommands {
        public AddRemarkCommand(UPart part, URemark remark) : base(part, remark) {
        }
        public override string ToString() { return "Add remark"; }
        public override void Execute() { Part.remarks.Add(Remark); }
        public override void Unexecute() { Part.remarks.Remove(Remark); }
    }

    public class ChangeRemarkCommand : RemarkCommands {
        public int Index;
        public URemark oldRemark;
        public ChangeRemarkCommand(UPart part, URemark remark, int index) : base(part, remark) {
            this.Index = index;
            oldRemark = Part.remarks[Index].Clone();
        }
        public override string ToString() { return "Change remark"; }
        public override void Execute() { Part.remarks[Index].updateRemark(Remark.text, Remark.color, Remark.position); }
        public override void Unexecute() { Part.remarks[Index].updateRemark(oldRemark.text, oldRemark.color, oldRemark.position); }
    }

    public class DeleteRemarkCommand : RemarkCommands {
        public int Index;
        public DeleteRemarkCommand(UPart part, URemark remark, int index) : base(part, remark) {
            this.Index = index;
        }
        public override string ToString() { return "Delete remark"; }
        public override void Execute() { Part.remarks.RemoveAt(Index); }
        public override void Unexecute() { Part.remarks.Insert(Index, Remark); }
    }

    public class MoveRemarkCommand : RemarkCommands {
        public int Index;
        public int Position;
        public int oldPosition;
        public MoveRemarkCommand(UPart part, URemark remark, int index, int position) : base(part, remark) {
            this.Index = index;
            this.Position = position;
            oldPosition = Part.remarks[Index].position;
        }
        public override string ToString() { return "Move remark positon"; }
        public override void Execute() { Part.remarks[Index].position = Position; }
        public override void Unexecute() { Part.remarks[Index].position = oldPosition; }
    }
}
