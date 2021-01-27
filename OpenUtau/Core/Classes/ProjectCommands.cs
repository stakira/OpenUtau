using OpenUtau.Core.USTx;

namespace OpenUtau.Core {
    public class BpmCommand : UCommand {
        public UProject Project { private set; get; }
        public double NewBpm { private set; get; }
        public double OldBpm { private set; get; }

        public BpmCommand(UProject project, double bpm) {
            Project = project;
            NewBpm = bpm;
            OldBpm = project.BPM;
        }

        public override void Execute() => Project.BPM = NewBpm;
        public override string ToString() => string.Format("Change BPM from {0} to {1}", NewBpm, OldBpm);
        public override void Unexecute() => Project.BPM = OldBpm;
    }
}
