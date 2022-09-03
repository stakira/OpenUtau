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
using DynamicData;
using DynamicData.Binding;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
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
    public class WaveformRefreshEvent { }

    public class NotesViewModel : ViewModelBase, ICmdSubscriber {
        [Reactive] public Rect Bounds { get; set; }
        public int TickCount => Part?.Duration ?? 480 * 4;
        public int TrackCount => ViewConstants.MaxTone;
        [Reactive] public double TickWidth { get; set; }
        public double TrackHeightMin => ViewConstants.NoteHeightMin;
        public double TrackHeightMax => ViewConstants.NoteHeightMax;
        [Reactive] public double TrackHeight { get; set; }
        [Reactive] public int TickOrigin { get; set; }
        [Reactive] public double TickOffset { get; set; }
        [Reactive] public double TrackOffset { get; set; }
        [Reactive] public int SnapDiv { get; set; }
        public ObservableCollectionExtended<int> SnapTicks { get; } = new ObservableCollectionExtended<int>();
        [Reactive] public double PlayPosX { get; set; }
        [Reactive] public double PlayPosHighlightX { get; set; }
        [Reactive] public double PlayPosHighlightWidth { get; set; }
        [Reactive] public bool PlayPosWaitingRendering { get; set; }
        [Reactive] public bool CursorTool { get; set; }
        [Reactive] public bool PenTool { get; set; }
        [Reactive] public bool PenPlusTool { get; set; }
        [Reactive] public bool EraserTool { get; set; }
        [Reactive] public bool DrawPitchTool { get; set; }
        [Reactive] public bool KnifeTool { get; set; }
        public ReactiveCommand<string, Unit> SelectToolCommand { get; }
        [Reactive] public bool ShowTips { get; set; }
        [Reactive] public bool PlayTone { get; set; }
        [Reactive] public bool ShowVibrato { get; set; }
        [Reactive] public bool ShowPitch { get; set; }
        [Reactive] public bool ShowFinalPitch { get; set; }
        [Reactive] public bool ShowWaveform { get; set; }
        [Reactive] public bool ShowPhoneme { get; set; }
        [Reactive] public bool IsSnapOn { get; set; }
        [Reactive] public string SnapDivText { get; set; }
        [Reactive] public Rect ExpBounds { get; set; }
        [Reactive] public string PrimaryKey { get; set; }
        [Reactive] public bool PrimaryKeyNotSupported { get; set; }
        [Reactive] public string SecondaryKey { get; set; }
        [Reactive] public double ExpTrackHeight { get; set; }
        [Reactive] public double ExpShadowOpacity { get; set; }
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
        [Reactive] public List<MenuItemViewModel> SnapDivs { get; set; }

        public ReactiveCommand<int, Unit> SetSnapUnitCommand { get; set; }

        // See the comments on TracksViewModel.playPosXToTickOffset
        private double playPosXToTickOffset => ViewportTicks / Bounds.Width;

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
        private int userSnapDiv = -2;

        public NotesViewModel() {
            SnapDivs = new List<MenuItemViewModel>();
            SetSnapUnitCommand = ReactiveCommand.Create<int>(div => {
                userSnapDiv = div;
                UpdateSnapDiv();
            });

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
                    UpdateSnapDiv();
                    SetPlayPos(DocManager.Inst.playPosTick, false);
                });
            this.WhenAnyValue(x => x.TickOffset)
                .Subscribe(tickOffset => {
                    SetPlayPos(DocManager.Inst.playPosTick, false);
                });
            this.WhenAnyValue(x => x.ExpBounds, x => x.PrimaryKey)
                .Subscribe(t => {
                    if (t.Item2 != null &&
                        Project.expressions.TryGetValue(t.Item2, out var descriptor) &&
                        descriptor.type == UExpressionType.Options &&
                        descriptor.options.Length > 0) {
                        ExpTrackHeight = t.Item1.Height / descriptor.options.Length;
                        ExpShadowOpacity = 0;
                    } else {
                        ExpTrackHeight = 0;
                        ExpShadowOpacity = 0.3;
                    }
                });
            this.WhenAnyValue(x => x.Project)
                .Subscribe(project => {
                    if (project == null) {
                        return;
                    }
                    SnapDivs.Clear();
                    SnapDivs.Add(new MenuItemViewModel {
                        Header = ThemeManager.GetString("pianoroll.toggle.snap.auto"),
                        Command = SetSnapUnitCommand,
                        CommandParameter = -2,
                    });
                    SnapDivs.Add(new MenuItemViewModel {
                        Header = ThemeManager.GetString("pianoroll.toggle.snap.autotriplet"),
                        Command = SetSnapUnitCommand,
                        CommandParameter = -3,
                    });
                    SnapDivs.AddRange(MusicMath.GetSnapDivs(project.resolution)
                        .Select(div => new MenuItemViewModel {
                            Header = $"1/{div}",
                            Command = SetSnapUnitCommand,
                            CommandParameter = div,
                        }));
                });

            CursorTool = false;
            PenTool = true;
            PenPlusTool = false;
            EraserTool = false;
            DrawPitchTool = false;
            KnifeTool = false;
            SelectToolCommand = ReactiveCommand.Create<string>(index => {
                CursorTool = index == "1";
                PenTool = index == "2";
                PenPlusTool = index == "2+";
                EraserTool = index == "3";
                DrawPitchTool = index == "4";
                KnifeTool = index == "5";
            });

            ShowTips = Preferences.Default.ShowTips;
            PlayTone = true;
            ShowVibrato = true;
            ShowPitch = true;
            ShowFinalPitch = true;
            ShowWaveform = true;
            ShowPhoneme = true;
            IsSnapOn = true;
            SnapDivText = string.Empty;

            TickWidth = ViewConstants.PianoRollTickWidthDefault;
            TrackHeight = ViewConstants.NoteHeightDefault;
            TrackOffset = 4 * 12 + 6;
            if (Preferences.Default.ShowTips) {
                Preferences.Default.ShowTips = false;
                Preferences.Save();
            }
            PrimaryKey = Core.Format.Ustx.VEL;
            SecondaryKey = Core.Format.Ustx.VOL;

            HitTest = new NotesViewModelHitTest(this);
            DocManager.Inst.AddSubscriber(this);
        }

        private void UpdateSnapDiv() {
            if (userSnapDiv > 0) {
                SnapDiv = userSnapDiv;
                SnapDivText = $"1/{userSnapDiv}";
                return;
            }
            MusicMath.GetSnapUnit(
                Project.resolution,
                ViewConstants.PianoRollMinTicklineWidth / TickWidth,
                userSnapDiv % 3 == 0,
                out int ticks,
                out int div);
            SnapDiv = div;
            SnapDivText = $"(1/{div})";
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

        public void TickToLineTick(int tick, out int left, out int right) {
            if (SnapTicks.Count == 0) {
                left = 0;
                right = Project.resolution;
                return;
            }
            int index = SnapTicks.BinarySearch(tick + TickOrigin);
            if (index < 0) {
                index = ~index - 1;
            }
            index = Math.Min(index, SnapTicks.Count - 2);
            index = Math.Max(index, 0);
            left = SnapTicks[index] - TickOrigin;
            right = SnapTicks[index + 1] - TickOrigin;
        }

        public void PointToLineTick(Point point, out int left, out int right) {
            int tick = PointToTick(point);
            TickToLineTick(tick, out left, out right);
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
            int snapUnit = project.resolution * 4 / SnapDiv;
            int tick = PointToTick(point);
            int snappedTick = (int)Math.Floor((double)tick / snapUnit) * snapUnit;
            UNote note = project.CreateNote(tone, snappedTick,
                useLastLength ? _lastNoteLength : IsSnapOn ? snappedTick : 15);
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
            if (singer == null || string.IsNullOrEmpty(singer.Portrait) || !Preferences.Default.ShowPortrait) {
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
                            var data = singer.LoadPortrait();
                            if (data == null) {
                                Portrait = null;
                                portraitSource = null;
                            } else {
                                using (var stream = new MemoryStream(data)) {
                                    var portrait = new Bitmap(stream);
                                    if (portrait.Size.Height > 800) {
                                        int width = (int)Math.Round(800 * portrait.Size.Width / portrait.Size.Height);
                                        portrait = portrait.CreateScaledBitmap(new PixelSize(width, 800));
                                    }
                                    Portrait = portrait;
                                    portraitSource = singer.Portrait;
                                }
                            }
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
            DeselectNotes();
            Part = null;
            LoadPortrait(null, null);
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

        public void SelectNote(UNote note) {
            TempSelectedNotes.Clear();
            if (Part == null) {
                return;
            }
            SelectedNotes.Add(note);
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

        public void CleanupSelectedNotes() {
            if (Part == null) {
                return;
            }
            var except = SelectedNotes.Except(Part.notes).ToHashSet();
            SelectedNotes.RemoveAll(note => except.Contains(note));
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
                TickToLineTick((int)TickOffset, out int left, out int right);
                int minPosition = DocManager.Inst.NotesClipboard.Select(note => note.position).Min();
                int offset = right - minPosition;
                var notes = DocManager.Inst.NotesClipboard.Select(note => note.Clone()).ToList();
                notes.ForEach(note => note.position += offset);
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new AddNoteCommand(Part, notes));
                int minDurTick = Part.GetMinDurTick(Project);
                if (Part.Duration < minDurTick) {
                    DocManager.Inst.ExecuteCmd(new ResizePartCommand(Project, Part, minDurTick));
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
            DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(Part, note, vibrato.length == 0 ? NotePresets.Default.DefaultVibrato.VibratoLength : 0));
            DocManager.Inst.EndUndoGroup();
        }

        private void SetPlayPos(int tick, bool waitingRendering) {
            PlayPosWaitingRendering = waitingRendering;
            if (waitingRendering) {
                return;
            }
            tick -= Part?.position ?? 0;
            PlayPosX = TickToneToPoint(tick, 0).X;
            TickToLineTick(tick, out int left, out int right);
            PlayPosHighlightX = TickToneToPoint(left, 0).X;
            PlayPosHighlightWidth = (right - left) * TickWidth;
        }

        private void FocusNote(UNote note) {
            TickOffset = TickOffset = Math.Clamp(note.position + note.duration * 0.5 - ViewportTicks * 0.5, 0, HScrollBarMax);
            TrackOffset = Math.Clamp(ViewConstants.MaxTone - note.tone + 2 - ViewportTracks * 0.5, 0, VScrollBarMax);
        }

        internal (UNote[], string[]) PrepareInsertLyrics() {
            var ordered = SelectedNotes.OrderBy(n => n.position);
            var first = ordered.First();
            var last = ordered.Last();
            List<UNote> notes = new List<UNote>();
            var note = first;
            while (note != last) {
                notes.Add(note);
                note = note.Next;
            }
            notes.Add(note);
            var lyrics = notes.Select(n => n.lyric).ToArray();
            while (note.Next != null) {
                note = note.Next;
                notes.Add(note);
            }
            return (notes.ToArray(), lyrics);
        }

        bool IsExpSupported(string expKey) {
            if (Project == null || Part == null) {
                return true;
            }
            var track = Project.tracks[Part.trackNo];
            if (track.Renderer == null) {
                return true;
            }
            if (Project.expressions.TryGetValue(expKey, out var descriptor)) {
                return track.Renderer.SupportsExpression(descriptor);
            }
            if (expKey == track.VoiceColorExp.abbr) {
                return track.Renderer.SupportsExpression(track.VoiceColorExp);
            }
            return true;
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is UNotification notif) {
                if (cmd is LoadPartNotification loadPart) {
                    LoadPart(loadPart.part, loadPart.project);
                    double tickOffset = loadPart.tick - loadPart.part.position - Bounds.Width / TickWidth / 2;
                    TickOffset = Math.Clamp(tickOffset, 0, HScrollBarMax);
                    PrimaryKeyNotSupported = !IsExpSupported(PrimaryKey);
                } else if (cmd is LoadProjectNotification) {
                    UnloadPart();
                    LoadPortrait(null, null);
                    PrimaryKeyNotSupported = !IsExpSupported(PrimaryKey);
                } else if (cmd is SelectExpressionNotification selectExp) {
                    SecondaryKey = PrimaryKey;
                    PrimaryKey = selectExp.ExpKey;
                    PrimaryKeyNotSupported = !IsExpSupported(PrimaryKey);
                } else if (cmd is SetPlayPosTickNotification setPlayPosTick) {
                    SetPlayPos(setPlayPosTick.playPosTick, setPlayPosTick.waitingRendering);
                    MaybeAutoScroll();
                } else if (cmd is FocusNoteNotification focusNote) {
                    if (focusNote.part == Part) {
                        FocusNote(focusNote.note);
                    }
                } else if (cmd is ValidateProjectNotification
                    || cmd is SingersRefreshedNotification
                    || cmd is PhonemizedNotification) {
                    OnPartModified();
                    MessageBus.Current.SendMessage(new NotesRefreshEvent());
                } else if (notif is PartRenderedNotification && notif.part == Part) {
                    MessageBus.Current.SendMessage(new WaveformRefreshEvent());
                }
            } else if (cmd is PartCommand partCommand) {
                if (cmd is ReplacePartCommand replacePart) {
                    if (!isUndo) {
                        LoadPart(replacePart.newPart, replacePart.project);
                    } else {
                        LoadPart(replacePart.part, replacePart.project);
                    }
                }
                if (partCommand.part != Part) {
                    return;
                }
                if (cmd is RemovePartCommand) {
                    if (!isUndo) {
                        UnloadPart();
                    }
                } else if (cmd is AddPartCommand addPart) {
                    if (isUndo && addPart.part == Part) {
                        UnloadPart();
                    }
                } else if (cmd is ResizePartCommand) {
                    OnPartModified();
                } else if (cmd is MovePartCommand) {
                    OnPartModified();
                }
            } else if (cmd is NoteCommand noteCommand) {
                CleanupSelectedNotes();
                if (noteCommand.Part == Part) {
                    MessageBus.Current.SendMessage(new NotesRefreshEvent());
                }
            } else if (cmd is ExpCommand) {
                MessageBus.Current.SendMessage(new NotesRefreshEvent());
            } else if (cmd is TrackCommand) {
                if (cmd is RemoveTrackCommand removeTrack) {
                    if (removeTrack.removedParts.Contains(Part)) {
                        UnloadPart();
                    }
                }
                MessageBus.Current.SendMessage(new NotesRefreshEvent());
                if (cmd is TrackChangeSingerCommand trackChangeSinger) {
                    if (Part != null && trackChangeSinger.track.TrackNo == Part.trackNo) {
                        LoadPortrait(Part, Project);
                    }
                }
                PrimaryKeyNotSupported = !IsExpSupported(PrimaryKey);
            }
        }

        private void MaybeAutoScroll() {
            var autoScrollPreference = Convert.ToBoolean(Preferences.Default.PlaybackAutoScroll);
            if (autoScrollPreference) {
                AutoScroll();
            }
        }

        private void AutoScroll() {
            double scrollDelta = GetScrollValueDelta();
            TickOffset = Math.Clamp(TickOffset + scrollDelta, 0, HScrollBarMax);
        }

        private double GetScrollValueDelta() {
            var pageScroll = Preferences.Default.PlaybackAutoScroll == 2;
            if (pageScroll) {
                return GetPageScrollScrollValueDelta();
            }
            return GetStationaryCursorScrollValueDelta();
        }

        private double GetStationaryCursorScrollValueDelta() {
            double rightMargin = Preferences.Default.PlayPosMarkerMargin * Bounds.Width;
            if (PlayPosX > rightMargin) {
                return (PlayPosX - rightMargin) * playPosXToTickOffset;
            } else if (PlayPosX < 0) {
                return PlayPosX * playPosXToTickOffset;
            }
            return 0;
        }

        private double GetPageScrollScrollValueDelta() {
            double leftMargin = (1 - Preferences.Default.PlayPosMarkerMargin) * Bounds.Width;
            if (PlayPosX > Bounds.Width) {
                return (Bounds.Width - leftMargin) * playPosXToTickOffset;
            } else if (PlayPosX < 0) {
                return (PlayPosX - leftMargin) * playPosXToTickOffset;
            }
            return 0;
        }
    }
}
