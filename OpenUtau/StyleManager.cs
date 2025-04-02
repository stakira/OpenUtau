using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
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
                return;
            }

            if (StyleTypeNames.TryGetValue(i, out var uriKey) &&
                Application.Current.Resources.TryGetValue(uriKey, out var obj) &&
                obj is string styleUrl &&
                !string.IsNullOrEmpty(styleUrl)) {

                Log.Debug($"Loading style: {styleUrl}");

                try {
                    var fullUri = new Uri(styleUrl, UriKind.Absolute);
                    var styleInclude = new StyleInclude(fullUri) { Source = fullUri };
                    Application.Current.Styles.Add(styleInclude);
                    Log.Debug($"Successfully added style: {fullUri}");
                } catch (Exception ex) {
                    Log.Error($"Failed to load style '{styleUrl}': {ex.Message}");
                }

            } else {
                Log.Error($"Style with key '{i}' does not exist or is invalid in Application.Resources.");
            }
        }

        public static void SetStyles(Window window, int i) {
            if (Application.Current == null) {
                return;
            }
            if (StyleTypeNames.TryGetValue(i, out var uriKey) &&
                Application.Current.Resources.TryGetValue(uriKey, out var obj) &&
                obj is string styleUrl &&
                !string.IsNullOrEmpty(styleUrl)) {

                Log.Debug($"Loading style: {styleUrl}");

                try {
                    var fullUri = new Uri(styleUrl, UriKind.Absolute);
                    var styleInclude = new StyleInclude(fullUri) { Source = fullUri };

                    window.Styles.Add(styleInclude);
                    Log.Debug($"Successfully added style: {fullUri}");
                } catch (Exception ex) {
                    Log.Error($"Failed to load style '{styleUrl}': {ex.Message}");
                }

            } else {
                Log.Error($"Style with key '{i}' does not exist or is invalid in Application.Resources.");
            }
        }
    }
}
