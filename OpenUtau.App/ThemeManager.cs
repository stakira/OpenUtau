using Avalonia.Controls;
using Avalonia.Media;
using OpenUtau.App.Controls;
using ReactiveUI;

namespace OpenUtau.App {
    class ThemeChangedEvent { }

    class ThemeManager {
        public static IBrush ForegroundBrush = Brushes.Black;
        public static IBrush BackgroundBrush = Brushes.White;
        public static IBrush AccentBrush1 = Brushes.White;
        public static IPen AccentPen1 = new Pen(Brushes.White);
        public static IBrush AccentBrush1Semi = Brushes.Gray;
        public static IBrush AccentBrush2 = Brushes.Gray;
        public static IPen AccentPen2 = new Pen(Brushes.White);
        public static IPen AccentPen2Thick = new Pen(Brushes.White);
        public static IBrush AccentBrush2Semi = Brushes.Gray;
        public static IBrush AccentBrush3 = Brushes.Gray;
        public static IPen AccentPen3 = new Pen(Brushes.White);
        public static IPen AccentPen3Thick = new Pen(Brushes.White);
        public static IBrush AccentBrush3Semi = Brushes.Gray;
        public static IBrush TickLineBrushLow = Brushes.Black;
        public static IBrush BarNumberBrush = Brushes.Black;
        public static IPen BarNumberPen = new Pen(Brushes.White);
        public static IBrush KeyboardWhiteKeyBrush = Brushes.White;
        public static IBrush KeyboardWhiteKeyNameBrush = Brushes.Black;
        public static IBrush KeyboardCenterKeyBrush = Brushes.White;
        public static IBrush KeyboardCenterKeyNameBrush = Brushes.Black;
        public static IBrush KeyboardBlackKeyBrush = Brushes.Black;
        public static IBrush KeyboardBlackKeyNameBrush = Brushes.White;

        public static void LoadTheme(IResourceDictionary resDict) {
            object? outVar;
            if (resDict.TryGetResource("SystemControlForegroundBaseHighBrush", out outVar)) {
                ForegroundBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("SystemControlBackgroundAltHighBrush", out outVar)) {
                BackgroundBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush1", out outVar)) {
                AccentBrush1 = (IBrush)outVar!;
                AccentPen1 = new Pen(AccentBrush1);
            }
            if (resDict.TryGetResource("AccentBrush1Semi", out outVar)) {
                AccentBrush1Semi = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush2", out outVar)) {
                AccentBrush2 = (IBrush)outVar!;
                AccentPen2 = new Pen(AccentBrush2, 1);
                AccentPen2Thick = new Pen(AccentBrush2, 3);
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
            if (resDict.TryGetResource("KeyboardWhiteKeyBrush", out outVar)) {
                KeyboardWhiteKeyBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("KeyboardWhiteKeyNameBrush", out outVar)) {
                KeyboardWhiteKeyNameBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("KeyboardCenterKeyBrush", out outVar)) {
                KeyboardCenterKeyBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("KeyboardCenterKeyNameBrush", out outVar)) {
                KeyboardCenterKeyNameBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("KeyboardBlackKeyBrush", out outVar)) {
                KeyboardBlackKeyBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("KeyboardBlackKeyNameBrush", out outVar)) {
                KeyboardBlackKeyNameBrush = (IBrush)outVar!;
            }
            TextLayoutCache.Clear();
            MessageBus.Current.SendMessage(new ThemeChangedEvent());
        }
    }
}
