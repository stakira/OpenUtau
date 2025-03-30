using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau {
    class StyleManager {
        public static Dictionary<int, string> StyleTypeNames = new Dictionary<int, string>()
        {
            {0, "Default_Styles"},
            {1, "Default_PianoRollStyles"},
            {2, "MacOS_Styles"},
            {3, "MacOS_PianoRollStyles"},
        };

        public static void SetAppStyles(int i) {
            if (Application.Current == null) {
                return; // macOSでない場合やApplicationがnullの場合は処理をスキップ
            }

            if (StyleTypeNames.TryGetValue(i, out var uriKey) &&
                Application.Current.Resources.TryGetValue(uriKey, out var obj) &&
                obj is string styleUrl &&
                !string.IsNullOrEmpty(styleUrl)) {
                Application.Current.Styles.Add(new StyleInclude(new Uri(styleUrl)));
            } else {
                Log.Error($"Style with key '{i}' does not exist or is invalid in Application.Resources.");
            }
        }

        public static void SetStyles(Window window, int i) {
            if (Application.Current == null) {
                return; // macOSでない場合やApplicationがnullの場合は処理をスキップ
            }

            if (StyleTypeNames.TryGetValue(i, out var uriKey) &&
                Application.Current.Resources.TryGetValue(uriKey, out var obj) &&
                obj is string styleUrl &&
                !string.IsNullOrEmpty(styleUrl)) {
                window.Styles.Add(new StyleInclude(new Uri(styleUrl)));
            } else {
                Log.Error($"Style with key '{i}' does not exist or is invalid in Application.Resources.");
            }
        }
    }
}
