using System;
using System.IO;
using System.Text;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using OpenUtau.Core;

namespace OpenUtau.Colors;
public class CustomTheme {
    private static ThemeYaml Default;
    private static ResourceDictionary? themeDict;
    private static readonly string themeDictId = Guid.NewGuid().ToString();
    public static string Name => Default.Name;
    public static bool IsDarkMode => Default.IsDarkMode;

    static CustomTheme() {
        Load();
        Default ??= new ThemeYaml();
    }

    /// <summary>
    /// Loads the custom theme from <c>theme.yaml</c>.
    /// If the file does not exist, creates a new file with default settings and saves it.
    /// </summary>
    public static void Load() {
        if (File.Exists(PathManager.Inst.ThemeFilePath)) {
            Default = Yaml.DefaultDeserializer.Deserialize<ThemeYaml>(
                File.ReadAllText(PathManager.Inst.ThemeFilePath, Encoding.UTF8));
        } else {
            Default = new ThemeYaml();
            Save(Default);
        }
        UpdateDictOnThread();
    }

    private static void Save(ThemeYaml yaml) {
        PathManager path = new PathManager();
        Directory.CreateDirectory(path.DataPath);
        File.WriteAllText(path.ThemeFilePath, Yaml.DefaultSerializer.Serialize(yaml), Encoding.UTF8);
        UpdateDictOnThread();
    }

    /// <summary>
    /// Returns custom theme settings as <c>IResourceDictionary</c>.
    /// </summary>
    /// <returns><c>IResourceDictionary</c> with the custom theme settings.</returns>
    public static IResourceDictionary ThemeDict() {
        if (themeDict == null) {
            themeDict = new ResourceDictionary {["__CustomThemeId"] = themeDictId};
            UpdateDictOnThread();
        }
        return themeDict;
    }

    private static void UpdateDictOnThread() {
        if (Application.Current != null && !Dispatcher.UIThread.CheckAccess()) {
            Dispatcher.UIThread.Post(UpdateDict);
        } else {
            UpdateDict();
        }
    }

    private static void UpdateDict() {
        if (themeDict == null) return;
        themeDict.Clear();
        var yaml = Default ?? new ThemeYaml();
        var t = typeof(ThemeYaml);
        var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var f in fields) {
            string key = f.Name;
            object? rawValue = f.GetValue(yaml);
            if (rawValue is bool b) {
                themeDict[key] = b;
            } else if (rawValue is string s) {
                try {
                    themeDict[key] = Color.Parse(s);
                } catch {
                    themeDict[key] = s;
                }
            } else {
                themeDict[key] = rawValue ?? string.Empty;
            }
        }
        themeDict["__CustomThemeId"] = themeDictId;
    }

    [Serializable]
    public class ThemeYaml {
        public string Name = "Custom YAML";
            
        public bool IsDarkMode = false;
        public string BackgroundColor = "#FFFFFF";
        public string BackgroundColorPointerOver = "#F0F0F0";
        public string BackgroundColorPressed = "#E0E0E0";
        public string BackgroundColorDisabled = "#D0D0D0";

        public string ForegroundColor = "#000000";
        public string ForegroundColorPointerOver = "#000000";
        public string ForegroundColorPressed = "#202020";
        public string ForegroundColorDisabled = "#808080";
        
        public string BorderColor = "#707070";
        public string BorderColorPointerOver = "#B0B0B0";

        public string SystemAccentColor = "#4EA6EA";
        public string SystemAccentColorLight1 = "#90CAF9";
        public string SystemAccentColorDark1 = "#1E88E5";

        public string NeutralAccentColor = "#ADA1B3";
        public string NeutralAccentColorPointerOver = "#948A99";
        public string AccentColor1 = "#4EA6EA";
        public string AccentColor2 = "#FF679D";
        public string AccentColor3 = "#E62E6E";

        public string TickLineColor = "#AFA3B5";
        public string BarNumberColor = "#AFA3B5";
        public string FinalPitchColor = "#C0C0C0";
        public string TrackBackgroundAltColor = "#F0F0F0";

        public string WhiteKeyColorLeft = "Transparent";
        public string WhiteKeyColorRight = "Transparent";
        public string WhiteKeyNameColor = "#FF347c";
            
        public string CenterKeyColorLeft = "#FFDDE6";
        public string CenterKeyColorRight = "#FFCEDC";
        public string CenterKeyNameColor = "#FF347C";
            
        public string BlackKeyColorLeft = "#FF71A3";
        public string BlackKeyColorRight = "#FF347C";
        public string BlackKeyNameColor = "#FFFFFF";
    }
}

