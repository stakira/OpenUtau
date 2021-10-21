using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Toolkit.Mvvm.Input;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.UI.Controls;
using Serilog;

namespace OpenUtau.UI.Models {
    class MidiViewModel : INotifyPropertyChanged, ICmdSubscriber {
        # region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public UProject Project { get { return DocManager.Inst.Project; } }

        UVoicePart _part;
        public UVoicePart Part { get { return _part; } }
        public Classic.Plugin[] Plugins => DocManager.Inst.Plugins;
        public TransformerFactory[] Transformers => DocManager.Inst.TransformerFactories;

        public Canvas TimelineCanvas;
        public Canvas MidiCanvas;
        public Canvas ExpCanvas;
        public Canvas PhonemeCanvas;

        protected bool _updated = false;
        public void MarkUpdate() { _updated = true; }

        string _title = "New Part";
        double _trackHeight = UIConstants.NoteDefaultHeight;
        double _quarterCount = UIConstants.MinQuarterCount;
        double _quarterWidth = UIConstants.MidiQuarterDefaultWidth;
        double _viewWidth;
        double _viewHeight;
        double _offsetX = 0;
        double _offsetY = UIConstants.NoteDefaultHeight * 5 * 12;
        double _quarterOffset = 0;
        double _minTickWidth = UIConstants.MidiTickMinWidth;
        bool _showPhoneme = true;
        bool _showVibrato = true;
        bool _showPitch = true;
        bool _snap = true;

        public string Title { set { _title = value; OnPropertyChanged("Title"); } get { return "Midi Editor - " + _title; } }
        public double TotalHeight { get { return UIConstants.MaxNoteNum * _trackHeight - _viewHeight; } }
        public double TotalWidth { get { return _quarterCount * _quarterWidth - _viewWidth; } }
        public double QuarterCount { set { _quarterCount = value; HorizontalPropertiesChanged(); } get { return _quarterCount; } }
        public double TrackHeight {
            set {
                _trackHeight = Math.Max(ViewHeight / UIConstants.MaxNoteNum, Math.Max(UIConstants.NoteMinHeight, Math.Min(UIConstants.NoteMaxHeight, value)));
                VerticalPropertiesChanged();
            }
            get { return _trackHeight; }
        }

        public double QuarterWidth {
            set {
                _quarterWidth = Math.Max(ViewWidth / QuarterCount, Math.Max(UIConstants.MidiQuarterMinWidth, Math.Min(UIConstants.MidiQuarterMaxWidth, value)));
                HorizontalPropertiesChanged();
            }
            get { return _quarterWidth; }
        }

        public double ViewWidth { set { _viewWidth = value; HorizontalPropertiesChanged(); QuarterWidth = QuarterWidth; OffsetX = OffsetX; } get { return _viewWidth; } }
        public double ViewHeight { set { _viewHeight = value; VerticalPropertiesChanged(); TrackHeight = TrackHeight; OffsetY = OffsetY; } get { return _viewHeight; } }
        public double OffsetX { set { _offsetX = Math.Min(TotalWidth, Math.Max(0, value)); HorizontalPropertiesChanged(); } get { return _offsetX; } }
        public double OffsetY { set { _offsetY = Math.Min(TotalHeight, Math.Max(0, value)); VerticalPropertiesChanged(); } get { return _offsetY; } }
        public double ViewportSizeX {
            get {
                if (TotalWidth < 1) {
                    return 10000;
                } else {
                    return ViewWidth * (TotalWidth + ViewWidth) / TotalWidth;
                }
            }
        }
        public double ViewportSizeY {
            get {
                if (TotalHeight < 1) {
                    return 10000;
                } else {
                    return ViewHeight * (TotalHeight + ViewHeight) / TotalHeight;
                }
            }
        }
        public double SmallChangeX { get { return ViewportSizeX / 10; } }
        public double SmallChangeY { get { return ViewportSizeY / 10; } }
        public double QuarterOffset { set { _quarterOffset = value; HorizontalPropertiesChanged(); } get { return _quarterOffset; } }
        public double MinTickWidth { set { _minTickWidth = value; HorizontalPropertiesChanged(); } get { return _minTickWidth; } }
        public int BeatPerBar => Project.beatPerBar;
        public int BeatUnit => Project.beatUnit;
        public bool ShowVibrato { set { _showVibrato = value; notesElement.ShowVibrato = value; OnPropertyChanged("ShowVibrato"); } get { return _showVibrato; } }
        public bool ShowPitch { set { _showPitch = value; notesElement.ShowPitch = value; OnPropertyChanged("ShowPitch"); } get { return _showPitch; } }
        public bool ShowPhoneme { set { _showPhoneme = value; OnPropertyChanged("PhonemeVisibility"); OnPropertyChanged("ShowPhoneme"); } get { return _showPhoneme; } }
        public Visibility PhonemeVisibility { get { return _showPhoneme ? Visibility.Visible : Visibility.Collapsed; } }
        public bool Snap { set { _snap = value; OnPropertyChanged("Snap"); } get { return _snap; } }
        public bool Tips {
            set {
                Core.Util.Preferences.Default.ShowTips = value;
                Core.Util.Preferences.Save();
                OnPropertyChanged(nameof(Tips));
                OnPropertyChanged(nameof(TipsVisible));
            }
            get => Core.Util.Preferences.Default.ShowTips;
        }
        public Visibility TipsVisible => Tips ? Visibility.Visible : Visibility.Collapsed;

