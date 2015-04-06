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

        double _trackHeight = UIConstants.TrackDefaultHeight;
        double _trackCount = UIConstants.DefaultTrackCount;
        double _quarterCount = UIConstants.DefaultQuarterCount;
        double _quarterWidth = UIConstants.TrackWNoteDefaultWidth;
        double _offsetX = 0;
        double _offsetY = 0;
        double _quarterOffset = 0;
        double _minTickWidth = UIConstants.TrackTickMinWidth;
        int _beatPerBar = 4;
        int _beatUnit = 4;

        public double TotalHeight { get { return _trackCount * _trackHeight; } }
        public double TotalWidth { get { return _quarterCount * _quarterWidth; } }

        public double TrackHeight
        {
            set
            {
                _trackHeight = Math.Max(UIConstants.TrackMinHeight, Math.Min(UIConstants.TrackMaxHeight, value));
                OnPropertyChanged("TrackHeight");
                OnPropertyChanged("TotalHeight");
            }
            get { return _trackHeight; }
        }
        
        public double QuarterWidth
        {
            set
            {
                _quarterWidth = Math.Max(UIConstants.TrackWNoteMinWidth, Math.Min(UIConstants.TrackWNoteMaxWidth, value));
                OnPropertyChanged("QuarterWidth");
                OnPropertyChanged("TotalWidth");
            }
            get { return _quarterWidth; }
        }

        public double OffsetX { set { _offsetX = Math.Max(0, value); OnPropertyChanged("OffsetX"); } get { return _offsetX; } }
        public double OffsetY { set { _offsetY = Math.Max(0, value); OnPropertyChanged("OffsetY"); } get { return _offsetY; } }
        public double QuarterOffset { set { _quarterOffset = value; OnPropertyChanged("QuarterOffset"); } get { return _quarterOffset; } }
        public double MinTickWidth { set { _minTickWidth = value; OnPropertyChanged("MinTickWidth"); } get { return _minTickWidth; } }
        public int BeatPerBar { set { _beatPerBar = value; OnPropertyChanged("BeatPerBar"); } get { return _beatPerBar; } }
        public int BeatUnit { set { _beatUnit = value; OnPropertyChanged("BeatUnit"); } get { return _beatUnit; } }

        List<PartThumbnail> Thumbnails = new List<PartThumbnail>();
        List<Rectangle> ThumbnailBoxes = new List<Rectangle>();

        public TracksViewModel() {}

        public TracksViewModel(Canvas trackCanvas)
        {
            TrackCanvas = trackCanvas;
        }

        public void LoadProject(UProject project)
        {
            Project = project;

            foreach (PartThumbnail thumbnail in Thumbnails) TrackCanvas.Children.Remove(thumbnail);
            foreach (Rectangle box in ThumbnailBoxes) TrackCanvas.Children.Remove(box);
            Thumbnails.Clear();
            ThumbnailBoxes.Clear();

            int i = 0;
            foreach (UTrack track in project.Tracks) 
            {
                int j = 0;
                foreach (UPart part in track.Parts)
                {
                    double scaleX = 10.0 / project.Resolution;
                    double left = Math.Round(scaleX * part.PosTick);
                    double top = Math.Round(TrackHeight * i);
                    Thumbnails.Add(new PartThumbnail() { Brush = /*ThemeManager.NoteFillBrushes[0]*/Brushes.White, Part = part, ScaleX = scaleX });
                    Thumbnails.Last().Redraw();
                    Thumbnails.Last().FitHeight(TrackHeight-2);
                    TrackCanvas.Children.Add(Thumbnails.Last());
                    Canvas.SetTop(Thumbnails.Last(), top + 1);
                    Canvas.SetLeft(Thumbnails.Last(), left);
                    Canvas.SetZIndex(Thumbnails.Last(), UIConstants.PartThumbnailZIndex);
                    ThumbnailBoxes.Add(new Rectangle()
                    {
                        RadiusX = 4,
                        RadiusY = 4,
                        Width = Thumbnails.Last().Source.Width * scaleX,
                        Height = TrackHeight - 2,
                        Fill = ThemeManager.NoteFillBrushes[0],//NoteFillErrorBrushes[0],
                        Stroke = ThemeManager.NoteFillErrorBrushes[0],//ThemeManager.NoteFillBrushes[0],
                        StrokeThickness = 0//1
                    });
                    //RenderOptions.SetEdgeMode(ThumbnailBoxes.Last(), EdgeMode.Aliased);
                    TrackCanvas.Children.Add(ThumbnailBoxes.Last());
                    Canvas.SetTop(ThumbnailBoxes.Last(), top + 1);
                    Canvas.SetLeft(ThumbnailBoxes.Last(), left);
                    Canvas.SetZIndex(ThumbnailBoxes.Last(), UIConstants.PartRectangleZIndex);

                    j++;
                }
                i ++;
            }

            TrackBackground tb = new TrackBackground() { TrackHeight = TrackHeight };
            TrackCanvas.Children.Add(tb);
        }

        public void Redraw()
        {

        }

        public void Rescroll()
        {

        }
    }
}
