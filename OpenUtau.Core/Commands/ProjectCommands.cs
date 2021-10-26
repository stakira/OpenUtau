using System;
using System.Linq;
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
            newBpm = Math.Clamp(bpm, 10, 1000);
            oldBpm = project.bpm;
        }
        public override void Execute() => project.bpm = newBpm;
        public override string ToString() => $"Change BPM from {newBpm} to {oldBpm}";
        public override void Unexecute() => project.bpm = oldBpm;
    }

    public class TimeSignatureCommand : ProjectCommand {
        public readonly int oldBeatPerBar;
        public readonly int oldBeatUnit;
        public readonly int newBeatPerBar;
        public readonly int newBeatUnit;
        public TimeSignatureCommand(UProject project, int beatPerBar, int beatUnit) : base(project) {
            oldBeatPerBar = project.beatPerBar;
            oldBeatUnit = project.beatUnit;
            newBeatPerBar = beatPerBar;
            newBeatUnit = beatUnit;
        }
        public override string ToString() => $"Change time signature for {oldBeatPerBar}/{oldBeatUnit} to {newBeatPerBar}/{newBeatUnit}";
        public override void Execute() {
            project.beatPerBar = newBeatPerBar;
            project.beatUnit = newBeatUnit;
        }
        public override void Unexecute() {
            project.beatPerBar = oldBeatPerBar;
            project.beatUnit = oldBeatUnit;
        }
    }

    public class ConfigureExpressionsCommand : ProjectCommand {
        readonly UExpressionDescriptor[] oldDescriptors;
        readonly UExpressionDescriptor[] newDescriptors;
        public ConfigureExpressionsCommand(
            UProject project,
            UExpressionDescriptor[] descriptors) : base(project) {
            oldDescriptors = project.expressions.Values.ToArray();
            newDescriptors = descriptors;
        }
        public override string ToString() => "Configure expressions";
        public override void Execute() {
            project.expressions = newDescriptors.ToDictionary(descriptor => descriptor.abbr);
            project.parts
                .Where(part => part is UVoicePart)
                .ToList()
                .ForEach(part => part.AfterLoad(project, project.tracks[part.trackNo]));
            project.Validate();
        }
        public override void Unexecute() {
            project.expressions = oldDescriptors.ToDictionary(descriptor => descriptor.abbr);
            project.parts
                .Where(part => part is UVoicePart)
                .ToList()
                .ForEach(part => part.AfterLoad(project, project.tracks[part.trackNo]));
            project.Validate();
        }
    }
}