        public void HorizontalPropertiesChanged() {
            OnPropertyChanged("QuarterWidth");
            OnPropertyChanged("TotalWidth");
            OnPropertyChanged("OffsetX");
            OnPropertyChanged("ViewportSizeX");
            OnPropertyChanged("SmallChangeX");
            OnPropertyChanged("QuarterOffset");
            OnPropertyChanged("QuarterCount");
            OnPropertyChanged("MinTickWidth");
            OnPropertyChanged("BeatPerBar");
            OnPropertyChanged("BeatUnit");
            MarkUpdate();
        }

        public void VerticalPropertiesChanged() {
            OnPropertyChanged("TrackHeight");
            OnPropertyChanged("TotalHeight");
            OnPropertyChanged("OffsetY");
            OnPropertyChanged("ViewportSizeY");
            OnPropertyChanged("SmallChangeY");
            MarkUpdate();
        }

        #endregion

        readonly Dictionary<string, FloatExpElement> expElements = new Dictionary<string, FloatExpElement>();
        public NotesElement notesElement;
        public PhonemesElement phonemesElement;
        public FloatExpElement visibleExpElement, shadowExpElement;

        public MidiViewModel() { }

        public void RedrawIfUpdated() {
            if (_updated) {
                if (visibleExpElement != null) {
                    visibleExpElement.X = -OffsetX;
                    visibleExpElement.ScaleX = QuarterWidth / Project.resolution;
                    visibleExpElement.VisualHeight = ExpCanvas.ActualHeight;
                    visibleExpElement.MarkUpdate();
                }
                if (shadowExpElement != null) {
                    shadowExpElement.X = -OffsetX;
                    shadowExpElement.ScaleX = QuarterWidth / Project.resolution;
                    shadowExpElement.VisualHeight = ExpCanvas.ActualHeight;
                    shadowExpElement.MarkUpdate();
                }
                if (notesElement != null) {
                    notesElement.X = -OffsetX;
                    notesElement.Y = -OffsetY;
                    notesElement.VisualHeight = MidiCanvas.ActualHeight;
                    notesElement.TrackHeight = TrackHeight;
                    notesElement.QuarterWidth = QuarterWidth;
                }
                if (phonemesElement != null) {
                    phonemesElement.X = -OffsetX;
                    phonemesElement.QuarterWidth = QuarterWidth;
                }
                updatePlayPosMarker();
            }
            _updated = false;
            foreach (var pair in expElements) {
                pair.Value.RedrawIfUpdated();
            }

            if (notesElement != null) {
                notesElement.RedrawIfUpdated();
            }

            if (phonemesElement != null && ShowPhoneme) {
                phonemesElement.RedrawIfUpdated();
            }
        }

        # region PlayPosMarker

