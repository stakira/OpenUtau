using Avalonia;
using Avalonia.Media;

namespace OpenUtau.Colors;
public class CustomTheme {
    public static bool IsDarkMode = false;
    public static void ApplyTheme() {
        if (Application.Current != null) {
            Application.Current.Resources["IsDarkMode"] = IsDarkMode; 
            Application.Current.Resources["BackgroundColor"] = Color.Parse("#259cc6");
            Application.Current.Resources["BackgroundColorPointerOver"] = Color.Parse("#259cc6");
            Application.Current.Resources["BackgroundColorPressed"] = Color.Parse("#259cc6");
            Application.Current.Resources["BackgroundColorDisabled"] = Color.Parse("#259cc6");  
            
            Application.Current.Resources["ForegroundColor"] = Color.Parse("#259cc6");
            Application.Current.Resources["ForegroundColorPointerOver"] = Color.Parse("#259cc6");
            Application.Current.Resources["ForegroundColorPressed"] = Color.Parse("#259cc6");
            Application.Current.Resources["ForegroundColorDisabled"] = Color.Parse("#259cc6");
            
            Application.Current.Resources["BorderColor"] = Color.Parse("#259cc6");
            Application.Current.Resources["BorderColorPointerOver"] = Color.Parse("#259cc6");
            
            Application.Current.Resources["SystemAccentColor"] = Color.Parse("#259cc6");
            Application.Current.Resources["SystemAccentColorLight1"] = Color.Parse("#259cc6");
            Application.Current.Resources["SystemAccentColorDark1"] = Color.Parse("#259cc6");
            
            Application.Current.Resources["NeutralAccentColor"] = Color.Parse("#259cc6");
            Application.Current.Resources["NeutralAccentColorPointerOver"] = Color.Parse("#259cc6");
            Application.Current.Resources["AccentColor1"] = Color.Parse("#259cc6");
            Application.Current.Resources["AccentColor2"] = Color.Parse("#259cc6");
            Application.Current.Resources["AccentColor3"] = Color.Parse("#259cc6");
            
            Application.Current.Resources["TickLineColor"] = Color.Parse("#259cc6");
            Application.Current.Resources["BarNumberColor"] = Color.Parse("#259cc6");
            Application.Current.Resources["FinalPitchColor"] = Color.Parse("#259cc6");
            Application.Current.Resources["TrackBackgroundAltColor"] = Color.Parse("#259cc6");
            
            Application.Current.Resources["WhiteKeyColorLeft"] = Color.Parse("#259cc6");
            Application.Current.Resources["WhiteKeyColorRight"] = Color.Parse("#259cc6");
            Application.Current.Resources["WhiteKeyNameColor"] = Color.Parse("#259cc6");
            
            Application.Current.Resources["CenterKeyColorLeft"] = Color.Parse("#259cc6");
            Application.Current.Resources["CenterKeyColorRight"] = Color.Parse("#259cc6");
            Application.Current.Resources["CenterKeyNameColor"] = Color.Parse("#259cc6");
            
            Application.Current.Resources["BlackKeyColorLeft"] = Color.Parse("#259cc6");
            Application.Current.Resources["BlackKeyColorRight"] = Color.Parse("#259cc6");
            Application.Current.Resources["BlackKeyNameColor"] = Color.Parse("#259cc6");
        }
    }
}

