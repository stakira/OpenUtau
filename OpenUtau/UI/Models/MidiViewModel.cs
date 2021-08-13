using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;

using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.UI.Controls;

namespace OpenUtau.UI.Models
{
    class MidiViewModel : INotifyPropertyChanged, ICmdSubscriber
    {
        # region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public UProject Project { get { return DocManager.Inst.Project; } }
        
        UVoicePart _part;
        public UVoicePart Part { get { return _part; } }

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
        int _beatPerBar = 4;
        int _beatUnit = 4;
        int _visualPosTick;
        int _visualDurTick;
        bool _showPhoneme = true;
        bool _showPitch = true;
        bool _snap = true;

        public string Title { set { _title = value; OnPropertyChanged("Title"); } get { return "Midi Editor - " + _title; } }
        public double TotalHeight { get { return UIConstants.MaxNoteNum * _trackHeight - _viewHeight; } }
        public double TotalWidth { get { return _quarterCount * _quarterWidth - _viewWidth; } }
        public double QuarterCount { set { _quarterCount = value; HorizontalPropertiesChanged(); } get { return _quarterCount; } }
        public double TrackHeight
        {
            set
            {
                _trackHeight = Math.Max(ViewHeight / UIConstants.MaxNoteNum, Math.Max(UIConstants.NoteMinHeight, Math.Min(UIConstants.NoteMaxHeight, value)));
                VerticalPropertiesChanged();
            }
            get { return _trackHeight; }
        }

        public double QuarterWidth
        {
            set
            {
                _quarterWidth = Math.Max(ViewWidth / QuarterCount, Math.Max(UIConstants.MidiQuarterMinWidth, Math.Min(UIConstants.MidiQuarterMaxWidth, value)));
                HorizontalPropertiesChanged();
            }
            get { return _quarterWidth; }
        }

        public double ViewWidth { set { _viewWidth = value; HorizontalPropertiesChanged(); QuarterWidth = QuarterWidth; OffsetX = OffsetX; } get { return _viewWidth; } }
        public double ViewHeight { set { _viewHeight = value; VerticalPropertiesChanged(); TrackHeight = TrackHeight; OffsetY = OffsetY; } get { return _viewHeight; } }
        public double OffsetX { set { _offsetX = Math.Min(TotalWidth, Math.Max(0, value)); HorizontalPropertiesChanged(); } get { return _offsetX; } }
        public double OffsetY { set { _offsetY = Math.Min(TotalHeight, Math.Max(0, value)); VerticalPropertiesChanged(); } get { return _offsetY; } }
        public double ViewportSizeX { get { if (TotalWidth < 1) return 10000; else return ViewWidth * (TotalWidth + ViewWidth) / TotalWidth; } }
        public double ViewportSizeY { get { if (TotalHeight < 1) return 10000; else return ViewHeight * (TotalHeight + ViewHeight) / TotalHeight; } }
        public double SmallChangeX { get { return ViewportSizeX / 10; } }
        public double SmallChangeY { get { return ViewportSizeY / 10; } }
        public double QuarterOffset { set { _quarterOffset = value; HorizontalPropertiesChanged(); } get { return _quarterOffset; } }
        public double MinTickWidth { set { _minTickWidth = value; HorizontalPropertiesChanged(); } get { return _minTickWidth; } }
        public int BeatPerBar { set { _beatPerBar = value; HorizontalPropertiesChanged(); } get { return _beatPerBar; } }
        public int BeatUnit { set { _beatUnit = value; HorizontalPropertiesChanged(); } get { return _beatUnit; } }
        public bool ShowPitch { set { _showPitch = value; notesElement.ShowPitch = value; OnPropertyChanged("ShowPitch"); } get { return _showPitch; } }
        public bool ShowPhoneme { set { _showPhoneme = value; OnPropertyChanged("PhonemeVisibility"); OnPropertyChanged("ShowPhoneme"); } get { return _showPhoneme; } }
        public Visibility PhonemeVisibility { get { return _showPhoneme ? Visibility.Visible : Visibility.Collapsed; } }
        public bool Snap { set { _snap = value; OnPropertyChanged("Snap"); } get { return _snap; } }

