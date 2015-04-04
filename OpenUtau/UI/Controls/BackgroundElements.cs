using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using OpenUtau.UI.Models;

namespace OpenUtau.UI.Controls
{
    class TrackBackground : FrameworkElement
    {
        double _trackHeight = UIConstants.TrackDefaultHeight;
        double _verticalOffset = 0;

        public double TrackHeight { set { _trackHeight = value; this.InvalidateVisual(); } get { return _trackHeight; } }
        public double VerticalOffset { set { _verticalOffset = value; this.InvalidateVisual(); } get { return _verticalOffset; } }

        Size _size;

        public TrackBackground()
        {
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VisualEdgeMode = EdgeMode.Aliased;
            this.SizeChanged += (o, e) => { _size = e.NewSize; this.InvalidateVisual(); };
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            int firstTrack = (int)(VerticalOffset / TrackHeight);
            bool alt = firstTrack % 2 == 1;
            double top = TrackHeight * firstTrack - VerticalOffset;

            while (top < _size.Height)
            {
                drawingContext.DrawRectangle(
                    alt ? ThemeManager.TrackBackgroundBrush : ThemeManager.TrackBackgroundBrushAlt,
                    null,
                    new Rect(0, top, _size.Width, TrackHeight));
                top += TrackHeight;
                alt = !alt;
            }

        }
    }

    class KeyTrackBackground : FrameworkElement
    {
        double _keyTrackHeight = UIConstants.NoteDefaultHeight;
        double _verticalOffset = 0;

        public double KeyTrackHeight { set { _keyTrackHeight = value; this.InvalidateVisual(); } get { return _keyTrackHeight; } }
        public double VerticalOffset { set { _verticalOffset = value; this.InvalidateVisual(); } get { return _verticalOffset; } }

        Size _size;

        public KeyTrackBackground()
        {
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VisualEdgeMode = EdgeMode.Aliased;
            this.SizeChanged += (o, e) => { _size = e.NewSize; this.InvalidateVisual(); };
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            int firstTrack = (int)(VerticalOffset / KeyTrackHeight);
            int alt = firstTrack % 12;
            double top = KeyTrackHeight * firstTrack - VerticalOffset;

            while (top < _size.Height)
            {
                drawingContext.DrawRectangle(
                    MusicMath.IsBlackNote(alt) ? ThemeManager.TrackBackgroundBrushAlt : ThemeManager.TrackBackgroundBrush,
                    null,
                    new Rect(0, top, _size.Width, KeyTrackHeight));
                top += KeyTrackHeight;
                alt = (alt + 1) % 12;
            }

        }
    }

    public static class MusicMath
    {
        public enum ZoomLevel : int { Bar, Beat, QuaterNote, HalfNote, EighthNote, SixteenthNote, ThritySecondNote, SixtyfourthNote };

        public static double TickToNote(int tick, int resolution) { return tick / resolution; }

        public static int[] BlackNoteNums = { 1, 3, 6, 8, 10 };
        public static bool IsBlackNote(int noteNum) { return BlackNoteNums.Contains(noteNum % 12); }

        public static ZoomLevel getZoomLevel(double wholeNoteWidth)
        {
            switch ((int)Math.Log(wholeNoteWidth / 6, 2))
            {
                case 0: return (ZoomLevel)0;
            }
            if (wholeNoteWidth < UIConstants.NoteMinDisplayWidth) return ZoomLevel.Beat;
            else if (wholeNoteWidth < UIConstants.NoteMinDisplayWidth * 2) return ZoomLevel.HalfNote;
            else if (wholeNoteWidth < UIConstants.NoteMinDisplayWidth * 4) return ZoomLevel.QuaterNote;
            else if (wholeNoteWidth < UIConstants.NoteMinDisplayWidth * 8) return ZoomLevel.EighthNote;
            else if (wholeNoteWidth < UIConstants.NoteMinDisplayWidth * 16) return ZoomLevel.SixteenthNote;
            else if (wholeNoteWidth < UIConstants.NoteMinDisplayWidth * 32) return ZoomLevel.ThritySecondNote;
            else return ZoomLevel.SixtyfourthNote;
        }
    }

    class TickBackground : FrameworkElement
    {
        double _wholeNoteWidth = UIConstants.MidiWNoteDefaultWidth;
        double _horizontalOffset = 0;
        double _startOffset = 0;
        //double _zoomLevel = Z;

        public double WholeNoteWidth { set { _wholeNoteWidth = value; this.InvalidateVisual(); } get { return _wholeNoteWidth; } }
        public double HorizonOffset { set { _horizontalOffset = value; this.InvalidateVisual(); } get { return _horizontalOffset; } }
        public double StartOffset { set { _startOffset = value; this.InvalidateVisual(); } get { return _startOffset; } }
        Size _size;

        Pen pen;

        public TickBackground()
        {
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VisualEdgeMode = EdgeMode.Aliased;
            this.SizeChanged += (o, e) => { _size = e.NewSize; this.InvalidateVisual(); };
            pen = new Pen(ThemeManager.TickLineBrushDark, 1) { DashStyle = new DashStyle(UIConstants.DashLineArray, 0) };
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            int firstTrack = 0;// (int)(VerticalOffset / KeyTrackHeight);
            //int alt = firstTrack % 12;
            double left = 0.5;

            while (left < _size.Width)
            {
                drawingContext.DrawLine(pen, new Point(left, 0.5), new Point(left, ActualHeight - 0.5));
                left += 16;
            }
        }
    }
}
