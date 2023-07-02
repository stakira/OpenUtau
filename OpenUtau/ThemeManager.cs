using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using OpenUtau.App.Controls;
using ReactiveUI;

namespace OpenUtau.App {
    class ThemeChangedEvent { }

    class ThemeManager {
        public static bool IsDarkMode = false;
        public static IBrush ForegroundBrush = Brushes.Black;
        public static IBrush BackgroundBrush = Brushes.White;
        public static IBrush NeutralAccentBrush = Brushes.Gray;
        public static IBrush NeutralAccentBrushSemi = Brushes.Gray;
        public static IPen NeutralAccentPen = new Pen(Brushes.Black);
        public static IPen NeutralAccentPenSemi = new Pen(Brushes.Black);
        public static IBrush AccentBrush1 = Brushes.White;
        public static IPen AccentPen1 = new Pen(Brushes.White);
        public static IPen AccentPen1Thickness2 = new Pen(Brushes.White);
        public static IPen AccentPen1Thickness3 = new Pen(Brushes.White);
        public static IBrush AccentBrush1Semi = Brushes.Gray;
        public static IBrush AccentBrush2 = Brushes.Gray;
        public static IPen AccentPen2 = new Pen(Brushes.White);
        public static IPen AccentPen2Thickness2 = new Pen(Brushes.White);
        public static IPen AccentPen2Thickness3 = new Pen(Brushes.White);
        public static IBrush AccentBrush2Semi = Brushes.Gray;
        public static IBrush AccentBrush3 = Brushes.Gray;
        public static IPen AccentPen3 = new Pen(Brushes.White);
        public static IPen AccentPen3Thick = new Pen(Brushes.White);
        public static IBrush AccentBrush3Semi = Brushes.Gray;
        public static IBrush TickLineBrushLow = Brushes.Black;
        public static IBrush BarNumberBrush = Brushes.Black;
        public static IPen BarNumberPen = new Pen(Brushes.White);
        public static IBrush FinalPitchBrush = Brushes.Gray;
        public static IPen FinalPitchPen = new Pen(Brushes.Gray);
        public static IBrush WhiteKeyBrush = Brushes.White;
        public static IBrush WhiteKeyNameBrush = Brushes.Black;
        public static IBrush CenterKeyBrush = Brushes.White;
        public static IBrush CenterKeyNameBrush = Brushes.Black;
        public static IBrush BlackKeyBrush = Brushes.Black;
        public static IBrush BlackKeyNameBrush = Brushes.White;
        public static IBrush ExpBrush = Brushes.White;
        public static IBrush ExpNameBrush = Brushes.Black;
        public static IBrush ExpShadowBrush = Brushes.Gray;
        public static IBrush ExpShadowNameBrush = Brushes.White;
        public static IBrush ExpActiveBrush = Brushes.Black;
        public static IBrush ExpActiveNameBrush = Brushes.White;

        public static List<TrackColor> TrackColors = new List<TrackColor>(){
                new TrackColor("Pink", "#F06292", "#E91E63", "#F48FB1"),
                new TrackColor("Red", "#EF5350", "#E53935", "#E57373"),
                new TrackColor("Orange", "#FF8A65", "#FF5722", "#FFAB91"),
                new TrackColor("Yellow", "#FBC02D", "#F9A825", "#FDD835"),
                new TrackColor("Light Green", "#CDDC39", "#AFB42B", "#DCE775"),
                new TrackColor("Green", "#66BB6A", "#43A047", "#A5D6A7"),
                new TrackColor("Light Blue", "#4FC3F7", "#03A9F4", "#81D4FA"),
                new TrackColor("Blue", "#4EA6EA", "#1E88E5", "#90CAF9"),
                new TrackColor("Purple", "#BA68C8", "#9C27B0", "#CE93D8"),
                new TrackColor("Pink2", "#E91E63", "#C2185B", "#F06292"),
                new TrackColor("Red2", "#D32F2F", "#B71C1C", "#EF5350"),
                new TrackColor("Orange2", "#FF5722", "#E64A19", "#FF8A65"),
                new TrackColor("Yellow2", "#FF8F00", "#FF6F00", "#FFB300"),
                new TrackColor("Light Green2", "#AFB42B", "#827717", "#CDDC39"),
                new TrackColor("Green2", "#2E7D32", "#1B5E20", "#4CAF50"),
                new TrackColor("Light Blue2", "#1976D2", "#0D47A1", "#2196F3"),
                new TrackColor("Blue2", "#3949AB", "#283593", "#5C6BC0"),
                new TrackColor("Purple2", "#7B1FA2", "#4A148C", "#9C27B0"),
            };