        public void HorizontalPropertiesChanged()
        {
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

        public void VerticalPropertiesChanged()
        {
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

        public void RedrawIfUpdated()
        {
            if (_updated)
            {
                if (visibleExpElement != null)
                {
                    visibleExpElement.X = -OffsetX;
                    visibleExpElement.ScaleX = QuarterWidth / Project.resolution;
                    visibleExpElement.VisualHeight = ExpCanvas.ActualHeight;
                }
                if (shadowExpElement != null)
                {
                    shadowExpElement.X = -OffsetX;
                    shadowExpElement.ScaleX = QuarterWidth / Project.resolution;
                    shadowExpElement.VisualHeight = ExpCanvas.ActualHeight;
                }
                if (notesElement != null)
                {
                    notesElement.X = -OffsetX;
                    notesElement.Y = -OffsetY;
                    notesElement.VisualHeight = MidiCanvas.ActualHeight;
                    notesElement.TrackHeight = TrackHeight;
                    notesElement.QuarterWidth = QuarterWidth;
                }
                if (phonemesElement != null)
                {
                    phonemesElement.X = -OffsetX;
                    phonemesElement.QuarterWidth = QuarterWidth;
                }
                updatePlayPosMarker();
            }
            _updated = false;
            foreach (var pair in expElements) pair.Value.RedrawIfUpdated();
            if (notesElement != null) notesElement.RedrawIfUpdated();
            if (phonemesElement != null && ShowPhoneme) phonemesElement.RedrawIfUpdated();
        }

        # region PlayPosMarker

        public int playPosTick = 0;
        Path playPosMarker;
        Rectangle playPosMarkerHighlight;

        private void initPlayPosMarker()
        {
            playPosTick = DocManager.Inst.playPosTick;
            if (playPosMarker == null)
            {
                playPosMarker = new Path()
                {
                    Fill = ThemeManager.TickLineBrushDark,
                    Data = Geometry.Parse("M 0 0 L 13 0 L 13 3 L 6.5 9 L 0 3 Z")
                };
                TimelineCanvas.Children.Add(playPosMarker);

                playPosMarkerHighlight = new Rectangle()
                {
                    Fill = ThemeManager.TickLineBrushDark,
                    Opacity = 0.25,
                    Width = 32
                };
                MidiCanvas.Children.Add(playPosMarkerHighlight);
                Canvas.SetZIndex(playPosMarkerHighlight, UIConstants.PosMarkerHightlighZIndex);
            }
        }

        public void updatePlayPosMarker()
        {
            double quarter = (double)(playPosTick - Part.PosTick) / DocManager.Inst.Project.resolution;
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

        public void SelectAll() { SelectedNotes.Clear(); foreach (UNote note in Part.notes) { SelectedNotes.Add(note); note.Selected = true; } DocManager.Inst.ExecuteCmd(new RedrawNotesNotification()); }
        public void DeselectAll() { SelectedNotes.Clear(); foreach (UNote note in Part.notes) note.Selected = false; DocManager.Inst.ExecuteCmd(new RedrawNotesNotification()); }

        public void SelectNote(UNote note) { if (!SelectedNotes.Contains(note)) { SelectedNotes.Add(note); note.Selected = true; DocManager.Inst.ExecuteCmd(new RedrawNotesNotification()); } }
        public void DeselectNote(UNote note) { SelectedNotes.Remove(note); note.Selected = false; DocManager.Inst.ExecuteCmd(new RedrawNotesNotification()); }

        public void SelectTempNote(UNote note) { TempSelectedNotes.Add(note); note.Selected = true; }
        public void TempSelectInBox(double quarter1, double quarter2, int noteNum1, int noteNum2)
        {
            if (quarter2 < quarter1) { double temp = quarter1; quarter1 = quarter2; quarter2 = temp; }
            if (noteNum2 < noteNum1) { int temp = noteNum1; noteNum1 = noteNum2; noteNum2 = temp; }
            int tick1 = (int)(quarter1 * Project.resolution);
            int tick2 = (int)(quarter2 * Project.resolution);
            foreach (UNote note in TempSelectedNotes) note.Selected = false;
            TempSelectedNotes.Clear();
            foreach (UNote note in Part.notes)
            {
                if (note.position <= tick2 && note.position + note.duration >= tick1 &&
                    note.noteNum >= noteNum1 && note.noteNum <= noteNum2) SelectTempNote(note);
            }
            DocManager.Inst.ExecuteCmd(new RedrawNotesNotification());
        }

        public void DoneTempSelect()
        {
            foreach (UNote note in TempSelectedNotes) SelectNote(note);
            TempSelectedNotes.Clear();
        }

        # endregion

        # region Calculation

        public double GetSnapUnit() { return Snap ? OpenUtau.Core.MusicMath.getZoomRatio(QuarterWidth, BeatPerBar, BeatUnit, MinTickWidth) : 1.0 / 96; }
        public double CanvasToQuarter(double X) { return (X + OffsetX) / QuarterWidth; }
        public double QuarterToCanvas(double X) { return X * QuarterWidth - OffsetX; }
        public double CanvasToSnappedQuarter(double X)
        {
            double quater = CanvasToQuarter(X);
            double snapUnit = GetSnapUnit();
            return (int)(quater / snapUnit) * snapUnit;
        }
        public double CanvasToNextSnappedQuarter(double X)
        {
            double quater = CanvasToQuarter(X);
            double snapUnit = GetSnapUnit();
            return (int)(quater / snapUnit) * snapUnit + snapUnit;
        }
        public double CanvasRoundToSnappedQuarter(double X)
        {
            double quater = CanvasToQuarter(X);
            double snapUnit = GetSnapUnit();
            return Math.Round(quater / snapUnit) * snapUnit;
        }
        public int CanvasToSnappedTick(double X) { return (int)(CanvasToSnappedQuarter(X) * Project.resolution); }
        public double TickToCanvas(int tick) { return (int)(QuarterToCanvas((double)tick / Project.resolution)); }

        public int CanvasToNoteNum(double Y) { return UIConstants.MaxNoteNum - 1 - (int)((Y + OffsetY) / TrackHeight); }
        public double CanvasToPitch(double Y) { return UIConstants.MaxNoteNum - 1 - (Y + OffsetY) / TrackHeight + 0.5; }
        public double NoteNumToCanvas(int noteNum) { return TrackHeight * (UIConstants.MaxNoteNum - 1 - noteNum) - OffsetY; }
        public double NoteNumToCanvas(double noteNum) { return TrackHeight * (UIConstants.MaxNoteNum - 1 - noteNum) - OffsetY; }

        public bool NoteIsInView(UNote note) // FIXME : improve performance
        {
            double leftTick = OffsetX / QuarterWidth * Project.resolution - 512;
            double rightTick = leftTick + ViewWidth / QuarterWidth * Project.resolution + 512;
            return (note.position < rightTick && note.End > leftTick);
        }

        # endregion

        # region Cmd Handling

        private void UnloadPart()
        {
            SelectedNotes.Clear();
            Title = "";
            _part = null;

            if (notesElement != null)
            {
                notesElement.Part = null;
            }
            if (phonemesElement != null)
            {
                phonemesElement.Part = null;
            }

            foreach (var pair in expElements) { pair.Value.Part = null; pair.Value.MarkUpdate(); pair.Value.RedrawIfUpdated(); }
        }

        private void LoadPart(UPart part, UProject project)
        {
            if (part == Part) return;
            if (!(part is UVoicePart)) return;
            UnloadPart();
            _part = (UVoicePart)part;

            OnPartModified();

            if (notesElement == null)
            {
                notesElement = new NotesElement() { Key = "pitchbend", Part = this.Part, midiVM = this };
                MidiCanvas.Children.Add(notesElement);
            }
            else
            {
                notesElement.Part = this.Part;
            }

            if (phonemesElement == null)
            {
                phonemesElement = new PhonemesElement() { Part = this.Part, midiVM = this };
                PhonemeCanvas.Children.Add(phonemesElement);
            }else
            {
                phonemesElement.Part = this.Part;
            }

            foreach (var pair in expElements) { pair.Value.Part = this.Part; pair.Value.MarkUpdate(); }
            initPlayPosMarker();
        }

        private void OnPartModified()
        {
            Title = Part.Name;
            QuarterOffset = (double)Part.PosTick / Project.resolution;
            QuarterCount = (double)Part.DurTick / Project.resolution;
            QuarterWidth = QuarterWidth;
            OffsetX = OffsetX;
            MarkUpdate();
            _visualPosTick = Part.PosTick;
            _visualDurTick = Part.DurTick;
        }

        private void OnSelectExpression(UNotification cmd)
        {
            var _cmd = cmd as SelectExpressionNotification;
            if (!expElements.ContainsKey(_cmd.ExpKey))
            {
                var expEl = new FloatExpElement() { Key = _cmd.ExpKey, Part = this.Part, midiVM = this };
                expElements.Add(_cmd.ExpKey, expEl);
                ExpCanvas.Children.Add(expEl);
            }

            if (_cmd.UpdateShadow) shadowExpElement = visibleExpElement;
            visibleExpElement = expElements[_cmd.ExpKey];
            visibleExpElement.MarkUpdate();
            this.MarkUpdate();

            foreach (var pair in expElements) pair.Value.DisplayMode = ExpDisMode.Hidden;
            if (shadowExpElement != null) shadowExpElement.DisplayMode = ExpDisMode.Shadow;
            visibleExpElement.DisplayMode = ExpDisMode.Visible;
        }

        private void OnPlayPosSet(int playPosTick)
        {
            this.playPosTick = playPosTick;
            double playPosPix = TickToCanvas(playPosTick);
            if (playPosPix > MidiCanvas.ActualWidth * UIConstants.PlayPosMarkerMargin)
                OffsetX += playPosPix - MidiCanvas.ActualWidth * UIConstants.PlayPosMarkerMargin;
            MarkUpdate();
        }

        private void OnPitchModified()
        {
            MarkUpdate();
            notesElement.MarkUpdate();
        }

        # endregion

        # region ICmdSubscriber

        public void OnNext(UCommand cmd, bool isUndo)
        {
            if (cmd is NoteCommand)
            {
                notesElement.MarkUpdate();
                phonemesElement.MarkUpdate();
            }
            else if (cmd is PartCommand)
            {
                var _cmd = cmd as PartCommand;
                if (_cmd.part != this.Part) return;
                else if (_cmd is RemovePartCommand) UnloadPart();
                else if (_cmd is ResizePartCommand) OnPartModified();
                else if (_cmd is MovePartCommand) OnPartModified();
            }
            else if (cmd is ExpCommand)
            {
                var _cmd = cmd as ExpCommand;
                if (_cmd is SetUExpressionCommand) expElements[_cmd.Key].MarkUpdate();
                else if (_cmd is PitchExpCommand) OnPitchModified();
            }
            else if (cmd is UNotification)
            {
                var _cmd = cmd as UNotification;
                if (_cmd is LoadPartNotification) LoadPart(_cmd.part, _cmd.project);
                else if (_cmd is LoadProjectNotification) UnloadPart();
                else if (_cmd is SelectExpressionNotification) OnSelectExpression(_cmd);
                else if (_cmd is ShowPitchExpNotification) { }
                else if (_cmd is HidePitchExpNotification) { }
                else if (_cmd is RedrawNotesNotification) {
                    if (notesElement != null) notesElement.MarkUpdate();
                    if (phonemesElement != null) phonemesElement.MarkUpdate();
                }
                else if (_cmd is SetPlayPosTickNotification) { OnPlayPosSet(((SetPlayPosTickNotification)_cmd).playPosTick); }
            }
        }

        # endregion

    }
}
