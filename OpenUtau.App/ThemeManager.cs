using Avalonia.Controls;
using Avalonia.Media;

namespace OpenUtau.App {
    class ThemeManager {
        public static IBrush? AccentBrush1;
        public static IBrush? AccentBrush2;
        public static IBrush? TickLineBrushLow;
        public static IBrush? BarNumberBrush;

        public static void LoadTheme(IResourceDictionary resDict) {
            object? outVar;
            resDict.TryGetResource("AccentBrush1", out outVar);
            AccentBrush1 = (IBrush?)outVar;
            resDict.TryGetResource("AccentBrush2", out outVar);
            AccentBrush2 = (IBrush?)outVar;
            resDict.TryGetResource("TickLineBrushLow", out outVar);
            TickLineBrushLow = (IBrush?)outVar;
            resDict.TryGetResource("BarNumberBrush", out outVar);
            BarNumberBrush = (IBrush?)outVar;
        }
    }
}
