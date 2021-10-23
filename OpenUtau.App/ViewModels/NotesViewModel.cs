using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

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
        public int TickCount => Part?.Duration ?? 480 * 4;
        public int TrackCount => ViewConstants.MaxTone;
        [Reactive] public double TickWidth { get; set; }
        public double TrackHeightMin => ViewConstants.NoteHeightMin;
        public double TrackHeightMax => ViewConstants.NoteHeightMax;
        [Reactive] public double TrackHeight { get; set; }
        [Reactive] public double TickOrigin { get; set; }
        [Reactive] public double TickOffset { get; set; }
        [Reactive] public double TrackOffset { get; set; }
        [Reactive] public int SnapUnit { get; set; }
        public double SnapUnitWidth => snapUnitWidth.Value;
        [Reactive] public double PlayPosX { get; set; }
        [Reactive] public double PlayPosHighlightX { get; set; }
        [Reactive] public bool CursorTool { get; set; }
        [Reactive] public bool PencilTool { get; set; }
        [Reactive] public bool EraserTool { get; set; }
        [Reactive] public bool TaggerTool { get; set; }
        public ReactiveCommand<string, Unit> SelectToolCommand { get; }
        [Reactive] public bool ShowTips { get; set; }
        [Reactive] public bool PlayTone { get; set; }
        [Reactive] public bool ShowVibrato { get; set; }
        [Reactive] public bool ShowPitch { get; set; }
        [Reactive] public bool ShowPhoneme { get; set; }
        [Reactive] public bool IsSnapOn { get; set; }
        [Reactive] public string SnapUnitText { get; set; }
        [Reactive] public bool ShowExpValueTip { get; set; }
        [Reactive] public string ExpValueTipText { get; set; }
        [Reactive] public string PrimaryKey { get; set; }
        [Reactive] public string SecondaryKey { get; set; }
        [Reactive] public UVoicePart? Part { get; set; }
        [Reactive] public Bitmap? Portrait { get; set; }
        [Reactive] public IBrush? PortraitMask { get; set; }
        public double ViewportTicks => viewportTicks.Value;
        public double ViewportTracks => viewportTracks.Value;
        public double SmallChangeX => smallChangeX.Value;
        public double SmallChangeY => smallChangeY.Value;
        public double HScrollBarMax => Math.Max(0, TickCount - ViewportTicks);
        public double VScrollBarMax => Math.Max(0, TrackCount - ViewportTracks);
        public UProject Project => DocManager.Inst.Project;

        private readonly ObservableAsPropertyHelper<double> snapUnitWidth;
        private readonly ObservableAsPropertyHelper<double> viewportTicks;
        private readonly ObservableAsPropertyHelper<double> viewportTracks;
        private readonly ObservableAsPropertyHelper<double> smallChangeX;
        private readonly ObservableAsPropertyHelper<double> smallChangeY;

        public readonly List<UNote> SelectedNotes = new List<UNote>();
        private readonly HashSet<UNote> TempSelectedNotes = new HashSet<UNote>();

        internal NotesViewModelHitTest HitTest;
        private int _lastNoteLength = 480;
        private string? portraitSource;
        private readonly object portraitLock = new object();

        public NotesViewModel() {
            snapUnitWidth = this.WhenAnyValue(x => x.SnapUnit, x => x.TickWidth)
                .Select(v => v.Item1 * v.Item2)
                .ToProperty(this, v => v.SnapUnitWidth);
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
            this.WhenAnyValue(x => x.Bounds)
                .Subscribe(_ => {
                    OnXZoomed(new Point(), 0);
                    OnYZoomed(new Point(), 0);
                });

            this.WhenAnyValue(x => x.TickWidth)
                .Subscribe(tickWidth => {
                    int div = Project.beatUnit;
                    int ticks = Project.resolution * 4 / Project.beatUnit;
                    double width = ticks * tickWidth;
                    while (width / 2 >= ViewConstants.PianoRollMinTicklineWidth && ticks % 2 == 0) {
                        width /= 2;
                        ticks /= 2;
                        div *= 2;
                    }
                    SnapUnit = ticks;
                    SnapUnitText = $"1/{div}";
                });
            this.WhenAnyValue(x => x.TickOffset)
                .Subscribe(tickOffset => {
                    SetPlayPos(DocManager.Inst.playPosTick, true);
                });

            CursorTool = false;
            PencilTool = true;
            EraserTool = false;
            TaggerTool = false;
            SelectToolCommand = ReactiveCommand.Create<string>(index => {
                CursorTool = index == "1";
                PencilTool = index == "2";
                EraserTool = index == "3";
                TaggerTool = index == "4";
            });

            ShowTips = Core.Util.Preferences.Default.ShowTips;
            PlayTone = true;
            ShowVibrato = true;
            ShowPitch = true;
            ShowPhoneme = true;
            IsSnapOn = true;
            SnapUnitText = string.Empty;

            TickWidth = ViewConstants.PianoRollTickWidthDefault;
            TrackHeight = ViewConstants.NoteHeightDefault;
            ExpValueTipText = string.Empty;
            TrackOffset = 4 * 12 + 6;
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
            bool recenter = true;
            if (TickOffset == 0 && position.X < 0.1) {
                recenter = false;
            }
            double center = TickOffset + position.X * ViewportTicks;
            double tickWidth = TickWidth * (1.0 + delta * 2);
            tickWidth = Math.Clamp(tickWidth, ViewConstants.PianoRollTickWidthMin, ViewConstants.PianoRollTickWidthMax);
            tickWidth = Math.Max(tickWidth, Bounds.Width / TickCount);
            TickWidth = tickWidth;
            double tickOffset = recenter
                    ? center - position.X * ViewportTicks
                    : TickOffset;
            TickOffset = Math.Clamp(tickOffset, 0, HScrollBarMax);
            Notify();
        }

        public void OnYZoomed(Point position, double delta) {
            double center = TrackOffset + position.Y * ViewportTracks;
            double trackHeight = TrackHeight * (1.0 + delta * 2);
            trackHeight = Math.Clamp(trackHeight, ViewConstants.NoteHeightMin, ViewConstants.NoteHeightMax);
            trackHeight = Math.Max(trackHeight, Bounds.Height / TrackCount);
            TrackHeight = trackHeight;
            double trackOffset = center - position.Y * ViewportTracks;
            TrackOffset = Math.Clamp(trackOffset, 0, VScrollBarMax);
            Notify();
        }

        private void Notify() {
            this.RaisePropertyChanged(nameof(TickCount));
            this.RaisePropertyChanged(nameof(HScrollBarMax));
            this.RaisePropertyChanged(nameof(ViewportTicks));
            this.RaisePropertyChanged(nameof(TrackCount));
            this.RaisePropertyChanged(nameof(VScrollBarMax));
            this.RaisePropertyChanged(nameof(ViewportTracks));
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

        public UNote? MaybeAddNote(Point point, bool useLastLength) {
            if (Part == null) {
                return null;
            }
            var project = DocManager.Inst.Project;
            int tone = PointToTone(point);
            if (tone >= ViewConstants.MaxTone || tone < 0) {
                return null;
            }
            UNote note = project.CreateNote(tone, PointToSnappedTick(point),
                useLastLength ? _lastNoteLength : IsSnapOn ? SnapUnit : 15);
            DocManager.Inst.ExecuteCmd(new AddNoteCommand(Part, note));
            return note;
        }

        private void LoadPart(UPart part, UProject project) {
            if (!(part is UVoicePart)) {
                return;
            }
            UnloadPart();
            Part = part as UVoicePart;
            OnPartModified();
            LoadPortrait(part, project);
        }

        private void LoadPortrait(UPart? part, UProject? project) {
            if (part == null || project == null) {
                lock (portraitLock) {
                    Portrait = null;
                    portraitSource = null;
                }
                return;
            }
            var singer = project.tracks[part.trackNo].Singer;
            if (singer == null || string.IsNullOrEmpty(singer.Portrait)) {
                lock (portraitLock) {
                    Portrait = null;
                    portraitSource = null;
                }
                return;
            }
            if (portraitSource != singer.Portrait) {
                lock (portraitLock) {
                    Portrait = null;
                    portraitSource = null;
                }
                PortraitMask = new SolidColorBrush(Colors.White, singer.PortraitOpacity);
                Task.Run(() => {
                    lock (portraitLock) {
                        try {
                            Portrait?.Dispose();
                            using (var stream = File.OpenRead(singer.Portrait)) {
                                var portrait = new Bitmap(stream);
                                if (portrait.Size.Height > 800) {
                                    stream.Seek(0, SeekOrigin.Begin);
                                    portrait = Bitmap.DecodeToHeight(stream, 800);
                                }
                                Portrait = portrait;
                            }
                            portraitSource = singer.Portrait;
                        } catch (Exception e) {
                            Portrait?.Dispose();
                            Portrait = null;
                            portraitSource = null;
                            Log.Error(e, $"Failed to load Portrait {singer.Portrait}");
                        }
                    }
                });
            }
        }

        private void UnloadPart() {
            Part = null;
        }

        private void OnPartModified() {
            if (Part == null) {
                return;
            }
            TickOrigin = Part.position;
            Notify();
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
            if (Part != null && DocManager.Inst.NotesClipboard != null && DocManager.Inst.NotesClipboard.Count > 0) {
                int position = (int)(Math.Ceiling(TickOffset / SnapUnit) * SnapUnit);
                int minPosition = DocManager.Inst.NotesClipboard.Select(note => note.position).Min();
                int offset = position - (int)Math.Floor((double)minPosition / SnapUnit) * SnapUnit;
                var notes = DocManager.Inst.NotesClipboard.Select(note => note.Clone()).ToList();
                notes.ForEach(note => note.position += offset);
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new AddNoteCommand(Part, notes));
                if (Part.Duration < Part.GetMinDurTick(Project)) {
                    DocManager.Inst.ExecuteCmd(new ResizePartCommand(Project, Part, Part.GetBarDurTick(Project)));
                }
                DocManager.Inst.EndUndoGroup();
                DeselectNotes();
                SelectedNotes.AddRange(notes);
                MessageBus.Current.SendMessage(
                    new NotesSelectionEvent(
                        SelectedNotes.ToArray(), TempSelectedNotes.ToArray()));
            }
        }

        public void ToggleVibrato(UNote note) {
            var vibrato = note.vibrato;
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(Part, note, vibrato.length == 0 ? 75f : 0));
            DocManager.Inst.EndUndoGroup();
        }

        private void SetPlayPos(int tick, bool noScroll = false) {
            tick = tick - Part?.position ?? 0;
            double playPosX = TickToneToPoint(tick, 0).X;
            double scroll = 0;
            if (!noScroll && playPosX > PlayPosX) {
                double margin = ViewConstants.PlayPosMarkerMargin * Bounds.Width;
                if (playPosX > margin) {
                    scroll = playPosX - margin;
                }
                TickOffset = Math.Clamp(TickOffset + scroll, 0, HScrollBarMax);
            }
            PlayPosX = playPosX;
            int highlightTick = (int)Math.Floor((double)tick / SnapUnit) * SnapUnit;
            PlayPosHighlightX = TickToneToPoint(highlightTick, 0).X;
        }

        private void FocusNote(UNote note) {
            TickOffset = TickOffset = Math.Clamp(note.position + note.duration * 0.5 - ViewportTicks * 0.5, 0, HScrollBarMax);
            TrackOffset = Math.Clamp(ViewConstants.MaxTone - note.tone + 2 - ViewportTracks * 0.5, 0, VScrollBarMax);
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is UNotification) {
                if (cmd is LoadPartNotification loadPart) {
                    LoadPart(loadPart.part, loadPart.project);
                } else if (cmd is LoadProjectNotification) {
                    UnloadPart();
                    LoadPortrait(null, null);
                } else if (cmd is SelectExpressionNotification selectExp) {
                    SecondaryKey = PrimaryKey;
                    PrimaryKey = selectExp.ExpKey;
                } else if (cmd is SetPlayPosTickNotification setPlayPosTick) {
                    SetPlayPos(setPlayPosTick.playPosTick);
                } else if (cmd is FocusNoteNotification focusNote) {
                    if (focusNote.part == Part) {
                        FocusNote(focusNote.note);
                    }
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
            } else if (cmd is TrackCommand) {
                MessageBus.Current.SendMessage(new NotesRefreshEvent());
                if (cmd is TrackChangeSingerCommand trackChangeSinger) {
                    if (Part != null && trackChangeSinger.track.TrackNo == Part.trackNo) {
                        LoadPortrait(Part, Project);
                    }
                }
            }
        }
    }
}
