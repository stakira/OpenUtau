using Avalonia.Controls;
using Avalonia.Media;
using OpenUtau.App.Controls;
using ReactiveUI;

namespace OpenUtau.App {
    class ThemeChangedEvent { }

    class ThemeManager {
        public static IBrush? ForegroundBrush;
        public static IBrush? BackgroundBrush;
        public static IBrush? AccentBrush1;
        public static IPen? AccentPen1;
        public static IBrush? AccentBrush1Semi;
        public static IBrush? AccentBrush2;
        public static IPen? AccentPen2;
        public static IPen? AccentPen2Thick;
        public static IBrush? AccentBrush2Semi;
        public static IBrush? AccentBrush3;
        public static IPen? AccentPen3;
        public static IPen? AccentPen3Thick;
        public static IBrush? AccentBrush3Semi;
        public static IBrush? TickLineBrushLow;
        public static IBrush? BarNumberBrush;
        public static IPen? BarNumberPen;

        public static void LoadTheme(IResourceDictionary resDict) {
            object? outVar;
            if (resDict.TryGetResource("SystemControlForegroundBaseHighBrush", out outVar)) {
                ForegroundBrush = (IBrush?)outVar;
            }
            if (resDict.TryGetResource("SystemControlBackgroundAltHighBrush", out outVar)) {
                BackgroundBrush = (IBrush?)outVar;
            }
            if (resDict.TryGetResource("AccentBrush1", out outVar)) {
                AccentBrush1 = (IBrush?)outVar;
                AccentPen1 = new Pen(AccentBrush1);
            }
            if (resDict.TryGetResource("AccentBrush1Semi", out outVar)) {
                AccentBrush1Semi = (IBrush?)outVar;
            }
            if (resDict.TryGetResource("AccentBrush2", out outVar)) {
                AccentBrush2 = (IBrush?)outVar;
                AccentPen2 = new Pen(AccentBrush2, 1);
                AccentPen2Thick = new Pen(AccentBrush2, 3);
            }
            if (resDict.TryGetResource("AccentBrush2Semi", out outVar)) {
                AccentBrush2Semi = (IBrush?)outVar;
            }
            if (resDict.TryGetResource("AccentBrush3", out outVar)) {
                AccentBrush3 = (IBrush?)outVar;
                AccentPen3 = new Pen(AccentBrush3, 1);
                AccentPen3Thick = new Pen(AccentBrush3, 3);
            }
            if (resDict.TryGetResource("AccentBrush3Semi", out outVar)) {
                AccentBrush3Semi = (IBrush?)outVar;
            }
            if (resDict.TryGetResource("TickLineBrushLow", out outVar)) {
                TickLineBrushLow = (IBrush?)outVar;
            }
            if (resDict.TryGetResource("BarNumberBrush", out outVar)) {
                BarNumberBrush = (IBrush?)outVar;
                BarNumberPen = new Pen(BarNumberBrush, 1);
            }
            TextLayoutCache.Clear();
            MessageBus.Current.SendMessage(new ThemeChangedEvent());
        }
    }
}
