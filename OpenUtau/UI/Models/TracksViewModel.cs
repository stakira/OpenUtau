using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.ComponentModel;

using OpenUtau.Core;
using OpenUtau.Core.USTx;
using OpenUtau.UI.Models;
using OpenUtau.UI.Controls;

namespace OpenUtau.UI.Models
{
    class TracksViewModel : INotifyPropertyChanged, ICmdSubscriber
    {
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
        public Canvas TrackCanvas;
        public Canvas HeaderCanvas;

        protected bool _updated = false;
        public void MarkUpdate() { _updated = true; }

        double _trackHeight = UIConstants.TrackDefaultHeight;
        double _trackCount = UIConstants.MinTrackCount;
        double _quarterCount = UIConstants.MinQuarterCount;
        double _quarterWidth = UIConstants.TrackQuarterDefaultWidth;
        double _viewWidth = 0;
        double _viewHeight = 0;
        double _offsetX = 0;
        double _offsetY = 0;
        double _quarterOffset = 0;
        double _minTickWidth = UIConstants.TrackTickMinWidth;
        int _beatPerBar = 4;
        int _beatUnit = 4;

        public string Title { get { if (Project != null) return "OpenUtau - [" + Project.Name + "]"; else return "OpenUtau"; } }
        public double TotalHeight { get { return _trackCount * _trackHeight - _viewHeight; } }
        public double TotalWidth { get { return _quarterCount * _quarterWidth - _viewWidth; } }
        public double TrackCount { set { if (_trackCount != value) { _trackCount = value; VerticalPropertiesChanged(); } } get { return _trackCount; } }
        public double QuarterCount { set { if (_quarterCount != value) { _quarterCount = value; HorizontalPropertiesChanged(); } } get { return _quarterCount; } }
        public double TrackHeight
        {
            set
            {
                _trackHeight = Math.Max(UIConstants.TrackMinHeight, Math.Min(UIConstants.TrackMaxHeight, value));
                VerticalPropertiesChanged();
            }
            get { return _trackHeight; }
        }
        
        public double QuarterWidth
        {
            set
            {
                _quarterWidth = Math.Max(UIConstants.TrackQuarterMinWidth, Math.Min(UIConstants.TrackQuarterMaxWidth, value));
                HorizontalPropertiesChanged();
            }
            get { return _quarterWidth; }
        }

        public double ViewWidth { set { if (_viewWidth != value) { _viewWidth = value; HorizontalPropertiesChanged(); } } get { return _viewWidth; } }
        public double ViewHeight { set { if (_viewHeight != value) { _viewHeight = value; VerticalPropertiesChanged(); } } get { return _viewHeight; } }
        public double OffsetX { set { _offsetX = Math.Max(0, value); HorizontalPropertiesChanged(); } get { return _offsetX; } }
        public double OffsetY { set { _offsetY = Math.Max(0, value); VerticalPropertiesChanged(); } get { return _offsetY; } }
        public double ViewportSizeX { get { if (TotalWidth <= 0) return 10000; else return ViewWidth * (TotalWidth + ViewWidth) / TotalWidth; } }
        public double ViewportSizeY { get { if (TotalHeight <= 0) return 10000; else return ViewHeight * (TotalHeight + ViewHeight) / TotalHeight; } }
        public double SmallChangeX { get { return ViewportSizeX / 10; } }
        public double SmallChangeY { get { return ViewportSizeY / 10; } }
        public double QuarterOffset { set { _quarterOffset = value; HorizontalPropertiesChanged(); } get { return _quarterOffset; } }
        public double MinTickWidth { set { _minTickWidth = value; HorizontalPropertiesChanged(); } get { return _minTickWidth; } }
        public int BeatPerBar { set { _beatPerBar = value; HorizontalPropertiesChanged(); } get { return _beatPerBar; } }
        public int BeatUnit { set { _beatUnit = value; HorizontalPropertiesChanged(); } get { return _beatUnit; } }

