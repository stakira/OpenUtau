using OpenUtau.Core.Ustx;

namespace OpenUtau.Core {
    public abstract class ProjectCommand : UCommand {
        public readonly UProject project;

        public ProjectCommand(UProject project) {
            this.project = project;
        }
    }

    public class BpmCommand : ProjectCommand {
        public readonly double newBpm;
        public readonly double oldBpm;

        public BpmCommand(UProject project, double bpm) : base(project) {
            newBpm = bpm;
            oldBpm = project.bpm;
        }

        public override void Execute() => project.bpm = newBpm;
        public override string ToString() => string.Format("Change BPM from {0} to {1}", newBpm, oldBpm);
        public override void Unexecute() => project.bpm = oldBpm;
    }
}
