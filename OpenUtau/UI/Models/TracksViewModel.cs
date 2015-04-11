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

using OpenUtau.Core.USTx;
using OpenUtau.UI.Models;
using OpenUtau.UI.Controls;

namespace OpenUtau.UI.Models
{
    public class TracksViewModel : INotifyPropertyChanged
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

        public UProject Project;
        public Canvas TrackCanvas;

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

        List<PartThumbnail> SelectedThumbnails = new List<PartThumbnail>();
        List<PartThumbnail> Thumbnails = new List<PartThumbnail>();

        List<WaveThumbnail> WaveThumbnails = new List<WaveThumbnail>();

        public TracksViewModel() { }

        public void DeselectAll()
        {
            SelectedThumbnails.Clear();
        }

        public void UnloadProject()
        {
            foreach (PartThumbnail thumbnail in Thumbnails)
            {
                TrackCanvas.Children.Remove(thumbnail);
                TrackCanvas.Children.Remove(thumbnail.Box);
            }
            Thumbnails.Clear();
            Project = null;
        }

        public void AddPart(UPart part)
        {
            if (part is UWave)
            {
                WaveThumbnail waveThumb = new WaveThumbnail() { Brush = Brushes.White };
                waveThumb.Wave = (UWave)part;
                waveThumb.Redraw();
                TrackCanvas.Children.Add(waveThumb.WavePath);
                Canvas.SetZIndex(waveThumb.WavePath, UIConstants.PartThumbnailZIndex);

                Rectangle thumbBox = new Rectangle() { RadiusX = 4, RadiusY = 4, Fill = ThemeManager.NoteFillBrushes[0], Stroke = ThemeManager.NoteFillErrorBrushes[0], StrokeThickness = 0 };
                TrackCanvas.Children.Add(thumbBox);
                Canvas.SetZIndex(thumbBox, UIConstants.PartRectangleZIndex);

                waveThumb.Box = thumbBox;
                WaveThumbnails.Add(waveThumb);
            }
            else
            {
                PartThumbnail partThumb = new PartThumbnail() { Brush = Brushes.White, Part = part };
                partThumb.Redraw();
                TrackCanvas.Children.Add(partThumb);
                Canvas.SetZIndex(partThumb, UIConstants.PartThumbnailZIndex);

                Rectangle thumbBox = new Rectangle() { RadiusX = 4, RadiusY = 4, Fill = ThemeManager.NoteFillBrushes[0], Stroke = ThemeManager.NoteFillErrorBrushes[0], StrokeThickness = 0 };
                TrackCanvas.Children.Add(thumbBox);
                Canvas.SetZIndex(thumbBox, UIConstants.PartRectangleZIndex);

                partThumb.Box = thumbBox;
                Thumbnails.Add(partThumb);
            }
            UpdateViewSize();
            MarkUpdate();
        }

        public void LoadProject(UProject project)
        {
            UnloadProject();
            Project = project;

            foreach (UPart part in project.Parts)
            {
                AddPart(part);
            }
        }

        public void RedrawIfUpdated()
        {
            if (_updated)
            {
                foreach (PartThumbnail thumb in Thumbnails)
                {
                    if (thumb.Modified) thumb.Redraw();
                    thumb.X = -OffsetX + thumb.PosTick * QuarterWidth / Project.Resolution;
                    thumb.Y = -OffsetY + thumb.TrackNo * TrackHeight + 1;
                    thumb.FitHeight(TrackHeight - 2);
                    thumb.ScaleX = QuarterWidth / Project.Resolution;

                    thumb.Box.Height = TrackHeight - 2;
                    thumb.Box.Width = thumb.DisplayWidth - 1;
                    Canvas.SetTop(thumb.Box, thumb.Y);
                    Canvas.SetLeft(thumb.Box, thumb.X + 1);
                }
                foreach (WaveThumbnail thumb in WaveThumbnails)
                {
                    thumb.X = -OffsetX + thumb.PosTick * QuarterWidth / Project.Resolution;
                    thumb.Y = -OffsetY + (thumb.TrackNo + 0.5) * TrackHeight;
                    thumb.FitHeight(TrackHeight / 2 - 1);
                    thumb.ScaleX = QuarterWidth / Project.Resolution * 15;

                    thumb.Box.Height = TrackHeight - 2;
                    thumb.Box.Width = thumb.DisplayWidth - 1;
                    Canvas.SetTop(thumb.Box, -OffsetY + thumb.TrackNo * TrackHeight + 1);
                    Canvas.SetLeft(thumb.Box, thumb.X + 1);
                }
            }
            _updated = false;
        }

        public void UpdatePartThumbnail(UPart part)
        {
            foreach (PartThumbnail thumb in Thumbnails) if (thumb.Part == part) thumb.Redraw();
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
            int durTick = 0;
            foreach (UNote note in part.Notes) durTick = Math.Max(durTick, note.PosTick + note.DurTick);
            return durTick;
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
    }
}
