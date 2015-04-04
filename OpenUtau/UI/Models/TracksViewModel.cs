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

using OpenUtau.Core.USTx;
using OpenUtau.UI.Models;
using OpenUtau.UI.Controls;

namespace OpenUtau.UI.Models
{
    class TracksViewModel
    {
        public UProject Project;
        public Canvas TrackCanvas;

        double _trackHeigt = UIConstants.TrackDefaultHeight;
        double _wholeNoteWidth = UIConstants.TrackWNoteDefaultWidth;

        public double TrackHeigt
        {
            set { _trackHeigt = Math.Max(UIConstants.TrackMinHeight, Math.Min(UIConstants.TrackMaxHeight, value)); }
            get { return _trackHeigt; }
        }
        
        public double WholeNoteWidth
        {
            set { _wholeNoteWidth = Math.Max(UIConstants.TrackWNoteMinWidth, Math.Min(UIConstants.TrackWNoteMaxWidth, value)); }
            get { return _wholeNoteWidth; }
        }

        List<PartThumbnail> Thumbnails = new List<PartThumbnail>();
        List<Rectangle> ThumbnailBoxes = new List<Rectangle>();

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
                    double top = Math.Round(TrackHeigt * i);
                    Thumbnails.Add(new PartThumbnail() { Brush = /*ThemeManager.NoteFillBrushes[0]*/Brushes.White, Part = part, ScaleX = scaleX });
                    Thumbnails.Last().Redraw();
                    Thumbnails.Last().FitHeight(TrackHeigt-2);
                    TrackCanvas.Children.Add(Thumbnails.Last());
                    Canvas.SetTop(Thumbnails.Last(), top + 1);
                    Canvas.SetLeft(Thumbnails.Last(), left);
                    Canvas.SetZIndex(Thumbnails.Last(), UIConstants.PartThumbnailZIndex);
                    ThumbnailBoxes.Add(new Rectangle()
                    {
                        RadiusX = 4,
                        RadiusY = 4,
                        Width = Thumbnails.Last().Source.Width * scaleX,
                        Height = TrackHeigt - 2,
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

            TrackBackground tb = new TrackBackground() { TrackHeight = TrackHeigt };
            TrackCanvas.Children.Add(tb);
        }

        public void Redraw()
        {

        }
    }

    class ThumbContainer : FrameworkElement
    {
        PartThumbnail Thumb;
        Rectangle Box;

        public void AddThumb(PartThumbnail thumb)
        {
            Thumb = thumb;
            AddVisualChild(thumb);
            AddLogicalChild(thumb);
            this.InvalidateVisual();
        }

        public void AddBox(Rectangle box)
        {
            Box = box;
            AddVisualChild(box);
            AddLogicalChild(box);
            this.InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size requiredSize = new Size();

            Thumb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            Box.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            requiredSize.Height = Math.Max(Thumb.DesiredSize.Width, Box.DesiredSize.Width);
            requiredSize.Width = Math.Max(Thumb.DesiredSize.Width, Box.DesiredSize.Width);

            return requiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Thumb.Arrange(new Rect(finalSize));
            Box.Arrange(new Rect(finalSize));
            return base.ArrangeOverride(finalSize);
        }

    }
}
