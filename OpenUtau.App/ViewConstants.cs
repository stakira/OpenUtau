namespace OpenUtau.App {
    static class ViewConstants {
        public const double TickWidthMax = 256.0 / 480.0;
        public const double TickWidthMin = 4.0 / 480.0;
        public const double TickWidthDefault = 16.0 / 480.0;
        public const double MinTicklineWidth = 12.0;

        public const double TrackHeightMax = 256;
        public const double TrackHeightMin = 40;
        public const double TrackHeightDefault = 64;

        public const double PianoRollTickWidthMax = 640.0 / 480.0;
        public const double PianoRollTickWidthMin = 4.0 / 480.0;
        public const double PianoRollTickWidthDefault = 128.0 / 480.0;
        public const double PianoRollTickWidthShowDetails = 64.0 / 480.0;
        public const double PianoRollMinTicklineWidth = 12.0;

        public const double NoteHeightMax = 128;
        public const double NoteHeightMin = 8;
        public const double NoteHeightDefault = 22;

        public const int MaxTone = 12 * 11;

        public const int PosMarkerHightlighZIndex = -100;

        public const int ResizeMargin = 8;

        public const double MidiQuarterMaxWidth = 512;
        public const double MidiQuarterMinWidth = 4;
        public const double MidiQuarterDefaultWidth = 128;

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
