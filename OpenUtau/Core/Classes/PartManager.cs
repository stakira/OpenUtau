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
                UpdatePart(partContainer.Part);
            }
        }

        public void UpdatePart(UVoicePart part) {
            if (part == null) {
                return;
            }
            CheckOverlappedNotes(part);
            UpdatePhonemeDurTick(part);
            UpdatePhonemeOto(part);
            UpdateOverlapAdjustment(part);
            UpdateEnvelope(part);
            UpdatePitchBend(part);
            DocManager.Inst.ExecuteCmd(new RedrawNotesNotification(), true);
        }

        private void UpdatePitchBend(UVoicePart part) {
            UNote lastNote = null;
            foreach (UNote note in part.notes) {
                if (note.pitch.snapFirst) {
                    if (note.phonemes.Count > 0 && lastNote != null && (note.phonemes[0].Overlapped || note.position == lastNote.End))
                        note.pitch.data[0].Y = (lastNote.noteNum - note.noteNum) * 10;
                    else
                        note.pitch.data[0].Y = 0;
                }
                lastNote = note;
            }
        }

        public void ResnapPitchBend(UVoicePart part) {
            UNote lastNote = null;
            foreach (UNote note in part.notes) {
                if (!note.pitch.snapFirst) {
                    if (note.phonemes.Count > 0 && note.phonemes[0].Overlapped && lastNote != null)
                        if (note.pitch.data[0].Y == (lastNote.noteNum - note.noteNum) * 10)
                            note.pitch.snapFirst = true;
                }
                lastNote = note;
            }
        }

        private void UpdateEnvelope(UVoicePart part) {
            foreach (UNote note in part.notes) {
                foreach (UPhoneme phoneme in note.phonemes) {
                    Vector2 p0, p1, p2, p3, p4;
                    p0.X = (float)-phoneme.preutter;
                    p1.X = (float)(p0.X + (phoneme.Overlapped ? phoneme.overlap : 5f));
                    p2.X = Math.Max(0f, p1.X);
                    p3.X = (float)(DocManager.Inst.Project.TickToMillisecond(phoneme.Duration) - phoneme.TailIntrude);
                    p4.X = (float)(p3.X + phoneme.TailOverlap);

                    p0.Y = 0f;
                    p1.Y = phoneme.Parent.expressions["vol"].value;
                    p1.X = (float)(p0.X + (phoneme.Overlapped ? phoneme.overlap : 5f) * phoneme.Parent.expressions["acc"].value / 100f);
                    p1.Y = phoneme.Parent.expressions["acc"].value * phoneme.Parent.expressions["vol"].value / 100f;
                    p2.Y = phoneme.Parent.expressions["vol"].value;
                    p3.Y = phoneme.Parent.expressions["vol"].value;
                    p3.X -= (p3.X - p2.X) * phoneme.Parent.expressions["dec"].value / 500f;
                    p3.Y *= 1f - phoneme.Parent.expressions["dec"].value / 100f;
                    p4.Y = 0f;

                    phoneme.envelope.data[0] = p0;
                    phoneme.envelope.data[1] = p1;
                    phoneme.envelope.data[2] = p2;
                    phoneme.envelope.data[3] = p3;
                    phoneme.envelope.data[4] = p4;
                }
            }
        }

        private void UpdateOverlapAdjustment(UVoicePart part) {
            UPhoneme lastPhoneme = null;
            foreach (UNote note in part.notes) {
                foreach (UPhoneme phoneme in note.phonemes) {
                    if (lastPhoneme != null) {
                        int gapTick = phoneme.Parent.position + phoneme.position - lastPhoneme.Parent.position - lastPhoneme.EndPosition;
                        float gapMs = (float)DocManager.Inst.Project.TickToMillisecond(gapTick);
                        if (gapMs < phoneme.preutter) {
                            phoneme.Overlapped = true;
                            float lastDurMs = (float)DocManager.Inst.Project.TickToMillisecond(lastPhoneme.Duration);
                            float correctionRatio = (lastDurMs + Math.Min(0, gapMs)) / 2 / (phoneme.preutter - phoneme.overlap);
                            if (phoneme.preutter - phoneme.overlap > gapMs + lastDurMs / 2) {
                                phoneme.OverlapCorrection = true;
                                phoneme.preutter = gapMs + (phoneme.preutter - gapMs) * correctionRatio;
                                phoneme.overlap *= correctionRatio;
                            } else if (phoneme.preutter > gapMs + lastDurMs) {
                                phoneme.OverlapCorrection = true;
                                phoneme.overlap *= correctionRatio;
                                phoneme.preutter = gapMs + lastDurMs;
                            } else
                                phoneme.OverlapCorrection = false;

                            lastPhoneme.TailIntrude = phoneme.preutter - gapMs;
                            lastPhoneme.TailOverlap = phoneme.overlap;

                        } else {
                            phoneme.Overlapped = false;
                            lastPhoneme.TailIntrude = 0;
                            lastPhoneme.TailOverlap = 0;
                        }
                    } else phoneme.Overlapped = false;
                    lastPhoneme = phoneme;
                }
            }
        }

        private void UpdatePhonemeOto(UVoicePart part) {
            var singer = DocManager.Inst.Project.tracks[part.TrackNo].Singer;
            if (singer == null || !singer.Loaded) return;
            foreach (UNote note in part.notes) {
                foreach (UPhoneme phoneme in note.phonemes) {
                    if (phoneme.AutoRemapped) {
                        if (phoneme.phoneme.StartsWith("?")) {
                            phoneme.phoneme = phoneme.phoneme.Substring(1);
                            phoneme.AutoRemapped = false;
                        } else {
                            string noteString = MusicMath.GetNoteString(note.noteNum);
                            if (singer.PitchMap.ContainsKey(noteString))
                                phoneme.RemappedBank = singer.PitchMap[noteString];
                        }
                    }

                    if (singer.TryGetOto(phoneme.PhonemeRemapped, out UOto oto)) {
                        phoneme.Oto = oto;
                        phoneme.PhonemeError = false;
                        phoneme.overlap = (float)phoneme.Oto.Overlap;
                        phoneme.preutter = (float)phoneme.Oto.Preutter;
                        float vel = phoneme.Parent.expressions["vel"].value;
                        if (vel != 100) {
                            float stretchRatio = (float)Math.Pow(2f, 1.0f - vel / 100f);
                            phoneme.overlap *= stretchRatio;
                            phoneme.preutter *= stretchRatio;
                        }
                    } else {
                        phoneme.PhonemeError = true;
                        phoneme.overlap = 0;
                        phoneme.preutter = 0;
                    }
                }
            }
        }

        private void UpdatePhonemeDurTick(UVoicePart part) {
            UPhoneme lastPhoneme = null;
            foreach (UNote note in part.notes) {
                foreach (UPhoneme phoneme in note.phonemes) {
                    phoneme.Duration = phoneme.Parent.duration - phoneme.position;
                    if (lastPhoneme != null)
                        if (lastPhoneme.Parent == phoneme.Parent)
                            lastPhoneme.Duration = phoneme.position - lastPhoneme.position;
                    lastPhoneme = phoneme;
                }
            }
        }

        private void CheckOverlappedNotes(UVoicePart part) {
            UNote lastNote = null;
            foreach (UNote note in part.notes) {
                if (lastNote != null && lastNote.End > note.position) {
                    lastNote.Error = true;
                    note.Error = true;
                } else note.Error = false;
                lastNote = note;
            }
        }

        # region Cmd Handling

        private void RefreshProject(UProject project) {
            foreach (UPart part in project.parts) {
                if (part is UVoicePart vPart) {
                    UpdatePart(vPart);
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
