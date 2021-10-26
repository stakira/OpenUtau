using OpenUtau.Core.Ustx;

namespace OpenUtau.Core {
    public class UNotification : UCommand {
        public UProject project;
        public UPart part;
        public override void Execute() { }
        public override void Unexecute() { }
        public override string ToString() => "Notification";
    }

    /// <summary>
    /// Message for user's information.
    /// </summary>
    public class UserMessageNotification : UNotification {
        public string message;
        public UserMessageNotification(string message) {
            this.message = message;
        }
        public override string ToString() => $"User message: {message}";
    }

    public class LoadPartNotification : UNotification {
        public readonly int tick;
        public LoadPartNotification(UPart part, UProject project, int tick) {
            this.part = part;
            this.project = project;
            this.tick = tick;
        }
        public override string ToString() => "Load part";
    }

    public class LoadProjectNotification : UNotification {
        public LoadProjectNotification(UProject project) {
            this.project = project;
        }
        public override string ToString() => "Load project";
    }

    public class SaveProjectNotification : UNotification {
        public string Path;
        public SaveProjectNotification(string path) {
            Path = path;
        }
        public override string ToString() => "Save project";
    }

    public class RedrawNotesNotification : UNotification {
        public override string ToString() => "Redraw Notes";
    }

    public class ChangeExpressionListNotification : UNotification {
        public override string ToString() => "Change expression list";
    }

    public class SelectExpressionNotification : UNotification {
        public string ExpKey;
        public bool UpdateShadow;
        public int SelectorIndex;
        public SelectExpressionNotification(string expKey, int index, bool updateShadow) {
            ExpKey = expKey;
            SelectorIndex = index;
            UpdateShadow = updateShadow;
        }
        public override string ToString() => $"Select expression {ExpKey}";
    }

    public class ShowPitchExpNotification : UNotification {
        public override string ToString() => "Show pitch expression list";
    }

    public class HidePitchExpNotification : UNotification {
        public override string ToString() => "Hide pitch expression list";
    }

    // Notification for UI to move PlayPosMarker
    public class SetPlayPosTickNotification : UNotification {
        public int playPosTick;
        public override bool Silent => true;
        public SetPlayPosTickNotification(int tick) {
            playPosTick = tick;
        }
        public override string ToString() => $"Set play position to tick {playPosTick}";
    }

    // Notification for playback manager to change play position
    public class SeekPlayPosTickNotification : UNotification {
        public int playPosTick;
        public override bool Silent => true;
        public SeekPlayPosTickNotification(int tick) {
            playPosTick = tick;
        }
        public override string ToString() => $"Seek play position to tick {playPosTick}";
    }

    public class ProgressBarNotification : UNotification {
        public double Progress;
        public string Info;
        public override bool Silent => true;
        public ProgressBarNotification(double progress, string info) {
            Progress = progress;
            Info = info;
        }
        public override string ToString() => $"Set progress {Progress} {Info}";
    }

    public class VolumeChangeNotification : UNotification {
        public double Volume;
        public int TrackNo;
        public override bool Silent => true;
        public VolumeChangeNotification(int trackNo, double volume) {
            TrackNo = trackNo;
            Volume = volume;
        }
        public override string ToString() => $"Set track {TrackNo} volume to {Volume}";
    }

    public class SoloTrackNotification : UNotification {
        public readonly int trackNo;
        public readonly bool solo;
        public SoloTrackNotification(int trackNo, bool solo) {
            this.trackNo = trackNo;
            this.solo = solo;
        }
        public override string ToString() => $"Solo track {solo}";
    }

    public class SingersChangedNotification : UNotification {
        public SingersChangedNotification() { }
        public override string ToString() => "Singers changed.";
    }

    public class WillRemoveTrackNotification : UNotification {
        public int TrackNo;
        public WillRemoveTrackNotification(int trackNo) {
            TrackNo = trackNo;
        }
        public override string ToString() => $"Will remove track {TrackNo}.";
    }

    public class FocusNoteNotification : UNotification {
        public readonly UNote note;
        public FocusNoteNotification(UPart part, UNote note) {
            this.part = part;
            this.note = note;
        }
        public override string ToString() => $"Focus note {note.lyric} at {note.position}.";
    }

    public class PreRenderNotification : UNotification {
        public override string ToString() => $"Pre-render notification.";
    }
}