        public static void LoadTheme() {
            if (Application.Current == null) {
                return;
            }
            IResourceDictionary resDict = Application.Current.Resources;
            object? outVar;
            IsDarkMode = false;
            var themeVariant = ThemeVariant.Default;
            if (resDict.TryGetResource("IsDarkMode", themeVariant, out outVar)) {
                if (outVar is bool b) {
                    IsDarkMode = b;
                }
            }
            if (resDict.TryGetResource("SystemControlForegroundBaseHighBrush", themeVariant, out outVar)) {
                ForegroundBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("SystemControlBackgroundAltHighBrush", themeVariant, out outVar)) {
                BackgroundBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("NeutralAccentBrush", themeVariant, out outVar)) {
                NeutralAccentBrush = (IBrush)outVar!;
                NeutralAccentPen = new Pen(NeutralAccentBrush, 1);
            }
            if (resDict.TryGetResource("NeutralAccentBrushSemi", themeVariant, out outVar)) {
                NeutralAccentBrushSemi = (IBrush)outVar!;
                NeutralAccentPenSemi = new Pen(NeutralAccentBrushSemi, 1);
            }
            if (resDict.TryGetResource("AccentBrush1", themeVariant, out outVar)) {
                AccentBrush1 = (IBrush)outVar!;
                AccentPen1 = new Pen(AccentBrush1);
                AccentPen1Thickness2 = new Pen(AccentBrush1, 2);
                AccentPen1Thickness3 = new Pen(AccentBrush1, 3);
            }
            if (resDict.TryGetResource("AccentBrush1Semi", themeVariant, out outVar)) {
                AccentBrush1Semi = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush2", themeVariant, out outVar)) {
                AccentBrush2 = (IBrush)outVar!;
                AccentPen2 = new Pen(AccentBrush2, 1);
                AccentPen2Thickness2 = new Pen(AccentBrush2, 2);
                AccentPen2Thickness3 = new Pen(AccentBrush2, 3);
            }
            if (resDict.TryGetResource("AccentBrush2Semi", themeVariant, out outVar)) {
                AccentBrush2Semi = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush3", themeVariant, out outVar)) {
                AccentBrush3 = (IBrush)outVar!;
                AccentPen3 = new Pen(AccentBrush3, 1);
                AccentPen3Thick = new Pen(AccentBrush3, 3);
            }
            if (resDict.TryGetResource("AccentBrush3Semi", themeVariant, out outVar)) {
                AccentBrush3Semi = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("TickLineBrushLow", themeVariant, out outVar)) {
                TickLineBrushLow = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("BarNumberBrush", themeVariant, out outVar)) {
                BarNumberBrush = (IBrush)outVar!;
                BarNumberPen = new Pen(BarNumberBrush, 1);
            }
            if (resDict.TryGetResource("FinalPitchBrush", themeVariant, out outVar)) {
                FinalPitchBrush = (IBrush)outVar!;
                FinalPitchPen = new Pen(FinalPitchBrush, 1);
            }
            if (resDict.TryGetResource("WhiteKeyBrush", themeVariant, out outVar)) {
                WhiteKeyBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("WhiteKeyNameBrush", themeVariant, out outVar)) {
                WhiteKeyNameBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("CenterKeyBrush", themeVariant, out outVar)) {
                CenterKeyBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("CenterKeyNameBrush", themeVariant, out outVar)) {
                CenterKeyNameBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("BlackKeyBrush", themeVariant, out outVar)) {
                BlackKeyBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("BlackKeyNameBrush", themeVariant, out outVar)) {
                BlackKeyNameBrush = (IBrush)outVar!;
            }
            if (!IsDarkMode) {
                ExpBrush = WhiteKeyBrush;
                ExpNameBrush = WhiteKeyNameBrush;
                ExpActiveBrush = BlackKeyBrush;
                ExpActiveNameBrush = BlackKeyNameBrush;
                ExpShadowBrush = CenterKeyBrush;
                ExpShadowNameBrush = CenterKeyNameBrush;
            } else {
                ExpBrush = BlackKeyBrush;
                ExpNameBrush = BlackKeyNameBrush;
                ExpActiveBrush = WhiteKeyBrush;
                ExpActiveNameBrush = WhiteKeyNameBrush;
                ExpShadowBrush = CenterKeyBrush;
                ExpShadowNameBrush = CenterKeyNameBrush;
            }
            TextLayoutCache.Clear();
            MessageBus.Current.SendMessage(new ThemeChangedEvent());
        }

        public static void ChangeTrackColor(string color) {
            if (Application.Current == null) {
                return;
            }
            try {
                IResourceDictionary resDict = Application.Current.Resources;
                TrackColor tcolor = GetTrackColor(color);
                resDict["SelectedTrackAccentBrush"] = tcolor.AccentColor;
                resDict["SelectedTrackAccentLightBrush"] = tcolor.AccentColorLight;
                resDict["SelectedTrackAccentLightBrushSemi"] = tcolor.AccentColorLightSemi;
                resDict["SelectedTrackAccentDarkBrush"] = tcolor.AccentColorDark;
            } catch { }
        }

        public static string GetString(string key) {
            if (Application.Current == null) {
                return key;
            }
            IResourceDictionary resDict = Application.Current.Resources;
            if (resDict.TryGetResource(key, ThemeVariant.Default, out var outVar) && outVar is string s) {
                return s;
            }
            return key;
        }

        public static TrackColor GetTrackColor(string name) {
            if (TrackColors.Any(c => c.Name == name)) {
                return TrackColors.First(c => c.Name == name);
            }
            return TrackColors.First(c => c.Name == "Blue");
        }
    }

    public class TrackColor {
        public string Name { get; set; } = "";
        public SolidColorBrush AccentColor { get; set; }
        public SolidColorBrush AccentColorDark { get; set; } // Pressed
        public SolidColorBrush AccentColorLight { get; set; } // PointerOver
        public SolidColorBrush AccentColorLightSemi { get; set; } // BackGround

        public TrackColor(string name, string accentColor, string darkColor, string lightColor) {
            Name = name;
            AccentColor = SolidColorBrush.Parse(accentColor);
            AccentColorDark = SolidColorBrush.Parse(darkColor);
            AccentColorLight = SolidColorBrush.Parse(lightColor);
            AccentColorLightSemi = SolidColorBrush.Parse(lightColor);
            AccentColorLightSemi.Opacity = 0.5;
        }
    }
}
