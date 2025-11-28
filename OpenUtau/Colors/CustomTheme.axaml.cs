using System;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Media;
using OpenUtau.Core;

namespace OpenUtau.Colors;
public class CustomTheme {
    public static ThemeYaml Default;
    
    static CustomTheme() {
        Load();
        if (Default == null) {
            Default = new ThemeYaml();
        }
    }

    public static void Load() {
        if (File.Exists(PathManager.Inst.ThemeFilePath)) {
            Default = Yaml.DefaultDeserializer.Deserialize<ThemeYaml>(File.ReadAllText(PathManager.Inst.ThemeFilePath,
                Encoding.UTF8));
        } else {
            Save();
        }
    }

    public static void Save() {
        PathManager path = new PathManager();
        Default = new ThemeYaml();
            Directory.CreateDirectory(path.DataPath);
            File.WriteAllText(path.ThemeFilePath, Yaml.DefaultSerializer.Serialize(Default), Encoding.UTF8);
    }

    public static void ApplyTheme() {
        Load();
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

