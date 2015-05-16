using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenUtau.Core.USTx;

namespace OpenUtau.Core
{
    public abstract class TrackCommand : UCommand
    {
        public UProject project;
        public UTrack track;
        public void UpdateTrackNo() { for (int i = 0; i < project.Tracks.Count; i++) project.Tracks[i].TrackNo = i; }
    }

    public class AddTrackCommand : TrackCommand
    {
        public AddTrackCommand(UProject project, UTrack track) { this.project = project; this.track = track; }
        public override string ToString() { return "Add track"; }
        public override void Execute()
        {
            if (track.TrackNo < project.Tracks.Count) project.Tracks.Insert(track.TrackNo, track);
            else project.Tracks.Add(track);
            UpdateTrackNo();
        }
        public override void Unexecute() { project.Tracks.Remove(track); UpdateTrackNo(); }
    }

    public class RemoveTrackCommand : TrackCommand
    {
        public RemoveTrackCommand(UProject project, UTrack track) { this.project = project; this.track = track; }
        public override string ToString() { return "Remove track"; }
        public override void Execute() { project.Tracks.Remove(track); UpdateTrackNo(); }
        public override void Unexecute()
        {
            if (track.TrackNo < project.Tracks.Count) project.Tracks.Insert(track.TrackNo, track);
            else project.Tracks.Add(track);
            UpdateTrackNo();
        }
    }
}
