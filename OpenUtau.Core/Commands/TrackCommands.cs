using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core {
    public abstract class TrackCommand : UCommand {
        public UProject project;
        public UTrack track;
        public override ValidateOptions ValidateOptions => new ValidateOptions {
            SkipTiming = true,
        };
        public void UpdateTrackNo() {
            Dictionary<int, int> trackNoRemapTable = new Dictionary<int, int>();
            for (int i = 0; i < project.tracks.Count; i++) {
                if (project.tracks[i].TrackNo != i) {
                    trackNoRemapTable.Add(project.tracks[i].TrackNo, i);
                    project.tracks[i].TrackNo = i;
                }
            }

            foreach (var part in project.parts) {
                if (trackNoRemapTable.Keys.Contains(part.trackNo))
                    part.trackNo = trackNoRemapTable[part.trackNo];
            }
        }
    }

    public class AddTrackCommand : TrackCommand {
        public AddTrackCommand(UProject project, UTrack track) { this.project = project; this.track = track; }
        public override string ToString() { return "Add track"; }
        public override void Execute() {
            if (track.TrackNo < project.tracks.Count) project.tracks.Insert(track.TrackNo, track);
            else project.tracks.Add(track);
            UpdateTrackNo();
        }
        public override void Unexecute() { project.tracks.Remove(track); UpdateTrackNo(); }
    }

    public class RemoveTrackCommand : TrackCommand {
        public List<UPart> removedParts = new List<UPart>();
        public RemoveTrackCommand(UProject project, UTrack track) {
            this.project = project;
            this.track = track;
            foreach (var part in project.parts) {
                if (part.trackNo == track.TrackNo)
                    removedParts.Add(part);
            }
        }
        public override string ToString() { return "Remove track"; }
        public override void Execute() {
            project.tracks.Remove(track);
            foreach (var part in removedParts) {
                project.parts.Remove(part);
                part.trackNo = -1;
            }
            UpdateTrackNo();
        }
        public override void Unexecute() {
            if (track.TrackNo < project.tracks.Count)
                project.tracks.Insert(track.TrackNo, track);
            else
                project.tracks.Add(track);
            foreach (var part in removedParts)
                project.parts.Add(part);
            track.TrackNo = -1;
            UpdateTrackNo();
        }
    }

    public class MoveTrackCommand : TrackCommand {
        public readonly int index;
        public MoveTrackCommand(UProject project, UTrack track, bool up) {
            this.project = project;
            this.track = track;
            index = track.TrackNo + (up ? -1 : 0);
        }
        public override string ToString() => "Move track";
        public override void Execute() {
            project.tracks.Reverse(index, 2);
            UpdateTrackNo();
        }
        public override void Unexecute() {
            project.tracks.Reverse(index, 2);
            UpdateTrackNo();
        }
    }

    public class TrackChangeSingerCommand : TrackCommand {
        readonly USinger newSinger, oldSinger;
        public TrackChangeSingerCommand(UProject project, UTrack track, USinger newSinger) {
            this.project = project;
            this.track = track;
            this.newSinger = newSinger;
            this.oldSinger = track.Singer;
        }
        public override string ToString() { return "Change singer"; }
        public override void Execute() { track.Singer = newSinger; }
        public override void Unexecute() { track.Singer = oldSinger; }
    }

    public class TrackChangePhonemizerCommand : TrackCommand {
        readonly Phonemizer newPhonemizer, oldPhonemizer;
        public TrackChangePhonemizerCommand(UProject project, UTrack track, Phonemizer newPhonemizer) {
            this.project = project;
            this.track = track;
            this.newPhonemizer = newPhonemizer;
            this.oldPhonemizer = track.Phonemizer;
        }
        public override string ToString() { return "Change phonemizer"; }
        public override void Execute() {
            track.Phonemizer = newPhonemizer;
            track.Phonemizer.SetSinger(track.Singer);
        }
        public override void Unexecute() {
            track.Phonemizer = oldPhonemizer;
            track.Phonemizer.SetSinger(track.Singer);
        }
    }

    public class TrackChangeRendererCommand : TrackCommand {
        readonly string newRenderer, oldRenderer;
        public TrackChangeRendererCommand(UProject project, UTrack track, string newRenderer) {
            this.project = project;
            this.track = track;
            this.newRenderer = newRenderer;
            this.oldRenderer = track.Renderer?.ToString();
        }
        public override string ToString() { return "Change phonemizer"; }
        public override void Execute() {
            track.Renderer = Render.Renderers.CreateRenderer(newRenderer);
        }
        public override void Unexecute() {
            track.Renderer = Render.Renderers.CreateRenderer(oldRenderer);
        }
    }
}
