using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using OpenUtau.App.Controls;
using OpenUtau.Core.Util;
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
        public static IPen AccentPen1Dark = new Pen(Brushes.White);
        public static IPen AccentPen1Thickness2 = new Pen(Brushes.White);
        public static IPen AccentPen1Thickness3 = new Pen(Brushes.White);
        public static IBrush AccentBrush1Semi = Brushes.Gray;
        public static IBrush AccentBrush1Note = Brushes.White;
        public static IBrush AccentBrush1NoteDark = Brushes.White;
        public static IBrush AccentBrush1NoteSemi = Brushes.Gray;
        public static IBrush AccentBrushLightSemi = Brushes.Gray;
        public static IBrush AccentBrushLight = Brushes.Gray;
        public static IBrush AccentBrush1NoteDarkSemi = Brushes.Gray;
        public static IBrush AccentBrush1PartSemi = Brushes.White;
        public static IBrush AccentBrush2 = Brushes.Gray;
        public static IPen AccentPen2 = new Pen(Brushes.White);
        public static IPen AccentPen2Dark = new Pen(Brushes.White);
        public static IPen AccentPen2Thickness2 = new Pen(Brushes.White);
        public static IPen AccentPen2Thickness3 = new Pen(Brushes.White);
        public static IPen AccentPen2Light = new Pen(Brushes.White);
        public static IPen AccentPen2Thickness2Light = new Pen(Brushes.White);
        public static IPen AccentPen2Thickness3Light = new Pen(Brushes.White);
        public static IBrush AccentBrush2Semi = Brushes.Gray;
        public static IBrush AccentBrush3 = Brushes.Gray;
        public static IPen AccentPen3 = new Pen(Brushes.White);
        public static IPen AccentPen3Thick = new Pen(Brushes.White);
        public static IPen AccentPen3SemiThick = new Pen(Brushes.White);
        public static IBrush AccentBrush3Semi = Brushes.Gray;
        public static IPen NoteBorderPen = new Pen(Brushes.White, 1);
        public static IPen NoteBorderPenPressed = new Pen(Brushes.White, 1);
        public static IBrush TickLineBrushLow = Brushes.Black;
        public static IBrush BarNumberBrush = Brushes.Black;
        public static IPen BarNumberPen = new Pen(Brushes.White);
        public static IBrush FinalPitchBrush = Brushes.Gray;
        public static IPen FinalPitchPen = new Pen(Brushes.Gray);
        public static IPen FinalPitchPenThick = new Pen(Brushes.Gray);
        public static IPen FinalPitchPenTransparent = new Pen(Brushes.White, 1);
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
                new TrackColor("Pink", "#F06292", "#EC407A", "#F48FB1", "#FAC7D8"),
                new TrackColor("Red", "#EF5350", "#E53935", "#E57373", "#F2B9B9"),
                new TrackColor("Orange", "#FF8A65", "#FF7043", "#FFAB91", "#FFD5C8"),
                new TrackColor("Yellow", "#FBC02D", "#F9A825", "#FDD835", "#FEF1B6"),
                new TrackColor("Light Green", "#CDDC39", "#C0CA33", "#DCE775", "#F2F7CE"),
                new TrackColor("Green", "#66BB6A", "#43A047", "#A5D6A7", "#D2EBD3"),
                new TrackColor("Light Blue", "#4FC3F7", "#29B6F6", "#81D4FA", "#C0EAFD"),
                new TrackColor("Blue", "#4EA6EA", "#1E88E5", "#90CAF9", "#C8E5FC"),
                new TrackColor("Purple", "#BA68C8", "#AB47BC", "#CE93D8", "#E7C9EC"),
                new TrackColor("Pink2", "#E91E63", "#C2185B", "#F06292", "#F8B1C9"),
                new TrackColor("Red2", "#D32F2F", "#B71C1C", "#EF5350", "#F7A9A8"),
                new TrackColor("Orange2", "#FF5722", "#E64A19", "#FF7043", "#FFB8A1"),
                new TrackColor("Yellow2", "#FF8F00", "#FF7F00", "#FFB300", "#FFE097"),
                new TrackColor("Light Green2", "#AFB42B", "#9E9D24", "#CDDC39", "#E6EE9C"),
                new TrackColor("Green2", "#2E7D32", "#1B5E20", "#43A047", "#A1D0A3"),
                new TrackColor("Light Blue2", "#1976D2", "#0D47A1", "#2196F3", "#90CBF9"),
                new TrackColor("Blue2", "#3949AB", "#283593", "#5C6BC0", "#AEB5E0"),
                new TrackColor("Purple2", "#7B1FA2", "#4A148C", "#AB47BC", "#D5A3DE"),
                // New 18 colors below + dark counterparts:
                new TrackColor("Rose", "#DB5C8B", "#F06292", "#F8BBD0", "#FCE4EC"),
                new TrackColor("Red3", "#FF5252", "#E53935", "#FF8A80", "#FFCDD2"),
                new TrackColor("Coral", "#FF8A80", "#FF5252", "#FFAB91", "#FFCDD2"),
                new TrackColor("Amber", "#FFC107", "#FFA000", "#FFD54F", "#FFE082"),
                new TrackColor("Lime", "#D4E157", "#C0CA33", "#E6EE9C", "#F0F4C3"),
                new TrackColor("Moss", "#9CCC65", "#7CB342", "#C5E1A5", "#E6EE9C"),
                new TrackColor("Mint", "#80CBC4", "#4DB6AC", "#B2DFDB", "#E0F2F1"),
                new TrackColor("Teal", "#26A69A", "#00897B", "#4DB6AC", "#B2DFDB"),
                new TrackColor("Cyan", "#00BCD4", "#0097A7", "#4DD0E1", "#B2EBF2"),
                // first dark
                new TrackColor("Dark Rose", "#AD1457", "#880E4F", "#C2185B", "#E91E63"),
                new TrackColor("Crimson", "#B71C1C", "#7F0000", "#D32F2F", "#EF5350"),
                new TrackColor("Deep Coral", "#C62828", "#AD1457", "#E53935", "#FF8A65"),
                new TrackColor("Dark Amber", "#FF8F00", "#EF6C00", "#FFB300", "#FFCA28"),
                new TrackColor("Dark Lime", "#AFB42B", "#9E9D24", "#C0CA33", "#D4E157"),
                new TrackColor("Dark Moss", "#689F38", "#558B2F", "#8BC34A", "#AED581"),
                new TrackColor("Dark Mint", "#00796B", "#004D40", "#26A69A", "#80CBC4"),
                new TrackColor("Dark Teal", "#00695C", "#004D40", "#00796B", "#26A69A"),
                new TrackColor("Dark Cyan", "#00838F", "#006064", "#00ACC1", "#4DD0E1"),

                new TrackColor("Sky", "#81D4FA", "#29B6F6", "#B3E5FC", "#E1F5FE"),
                new TrackColor("Indigo", "#5C6BC0", "#3F51B5", "#9FA8DA", "#C5CAE9"),
                new TrackColor("Deep Purple", "#673AB7", "#512DA8", "#9575CD", "#D1C4E9"),
                new TrackColor("Plum", "#9575CD", "#7E57C2", "#B39DDB", "#D7CDE8"),
                new TrackColor("Lavender", "#B39DDB", "#9575CD", "#D1C4E9", "#F3E5F5"),
                new TrackColor("Brown", "#8D6E63", "#6D4C41", "#BCAAA4", "#D7CCC8"),
                new TrackColor("Gray", "#90A4AE", "#607D8B", "#B0BEC5", "#CFD8DC"),
                new TrackColor("Steel", "#607D8B", "#455A64", "#90A4AE", "#CFD8DC"),
                new TrackColor("Slate", "#78909C", "#546E7A", "#B0BEC5", "#ECEFF1"),
                // 2nds dark
                new TrackColor("Dark Sky", "#0288D1", "#0277BD", "#03A9F4", "#81D4FA"),
                new TrackColor("Dark Indigo", "#303F9F", "#1A237E", "#3949AB", "#5C6BC0"),
                new TrackColor("Dark Deep Purple", "#4527A0", "#311B92", "#673AB7", "#9575CD"),
                new TrackColor("Dark Plum", "#512DA8", "#4527A0", "#7E57C2", "#9575CD"),
                new TrackColor("Dark Lavender", "#7E57C2", "#673AB7", "#9575CD", "#B39DDB"),
                new TrackColor("Dark Brown", "#5D4037", "#3E2723", "#8D6E63", "#A1887F"),
                new TrackColor("Dark Gray", "#455A64", "#263238", "#78909C", "#90A4AE"),
                new TrackColor("Dark Steel", "#37474F", "#263238", "#546E7A", "#78909C"),
                new TrackColor("Dark Slate", "#455A64", "#37474F", "#607D8B", "#90A4AE"),
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
                
            }
            if (resDict.TryGetResource("AccentBrush1Semi", themeVariant, out outVar)) {
                AccentBrush1Semi = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush1Note", themeVariant, out outVar)) {
                AccentBrush1Note = (IBrush)outVar!;
                AccentPen1 = new Pen(AccentBrush1Note);
                AccentPen1Thickness2 = new Pen(AccentBrush1Note, 2);
                AccentPen1Thickness3 = new Pen(AccentBrush1Note, 3);
            }
            if (resDict.TryGetResource("AccentBrush1NoteSemi", themeVariant, out outVar)) {
                AccentBrush1NoteSemi = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush1NoteDark", themeVariant, out outVar)) {
                AccentBrush1NoteDark = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush1NoteDarkSemi", themeVariant, out outVar)) {
                AccentBrush1NoteDarkSemi = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush1PartSemi", themeVariant, out outVar)) {
                AccentBrush1PartSemi = (IBrush)outVar!;
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
                AccentPen3SemiThick = new Pen(AccentBrush3, 2);
            }
            if (resDict.TryGetResource("AccentBrush3Semi", themeVariant, out outVar)) {
                AccentBrush3Semi = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("NoteBorderBrush", themeVariant, out outVar)) {
                NoteBorderPen = new Pen((IBrush)outVar!, 1);
            }
            if (resDict.TryGetResource("NoteBorderBrushPressed", themeVariant, out outVar)) {
                NoteBorderPenPressed = new Pen((IBrush)outVar!, 1);
            }
            if (resDict.TryGetResource("FinalPitchBrushTransparent", themeVariant, out outVar)) {
                FinalPitchPenTransparent = new Pen((IBrush)outVar!, 1);
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
                FinalPitchPenThick = new Pen(FinalPitchBrush, 1.5);
                
            }
            
            SetKeyboardBrush();
            TextLayoutCache.Clear();
            MessageBus.Current.SendMessage(new ThemeChangedEvent());
        }

        public static void ChangePianorollColor(string color) {
            if (Application.Current == null) {
                return;
            }
            try {
                IResourceDictionary resDict = Application.Current.Resources;
                TrackColor tcolor = GetTrackColor(color);
                resDict["SelectedTrackAccentBrush"] = tcolor.AccentColor;
                resDict["SelectedTrackAccentLightBrush"] = tcolor.AccentColorLight;
                resDict["SelectedTrackAccentLightBrushSemi"] = tcolor.AccentColorLightSemi;
                resDict["SelectedTrackAccentDarkBrushSemi"] = tcolor.AccentColorDarkSemi;
                resDict["SelectedTrackAccentDarkBrush"] = tcolor.AccentColorDark;
                resDict["SelectedTrackCenterKeyBrush"] = tcolor.AccentColorCenterKey;

                AccentBrush1Note = tcolor.AccentColor;
                AccentBrush1NoteDark = tcolor.AccentColorDark;
                AccentBrush1NoteSemi = tcolor.AccentColorSemi;
                AccentBrushLightSemi = tcolor.AccentColorLightSemi;
                AccentBrushLight = tcolor.AccentColorLight;
                AccentBrush1NoteDarkSemi = tcolor.AccentColorDarkSemi;
                AccentBrush1PartSemi = tcolor.AccentPartColor;
                // pen
                AccentPen1 = new Pen(AccentBrush1Note, 1);
                AccentPen1Dark = new Pen(tcolor.AccentColorDark, 1);
                AccentPen1Thickness2 = new Pen(AccentBrush1Note, 2);
                AccentPen1Thickness3 = new Pen(AccentBrush1Note, 3);
                AccentPen2 = new Pen(AccentBrush2, 1);
                AccentPen2Dark = new Pen(AccentBrush2, 1);
                AccentPen2Thickness2 = new Pen(AccentBrush2, 2);
                AccentPen2Thickness3 = new Pen(AccentBrush2, 3);
                AccentPen2Light = new Pen(AccentBrushLight, 1);
                AccentPen2Thickness2Light = new Pen(AccentBrushLight, 2);
                AccentPen2Thickness3Light = new Pen(AccentBrushLight, 3);
                NoteBorderPen = new Pen(tcolor.AccentColorDark, 2);
                SetKeyboardBrush();
            } catch {
            }
            MessageBus.Current.SendMessage(new ThemeChangedEvent());
        }

        private static void SetKeyboardBrush() {
            if (Application.Current == null) {
                return;
            }
            IResourceDictionary resDict = Application.Current.Resources;
            object? outVar;
            var themeVariant = ThemeVariant.Default;

            if (Preferences.Default.UseTrackColor) {
                if (IsDarkMode) {
                    if (resDict.TryGetResource("SelectedTrackAccentBrush", themeVariant, out outVar)) {
                        CenterKeyNameBrush = (IBrush)outVar!;
                        WhiteKeyBrush = (IBrush)outVar!;
                    }
                    if (resDict.TryGetResource("SelectedTrackCenterKeyBrush", themeVariant, out outVar)) {
                        CenterKeyBrush = (IBrush)outVar!;
                    }
                    if (resDict.TryGetResource("WhiteKeyNameBrush", themeVariant, out outVar)) {
                        WhiteKeyNameBrush = (IBrush)outVar!;
                    }
                    if (resDict.TryGetResource("BlackKeyBrush", themeVariant, out outVar)) {
                        BlackKeyBrush = (IBrush)outVar!;
                    }
                    if (resDict.TryGetResource("BlackKeyNameBrush", themeVariant, out outVar)) {
                        BlackKeyNameBrush = (IBrush)outVar!;
                    }
                    ExpBrush = BlackKeyBrush;
                    ExpNameBrush = BlackKeyNameBrush;
                    ExpActiveBrush = WhiteKeyBrush;
                    ExpActiveNameBrush = WhiteKeyNameBrush;
                    ExpShadowBrush = CenterKeyBrush;
                    ExpShadowNameBrush = CenterKeyNameBrush;
                } else { // LightMode
                    if (resDict.TryGetResource("SelectedTrackAccentBrush", themeVariant, out outVar)) {
                        CenterKeyNameBrush = (IBrush)outVar!;
                        WhiteKeyNameBrush = (IBrush)outVar!;
                        BlackKeyBrush = (IBrush)outVar!;
                    }
                    if (resDict.TryGetResource("SelectedTrackCenterKeyBrush", themeVariant, out outVar)) {
                        CenterKeyBrush = (IBrush)outVar!;
                    }
                    if (resDict.TryGetResource("WhiteKeyBrush", themeVariant, out outVar)) {
                        WhiteKeyBrush = (IBrush)outVar!;
                    }
                    if (resDict.TryGetResource("BlackKeyNameBrush", themeVariant, out outVar)) {
                        BlackKeyNameBrush = (IBrush)outVar!;
                    }
                    ExpBrush = WhiteKeyBrush;
                    ExpNameBrush = WhiteKeyNameBrush;
                    ExpActiveBrush = BlackKeyBrush;
                    ExpActiveNameBrush = BlackKeyNameBrush;
                    ExpShadowBrush = CenterKeyBrush;
                    ExpShadowNameBrush = CenterKeyNameBrush;
                }
            } else { // DefColor
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
            }
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

        public static bool TryGetString(string key, out string value) {
            if (Application.Current == null) {
                value = key;
                return false;
            }
            IResourceDictionary resDict = Application.Current.Resources;
            if (resDict.TryGetResource(key, ThemeVariant.Default, out var outVar) && outVar is string s) {
                value = s;
                return true;
            }
            value = key;
            return false;
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
        public SolidColorBrush AccentColorSemi { get; set; }
        public SolidColorBrush AccentColorDark { get; set; } // Pressed
        public SolidColorBrush AccentColorDarkSemi { get; set; }
        public SolidColorBrush AccentColorLight { get; set; } // PointerOver
        public SolidColorBrush AccentColorLightSemi { get; set; } // BackGround
        public SolidColorBrush AccentColorCenterKey { get; set; } // Keyboard
        public SolidColorBrush AccentPartColor { get; set; } // Part Color


        public TrackColor(string name, string accentColor, string darkColor, string lightColor, string centerKey) {
            Name = name;
            AccentColor = SolidColorBrush.Parse(accentColor);
            AccentColorSemi = SolidColorBrush.Parse(accentColor);
            AccentColorSemi.Opacity = 0.5;
            AccentColorDark = SolidColorBrush.Parse(darkColor);
            AccentColorLight = SolidColorBrush.Parse(lightColor);
            AccentColorLightSemi = SolidColorBrush.Parse(lightColor);
            AccentColorLightSemi.Opacity = 0.5;
            AccentColorDarkSemi = SolidColorBrush.Parse(darkColor);
            AccentColorDarkSemi.Opacity = 0.3;
            AccentColorCenterKey = SolidColorBrush.Parse(centerKey);
            AccentPartColor = SolidColorBrush.Parse(accentColor);
            AccentPartColor.Opacity = 0.8;
        }
    }
}
