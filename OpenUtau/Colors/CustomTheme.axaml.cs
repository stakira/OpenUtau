using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Media;
using OpenUtau.Core;
using OpenUtau.Core.Util;

namespace OpenUtau.Colors;
public class CustomTheme {
    public static Dictionary<string, string> Themes = [];
    public static ThemeYaml Default;

    static CustomTheme() {
        Default = new ThemeYaml();
        ListThemes();
    }

    public static void Load(string themeName) {
        if (!string.IsNullOrEmpty(themeName) && Themes.TryGetValue(themeName, out var themePath) && File.Exists(themePath)) {
            Default = Yaml.DefaultDeserializer.Deserialize<ThemeYaml>(File.ReadAllText(themePath,
                Encoding.UTF8));
        } else {
            Preferences.Default.ThemeName = "Light";
            Default = new ThemeYaml();
        }
    }

    public static void ListThemes() {
        Themes.Clear();
        Directory.CreateDirectory(PathManager.Inst.ThemesPath);
        foreach (var item in Directory.EnumerateFiles(PathManager.Inst.ThemesPath, "*.yaml")) {
            string baseName = Yaml.DefaultDeserializer.Deserialize<ThemeYaml>(File.ReadAllText(item, Encoding.UTF8)).Name;
            string themeName = baseName;
            int dupIter = 1;
            while (Themes.ContainsKey(themeName)) {
                themeName = $"{baseName} ({dupIter})";
                dupIter++;
            }
            Themes.Add(themeName, item);
        }
    }

    public static void ApplyTheme(string themeName) {
        Load(themeName);

        if (Application.Current != null) {
            Application.Current.Resources["IsDarkMode"] = Default.IsDarkMode; 
            Application.Current.Resources["BackgroundColor"] = Color.Parse($"{Default.BackgroundColor}");
            Application.Current.Resources["BackgroundColorPointerOver"] = Color.Parse($"{Default.BackgroundColorPointerOver}");
            Application.Current.Resources["BackgroundColorPressed"] = Color.Parse($"{Default.BackgroundColorPressed}");
            Application.Current.Resources["BackgroundColorDisabled"] = Color.Parse($"{Default.BackgroundColorDisabled}");  
            
            Application.Current.Resources["ForegroundColor"] = Color.Parse($"{Default.ForegroundColor}");
            Application.Current.Resources["ForegroundColorPointerOver"] = Color.Parse($"{Default.ForegroundColorPointerOver}");
            Application.Current.Resources["ForegroundColorPressed"] = Color.Parse($"{Default.ForegroundColorPressed}");
            Application.Current.Resources["ForegroundColorDisabled"] = Color.Parse($"{Default.ForegroundColorDisabled}");
            
            Application.Current.Resources["BorderColor"] = Color.Parse($"{Default.BorderColor}");
            Application.Current.Resources["BorderColorPointerOver"] = Color.Parse($"{Default.BorderColorPointerOver}");
            
            Application.Current.Resources["SystemAccentColor"] = Color.Parse($"{Default.SystemAccentColor}");
            Application.Current.Resources["SystemAccentColorLight1"] = Color.Parse($"{Default.SystemAccentColorLight1}");
            Application.Current.Resources["SystemAccentColorDark1"] = Color.Parse($"{Default.SystemAccentColorDark1}");
            
            Application.Current.Resources["NeutralAccentColor"] = Color.Parse($"{Default.NeutralAccentColor}");
            Application.Current.Resources["NeutralAccentColorPointerOver"] = Color.Parse($"{Default.NeutralAccentColorPointerOver}");
            Application.Current.Resources["AccentColor1"] = Color.Parse($"{Default.AccentColor1}");
            Application.Current.Resources["AccentColor2"] = Color.Parse($"{Default.AccentColor2}");
            Application.Current.Resources["AccentColor3"] = Color.Parse($"{Default.AccentColor3}");
            
            Application.Current.Resources["TickLineColor"] = Color.Parse($"{Default.TickLineColor}");
            Application.Current.Resources["BarNumberColor"] = Color.Parse($"{Default.BarNumberColor}");
            Application.Current.Resources["FinalPitchColor"] = Color.Parse($"{Default.FinalPitchColor}");
            Application.Current.Resources["TrackBackgroundAltColor"] = Color.Parse($"{Default.TrackBackgroundAltColor}");
            
            Application.Current.Resources["WhiteKeyColorLeft"] = Color.Parse($"{Default.WhiteKeyColorLeft}");
            Application.Current.Resources["WhiteKeyColorRight"] = Color.Parse($"{Default.WhiteKeyColorRight}");
            Application.Current.Resources["WhiteKeyNameColor"] = Color.Parse($"{Default.WhiteKeyNameColor}");
            
            Application.Current.Resources["CenterKeyColorLeft"] = Color.Parse($"{Default.CenterKeyColorLeft}");
            Application.Current.Resources["CenterKeyColorRight"] = Color.Parse($"{Default.CenterKeyColorRight}");
            Application.Current.Resources["CenterKeyNameColor"] = Color.Parse($"{Default.CenterKeyNameColor}");
            
            Application.Current.Resources["BlackKeyColorLeft"] = Color.Parse($"{Default.BlackKeyColorLeft}");
            Application.Current.Resources["BlackKeyColorRight"] = Color.Parse($"{Default.BlackKeyColorRight}");
            Application.Current.Resources["BlackKeyNameColor"] = Color.Parse($"{Default.BlackKeyNameColor}");
        }
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

