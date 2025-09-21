using Avalonia.Input;

namespace OpenUtau.App { 
    public class ViewConstants {
        public Core.Ustx.UProject? project;

        public double TickWidthMax => project != null ? 256.0 / project.resolution : 256.0 / 480.0;
        public double TickWidthMin => project != null ? 4.0 / project.resolution : 4.0 / 480.0;
        public double TickWidthDefault => project != null ? 24.0 / project.resolution : 24.0 / 480.0;
        public const double MinTicklineWidth = 12.0;

        public const double TrackHeightMax = 144;
        public const double TrackHeightMin = 44;
        public const double TrackHeightDefault = 104;
        public const double TrackHeightDelta = 20;

        public double PianoRollTickWidthMax => project != null ? 640.0 / project.resolution : 640.0 / 480.0;
        public double PianoRollTickWidthMin => project != null ? 4.0 / project.resolution : 4.0 / 480.0;
        public double PianoRollTickWidthDefault => project != null ? 128.0 / project.resolution : 128.0 / 480.0;
        public double PianoRollTickWidthShowDetails => project != null ? 64.0 / project.resolution : 64.0 / 480.0;
        public const double PianoRollMinTicklineWidth = 12.0;

        public const double NoteHeightMax = 128;
        public const double NoteHeightMin = 8;
        public const double NoteHeightDefault = 22;

        public const int MaxTone = 12 * 11;

        public static readonly Cursor cursorCross = new Cursor(StandardCursorType.Cross);
        public static readonly Cursor cursorHand = new Cursor(StandardCursorType.Hand);
        public static readonly Cursor cursorNo = new Cursor(StandardCursorType.No);
        public static readonly Cursor cursorSizeAll = new Cursor(StandardCursorType.SizeAll);
        public static readonly Cursor cursorSizeNS = new Cursor(StandardCursorType.SizeNorthSouth);
        public static readonly Cursor cursorSizeWE = new Cursor(StandardCursorType.SizeWestEast);

        public const int PosMarkerHightlighZIndex = -100;

        public const int ResizeMargin = 8;

        public const int MinTrackCount = 8;
        public const int MinQuarterCount = 256;
        public const int SpareTrackCount = 4;
        public const int SpareQuarterCount = 16;

        public const double TickMinDisplayWidth = 6;
        public const double NoteMinDisplayWidth = 2;

        public const int PartRectangleZIndex = 100;
        public const int PartThumbnailZIndex = 200;
        public const int PartElementZIndex = 200;

        public const int ExpressionHiddenZIndex = 0;
        public const int ExpressionVisibleZIndex = 200;
        public const int ExpressionShadowZIndex = 100;
    }
}