        public int playPosTick = 0;
        Path playPosMarker;
        Rectangle playPosMarkerHighlight;

        private void initPlayPosMarker() {
            playPosTick = DocManager.Inst.playPosTick;
            if (playPosMarker == null) {
                playPosMarker = new Path() {
                    Fill = ThemeManager.TickLineBrushDark,
                    Data = Geometry.Parse("M 0 0 L 13 0 L 13 3 L 6.5 9 L 0 3 Z")
                };
                TimelineCanvas.Children.Add(playPosMarker);

                playPosMarkerHighlight = new Rectangle() {
                    Fill = ThemeManager.TickLineBrushDark,
                    Opacity = 0.25,
                    Width = 32
                };
                MidiCanvas.Children.Add(playPosMarkerHighlight);
                Canvas.SetZIndex(playPosMarkerHighlight, UIConstants.PosMarkerHightlighZIndex);
            }
        }

        public void updatePlayPosMarker() {
            double quarter = (double)(playPosTick - Part.position) / DocManager.Inst.Project.resolution;
            int playPosMarkerOffset = (int)Math.Round(QuarterToCanvas(quarter) + 0.5);
            Canvas.SetLeft(playPosMarker, playPosMarkerOffset - 6);
            playPosMarkerHighlight.Height = MidiCanvas.ActualHeight;
            double zoomRatio = MusicMath.getZoomRatio(QuarterWidth, BeatPerBar, BeatUnit, MinTickWidth);
            double interval = zoomRatio * QuarterWidth;
            int left = (int)Math.Round(QuarterToCanvas((int)(quarter / zoomRatio) * zoomRatio) + 0.5);
            playPosMarkerHighlight.Width = interval;
            Canvas.SetLeft(playPosMarkerHighlight, left);
        }

        # endregion

        # region Selection

        public List<UNote> SelectedNotes = new List<UNote>();
        public List<UNote> TempSelectedNotes = new List<UNote>();

        public void SelectAll() {
            SelectedNotes.Clear();
            SelectedNotes.AddRange(Part.notes);
            foreach (UNote note in Part.notes) {
                note.Selected = true;
            }
            DocManager.Inst.ExecuteCmd(new RedrawNotesNotification());
        }
        public void DeselectAll() {
            SelectedNotes.Clear();
            foreach (UNote note in Part.notes) {
                note.Selected = false;
            }
            DocManager.Inst.ExecuteCmd(new RedrawNotesNotification());
        }

        public void SelectNote(UNote note) {
            if (!SelectedNotes.Contains(note)) {
                SelectedNotes.Add(note);
                note.Selected = true;
                DocManager.Inst.ExecuteCmd(new RedrawNotesNotification());
            }
        }
        public void DeselectNote(UNote note) {
            SelectedNotes.Remove(note);
            note.Selected = false;
            DocManager.Inst.ExecuteCmd(new RedrawNotesNotification());
        }

        public void SelectTempNote(UNote note) {
            TempSelectedNotes.Add(note);
            note.Selected = true;
        }
        public void TempSelectInBox(double quarter1, double quarter2, int noteNum1, int noteNum2) {
            if (quarter2 < quarter1) { double temp = quarter1; quarter1 = quarter2; quarter2 = temp; }
            if (noteNum2 < noteNum1) { int temp = noteNum1; noteNum1 = noteNum2; noteNum2 = temp; }
            int tick1 = (int)(quarter1 * Project.resolution);
            int tick2 = (int)(quarter2 * Project.resolution);
            foreach (UNote note in TempSelectedNotes) {
                note.Selected = false;
            }

            TempSelectedNotes.Clear();
            foreach (UNote note in Part.notes) {
                if (note.position <= tick2 && note.position + note.duration >= tick1 &&
                    note.tone >= noteNum1 && note.tone <= noteNum2) {
                    SelectTempNote(note);
                }
            }
            DocManager.Inst.ExecuteCmd(new RedrawNotesNotification());
        }

        public void DoneTempSelect() {
            foreach (UNote note in TempSelectedNotes) {
                SelectNote(note);
            }
            TempSelectedNotes.Clear();
        }

        # endregion

