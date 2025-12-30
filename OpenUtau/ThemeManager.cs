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
        public static IBrush NeutralAccentBrushSemi2 = Brushes.Gray;
        public static IBrush NeutralAccentBrushSemi3 = Brushes.Gray;
        public static IPen NeutralAccentPen = new Pen(Brushes.Black);
        public static IPen NeutralAccentPenSemi = new Pen(Brushes.Black);
        public static IBrush NoteTextBrush = Brushes.White;
        public static IBrush AccentBrush1 = Brushes.White;
        public static IPen AccentPen1 = new Pen(Brushes.White);
        public static IPen AccentPen1Dark = new Pen(Brushes.White);
        public static IPen AccentPen1Thickness2 = new Pen(Brushes.White);
        public static IPen AccentPen1Thickness3 = new Pen(Brushes.White);
        public static IBrush AccentBrush1Semi = Brushes.Gray;
        public static IBrush AccentBrush1Semi2 = Brushes.Gray;
        public static IBrush AccentBrush1Note = Brushes.White;
        public static IBrush AccentBrush1NoteDark = Brushes.White;
        public static IBrush AccentBrush1NoteSemi = Brushes.Gray;
        public static IBrush AccentBrushLightSemi = Brushes.Gray;
        public static IBrush AccentBrushLight = Brushes.Gray;
        public static IBrush AccentBrush1NoteLightSemi = Brushes.Gray;
        public static IBrush AccentBrush1NoteLightSemi2 = Brushes.Gray;
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
        public static IBrush AccentBrush2Semi2 = Brushes.Gray;
        public static IBrush AccentBrush2Semi3 = Brushes.Gray;
        public static IBrush AccentBrush2Semi4 = Brushes.Gray;

        public static IBrush AccentBrush3 = Brushes.Gray;
        public static IPen AccentPen3 = new Pen(Brushes.White);
        public static IPen AccentPen3Thick = new Pen(Brushes.White);
        public static IPen AccentPen3SemiThick = new Pen(Brushes.White);
        public static IBrush AccentBrush3Semi = Brushes.Gray;
        public static IBrush AccentBrush4 = Brushes.Gray;
        public static IPen NoteBorderPen = new Pen(Brushes.White, 1);
        public static IPen NoteBorderPenPressed = new Pen(Brushes.White, 1);
        public static IBrush TickLineBrushLow = Brushes.Black;
        public static IBrush BarNumberBrush = Brushes.Black;
        public static IPen BarNumberPen = new Pen(Brushes.White);
        public static IBrush FinalPitchBrush = Brushes.Gray;
        public static IPen FinalPitchPen = new Pen(Brushes.Gray);
        public static IBrush RealCurveFillBrush = Brushes.Gray;
        public static IBrush RealCurveStrokeBrush = Brushes.Gray;
        public static IPen RealCurvePen = new Pen(Brushes.Gray, 1D, DashStyle.Dash);
        public static IPen FinalPitchPenThick = new Pen(Brushes.Gray);
        public static IPen PitchPenThickDark = new Pen(Brushes.Gray);
        public static IPen PitchPenThickColored = new Pen(Brushes.Gray);
        public static IPen PitchPenThickLight = new Pen(Brushes.Gray);
        public static IPen PitchPenCenter = new Pen(Brushes.Gray);
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
            new TrackColor("Pink", "#f06292", "#d02564", "#f48fb1", "#f7c5d6", "#FFFFFF"),
            new TrackColor("Red", "#ef5350", "#b73336", "#e67474", "#f2b9b9", "#FFFFFF"),
            new TrackColor("Orange", "#ff8a65", "#dc6849", "#ffac92", "#ffd5c8", "#FFFFFF"),
            new TrackColor("Yellow", "#fbc02d", "#e0952c", "#fed936", "#fff2b7", "#141414"), //
            new TrackColor("Light Green", "#cddc39", "#a5b849", "#dce775", "#f2f7ce", "#141414"), //
            new TrackColor("Green", "#66bb6a", "#3e8e59", "#a6d7a8", "#d2ebd3", "#FFFFFF"),
            new TrackColor("Light Blue", "#4fc3f7", "#318acf", "#81d4fa", "#c0eafd", "#FFFFFF"),
            new TrackColor("Blue", "#4ea6ea", "#1c78d4", "#90caf9", "#c8e5fc", "#FFFFFF"),
            new TrackColor("Purple", "#ba68c8", "#9040a8", "#ce93d8", "#e7c9ec", "#FFFFFF"),
            new TrackColor("Pink2", "#e91e63", "#a91d4f", "#f06292", "#f8b1c9", "#FFFFFF"),
            new TrackColor("Red2", "#d32f2f", "#a21e25", "#ef5350", "#f7a9a8", "#FFFFFF"),
            new TrackColor("Orange2", "#ff5722", "#c6381a", "#FF7043", "#FFB8A1", "#FFFFFF"),
            new TrackColor("Yellow2", "#FF8F00", "#dc6706", "#FFB300", "#FFE097", "#FFFFFF"),
            new TrackColor("Light Green2", "#afb42b", "#8a9120", "#cddc39", "#e6ee9c", "#FFFFFF"),
            new TrackColor("Green2", "#2e7d32", "#195923", "#43a047", "#a1d0a3", "#FFFFFF"),
            new TrackColor("Light Blue2", "#1976d2", "#0d47a1", "#2196F3", "#90CBF9", "#FFFFFF"),
            new TrackColor("Blue2", "#3949AB", "#25348a", "#5C6BC0", "#AEB5E0", "#FFFFFF"),
            new TrackColor("Purple2", "#7B1FA2", "#4e1a80", "#AB47BC", "#D5A3DE", "#FFFFFF"),
            new TrackColor("Rose", "#f06292", "#c34278", "#F8BBD0", "#FCE4EC", "#FFFFFF"),
            new TrackColor("Red3", "#FF5252", "#cf3134", "#FF8A80", "#FFCDD2", "#FFFFFF"),
            new TrackColor("Coral", "#ff8a80", "#e65b5c", "#FFAB91", "#FFCDD2", "#FFFFFF"),
            new TrackColor("Amber", "#ffc107", "#e8950b", "#FFD54F", "#FFE082", "#141414"), //
            new TrackColor("Lime", "#d7e64e", "#b1c431", "#E6EE9C", "#f0f2cf", "#141414"), //
            new TrackColor("Moss", "#adcf61", "#74a74a", "#c5e1a5", "#e6ee9c", "#141414"), //
            new TrackColor("Mint", "#80cbc4", "#42a79d", "#b2dfdb", "#e0f2f1", "#141414"), //
            new TrackColor("Teal", "#26a69a", "#067b76", "#4DB6AC", "#B2DFDB", "#FFFFFF"),
            new TrackColor("Cyan", "#00BCD4", "#017d94", "#4DD0E1", "#B2EBF2", "#FFFFFF"),
            // first dark
            new TrackColor("Dark Rose", "#AD1457", "#820c53", "#C2185B", "#E91E63", "#FFFFFF"),
            new TrackColor("Crimson", "#B71C1C", "#7f0000", "#D32F2F", "#eb6c57", "#FFFFFF"),
            new TrackColor("Deep Coral", "#c63628", "#961330", "#e53935", "#ff8a65", "#FFFFFF"),
            new TrackColor("Dark Amber", "#f3a11c", "#df6817", "#f9c22e", "#ffd34d", "#FFFFFF"),
            new TrackColor("Dark Lime", "#a0af28", "#678427", "#bfcf39", "#d5e258", "#FFFFFF"),
            new TrackColor("Dark Moss", "#689f38", "#498028", "#8BC34A", "#AED581", "#FFFFFF"),
            new TrackColor("Dark Mint", "#0c8161", "#075249", "#2baa80", "#72c6a5", "#FFFFFF"),
            new TrackColor("Dark Teal", "#00695c", "#034340", "#068072", "#4cad97", "#FFFFFF"),
            new TrackColor("Dark Cyan", "#00838f", "#05515d", "#00acc1", "#6fd8e1", "#FFFFFF"),

            new TrackColor("Sky", "#67c8f5", "#479fe3", "#B3E5FC", "#E1F5FE", "#141414"), //
            new TrackColor("Indigo", "#5c6bc0", "#3643a6", "#9fa8da", "#c5cae9", "#FFFFFF"),
            new TrackColor("Deep Purple", "#673ab7", "#452496", "#9575cd", "#d1c4e9", "#FFFFFF"),
            new TrackColor("Plum", "#9575CD", "#6b4bad", "#B39DDB", "#D7CDE8", "#FFFFFF"),
            new TrackColor("Lavender", "#B39DDB", "#775bb0", "#d1bde2", "#e7d7f0", "#FFFFFF"),
            new TrackColor("Brown", "#8D6E63", "#643c3a", "#bcaaa4", "#d7ccc8", "#FFFFFF"),
            new TrackColor("Gray", "#90a4ae", "#5b7785", "#b0bec5", "#cfd8dc", "#FFFFFF"),
            new TrackColor("Steel", "#607D8B", "#374c58", "#90A4AE", "#CFD8DC", "#FFFFFF"),
            new TrackColor("Slate", "#4c84a3", "#2d4c60", "#7aaac0", "#9ecadd", "#FFFFFF"),
            // 2nd dark
            new TrackColor("Dark Sky", "#0288d1", "#0f4995", "#03a9f4", "#81d4fa", "#FFFFFF"),
            new TrackColor("Dark Indigo", "#3e4fb8", "#1f2471", "#6373d4", "#5C6BC0", "#FFFFFF"),
            new TrackColor("Dark Deep Purple", "#563ba8", "#311b92", "#8c64d1", "#9575CD", "#FFFFFF"),
            new TrackColor("Dark Plum", "#5d38b5", "#37207d", "#7E57C2", "#9575CD", "#FFFFFF"),
            new TrackColor("Dark Lavender", "#7c52b9", "#523098", "#8b66c0", "#9d75cd", "#FFFFFF"),
            new TrackColor("Dark Brown", "#5D4037", "#3f2c29", "#8d6e63", "#A1887F", "#FFFFFF"),
            new TrackColor("Dark Gray", "#455a64", "#2b3c46", "#78909c", "#90a4ae", "#FFFFFF"),
            new TrackColor("Dark Steel", "#37474F", "#25313a", "#546e7a", "#78909c", "#FFFFFF"),
            new TrackColor("Dark Slate", "#4b575e", "#373f47", "#627179", "#76858d", "#FFFFFF"),
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
            if (resDict.TryGetResource("NeutralAccentBrushSemi2", themeVariant, out outVar)) {
                NeutralAccentBrushSemi2 = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("NeutralAccentBrushSemi3", themeVariant, out outVar)) {
                NeutralAccentBrushSemi3 = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush1", themeVariant, out outVar)) {
                AccentBrush1 = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush1Semi", themeVariant, out outVar)) {
                AccentBrush1Semi = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush1Semi2", themeVariant, out outVar)) {
                AccentBrush1Semi2 = (IBrush)outVar!;
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
            if (resDict.TryGetResource("AccentBrush2Semi2", themeVariant, out outVar)) {
                AccentBrush2Semi2 = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush2Semi3", themeVariant, out outVar)) {
                AccentBrush2Semi3 = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush2Semi4", themeVariant, out outVar)) {
                AccentBrush2Semi4 = (IBrush)outVar!;
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
            if (resDict.TryGetResource("AccentBrush4", themeVariant, out outVar)) {
                AccentBrush4 = (IBrush)outVar!;
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

                resDict["RealCurveFillBrush"] = tcolor.AccentColorLightSemi2;

                AccentBrush1Note = tcolor.AccentColor;
                AccentBrush1NoteDark = tcolor.AccentColorDark;
                AccentBrush1NoteSemi = tcolor.AccentColorSemi;
                AccentBrushLightSemi = tcolor.AccentColorLightSemi;
                AccentBrushLight = tcolor.AccentColorLight;
                AccentBrush1NoteDarkSemi = tcolor.AccentColorDarkSemi;
                AccentBrush1PartSemi = tcolor.AccentPartColor;
                NoteTextBrush = tcolor.NoteTextColor;
                AccentBrush1NoteLightSemi = tcolor.AccentColorLightSemi;
                AccentBrush1NoteLightSemi2 = tcolor.AccentColorLightSemi2;
                
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
                PitchPenThickColored = new Pen(AccentBrush1Note, 1.8);
                PitchPenThickDark = new Pen(tcolor.AccentColorDark, 1.8);
                PitchPenThickLight = new Pen(AccentBrushLight, 1.8);
                PitchPenCenter = new Pen(tcolor.AccentColorCenterKey, 1.8);
                
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
            TryGetString(key, out string value);
            return value;
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
        public SolidColorBrush AccentColorLightSemi2 { get; set; } // BackGround
        public SolidColorBrush AccentColorCenterKey { get; set; } // Keyboard
        public SolidColorBrush AccentPartColor { get; set; } // Part Color
        public SolidColorBrush NoteTextColor { get; set; } // Note text Color

        public TrackColor(string name, string accentColor, string darkColor, string lightColor, string centerKey, string textKey) {
            Name = name;
            AccentColor = SolidColorBrush.Parse(accentColor);
            AccentColorSemi = SolidColorBrush.Parse(accentColor);
            AccentColorSemi.Opacity = 0.5;
            AccentColorDark = SolidColorBrush.Parse(darkColor);
            AccentColorLight = SolidColorBrush.Parse(lightColor);
            AccentColorLightSemi = SolidColorBrush.Parse(lightColor);
            AccentColorLightSemi.Opacity = 0.5;
            AccentColorLightSemi2 = SolidColorBrush.Parse(lightColor);
            AccentColorLightSemi2.Opacity = 0.3;
            AccentColorDarkSemi = SolidColorBrush.Parse(darkColor);
            AccentColorDarkSemi.Opacity = 0.3;
            AccentColorCenterKey = SolidColorBrush.Parse(centerKey);
            AccentPartColor = SolidColorBrush.Parse(accentColor);
            AccentPartColor.Opacity = 0.8;
            NoteTextColor = SolidColorBrush.Parse(textKey);
        }
    }
}
