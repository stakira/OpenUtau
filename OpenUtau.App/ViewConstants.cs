namespace OpenUtau.App {
    static class ViewConstants {
        public const double TickWidthMax = 256.0 / 480.0;
        public const double TickWidthMin = 4.0 / 480.0;
        public const double TickWidthDefault = 16.0 / 480.0;

        public const double TrackHeightMax = 256;
        public const double TrackHeightMin = 40;
        public const double TrackHeightDefault = 64;

        public const int MaxNoteNum = 12 * 11;
        public const int HiddenNoteNum = 12 * 4;

        public const int PosMarkerHightlighZIndex = -100;

        public const int ResizeMargin = 8;

        public const double NoteMaxHeight = 128;
        public const double NoteMinHeight = 8;
        public const double NoteDefaultHeight = 22;

        public const double MidiQuarterMaxWidth = 512;
        public const double MidiQuarterMinWidth = 4;
        public const double MidiQuarterDefaultWidth = 128;
        public const double MidiQuarterMinWidthShowPhoneme = 64;
        public const double MidiTickMinWidth = 16;

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

        public const double PlayPosMarkerMargin = 0.92;
    }
}
