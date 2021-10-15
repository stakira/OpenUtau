using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OpenUtau.App.Controls;
using ReactiveUI;

namespace OpenUtau.App {
    class ThemeChangedEvent { }

    class ThemeManager {
        public static bool IsDarkMode = false;
        public static IBrush ForegroundBrush = Brushes.Black;
        public static IBrush BackgroundBrush = Brushes.White;
        public static IBrush AccentBrush1 = Brushes.White;
        public static IPen AccentPen1 = new Pen(Brushes.White);
        public static IPen AccentPen1Thickness2 = new Pen(Brushes.White);
        public static IPen AccentPen1Thickness3 = new Pen(Brushes.White);
        public static IBrush AccentBrush1Semi = Brushes.Gray;
        public static IBrush AccentBrush2 = Brushes.Gray;
        public static IPen AccentPen2 = new Pen(Brushes.White);
        public static IPen AccentPen2Thick = new Pen(Brushes.White);
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

        public static void LoadTheme() {
            IResourceDictionary resDict = Application.Current.Resources;
            object? outVar;
            IsDarkMode = false;
            if (resDict.TryGetResource("IsDarkMode", out outVar)) {
                if (outVar is bool b) {
                    IsDarkMode = b;
                }
            }
            if (resDict.TryGetResource("SystemControlForegroundBaseHighBrush", out outVar)) {
                ForegroundBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("SystemControlBackgroundAltHighBrush", out outVar)) {
                BackgroundBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush1", out outVar)) {
                AccentBrush1 = (IBrush)outVar!;
                AccentPen1 = new Pen(AccentBrush1);
                AccentPen1Thickness2 = new Pen(AccentBrush1, 2);
                AccentPen1Thickness3 = new Pen(AccentBrush1, 3);
            }
            if (resDict.TryGetResource("AccentBrush1Semi", out outVar)) {
                AccentBrush1Semi = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush2", out outVar)) {
                AccentBrush2 = (IBrush)outVar!;
                AccentPen2 = new Pen(AccentBrush2, 1);
                AccentPen2Thickness2 = new Pen(AccentBrush2, 2);
                AccentPen2Thickness3 = new Pen(AccentBrush2, 3);
            }
            if (resDict.TryGetResource("AccentBrush2Semi", out outVar)) {
                AccentBrush2Semi = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush3", out outVar)) {
                AccentBrush3 = (IBrush)outVar!;
                AccentPen3 = new Pen(AccentBrush3, 1);
                AccentPen3Thick = new Pen(AccentBrush3, 3);
            }
            if (resDict.TryGetResource("AccentBrush3Semi", out outVar)) {
                AccentBrush3Semi = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("TickLineBrushLow", out outVar)) {
                TickLineBrushLow = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("BarNumberBrush", out outVar)) {
                BarNumberBrush = (IBrush)outVar!;
                BarNumberPen = new Pen(BarNumberBrush, 1);
            }
            if (resDict.TryGetResource("WhiteKeyBrush", out outVar)) {
                WhiteKeyBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("WhiteKeyNameBrush", out outVar)) {
                WhiteKeyNameBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("CenterKeyBrush", out outVar)) {
                CenterKeyBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("CenterKeyNameBrush", out outVar)) {
                CenterKeyNameBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("BlackKeyBrush", out outVar)) {
                BlackKeyBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("BlackKeyNameBrush", out outVar)) {
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

        public static string GetString(string key) {
            IResourceDictionary resDict = Application.Current.Resources;
            if (resDict.TryGetResource(key, out var outVar) && outVar is string s) {
                return s;
            }
            return key;
        }
    }
}
