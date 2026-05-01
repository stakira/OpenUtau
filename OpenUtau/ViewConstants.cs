using Avalonia.Input;

namespace OpenUtau.App {
    /// <summary>
    /// รวมค่าคงที่สำหรับการแสดงผลในหน้าต่างหลักและ Piano Roll
    /// เน้นความละเอียดแม่นยำเพื่อให้การวาด UI สมบูรณ์แบบที่สุด
    /// </summary>
    static class ViewConstants {
        #region Timeline & Zoom (Ticks)
        // หน่วยเป็น pixel ต่อ tick (มาตรฐาน 480 ticks ต่อ quarter note)
        public const double TickWidthMax = 256.0 / 480.0;
        public const double TickWidthMin = 4.0 / 480.0;
        public const double TickWidthDefault = 24.0 / 480.0;
        public const double MinTicklineWidth = 12.0;

        public const double PianoRollTickWidthMax = 640.0 / 480.0;
        public const double PianoRollTickWidthMin = 4.0 / 480.0;
        public const double PianoRollTickWidthDefault = 128.0 / 480.0;
        public const double PianoRollTickWidthShowDetails = 64.0 / 480.0;
        public const double PianoRollMinTicklineWidth = 12.0;
        
        public const double TickMinDisplayWidth = 6;
        public const double NoteMinDisplayWidth = 2;
        #endregion

        #region Track Settings
        public const double TrackHeightMax = 144;
        public const double TrackHeightMin = 44;
        public const double TrackHeightDefault = 104;
        public const double TrackHeightDelta = 20;

        public const int MinTrackCount = 8;
        public const int MinQuarterCount = 256;
        public const int SpareTrackCount = 4;
        public const int SpareQuarterCount = 16;
        #endregion

        #region Piano Roll & Notes
        public const double NoteHeightMax = 128;
        public const double NoteHeightMin = 8;
        public const double NoteHeightDefault = 22;

        public const int MaxTone = 12 * 11; // ครอบคลุมช่วงเสียงทั้งหมด
        #endregion

        #region Expressions (Phonemes/Parameters)
        public const double ExpHeightMin = 132;
        public const double ExpHeightMax = 600;
        #endregion

        #region Cursors (UI Interaction)
        public static readonly Cursor cursorCross = new Cursor(StandardCursorType.Cross);
        public static readonly Cursor cursorHand = new Cursor(StandardCursorType.Hand);
        public static readonly Cursor cursorNo = new Cursor(StandardCursorType.No);
        public static readonly Cursor cursorSizeAll = new Cursor(StandardCursorType.SizeAll);
        public static readonly Cursor cursorSizeNS = new Cursor(StandardCursorType.SizeNorthSouth);
        public static readonly Cursor cursorSizeWE = new Cursor(StandardCursorType.SizeWestEast);
        #endregion

        #region Z-Index (Layering)
        // ควบคุมลำดับการวางซ้อนของ Object ใน Canvas
        public const int PosMarkerHightlighZIndex = -100; // อยู่หลังสุด
        public const int PartRectangleZIndex = 100;
        public const int PartThumbnailZIndex = 200;
        public const int PartElementZIndex = 200;

        public const int ExpressionHiddenZIndex = 0;
        public const int ExpressionShadowZIndex = 100;
        public const int ExpressionVisibleZIndex = 200;
        #endregion

        #region Miscellaneous
        public const int ResizeMargin = 8; // ขอบสำหรับการลากเพื่อขยายขนาด
        #endregion
    }
}
