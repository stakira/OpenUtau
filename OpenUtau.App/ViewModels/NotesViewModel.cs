using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reactive.Linq;
using Avalonia;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class NotesRefreshEvent { }
    public class NotesSelectionEvent {
        public readonly UNote[] selectedNotes;
        public readonly UNote[] tempSelectedNotes;
        public NotesSelectionEvent(UNote[] selectedNotes, UNote[] tempSelectedNotes) {
            this.selectedNotes = selectedNotes;
            this.tempSelectedNotes = tempSelectedNotes;
        }
    }
    public class NoteResizeEvent {
        public readonly UNote note;
        public NoteResizeEvent(UNote note) { this.note = note; }
    }
    public class NoteMoveEvent {
        public readonly UNote note;
        public NoteMoveEvent(UNote note) { this.note = note; }
    }

    public class NotesViewModel : ViewModelBase, ICmdSubscriber {
        [Reactive] public Rect Bounds { get; set; }
        [Reactive] public int TickCount { get; set; }
        public int TrackCount => ViewConstants.MaxTone;
        public double TickWidth {
            get => tickWidth;
            set => this.RaiseAndSetIfChanged(ref tickWidth, Math.Clamp(value, ViewConstants.PianoRollTickWidthMin, ViewConstants.PianoRollTickWidthMax));
        }
        public double TrackHeightMin => ViewConstants.NoteHeightMin;
        public double TrackHeightMax => ViewConstants.NoteHeightMax;
        public double TrackHeight {
            get => trackHeight;
            set => this.RaiseAndSetIfChanged(ref trackHeight, Math.Clamp(value, ViewConstants.NoteHeightMin, ViewConstants.NoteHeightMax));
        }
        [Reactive] public double TickOrigin { get; set; }
        [Reactive] public double TickOffset { get; set; }
        [Reactive] public double TrackOffset { get; set; }
        [Reactive] public int SnapUnit { get; set; }
        [Reactive] public bool IsSnapOn { get; set; }
        [Reactive] public bool ShowPitch { get; set; }
        [Reactive] public bool ShowVibrato { get; set; }
        [Reactive] public bool ShowPhoneme { get; set; }
        [Reactive] public bool ShowTips { get; set; }
        [Reactive] public bool ShowExpValueTip { get; set; }
        [Reactive] public string ExpValueTipText { get; set; }
        [Reactive] public string PrimaryKey { get; set; }
        [Reactive] public string SecondaryKey { get; set; }
        [Reactive] public UVoicePart? Part { get; set; }
        public double ViewportTicks => viewportTicks.Value;
        public double ViewportTracks => viewportTracks.Value;
        public double SmallChangeX => smallChangeX.Value;
        public double SmallChangeY => smallChangeY.Value;
        public UProject Project => DocManager.Inst.Project;

        private double tickWidth = ViewConstants.PianoRollTickWidthDefault;
        private double trackHeight = ViewConstants.NoteHeightDefault;
        private readonly ObservableAsPropertyHelper<double> viewportTicks;
        private readonly ObservableAsPropertyHelper<double> viewportTracks;
        private readonly ObservableAsPropertyHelper<double> smallChangeX;
        private readonly ObservableAsPropertyHelper<double> smallChangeY;

        public readonly List<UNote> SelectedNotes = new List<UNote>();
        private readonly HashSet<UNote> TempSelectedNotes = new HashSet<UNote>();

        internal NotesViewModelHitTest HitTest;
        private int _lastNoteLength = 480;

        public NotesViewModel() {
            viewportTicks = this.WhenAnyValue(x => x.Bounds, x => x.TickWidth)
                .Select(v => v.Item1.Width / v.Item2)
                .ToProperty(this, x => x.ViewportTicks);
            viewportTracks = this.WhenAnyValue(x => x.Bounds, x => x.TrackHeight)
                .Select(v => v.Item1.Height / v.Item2)
                .ToProperty(this, x => x.ViewportTracks);
            smallChangeX = this.WhenAnyValue(x => x.ViewportTicks)
                .Select(w => w / 8)
                .ToProperty(this, x => x.SmallChangeX);
            smallChangeY = this.WhenAnyValue(x => x.ViewportTracks)
                .Select(h => h / 8)
                .ToProperty(this, x => x.SmallChangeY);

            this.WhenAnyValue(x => x.TickWidth)
                .Subscribe(tickWidth => {
                    int ticks = Project.resolution * 4 / Project.beatUnit;
                    double width = ticks * tickWidth;
                    while (width / 2 >= ViewConstants.PianoRollMinTicklineWidth && ticks % 2 == 0) {
                        width /= 2;
                        ticks /= 2;
                    }
                    SnapUnit = ticks;
                });

            TrackOffset = 4 * 12 + 6;
            IsSnapOn = true;
            ShowPitch = true;
            ShowVibrato = true;
            ShowPhoneme = true;
            ShowTips = Core.Util.Preferences.Default.ShowTips;
            if (Core.Util.Preferences.Default.ShowTips) {
                Core.Util.Preferences.Default.ShowTips = false;
                Core.Util.Preferences.Save();
            }
            PrimaryKey = "vel";
            SecondaryKey = "vol";

            HitTest = new NotesViewModelHitTest(this);
            DocManager.Inst.AddSubscriber(this);
        }

        public void OnXZoomed(Point position, double delta) {
            double tick = TickOffset;
            bool recenter = true;
            if (TickOffset == 0 && position.X < 0.1) {
                recenter = false;
            }
            double center = TickOffset + position.X * ViewportTicks;
            double before = TickWidth;
            TickWidth *= 1.0 + delta * 2;
            if (before == TickWidth) {
                return;
            }
            if (recenter) {
                tick = Math.Max(0, center - position.X * ViewportTicks);
            }
            if (TickOffset != tick) {
                TickOffset = tick;
            } else {
                // Force a redraw when only ViewportWidth is changed.
                TickOffset = tick + 1;
                TickOffset = tick - 1;
            }
        }

        public void OnYZoomed(Point position, double delta) {
            TrackHeight *= 1.0 + delta;
            double track = TrackOffset;
            TrackOffset = track + 1;
            TrackOffset = track - 1;
            TrackOffset = track;
        }

        public int PointToTick(Point point) {
            return (int)(point.X / TickWidth + TickOffset);
        }
        public int PointToSnappedTick(Point point) {
            int tick = (int)(point.X / TickWidth + TickOffset);
            return (int)((double)tick / SnapUnit) * SnapUnit;
        }
        public int PointToTone(Point point) {
            return ViewConstants.MaxTone - 1 - (int)(point.Y / TrackHeight + TrackOffset);
        }
        public double PointToToneDouble(Point point) {
            return ViewConstants.MaxTone - 1 - (point.Y / TrackHeight + TrackOffset) + 0.5;
        }
        public Point TickToneToPoint(double tick, double tone) {
            return new Point(
                (tick - TickOffset) * TickWidth,
                (ViewConstants.MaxTone - 1 - tone - TrackOffset) * TrackHeight);
        }
        public Point TickToneToPoint(Vector2 tickTone) {
            return TickToneToPoint(tickTone.X, tickTone.Y);
        }
        public Size TickToneToSize(double ticks, double tone) {
            return new Size(ticks * TickWidth, tone * TrackHeight);
        }

        public UNote? MaybeAddNote(Point point) {
            if (Part == null) {
                return null;
            }
            var project = DocManager.Inst.Project;
            int tone = PointToTone(point);
            if (tone >= ViewConstants.MaxTone || tone < 0) {
                return null;
            }
            UNote note = project.CreateNote(tone, PointToSnappedTick(point), _lastNoteLength);
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new AddNoteCommand(Part, note));
            DocManager.Inst.EndUndoGroup();
            return note;
        }

        private void LoadPart(UPart part, UProject project) {
            if (!(part is UVoicePart)) {
                return;
            }
            UnloadPart();
            Part = part as UVoicePart;
            OnPartModified();
        }

        private void UnloadPart() {
            Part = null;
        }

        private void OnPartModified() {
            if (Part == null) {
                return;
            }
            TickOrigin = Part.position;
            TickCount = Part.Duration;
        }

        public void DeselectNotes() {
            SelectedNotes.Clear();
            TempSelectedNotes.Clear();
            MessageBus.Current.SendMessage(
                new NotesSelectionEvent(
                    SelectedNotes.ToArray(), TempSelectedNotes.ToArray()));
        }

        public void SelectAllNotes() {
            DeselectNotes();
            if (Part == null) {
                return;
            }
            SelectedNotes.AddRange(Part.notes);
            MessageBus.Current.SendMessage(
                new NotesSelectionEvent(
                    SelectedNotes.ToArray(), TempSelectedNotes.ToArray()));
        }

        public void TempSelectNotes(int x0, int x1, int y0, int y1) {
            TempSelectedNotes.Clear();
            if (Part == null) {
                return;
            }
            foreach (var note in Part.notes) {
                if (note.End > x0 && note.position < x1 && note.tone > y0 && note.tone <= y1) {
                    TempSelectedNotes.Add(note);
                }
            }
            MessageBus.Current.SendMessage(
                new NotesSelectionEvent(
                    SelectedNotes.ToArray(), TempSelectedNotes.ToArray()));
        }

        public void CommitTempSelectNotes() {
            var newSelection = SelectedNotes.Union(TempSelectedNotes).ToList();
            SelectedNotes.Clear();
            SelectedNotes.AddRange(newSelection);
            TempSelectedNotes.Clear();
            MessageBus.Current.SendMessage(
                new NotesSelectionEvent(
                    SelectedNotes.ToArray(), TempSelectedNotes.ToArray()));
        }

        public void TransposeSelection(int deltaNoteNum) {
            if (SelectedNotes.Count <= 0) {
                return;
            }
            if (SelectedNotes.Any(note => note.tone + deltaNoteNum <= 0 || note.tone + deltaNoteNum >= ViewConstants.MaxTone)) {
                return;
            }
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new MoveNoteCommand(Part, new List<UNote>(SelectedNotes), 0, deltaNoteNum));
            DocManager.Inst.EndUndoGroup();
        }

        internal void DeleteSelectedNotes() {
            if (SelectedNotes.Count <= 0) {
                return;
            }
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(Part, new List<UNote>(SelectedNotes)));
            DocManager.Inst.EndUndoGroup();
        }

        public void CopyNotes() {
            if (SelectedNotes.Count > 0) {
                DocManager.Inst.NotesClipboard = SelectedNotes.Select(note => note.Clone()).ToList();
            }
        }

        public void CutNotes() {
            if (SelectedNotes.Count > 0) {
                DocManager.Inst.NotesClipboard = SelectedNotes.Select(note => note.Clone()).ToList();
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(Part, SelectedNotes));
                DocManager.Inst.EndUndoGroup();
            }
        }

        public void PasteNotes() {
            if (DocManager.Inst.NotesClipboard != null && DocManager.Inst.NotesClipboard.Count > 0) {
                int position = (int)(Math.Ceiling(TickOffset / SnapUnit) * SnapUnit);
                int minPosition = DocManager.Inst.NotesClipboard.Select(note => note.position).Min();
                int offset = position - (int)Math.Floor((double)minPosition / SnapUnit) * SnapUnit;
                var notes = DocManager.Inst.NotesClipboard.Select(note => note.Clone()).ToList();
                notes.ForEach(note => note.position += offset);
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new AddNoteCommand(Part, notes));
                DocManager.Inst.EndUndoGroup();
                DeselectNotes();
                SelectedNotes.AddRange(notes);
                MessageBus.Current.SendMessage(
                    new NotesSelectionEvent(
                        SelectedNotes.ToArray(), TempSelectedNotes.ToArray()));
            }
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is UNotification) {
                if (cmd is LoadPartNotification loadPart) {
                    LoadPart(loadPart.part, loadPart.project);
                } else if (cmd is LoadProjectNotification) {
                    UnloadPart();
                } else if (cmd is SelectExpressionNotification selectExp) {
                    SecondaryKey = PrimaryKey;
                    PrimaryKey = selectExp.ExpKey;
                }
            } else if (cmd is PartCommand partCommand) {
                if (cmd is ReplacePartCommand replacePart) {
                    if (!isUndo) {
                        LoadPart(replacePart.part, replacePart.project);
                    } else {
                        LoadPart(replacePart.newPart, replacePart.project);
                    }
                }
                if (partCommand.part != Part) {
                    return;
                }
                if (cmd is RemovePartCommand) {
                    if (!isUndo) {
                        UnloadPart();
                    }
                } else if (cmd is ResizePartCommand) {
                    OnPartModified();
                } else if (cmd is MovePartCommand) {
                    OnPartModified();
                }
            } else if (cmd is NoteCommand noteCommand) {
                if (noteCommand.Part == Part) {
                    MessageBus.Current.SendMessage(new NotesRefreshEvent());
                }
            } else if (cmd is ExpCommand) {
                MessageBus.Current.SendMessage(new NotesRefreshEvent());
            }
        }
    }
}
