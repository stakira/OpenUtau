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
        public void UpdateTrackNo()
        {
            Dictionary<int, int> trackNoRemapTable = new Dictionary<int, int>();
            for (int i = 0; i < project.Tracks.Count; i++)
                if (project.Tracks[i].TrackNo != i)
                {
                    trackNoRemapTable.Add(project.Tracks[i].TrackNo, i);
                    project.Tracks[i].TrackNo = i;
                }
            foreach (var part in project.Parts)
                if (trackNoRemapTable.Keys.Contains(part.TrackNo))
                    part.TrackNo = trackNoRemapTable[part.TrackNo];
        }
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
        public List<UPart> removedParts = new List<UPart>();
        public RemoveTrackCommand(UProject project, UTrack track)
        {
            this.project = project;
            this.track = track;
            foreach (var part in project.Parts)
                if (part.TrackNo == track.TrackNo)
                    removedParts.Add(part);
        }
        public override string ToString() { return "Remove track"; }
        public override void Execute() {
            project.Tracks.Remove(track);
            foreach (var part in removedParts)
            {
                project.Parts.Remove(part);
                part.TrackNo = -1;
            }
            UpdateTrackNo();
        }
        public override void Unexecute()
        {
            if (track.TrackNo < project.Tracks.Count) project.Tracks.Insert(track.TrackNo, track);
            else project.Tracks.Add(track);
            foreach (var part in removedParts) project.Parts.Add(part);
            track.TrackNo = -1;
            UpdateTrackNo();
        }
    }

    public class TrackChangeSingerCommand : TrackCommand
    {
        USinger newSinger, oldSinger;
        public TrackChangeSingerCommand(UProject project, UTrack track, USinger newSinger) { this.project = project; this.track = track; this.newSinger = newSinger; this.oldSinger = track.Singer; }
        public override string ToString() { return "Change singer"; }
        public override void Execute() { track.Singer = newSinger; }
        public override void Unexecute() { track.Singer = oldSinger; }
    }
}
