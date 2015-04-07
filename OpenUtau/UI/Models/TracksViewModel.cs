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
        double _trackCount = UIConstants.DefaultTrackCount;
        double _quarterCount = UIConstants.DefaultQuarterCount;
        double _quarterWidth = UIConstants.TrackQuarterDefaultWidth;
        double _viewWidth = 0;
        double _viewHeight = 0;
        double _offsetX = 0;
        double _offsetY = 0;
        double _quarterOffset = 0;
        double _minTickWidth = UIConstants.TrackTickMinWidth;
        int _beatPerBar = 4;
        int _beatUnit = 4;

        public double TotalHeight { get { return _trackCount * _trackHeight - _viewHeight; } }
        public double TotalWidth { get { return _quarterCount * _quarterWidth - _viewWidth; } }

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

        public double ViewWidth { set { _viewWidth = value; HorizontalPropertiesChanged(); } get { return _viewWidth; } }
        public double ViewHeight { set { _viewHeight = value; VerticalPropertiesChanged(); } get { return _viewHeight; } }
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

        List<Tuple<PartThumbnail, Rectangle>> Thumbnails = new List<Tuple<PartThumbnail,Rectangle>>();

        public TracksViewModel() { }

        public void CloseProject()
        {
            foreach (Tuple<PartThumbnail,Rectangle> thumbnail in Thumbnails)
            {
                TrackCanvas.Children.Remove(thumbnail.Item1);
                TrackCanvas.Children.Remove(thumbnail.Item2);
            }
            Thumbnails.Clear();
            Project = null;
        }

        public void LoadProject(UProject project)
        {
            CloseProject();
            Project = project;

            foreach (UPart part in project.Parts)
            {
                PartThumbnail partThumb = new PartThumbnail() { Brush = ThemeManager.NoteFillBrushes[0], Part = part};
                partThumb.Redraw();
                TrackCanvas.Children.Add(partThumb);
                Canvas.SetZIndex(partThumb, UIConstants.PartThumbnailZIndex);

                Rectangle thumbBox = new Rectangle() { RadiusX = 2, RadiusY = 2, Fill = ThemeManager.NoteFillErrorBrushes[0], Stroke = ThemeManager.NoteFillBrushes[0], StrokeThickness = 1 };
                TrackCanvas.Children.Add(thumbBox);
                Canvas.SetZIndex(thumbBox, UIConstants.PartRectangleZIndex);

                Thumbnails.Add(new Tuple<PartThumbnail, Rectangle>(partThumb, thumbBox));
            }
            MarkUpdate();
        }

        public void RedrawIfUpdated()
        {
            if (_updated)
            {
                foreach (Tuple<PartThumbnail, Rectangle> thumb in Thumbnails)
                {
                    if (thumb.Item1.VisualDurTick != thumb.Item1.Part.DurTick) thumb.Item1.Redraw();
                    thumb.Item1.X = -OffsetX + thumb.Item1.PosTick * QuarterWidth / Project.Resolution;
                    thumb.Item1.Y = -OffsetY + thumb.Item1.TrackNo * TrackHeight + 1;
                    thumb.Item1.FitHeight(TrackHeight - 2);
                    thumb.Item1.ScaleX = QuarterWidth / Project.Resolution;

                    thumb.Item2.Height = TrackHeight - 2;
                    thumb.Item2.Width = thumb.Item1.DisplayWidth - 1;
                    Canvas.SetTop(thumb.Item2, thumb.Item1.Y);
                    Canvas.SetLeft(thumb.Item2, thumb.Item1.X + 1);
                }
            }
            _updated = false;
        }

        public double GetSnapUnit() { return OpenUtau.Core.MusicMath.getZoomRatio(QuarterWidth, BeatPerBar, BeatUnit, MinTickWidth); }
        public int CanvasToTrack(double Y) { return (int)((Y + OffsetY) / TrackHeight); }
        public double CanvasToQuarter(double X) { return (X + OffsetX) / QuarterWidth; }
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
    }
}
