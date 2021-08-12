using OpenUtau.Core.Ustx;

namespace OpenUtau.Core {
    public class BpmCommand : UCommand {
        public UProject Project { private set; get; }
        public double NewBpm { private set; get; }
        public double OldBpm { private set; get; }

        public BpmCommand(UProject project, double bpm) {
            Project = project;
            NewBpm = bpm;
            OldBpm = project.bpm;
        }

        public override void Execute() => Project.bpm = NewBpm;
        public override string ToString() => string.Format("Change BPM from {0} to {1}", NewBpm, OldBpm);
        public override void Unexecute() => Project.bpm = OldBpm;
    }
}
