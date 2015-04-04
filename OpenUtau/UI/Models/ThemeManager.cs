using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OpenUtau.UI.Models
{
    class ThemeManager
    {
        static public string[] noteStrings = new String[12] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        static int[] blackKeys = { 1, 3, 6, 8, 10 };

        // Window UI
        public static SolidColorBrush UIBackgroundBrushNormal = new SolidColorBrush();
        public static SolidColorBrush UIBackgroundBrushActive = new SolidColorBrush();
        public static SolidColorBrush UINeutralBrushNormal = new SolidColorBrush();
        public static SolidColorBrush UINeutralBrushActive = new SolidColorBrush();

        // Midi editor background

        public static SolidColorBrush TrackBackgroundBrush = new SolidColorBrush();
        public static SolidColorBrush TrackBackgroundBrushAlt = new SolidColorBrush();

        public static SolidColorBrush TickLineBrushLight = new SolidColorBrush();
        public static SolidColorBrush TickLineBrushDark = new SolidColorBrush();
        public static SolidColorBrush BarNumberBrush = new SolidColorBrush();

        // Midi editor markers
        public static SolidColorBrush PlayPosMarkerHighlightBrush = new SolidColorBrush();

        // Midi notes
        public static SolidColorBrush NoteFillSelectedBrush = new SolidColorBrush();
        public static SolidColorBrush NoteFillSelectedErrorBrushes = new SolidColorBrush();
        public static SolidColorBrush NoteStrokeSelectedBrush = new SolidColorBrush();
        public static SolidColorBrush NoteStrokeErrorBrush = new SolidColorBrush();

        public static List<SolidColorBrush> NoteFillBrushes = new List<SolidColorBrush>();
        public static List<SolidColorBrush> NoteStrokeBrushes = new List<SolidColorBrush>();
        public static List<SolidColorBrush> NoteFillErrorBrushes = new List<SolidColorBrush>();
        
        public static bool LoadTheme(){

            const int NumberOfChannel = 1;
            
            UIBackgroundBrushNormal.Color = (Color)Application.Current.FindResource("UIBackgroundColorNormal");
            UIBackgroundBrushActive.Color = (Color)Application.Current.FindResource("UIBackgroundColorActive");
            UINeutralBrushNormal.Color = (Color)Application.Current.FindResource("UINeutralColorNormal");
            UINeutralBrushActive.Color = (Color)Application.Current.FindResource("UINeutralColorActive");

            TrackBackgroundBrush.Color = (Color)Application.Current.FindResource("TrackBackgroundColor");
            TrackBackgroundBrushAlt.Color = (Color)Application.Current.FindResource("TrackBackgroundColorAlt");

            TickLineBrushLight.Color = (Color)Application.Current.FindResource("TickLineColorLight");
            TickLineBrushDark.Color = (Color)Application.Current.FindResource("TickLineColorDark");
            BarNumberBrush.Color = (Color)Application.Current.FindResource("BarNumberColor");
            
            PlayPosMarkerHighlightBrush.Color = (Color)Application.Current.FindResource("PlayPosMarkerHighlightColor");

            // Midi notes
            NoteFillSelectedBrush.Color = (Color)Application.Current.FindResource("NoteFillSelectedColorB");
            NoteFillSelectedErrorBrushes.Color = GetColorVariationAlpha(NoteFillSelectedBrush.Color, 127);

            NoteStrokeSelectedBrush.Color = (Color)Application.Current.FindResource("NoteStrokeSelectedColor");
            NoteStrokeErrorBrush.Color = (Color)Application.Current.FindResource("NoteStrokeErrorColor");

            for (int i = 0; i < NumberOfChannel; i++)
            {
                NoteFillBrushes.Add(new SolidColorBrush());
                NoteStrokeBrushes.Add(new SolidColorBrush());
                NoteFillErrorBrushes.Add(new SolidColorBrush());
                
                NoteFillBrushes[0].Color = (Color)Application.Current.FindResource("NoteFillColorBCh" + i);
                NoteFillErrorBrushes[0].Color = GetColorVariationAlpha(NoteFillBrushes[0].Color, 127);
                NoteStrokeBrushes[0].Color = (Color)Application.Current.FindResource("NoteStrokeColorCh" + i);
            }

            return true;
        }

        public static Color GetColorVariationAlpha(Color color, byte alpha)
        {
            return new Color()
            {
                R = color.R,
                G = color.G,
                B = color.B,
                A = alpha
            };
        }

        static public System.Windows.Media.Brush getNoteBackgroundBrush(int noteNo)
        {
            if (blackKeys.Contains(noteNo % 12)) return (LinearGradientBrush)System.Windows.Application.Current.FindResource("BlackKeyBrushNormal");
            else if (noteNo % 12 == 0) return (LinearGradientBrush)System.Windows.Application.Current.FindResource("CenterKeyBrushNormal");
            else return (LinearGradientBrush)System.Windows.Application.Current.FindResource("WhiteKeyBrushNormal");
        }

        static public System.Windows.Style getKeyStyle(int keyNo)
        {
            if (blackKeys.Contains(keyNo % 12)) return (System.Windows.Style)System.Windows.Application.Current.FindResource("BlackKeyStyle");
            else if (keyNo % 12 == 0) return (System.Windows.Style)System.Windows.Application.Current.FindResource("CenterKeyStyle");
            else return (System.Windows.Style)System.Windows.Application.Current.FindResource("WhiteKeyStyle");
        }

        static public System.Windows.Media.Brush getNoteBrush(int noteNo)
        {
            if (blackKeys.Contains(noteNo % 12)) return (SolidColorBrush)System.Windows.Application.Current.FindResource("BlackKeyNoteBrushNormal");
            else if (noteNo % 12 == 0) return (SolidColorBrush)System.Windows.Application.Current.FindResource("CenterKeyNoteBrushNormal");
            else return (SolidColorBrush)System.Windows.Application.Current.FindResource("WhiteKeyNoteBrushNormal");
        }

        static public System.Windows.Media.Brush getNoteTrackBrush(int noteNo)
        {
            if (blackKeys.Contains(noteNo % 12)) return (SolidColorBrush)System.Windows.Application.Current.FindResource("BlackKeyTrackBrushNormal");
            else return (SolidColorBrush)System.Windows.Application.Current.FindResource("WhiteKeyTrackBrushNormal");
        }

        static public System.Windows.Media.Brush getTickLineBrush()
        {
            return (SolidColorBrush)System.Windows.Application.Current.FindResource("ScrollBarBrushNormal");
        }

        static public System.Windows.Media.Brush getBarNumberBrush()
        {
            return (SolidColorBrush)System.Windows.Application.Current.FindResource("ScrollBarBrushActive");
        }
    }
}
