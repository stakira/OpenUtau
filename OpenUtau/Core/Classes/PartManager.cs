using System;
using System.Numerics;
using System.Threading;

using OpenUtau.Core.Ustx;

namespace OpenUtau.Core {
    public class PartManager : ICmdSubscriber {
        class PartContainer {
            public readonly object _obj = new object();
            public UVoicePart Part;
            public UProject Project;
        }

        readonly Timer timer;

        readonly PartContainer _partContainer = new PartContainer();

        public PartManager() {
            DocManager.Inst.AddSubscriber(this);
            timer = new Timer(Update, _partContainer, 0, 100);
        }

        private void Update(Object state) {
            var partContainer = state as PartContainer;
            lock (partContainer._obj) {
                var part = partContainer.Part;
                if (part == null) {
                    return;
                }
                var project = partContainer.Project;
                var track = project.tracks[partContainer.Part.TrackNo];
                UpdatePart(project, track, part);
            }
        }

        void UpdatePart(UProject project, UTrack track, UVoicePart part) {
            part.Validate(project, track);
            DocManager.Inst.ExecuteCmd(new RedrawNotesNotification(), true);
        }

        # region Cmd Handling

        private void RefreshProject(UProject project) {
            foreach (UPart p in project.parts) {
                if (p is UVoicePart part) {
                    var track = project.tracks[p.TrackNo];
                    UpdatePart(project, track, part);
                }
            }
        }

        # endregion

        # region ICmdSubscriber

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is PartCommand) {
                var _cmd = cmd as PartCommand;
                lock (_partContainer._obj) {
                    if (_cmd.part != _partContainer.Part) {
                        return;
                    }
                    if (_cmd is RemovePartCommand) {
                        _partContainer.Part = null;
                        _partContainer.Project = null;
                    }
                }
            } else if (cmd is BpmCommand) {
                var _cmd = cmd as BpmCommand;
                RefreshProject(_cmd.Project);
            } else if (cmd is UNotification) {
                if (cmd is LoadPartNotification) {
                    var _cmd = cmd as LoadPartNotification;
                    if (!(_cmd.part is UVoicePart)) {
                        return;
                    }
                    lock (_partContainer._obj) {
                        _partContainer.Part = (UVoicePart)_cmd.part;
                        _partContainer.Project = _cmd.project;
                    }
                } else if (cmd is LoadProjectNotification) {
                    var _cmd = cmd as LoadProjectNotification;
                    RefreshProject(_cmd.project);
                } else if (cmd is WillRemoveTrackNotification) {
                    var _cmd = cmd as WillRemoveTrackNotification;
                    lock (_partContainer._obj) {
                        if (_partContainer.Part != null && _cmd.TrackNo == _partContainer.Part.TrackNo) {
                            _partContainer.Part = null;
                            _partContainer.Project = null;
                        }
                    }
                }
            }
        }

        # endregion

    }
}