        public void HorizontalPropertiesChanged()
        {
            OnPropertyChanged("QuarterWidth");
            OnPropertyChanged("TotalWidth");
            OnPropertyChanged("OffsetX");
            OnPropertyChanged("ViewportSizeX");
            OnPropertyChanged("SmallChangeX");
            OnPropertyChanged("QuarterOffset");
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

        public List<UPart> SelectedParts = new List<UPart>();
        List<UPart> TempSelectedParts = new List<UPart>();
        List<PartElement> PartElements = new List<PartElement>();
        List<TrackHeader> TrackHeaders = new List<TrackHeader>();

        public TracksViewModel() { }

        # region Selection

        public void UpdateSelectedVisual()
        {
            foreach (PartElement partEl in PartElements)
            {
                if (SelectedParts.Contains(partEl.Part) || TempSelectedParts.Contains(partEl.Part)) partEl.Selected = true;
                else partEl.Selected = false;
            }
        }

        public void SelectAll() { SelectedParts.Clear(); foreach (UPart part in Project.Parts) SelectedParts.Add(part); UpdateSelectedVisual(); }
        public void DeselectAll() { SelectedParts.Clear(); UpdateSelectedVisual(); }

        public void SelectPart(UPart part) { if (!SelectedParts.Contains(part)) SelectedParts.Add(part); }
        public void DeselectPart(UPart part) { SelectedParts.Remove(part); }

        public void SelectTempPart(UPart part) { TempSelectedParts.Add(part); }
        public void TempSelectInBox(double quarter1, double quarter2, int track1, int track2)
        {
            if (quarter2 < quarter1) { double temp = quarter1; quarter1 = quarter2; quarter2 = temp; }
            if (track2 < track1) { int temp = track1; track1 = track2; track2 = temp; }
            int tick1 = (int)(quarter1 * Project.Resolution);
            int tick2 = (int)(quarter2 * Project.Resolution);
            TempSelectedParts.Clear();
            foreach (UPart part in Project.Parts)
            {
                if (part.PosTick <= tick2 && part.EndTick >= tick1 &&
                    part.TrackNo <= track2 && part.TrackNo >= track1) SelectTempPart(part);
            }
            UpdateSelectedVisual();
        }

        public void DoneTempSelect()
        {
            foreach (UPart part in TempSelectedParts) SelectPart(part);
            TempSelectedParts.Clear();
            UpdateSelectedVisual();
        }

        # endregion

        public PartElement GetPartElement(UPart part)
        {
            foreach (PartElement partEl in PartElements)
            {
                if (partEl.Part == part) return partEl;
            }
            return null;
        }

        public TrackHeader GetTrackHeader(UTrack track)
        {
            foreach (var trackHeader in TrackHeaders)
            {
                if (trackHeader.Track == track) return trackHeader;
            }
            return null;
        }

        public void RedrawIfUpdated()
        {
            if (_updated)
            {
                foreach (PartElement partElement in PartElements)
                {
                    if (partElement.Modified) partElement.Redraw();
                    partElement.X = -OffsetX + partElement.Part.PosTick * QuarterWidth / Project.Resolution;
                    partElement.Y = -OffsetY + partElement.Part.TrackNo * TrackHeight + 1;
                    partElement.VisualHeight = TrackHeight - 2;
                    partElement.ScaleX = QuarterWidth / Project.Resolution;
                }
                foreach (TrackHeader trackHeader in TrackHeaders)
                {
                    Canvas.SetTop(trackHeader, -OffsetY + TrackHeight * trackHeader.Track.TrackNo + 1);
                    trackHeader.Height = TrackHeight - 2;
                }
            }
            _updated = false;
        }

        public void UpdatePartElement(UPart part)
        {
            foreach (PartElement partElement in PartElements) if (partElement.Part == part) partElement.Redraw();
        }

        public void UpdateViewSize()
        {
            double quarterCount = UIConstants.MinQuarterCount;
            if (Project != null)
                foreach (UPart part in Project.Parts)
                    quarterCount = Math.Max(quarterCount, (part.DurTick + part.PosTick) / Project.Resolution + UIConstants.SpareQuarterCount);
            QuarterCount = quarterCount;

            int trackCount = UIConstants.MinTrackCount;
            if (Project != null) foreach (UPart part in Project.Parts) trackCount = Math.Max(trackCount, part.TrackNo + 1 + UIConstants.SpareTrackCount);
            TrackCount = trackCount;
        }

        public int GetPartMinDurTick(UPart part)
        {
            return part.GetMinDurTick(Project);
        }

        # region Calculation

        public double GetSnapUnit() { return OpenUtau.Core.MusicMath.getZoomRatio(QuarterWidth, BeatPerBar, BeatUnit, MinTickWidth); }
        public int CanvasToTrack(double Y) { return (int)((Y + OffsetY) / TrackHeight); }
        public double TrackToCanvas(int noteNum) { return TrackHeight * noteNum - OffsetY; }
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
        public int CanvasToSnappedTick(double X) { return (int)(CanvasToSnappedQuarter(X) * Project.Resolution); }

        # endregion

        # region Cmd Handling

        private void OnTrackAdded(UTrack track)
        {
            var trackHeader = new TrackHeader() { Track = track, Height = TrackHeight };
            TrackHeaders.Add(trackHeader);
            HeaderCanvas.Children.Add(trackHeader);
            Canvas.SetTop(trackHeader, -OffsetY + TrackHeight * trackHeader.Track.TrackNo + 1);
            trackHeader.Width = HeaderCanvas.ActualWidth;
            MarkUpdate();
        }

        private void OnTrackRemoved(UTrack track)
        {
            var trackHeader = GetTrackHeader(track);
            HeaderCanvas.Children.Remove(trackHeader);
            TrackHeaders.Remove(trackHeader);
            MarkUpdate();
        }

        private void OnPartAdded(UPart part)
        {
            PartElement partElement;
            if (part is UWavePart) partElement = new WavePartElement() { Part = part, Project = Project };
            else partElement = new VoicePartElement() { Part = part, Project = Project };

            partElement.Redraw();
            PartElements.Add(partElement);
            TrackCanvas.Children.Add(partElement);
            Canvas.SetZIndex(partElement, UIConstants.PartElementZIndex);

            UpdateViewSize();
            MarkUpdate();
        }

        private void OnPartRemoved(UPart part)
        {
            if (SelectedParts.Contains(part)) SelectedParts.Remove(part);
            var partElement = GetPartElement(part);
            TrackCanvas.Children.Remove(partElement);
            PartElements.Remove(partElement);

            UpdateViewSize();
            MarkUpdate();
        }

        private void OnProjectLoad(UProject project)
        {
            OnProjectUnload();

            foreach (UPart part in project.Parts)
            {
                OnPartAdded(part);
            }

            foreach (var track in project.Tracks)
            {
                OnTrackAdded(track);
            }
        }

        private void OnProjectUnload()
        {
            foreach (PartElement element in PartElements)
            {
                TrackCanvas.Children.Remove(element);
            }
            SelectedParts.Clear();
            PartElements.Clear();
            foreach (TrackHeader trackHeader in TrackHeaders)
            {
                HeaderCanvas.Children.Remove(trackHeader);
            }
            TrackHeaders.Clear();
            if (Project != null)
            {
                foreach (UPart part in Project.Parts) part.Dispose();
            }
        }

        # endregion

        # region ICmdSubscriber

        public void Subscribe(ICmdPublisher publisher) { if (publisher != null) publisher.Subscribe(this); }

        public void OnNext(UCommand cmd, bool isUndo)
        {
            if (cmd is NoteCommand)
            {
                var _cmd = cmd as NoteCommand;
                GetPartElement(_cmd.Part).Modified = true;
            }
            else if (cmd is PartCommand)
            {
                var _cmd = cmd as PartCommand;
                if (_cmd is AddPartCommand)
                {
                    if (!isUndo) OnPartAdded(_cmd.part);
                    else OnPartRemoved(_cmd.part);
                }
                else if (_cmd is RemovePartCommand)
                {
                    if (!isUndo) OnPartRemoved(_cmd.part);
                    else OnPartAdded(_cmd.part);
                }
                else if (_cmd is ResizePartCommand) MarkUpdate();
                else if (_cmd is MovePartCommand) MarkUpdate();
            }
            else if (cmd is TrackCommand)
            {
                var _cmd = cmd as TrackCommand;
                if (_cmd is AddTrackCommand)
                {
                    if (!isUndo) OnTrackAdded(_cmd.track);
                    else OnTrackRemoved(_cmd.track);
                }
                else if (_cmd is RemoveTrackCommand)
                {
                    if (!isUndo) OnTrackRemoved(_cmd.track);
                    else OnTrackAdded(_cmd.track);
                }
            }
            else if (cmd is LoadProjectNotification)
            {
                OnProjectLoad(((LoadProjectNotification)cmd).project);
            }
        }

        # endregion

    }
}
