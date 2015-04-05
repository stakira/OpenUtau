using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using OpenUtau.UI.Models;
using OpenUtau.Core.USTx;

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
                    new Rect(0, (int)top, _size.Width, TrackHeight));
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

        protected Size _size;

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
                    MusicMath.IsBlackKey(alt) ? ThemeManager.TrackBackgroundBrushAlt : ThemeManager.TrackBackgroundBrush,
                    null,
                    new Rect(0, (int)top, _size.Width, KeyTrackHeight));
                top += KeyTrackHeight;
                alt = (alt + 1) % 12;
            }
        }
    }

    class KeyboardBackground : KeyTrackBackground
    {
        protected override void OnRender(DrawingContext drawingContext)
        {
            int firstTrack = (int)(VerticalOffset / KeyTrackHeight);
            int alt = firstTrack;
            double top = KeyTrackHeight * firstTrack - VerticalOffset;

            while (top < _size.Height)
            {
                drawingContext.DrawRectangle(
                    MusicMath.IsBlackKey(alt) ? ThemeManager.BlackKeyBrushNormal : 
                    MusicMath.IsCenterKey(alt) ? ThemeManager.CenterKeyBrushNormal : ThemeManager.WhiteKeyBrushNormal,
                    null,
                    new Rect(0, (int)top, _size.Width, KeyTrackHeight));

                if (KeyTrackHeight >= 12)
                {
                    FormattedText text = new FormattedText(
                        MusicMath.GetKeyString(UIConstants.MaxNoteNum - alt - 1),
                        System.Threading.Thread.CurrentThread.CurrentUICulture,
                        FlowDirection.LeftToRight,
                        SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                        12,
                        MusicMath.IsBlackKey(alt) ? ThemeManager.BlackKeyNameBrushNormal :
                        MusicMath.IsCenterKey(alt) ? ThemeManager.CenterKeyNameBrushNormal : ThemeManager.WhiteKeyNameBrushNormal
                    );
                    drawingContext.DrawText(text, new Point(42 - text.Width, (int)(top + (KeyTrackHeight - text.Height) / 2)));
                }
                top += KeyTrackHeight;
                alt ++;
            }
        }
    }

    public enum ZoomLevel : int { Bar, Beat, HalfNote, QuaterNote, EighthNote, SixteenthNote, ThritySecondNote, SixtyfourthNote };

    public static class MusicMath
    {
        public static string[] noteStrings = new String[12] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        public static string GetKeyString(int keyNo) { return keyNo < 0 ? "" : noteStrings[keyNo % 12] + (keyNo / 12 - 2).ToString(); }

        public static double TickToNote(int tick, int resolution) { return tick / resolution; }

        public static int [] BlackNoteNums = { 1, 3, 6, 8, 10 };
        public static bool IsBlackKey(int noteNum) { return BlackNoteNums.Contains(noteNum % 12); }

        public static bool IsCenterKey(int noteNum) { return noteNum % 12 == 0; }
        
        public static double[] zoomRatios = { 1.0, 1.0/2, 1.0/4, 1.0/8, 1.0/16, 1.0/32, 1.0/64, 1.0/128, 1.0/256 };

        public static double getZoomRatio(double wholeNoteWidth, int beatPerBar, int beatUnit, double minWidth)
        {
            int i;

            switch (beatUnit)
            {
                case 2: i = 1; break;
                case 4: i = 2; break;
                case 8: i = 3; break;
                case 16: i = 4; break;
                default: throw new Exception("Invalid beat unit.");
            }

            if (beatPerBar % 4 == 0) i--; // level below bar is half bar, or 2 beatunit
            // else // otherwise level below bar is beat unit

            if (wholeNoteWidth * beatPerBar <= minWidth * beatUnit)
            {
                return beatPerBar / beatUnit; 
            }
            else
            {
                while (i + 1 < zoomRatios.Length && wholeNoteWidth * zoomRatios[i + 1] > UIConstants.NoteMinDisplayWidth) i++;
                return zoomRatios[i];
            }
        }
    }

    class TickBackground : FrameworkElement
    {
        double _wholeNoteWidth = UIConstants.MidiWNoteDefaultWidth;
        double _minTickWidth = UIConstants.MidiTickMinWidth;
        double _horizontalOffset = 0;
        double _barOffset = 0;
        int _beatPerBar = 3;
        int _beatUnit = 4;
        protected Size _size;

        public double WholeNoteWidth { set { _wholeNoteWidth = value; this.InvalidateVisual(); } get { return _wholeNoteWidth; } }
        public double MinTickWidth { set { _minTickWidth = value; this.InvalidateVisual(); } get { return _minTickWidth; } }
        public double HorizonOffset { set { _horizontalOffset = value; this.InvalidateVisual(); } get { return _horizontalOffset; } }
        public double BarOffset { set { _barOffset = value; this.InvalidateVisual(); } get { return _barOffset; } }
        public int BeatPerBar { set { _beatPerBar = value; this.InvalidateVisual(); } get { return _beatPerBar; } }
        public int BeatUnit { set { _beatUnit = value; this.InvalidateVisual(); } get { return _beatUnit; } }

        protected Pen darkPen, lightPen, dashedPen;

        System.Diagnostics.Stopwatch sw;
        public TickBackground()
        {
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VisualEdgeMode = EdgeMode.Aliased;
            this.SizeChanged += (o, e) => { _size = e.NewSize; this.InvalidateVisual(); };
            darkPen = new Pen(ThemeManager.TickLineBrushDark, 1);
            lightPen = new Pen(ThemeManager.TickLineBrushLight, 1);
            dashedPen = new Pen(ThemeManager.TickLineBrushLight, 1) { DashStyle = new DashStyle(UIConstants.DashLineArray, 0) };
            sw = new System.Diagnostics.Stopwatch();
            sw.Start();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {

            double zoomRatio = MusicMath.getZoomRatio(WholeNoteWidth, BeatPerBar, BeatUnit, MinTickWidth);
            double interval = zoomRatio * WholeNoteWidth;
            int tick = (int)(HorizonOffset / interval) + 1;
            double left = tick * interval - HorizonOffset;

            while (left < _size.Width)
            {
                double snappedLeft = Math.Round(left) + 0.5;
                if ((tick * zoomRatio * BeatUnit) % BeatPerBar == 0)
                {
                    drawingContext.DrawLine(darkPen, new Point(snappedLeft, -0.5), new Point(snappedLeft, ActualHeight + 0.5));
                }
                else if ((tick * zoomRatio * BeatUnit) % 1 == 0)
                {
                    drawingContext.DrawLine(darkPen, new Point(snappedLeft, -0.5), new Point(snappedLeft, ActualHeight + 0.5));
                }
                else if ((tick * zoomRatio * BeatUnit) % 0.25 == 0)
                {
                    drawingContext.DrawLine(lightPen, new Point(snappedLeft, -0.5), new Point(snappedLeft, ActualHeight + 0.5));
                }
                else
                {
                    drawingContext.DrawLine(dashedPen, new Point(snappedLeft, -0.5), new Point(snappedLeft, ActualHeight + 0.5));
                }
                left += interval;
                tick++;
            }

            System.Diagnostics.Debug.WriteLine("tick " + sw.Elapsed.TotalMilliseconds.ToString());
        }
    }

    class TimelineBackground : TickBackground
    {
        protected override void OnRender(DrawingContext drawingContext)
        {
            double zoomRatio = MusicMath.getZoomRatio(WholeNoteWidth, BeatPerBar, BeatUnit, MinTickWidth);
            double interval = zoomRatio * WholeNoteWidth;
            int tick = (int)(HorizonOffset / interval);
            double left = tick * interval - HorizonOffset;

            while (left < _size.Width)
            {
                double snappedLeft = Math.Round(left) + 0.5;
                System.Diagnostics.Debug.WriteLine("tick = " + tick.ToString());
                if ((tick * zoomRatio * BeatUnit) % BeatPerBar == 0)
                {
                    System.Diagnostics.Debug.WriteLine("tick draw = {0} zoomRatio {1} BeatUnit {2}, BeatPerBar {3}", tick, zoomRatio, BeatUnit, BeatPerBar);
                    if (left != 0) drawingContext.DrawLine(darkPen, new Point(snappedLeft, -0.5), new Point(snappedLeft, ActualHeight + 0.5));
                    drawingContext.DrawText(
                        new FormattedText(
                            ((tick * zoomRatio * BeatUnit) / BeatPerBar).ToString(),
                            System.Threading.Thread.CurrentThread.CurrentUICulture,
                            FlowDirection.LeftToRight, SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                            12,
                            darkPen.Brush),
                        new Point(snappedLeft + 3, 3)
                    );
                }
                left += interval;
                tick++;
            }
        }
    }
}
