using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.ComponentModel;

using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.UI.Controls;

namespace OpenUtau.UI.Models {
    class TracksViewModel : INotifyPropertyChanged, ICmdSubscriber {
        # region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public UProject Project { get { return DocManager.Inst.Project; } }
        public Canvas TimelineCanvas;
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

        public string Title { get { if (Project != null) return "OpenUtau - [" + Project.name + "]"; else return "OpenUtau"; } }
        public double TotalHeight { get { return _trackCount * _trackHeight - _viewHeight; } }
        public double TotalWidth { get { return _quarterCount * _quarterWidth - _viewWidth; } }
        public double TrackCount { set { if (_trackCount != value) { _trackCount = value; VerticalPropertiesChanged(); } } get { return _trackCount; } }
        public double QuarterCount { set { if (_quarterCount != value) { _quarterCount = value; HorizontalPropertiesChanged(); } } get { return _quarterCount; } }
        public double TrackHeight {
            set {
                _trackHeight = Math.Max(UIConstants.TrackMinHeight, Math.Min(UIConstants.TrackMaxHeight, value));
                VerticalPropertiesChanged();
            }
            get { return _trackHeight; }
        }

        public double QuarterWidth {
            set {
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
        public double BPM => Project.bpm;
        public int BeatPerBar => Project.beatPerBar;
        public int BeatUnit => Project.beatUnit;
        public TimeSpan PlayPosTime => TimeSpan.FromMilliseconds((int)Project.TickToMillisecond(playPosTick));

        public void HorizontalPropertiesChanged() {
            OnPropertyChanged(nameof(QuarterWidth));
            OnPropertyChanged(nameof(TotalWidth));
            OnPropertyChanged(nameof(OffsetX));
            OnPropertyChanged(nameof(ViewportSizeX));
            OnPropertyChanged(nameof(SmallChangeX));
            OnPropertyChanged(nameof(QuarterOffset));
            OnPropertyChanged(nameof(MinTickWidth));
            OnPropertyChanged(nameof(BeatPerBar));
            OnPropertyChanged(nameof(BeatUnit));
            MarkUpdate();
        }

        public void VerticalPropertiesChanged() {
            OnPropertyChanged(nameof(TrackHeight));
            OnPropertyChanged(nameof(TotalHeight));
            OnPropertyChanged(nameof(OffsetY));
            OnPropertyChanged(nameof(ViewportSizeY));
            OnPropertyChanged(nameof(SmallChangeY));
            MarkUpdate();
        }

        #endregion

        readonly List<PartElement> PartElements = new List<PartElement>();
        readonly List<TrackHeader> TrackHeaders = new List<TrackHeader>();

        public TracksViewModel() { }

        # region Selection

        public List<UPart> SelectedParts = new List<UPart>();
        readonly List<UPart> TempSelectedParts = new List<UPart>();

        public void UpdateSelectedVisual() {
            foreach (PartElement partEl in PartElements) {
                if (SelectedParts.Contains(partEl.Part) || TempSelectedParts.Contains(partEl.Part)) partEl.Selected = true;
                else partEl.Selected = false;
            }
        }

        public void SelectAll() { SelectedParts.Clear(); foreach (UPart part in Project.parts) SelectedParts.Add(part); UpdateSelectedVisual(); }
        public void DeselectAll() { SelectedParts.Clear(); UpdateSelectedVisual(); }

        public void SelectPart(UPart part) { if (!SelectedParts.Contains(part)) SelectedParts.Add(part); }
        public void DeselectPart(UPart part) { SelectedParts.Remove(part); }

        public void SelectTempPart(UPart part) { TempSelectedParts.Add(part); }
        public void TempSelectInBox(double quarter1, double quarter2, int track1, int track2) {
            if (quarter2 < quarter1) { double temp = quarter1; quarter1 = quarter2; quarter2 = temp; }
            if (track2 < track1) { int temp = track1; track1 = track2; track2 = temp; }
            int tick1 = (int)(quarter1 * Project.resolution);
            int tick2 = (int)(quarter2 * Project.resolution);
            TempSelectedParts.Clear();
            foreach (UPart part in Project.parts) {
                if (part.position <= tick2 && part.EndTick >= tick1 &&
                    part.trackNo <= track2 && part.trackNo >= track1) SelectTempPart(part);
            }
            UpdateSelectedVisual();
        }

        public void DoneTempSelect() {
            foreach (UPart part in TempSelectedParts) SelectPart(part);
            TempSelectedParts.Clear();
            UpdateSelectedVisual();
        }

        # endregion

        public PartElement GetPartElement(UPart part) {
            foreach (PartElement partEl in PartElements) {
                if (partEl.Part == part) return partEl;
            }
            return null;
        }

        public TrackHeader GetTrackHeader(UTrack track) {
            foreach (var trackHeader in TrackHeaders) {
                if (trackHeader.Track == track) return trackHeader;
            }
            return null;
        }

        public void RedrawIfUpdated() {
            if (_updated) {
                foreach (PartElement partElement in PartElements) {
                    if (partElement.Modified) partElement.Redraw();
                    partElement.X = -OffsetX + partElement.Part.position * QuarterWidth / Project.resolution;
                    partElement.Y = -OffsetY + partElement.Part.trackNo * TrackHeight + 1;
                    partElement.VisualHeight = TrackHeight - 2;
                    partElement.ScaleX = QuarterWidth / Project.resolution;
                    partElement.CanvasWidth = this.TrackCanvas.ActualWidth;
                }
                foreach (TrackHeader trackHeader in TrackHeaders) {
                    Canvas.SetTop(trackHeader, -OffsetY + TrackHeight * trackHeader.Track.TrackNo);
                    trackHeader.Height = TrackHeight;
                    trackHeader.UpdateTrackNo();
                }
                UpdatePlayPosMarker();
            }
            _updated = false;
            PlaybackManager.Inst.UpdatePlayPos();
        }

        public void UpdateViewSize() {
            double quarterCount = UIConstants.MinQuarterCount;
            if (Project != null)
                foreach (UPart part in Project.parts)
                    quarterCount = Math.Max(quarterCount, (part.Duration + part.position) / Project.resolution + UIConstants.SpareQuarterCount);
            QuarterCount = quarterCount;

            int trackCount = UIConstants.MinTrackCount;
            if (Project != null) foreach (UPart part in Project.parts) trackCount = Math.Max(trackCount, part.trackNo + 1 + UIConstants.SpareTrackCount);
            TrackCount = trackCount;
        }

        public int GetPartMinDurTick(UPart part) {
            return part.GetMinDurTick(Project);
        }

        # region PlayPosMarker

        public int playPosTick = 0;
        Path playPosMarker;
        Rectangle playPosMarkerHighlight;

        private void initPlayPosMarker() {
            playPosTick = 0;
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
                TrackCanvas.Children.Add(playPosMarkerHighlight);
            }
        }

        public void UpdatePlayPosMarker() {
            double quarter = (double)playPosTick / DocManager.Inst.Project.resolution;
            int playPosMarkerOffset = (int)Math.Round(QuarterToCanvas(quarter) + 0.5);
            Canvas.SetLeft(playPosMarker, playPosMarkerOffset - 6);
            playPosMarkerHighlight.Height = TrackCanvas.ActualHeight;
            double zoomRatio = MusicMath.getZoomRatio(QuarterWidth, BeatPerBar, BeatUnit, MinTickWidth);
            double interval = zoomRatio * QuarterWidth;
            int left = (int)Math.Round(QuarterToCanvas((int)(quarter / zoomRatio) * zoomRatio) + 0.5);
            playPosMarkerHighlight.Width = interval;
            Canvas.SetLeft(playPosMarkerHighlight, left);
        }

        # endregion

        # region Calculation

        public double GetSnapUnit() { return OpenUtau.Core.MusicMath.getZoomRatio(QuarterWidth, BeatPerBar, BeatUnit, MinTickWidth); }
        public int CanvasToTrack(double Y) { return (int)((Y + OffsetY) / TrackHeight); }
        public double TrackToCanvas(int noteNum) { return TrackHeight * noteNum - OffsetY; }
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
        public int CanvasToSnappedTick(double X) { return (int)(CanvasToSnappedQuarter(X) * Project.resolution); }

        # endregion

        # region Cmd Handling

        private void OnTrackAdded(UTrack track, List<UPart> addedParts = null) {
            var trackHeader = new TrackHeader() { Track = track, Height = TrackHeight };
            TrackHeaders.Add(trackHeader);
            HeaderCanvas.Children.Add(trackHeader);
            Canvas.SetTop(trackHeader, -OffsetY + TrackHeight * trackHeader.Track.TrackNo);
            trackHeader.Width = HeaderCanvas.ActualWidth;
            if (addedParts != null) foreach (var part in addedParts) OnPartAdded(part);
            MarkUpdate();
        }

        private void OnTrackRemoved(UTrack track, List<UPart> removedParts = null) {
            var trackHeader = GetTrackHeader(track);
            HeaderCanvas.Children.Remove(trackHeader);
            TrackHeaders.Remove(trackHeader);
            if (removedParts != null) foreach (var part in removedParts) OnPartRemoved(part);
            MarkUpdate();
        }

        private void OnPartAdded(UPart part) {
            PartElement partElement;
            if (part is UWavePart) {
                partElement = new WavePartElement(part) {
                    Project = Project,
                };
            } else {
                partElement = new VoicePartElement() {
                    Part = part,
                    Project = Project,
                };
            }

            partElement.Redraw();
            PartElements.Add(partElement);
            TrackCanvas.Children.Add(partElement);
            Canvas.SetZIndex(partElement, UIConstants.PartElementZIndex);

            UpdateViewSize();
            MarkUpdate();
        }

        private void OnPartRemoved(UPart part) {
            if (SelectedParts.Contains(part)) SelectedParts.Remove(part);
            var partElement = GetPartElement(part);
            TrackCanvas.Children.Remove(partElement);
            PartElements.Remove(partElement);

            UpdateViewSize();
            MarkUpdate();
        }

        private void OnProjectLoad(UProject project) {
            OnProjectUnload();

            foreach (UPart part in project.parts) {
                OnPartAdded(part);
            }

            foreach (var track in project.tracks) {
                OnTrackAdded(track);
            }

            OnPropertyChanged(nameof(BeatPerBar));
            OnPropertyChanged(nameof(BeatUnit));
            OnPropertyChanged(nameof(BPM));
            initPlayPosMarker();
        }

        private void OnProjectUnload() {
            foreach (PartElement element in PartElements)
                TrackCanvas.Children.Remove(element);
            SelectedParts.Clear();
            PartElements.Clear();
            foreach (TrackHeader trackHeader in TrackHeaders)
                HeaderCanvas.Children.Remove(trackHeader);
            TrackHeaders.Clear();
        }

        private void OnPlayPosSet(int playPosTick) {
            this.playPosTick = playPosTick;
            double playPosPix = QuarterToCanvas((double)playPosTick / Project.resolution);
            if (playPosPix > TrackCanvas.ActualWidth * UIConstants.PlayPosMarkerMargin) {
                OffsetX += playPosPix - TrackCanvas.ActualWidth * UIConstants.PlayPosMarkerMargin;
            }
            MarkUpdate();
            OnPropertyChanged(nameof(PlayPosTime));
        }

        # endregion

        # region ICmdSubscriber

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is NoteCommand) {
                var _cmd = cmd as NoteCommand;
                GetPartElement(_cmd.Part).Modified = true;
            } else if (cmd is PartCommand) {
                var _cmd = cmd as PartCommand;
                if (_cmd is AddPartCommand) {
                    if (!isUndo) OnPartAdded(_cmd.part);
                    else OnPartRemoved(_cmd.part);
                } else if (_cmd is RemovePartCommand) {
                    if (!isUndo) OnPartRemoved(_cmd.part);
                    else OnPartAdded(_cmd.part);
                } else if (_cmd is ReplacePartCommand rpCmd) {
                    var element = PartElements.Find(el => !isUndo ? el.Part == rpCmd.part : el.Part == rpCmd.newPart);
                    element.Part = !isUndo ? rpCmd.newPart : rpCmd.part;
                    element.Modified = true;
                    MarkUpdate();
                } else {
                    var element = PartElements.Find(el => el.Part == _cmd.part);
                    element.Modified = true;
                    MarkUpdate();
                }
            } else if (cmd is TrackCommand) {
                var _cmd = cmd as TrackCommand;
                if (_cmd is AddTrackCommand) {
                    if (!isUndo) OnTrackAdded(_cmd.track);
                    else OnTrackRemoved(_cmd.track);
                } else if (_cmd is RemoveTrackCommand) {
                    if (!isUndo) OnTrackRemoved(_cmd.track, ((RemoveTrackCommand)_cmd).removedParts);
                    else OnTrackAdded(_cmd.track, ((RemoveTrackCommand)_cmd).removedParts);
                } else if (_cmd is TrackChangeSingerCommand) {
                    foreach (var trackHeader in TrackHeaders) {
                        trackHeader.UpdateSingerName();
                    }
                } else if (_cmd is TrackChangePhonemizerCommand) {
                    foreach (var trackHeader in TrackHeaders) {
                        trackHeader.UpdatePhonemizerName();
                    }
                }
                MarkUpdate();
            } else if (cmd is BpmCommand) {
                OnPropertyChanged(nameof(BPM));
            } else if (cmd is TimeSignatureCommand) {
                OnPropertyChanged(nameof(BeatPerBar));
                OnPropertyChanged(nameof(BeatUnit));
            } else if (cmd is LoadProjectNotification) {
                OnProjectLoad(((LoadProjectNotification)cmd).project);
            } else if (cmd is SetPlayPosTickNotification) {
                var _cmd = cmd as SetPlayPosTickNotification;
                OnPlayPosSet(_cmd.playPosTick);
            } else if (cmd is UserMessageNotification) {
                var _cmd = cmd as UserMessageNotification;
                MessageBox.Show(_cmd.message);
            }
        }

        # endregion
    }
}