        public void TransposeSelection(int deltaNoteNum) {
            if (SelectedNotes.Count > 0) {
                DocManager.Inst.StartUndoGroup();
                if (SelectedNotes.Any(note => note.tone + deltaNoteNum <= 0 || note.tone + deltaNoteNum >= UIConstants.MaxNoteNum)) {
                    return;
                }
                DocManager.Inst.ExecuteCmd(new MoveNoteCommand(Part, new List<UNote>(SelectedNotes), 0, deltaNoteNum));
                DocManager.Inst.EndUndoGroup();
            }
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
                double snapUnit = GetSnapUnit();
                int snapUnitTick = (int)(snapUnit * Project.resolution);
                int position = (int)(Math.Ceiling(OffsetX / QuarterWidth / snapUnit) * snapUnitTick);
                int minPosition = DocManager.Inst.NotesClipboard.Select(note => note.position).Min();
                int offset = position - (int)Math.Floor((double)minPosition / snapUnitTick) * snapUnitTick;
                DocManager.Inst.NotesClipboard.ForEach(note => note.position += offset);
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new AddNoteCommand(Part,
                    DocManager.Inst.NotesClipboard.Select(note => note.Clone()).ToList()));
                DocManager.Inst.EndUndoGroup();
            }
        }

        # region Calculation

        public double GetSnapUnit() { return Snap ? OpenUtau.Core.MusicMath.getZoomRatio(QuarterWidth, BeatPerBar, BeatUnit, MinTickWidth) : 1.0 / 96; }
        public double CanvasToQuarter(double X) { return (X + OffsetX) / QuarterWidth; }
        public double QuarterToCanvas(double X) { return X * QuarterWidth - OffsetX; }
        public double CanvasToSnappedQuarter(double X) {
            double quater = CanvasToQuarter(X);
            double snapUnit = GetSnapUnit();
            return (int)(quater / snapUnit) * snapUnit;
        }
        public double CanvasToNextSnappedQuarter(double X) {
            double quater = CanvasToQuarter(X);
            double snapUnit = GetSnapUnit();
            return (int)(quater / snapUnit) * snapUnit + snapUnit;
        }
        public double CanvasRoundToSnappedQuarter(double X) {
            double quater = CanvasToQuarter(X);
            double snapUnit = GetSnapUnit();
            return Math.Round(quater / snapUnit) * snapUnit;
        }
        public int CanvasToTick(double X) { return (int)(CanvasToQuarter(X) * Project.resolution); }
        public int CanvasToSnappedTick(double X) { return (int)(CanvasToSnappedQuarter(X) * Project.resolution); }
        public double TickToCanvas(int tick) { return (int)QuarterToCanvas((double)tick / Project.resolution); }

        public int CanvasToNoteNum(double Y) { return UIConstants.MaxNoteNum - 1 - (int)((Y + OffsetY) / TrackHeight); }
        public float CanvasToTone(double Y) { return UIConstants.MaxNoteNum - 1 - (float)((Y + OffsetY) / TrackHeight); }
        public double CanvasToPitch(double Y) { return UIConstants.MaxNoteNum - 1 - (Y + OffsetY) / TrackHeight + 0.5; }
        public double NoteNumToCanvas(int noteNum) { return TrackHeight * (UIConstants.MaxNoteNum - 1 - noteNum) - OffsetY; }
        public double NoteNumToCanvas(double noteNum) { return TrackHeight * (UIConstants.MaxNoteNum - 1 - noteNum) - OffsetY; }
        public Point TickToneToCanvas(System.Numerics.Vector2 pos) {
            return new Point(TickToCanvas((int)pos.X), NoteNumToCanvas(pos.Y));
        }
        public bool NoteIsInView(UNote note) {
            double leftTick = OffsetX / QuarterWidth * Project.resolution - 512;
            double rightTick = leftTick + ViewWidth / QuarterWidth * Project.resolution + 512;
            return note.LeftBound < rightTick && note.RightBound > leftTick;
        }

        # endregion

        # region Cmd Handling

        private void UnloadPart() {
            SelectedNotes.Clear();
            Title = "";
            _part = null;

            if (notesElement != null) {
                notesElement.Part = null;
            }
            if (phonemesElement != null) {
                phonemesElement.Part = null;
            }

            foreach (var pair in expElements) {
                pair.Value.Part = null;
                pair.Value.MarkUpdate();
                pair.Value.RedrawIfUpdated();
            }
        }

        private void LoadPart(UPart part, UProject project) {
            if (part == Part) {
                return;
            }

            if (!(part is UVoicePart)) {
                return;
            }

            UnloadPart();
            _part = (UVoicePart)part;

            OnPartModified();

            if (notesElement == null) {
                notesElement = new NotesElement() { Key = "pitchbend", Part = this.Part, midiVM = this };
                MidiCanvas.Children.Add(notesElement);
            } else {
                notesElement.Part = this.Part;
            }

            if (phonemesElement == null) {
                phonemesElement = new PhonemesElement() { Part = this.Part, midiVM = this };
                PhonemeCanvas.Children.Add(phonemesElement);
            } else {
                phonemesElement.Part = this.Part;
            }

            foreach (var pair in expElements) { pair.Value.Part = this.Part; pair.Value.MarkUpdate(); }
            initPlayPosMarker();
        }

        private void OnPartModified() {
            Title = Part.name;
            QuarterOffset = (double)Part.position / Project.resolution;
            QuarterCount = (double)Part.Duration / Project.resolution;
            QuarterWidth = QuarterWidth;
            OffsetX = OffsetX;
            MarkUpdate();
        }

        private void OnSelectExpression(UNotification cmd) {
            var _cmd = cmd as SelectExpressionNotification;
            if (!expElements.ContainsKey(_cmd.ExpKey)) {
                var expEl = new FloatExpElement() { Key = _cmd.ExpKey, Part = this.Part, midiVM = this };
                expElements.Add(_cmd.ExpKey, expEl);
                ExpCanvas.Children.Add(expEl);
            }

            if (_cmd.UpdateShadow) {
                shadowExpElement = visibleExpElement;
            }

            visibleExpElement = expElements[_cmd.ExpKey];
            visibleExpElement.MarkUpdate();
            this.MarkUpdate();

            foreach (var pair in expElements) {
                pair.Value.DisplayMode = ExpDisMode.Hidden;
            }

            if (shadowExpElement != null) {
                shadowExpElement.DisplayMode = ExpDisMode.Shadow;
            }

            visibleExpElement.DisplayMode = ExpDisMode.Visible;
        }

        private void OnPlayPosSet(int playPosTick) {
            this.playPosTick = playPosTick;
            double playPosPix = TickToCanvas(playPosTick - (_part == null ? 0 : _part.position));
            if (playPosPix > MidiCanvas.ActualWidth * UIConstants.PlayPosMarkerMargin) {
                OffsetX += playPosPix - MidiCanvas.ActualWidth * UIConstants.PlayPosMarkerMargin;
            }

            MarkUpdate();
        }

        private void OnPitchModified() {
            MarkUpdate();
            notesElement.MarkUpdate();
        }

        private void FocusNote(UNote note) {
            OffsetX = (note.position + note.duration * 0.5) * QuarterWidth / Project.resolution - ViewWidth * 0.5;
            OffsetY = TrackHeight * (UIConstants.MaxNoteNum - note.tone + 2) - ViewHeight * 0.5;
            MarkUpdate();
        }

        # endregion

        private ICommand pluginCommand;
        public ICommand PluginCommand => pluginCommand ?? (pluginCommand = new RelayCommand<object>(OnPluginSelected));
        void OnPluginSelected(object obj) {
            var plugin = (Classic.Plugin)obj;
            var project = DocManager.Inst.Project;
            var tempFile = System.IO.Path.Combine(PathManager.Inst.GetCachePath(), "temp.ust");
            var newPart = (UVoicePart)Part.Clone();
            var sequence = Ust.WriteNotes(project, newPart, newPart.notes, tempFile);
            plugin.Run(tempFile);
            Ust.ParseDiffs(project, newPart, sequence, tempFile);
            newPart.AfterLoad(project, project.tracks[Part.trackNo]);
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new ReplacePartCommand(project, Part, newPart));
            DocManager.Inst.EndUndoGroup();
        }

        private ICommand transformerCommand;
        public ICommand TransformerCommand => transformerCommand ?? (transformerCommand = new RelayCommand<object>(OnTransformerSelected));
        void OnTransformerSelected(object obj) {
            var factory = (TransformerFactory)obj;
            try {
                var transformer = factory.Create();
                DocManager.Inst.StartUndoGroup();
                string[] newLyrics = new string[Part.notes.Count];
                int i = 0;
                foreach (var note in Part.notes) {
                    newLyrics[i++] = transformer.Transform(note.lyric);
                }
                DocManager.Inst.ExecuteCmd(new ChangeNoteLyricCommand(Part, Part.notes.ToArray(), newLyrics));
            } catch (Exception e) {
                Log.Error(e, $"Failed to run transformer {factory.name}");
                DocManager.Inst.ExecuteCmd(new UserMessageNotification(e.ToString()));
            } finally {
                DocManager.Inst.EndUndoGroup();
            }
        }

        # region ICmdSubscriber

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is TimeSignatureCommand) {
                OnPropertyChanged("BeatPerBar");
                OnPropertyChanged("BeatUnit");
            } else if (cmd is NoteCommand) {
                notesElement.MarkUpdate();
                phonemesElement.MarkUpdate();
                visibleExpElement.MarkUpdate();
                shadowExpElement.MarkUpdate();
            } else if (cmd is PartCommand) {
                var _cmd = cmd as PartCommand;
                if (_cmd is ReplacePartCommand rpCmd) {
                    if (isUndo && rpCmd.newPart == Part) {
                        LoadPart(rpCmd.part, rpCmd.project);
                        OnPartModified();
                    } else if (!isUndo && rpCmd.part == Part) {
                        LoadPart(rpCmd.newPart, rpCmd.project);
                        OnPartModified();
                    }
                } else if (_cmd.part != this.Part) {
                    return;
                } else if (_cmd is RemovePartCommand) {
                    UnloadPart();
                } else if (_cmd is ResizePartCommand) {
                    OnPartModified();
                } else if (_cmd is MovePartCommand) {
                    OnPartModified();
                }
                notesElement.MarkUpdate();
                phonemesElement.MarkUpdate();
                visibleExpElement.MarkUpdate();
                shadowExpElement.MarkUpdate();
            } else if (cmd is TrackCommand tcmd) {
                if (Part != null && tcmd.track.TrackNo == Part.trackNo) {
                    notesElement.MarkUpdate();
                    phonemesElement.MarkUpdate();
                    visibleExpElement.MarkUpdate();
                    shadowExpElement.MarkUpdate();
                }
            } else if (cmd is ExpCommand) {
                var _cmd = cmd as ExpCommand;
                if (_cmd is PitchExpCommand) {
                    OnPitchModified();
                } else {
                    expElements[_cmd.Key].MarkUpdate();
                }
                notesElement.MarkUpdate();
                phonemesElement.MarkUpdate();
            } else if (cmd is UNotification) {
                var _cmd = cmd as UNotification;
                if (_cmd is LoadPartNotification) {
                    LoadPart(_cmd.part, _cmd.project);
                } else if (_cmd is LoadProjectNotification) {
                    UnloadPart();
                } else if (_cmd is SelectExpressionNotification) {
                    OnSelectExpression(_cmd);
                } else if (_cmd is ShowPitchExpNotification) {
                } else if (_cmd is HidePitchExpNotification) {
                } else if (_cmd is RedrawNotesNotification) {
                    if (notesElement != null) {
                        notesElement.MarkUpdate();
                    }
                    if (phonemesElement != null) {
                        phonemesElement.MarkUpdate();
                    }
                } else if (_cmd is SetPlayPosTickNotification) {
                    OnPlayPosSet(((SetPlayPosTickNotification)_cmd).playPosTick);
                } else if (_cmd is FocusNoteNotification focusNote) {
                    if (focusNote.part == Part) {
                        FocusNote(focusNote.note);
                    };
                }
            }
        }

        # endregion

    }
}
