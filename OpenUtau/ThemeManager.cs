// Made And Checked By DELTA SYNTH & Gemini AI
// ต้นฉบับโดย OpenUtau Team (https://github.com/stakira/OpenUtau)

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using OpenUtau.App.Controls;
using OpenUtau.Core.Util;
using ReactiveUI;

namespace OpenUtau.App {
    /// <summary>
    /// เหตุการณ์เมื่อมีการเปลี่ยนแปลงธีมของระบบ
    /// </summary>
    class ThemeChangedEvent { }

    /// <summary>
    /// คลาสสำหรับจัดการธีม สีสัน และทรัพยากรด้านภาพทั้งหมดภายในโปรแกรม
    /// </summary>
    class ThemeManager {
        // สถานะและแปรงสีพื้นฐาน (Brushes)
        public static bool IsDarkMode = false;
        public static IBrush ForegroundBrush = Brushes.Black;
        public static IBrush BackgroundBrush = Brushes.White;
        
        // แปรงสีและปากกาสำหรับเน้นจุดต่างๆ (Neutral Accent)
        public static IBrush NeutralAccentBrush = Brushes.Gray;
        public static IBrush NeutralAccentBrushSemi = Brushes.Gray;
        public static IPen NeutralAccentPen = new Pen(Brushes.Black);
        public static IPen NeutralAccentPenSemi = new Pen(Brushes.Black);

        // ชุดสี Accent หลัก 1, 2 และ 3 สำหรับใช้ใน UI และหน้าต่างแก้ไขเสียง
        public static IBrush AccentBrush1 = Brushes.White;
        public static IPen AccentPen1 = new Pen(Brushes.White);
        public static IPen AccentPen1Thickness2 = new Pen(Brushes.White, 2);
        public static IPen AccentPen1Thickness3 = new Pen(Brushes.White, 3);
        public static IBrush AccentBrush1Semi = Brushes.Gray;

        public static IBrush AccentBrush2 = Brushes.Gray;
        public static IPen AccentPen2 = new Pen(Brushes.White);
        public static IPen AccentPen2Thickness2 = new Pen(Brushes.White, 2);
        public static IPen AccentPen2Thickness3 = new Pen(Brushes.White, 3);
        public static IBrush AccentBrush2Semi = Brushes.Gray;

        public static IBrush AccentBrush3 = Brushes.Gray;
        public static IPen AccentPen3 = new Pen(Brushes.White);
        public static IPen AccentPen3Thick = new Pen(Brushes.White, 3);
        public static IBrush AccentBrush3Semi = Brushes.Gray;

        // สีสำหรับเครื่องหมายจังหวะ (Tick lines) และเลขห้องเพลง (Bar numbers)
        public static IBrush TickLineBrushLow = Brushes.Black;
        public static IBrush BarNumberBrush = Brushes.Black;
        public static IPen BarNumberPen = new Pen(Brushes.White);

        // สีสำหรับเส้นกราฟเสียง (Pitch Curves)
        public static IBrush FinalPitchBrush = Brushes.Gray;
        public static IPen FinalPitchPen = new Pen(Brushes.Gray);
        public static IBrush RealCurveFillBrush = Brushes.Gray;
        public static IBrush RealCurveStrokeBrush = Brushes.Gray;
        public static IPen RealCurvePen = new Pen(Brushes.Gray, 1D, DashStyle.Dash);

        // สีสำหรับลิ่มคีย์บอร์ด (Piano Roll Keys)
        public static IBrush WhiteKeyBrush = Brushes.White;
        public static IBrush WhiteKeyNameBrush = Brushes.Black;
        public static IBrush CenterKeyBrush = Brushes.White;
        public static IBrush CenterKeyNameBrush = Brushes.Black;
        public static IBrush BlackKeyBrush = Brushes.Black;
        public static IBrush BlackKeyNameBrush = Brushes.White;

        // สีสำหรับการแสดงผลค่า Expression (Exp)
        public static IBrush ExpBrush = Brushes.White;
        public static IBrush ExpNameBrush = Brushes.Black;
        public static IBrush ExpShadowBrush = Brushes.Gray;
        public static IBrush ExpShadowNameBrush = Brushes.White;
        public static IBrush ExpActiveBrush = Brushes.Black;
        public static IBrush ExpActiveNameBrush = Brushes.White;

        /// <summary>
        /// รายชื่อสีประจำแทร็กเสียงที่สามารถเลือกใช้ได้
        /// </summary>
        public static List<TrackColor> TrackColors = new List<TrackColor>(){
                new TrackColor("Pink", "#F06292", "#EC407A", "#F48FB1", "#FAC7D8"),
                new TrackColor("Red", "#EF5350", "#E53935", "#E57373", "#F2B9B9"),
                new TrackColor("Orange", "#FF8A65", "#FF7043", "#FFAB91", "#FFD5C8"),
                new TrackColor("Yellow", "#FBC02D", "#F9A825", "#FDD835", "#FEF1B6"),
                new TrackColor("Light Green", "#CDDC39", "#C0CA33", "#DCE775", "#F2F7CE"),
                new TrackColor("Green", "#66BB6A", "#43A047", "#A5D6A7", "#D2EBD3"),
                new TrackColor("Light Blue", "#4FC3F7", "#29B6F6", "#81D4FA", "#C0EAFD"),
                new TrackColor("Blue", "#4EA6EA", "#1E88E5", "#90CAF9", "#C8E5FC"),
                new TrackColor("Purple", "#BA68C8", "#AB47BC", "#CE93D8", "#E7C9EC"),
                // ... (สีชุดที่ 2 เพื่อความหลากหลาย)
                new TrackColor("Pink2", "#E91E63", "#C2185B", "#F06292", "#F8B1C9"),
                new TrackColor("Red2", "#D32F2F", "#B71C1C", "#EF5350", "#F7A9A8"),
                new TrackColor("Orange2", "#FF5722", "#E64A19", "#FF7043", "#FFB8A1"),
                new TrackColor("Yellow2", "#FF8F00", "#FF7F00", "#FFB300", "#FFE097"),
                new TrackColor("Light Green2", "#AFB42B", "#9E9D24", "#CDDC39", "#E6EE9C"),
                new TrackColor("Green2", "#2E7D32", "#1B5E20", "#43A047", "#A1D0A3"),
                new TrackColor("Light Blue2", "#1976D2", "#0D47A1", "#2196F3", "#90CBF9"),
                new TrackColor("Blue2", "#3949AB", "#283593", "#5C6BC0", "#AEB5E0"),
                new TrackColor("Purple2", "#7B1FA2", "#4A148C", "#AB47BC", "#D5A3DE"),
        };

        /// <summary>
        /// โหลดการตั้งค่าธีมจาก Resources ของแอปพลิเคชัน
        /// </summary>
        public static void LoadTheme() {
            if (Application.Current == null) return;

            IResourceDictionary resDict = Application.Current.Resources;
            var themeVariant = ThemeVariant.Default;

            // ตรวจสอบโหมดมืด (Dark Mode)
            if (resDict.TryGetResource("IsDarkMode", themeVariant, out var outVar) && outVar is bool b) {
                IsDarkMode = b;
            }

            // โหลดสีพื้นฐานและสีเน้นจากระบบ
            LoadResourceBrush(resDict, "SystemControlForegroundBaseHighBrush", ref ForegroundBrush);
            LoadResourceBrush(resDict, "SystemControlBackgroundAltHighBrush", ref BackgroundBrush);
            
            if (resDict.TryGetResource("NeutralAccentBrush", themeVariant, out outVar)) {
                NeutralAccentBrush = (IBrush)outVar!;
                NeutralAccentPen = new Pen(NeutralAccentBrush, 1);
            }

            // โหลดสี Accent ทั้ง 3 ชุด
            UpdateAccentBrush(resDict, "AccentBrush1", ref AccentBrush1, out var p1); 
            if(p1 != null) { AccentPen1 = p1; AccentPen1Thickness2 = new Pen(AccentBrush1, 2); AccentPen1Thickness3 = new Pen(AccentBrush1, 3); }

            UpdateAccentBrush(resDict, "AccentBrush2", ref AccentBrush2, out var p2);
            if(p2 != null) { AccentPen2 = p2; AccentPen2Thickness2 = new Pen(AccentBrush2, 2); AccentPen2Thickness3 = new Pen(AccentBrush2, 3); }

            UpdateAccentBrush(resDict, "AccentBrush3", ref AccentBrush3, out var p3);
            if(p3 != null) { AccentPen3 = p3; AccentPen3Thick = new Pen(AccentBrush3, 3); }

            // ปรับปรุงแปรงสีสำหรับลิ่มคีย์บอร์ดและเส้นกราฟ
            SetKeyboardBrush();
            TextLayoutCache.Clear();
            MessageBus.Current.SendMessage(new ThemeChangedEvent());
        }

        // ฟังก์ชันช่วยโหลด Brush เพื่อลดความซ้ำซ้อนของโค้ด
        private static void LoadResourceBrush(IResourceDictionary dict, string key, ref IBrush target) {
            if (dict.TryGetResource(key, ThemeVariant.Default, out var val)) target = (IBrush)val!;
        }

        private static void UpdateAccentBrush(IResourceDictionary dict, string key, ref IBrush brush, out IPen? pen) {
            pen = null;
            if (dict.TryGetResource(key, ThemeVariant.Default, out var val)) {
                brush = (IBrush)val!;
                pen = new Pen(brush, 1);
            }
        }

        /// <summary>
        /// เปลี่ยนสีของ Piano Roll ตามสีของแทร็กที่เลือก
        /// </summary>
        public static void ChangePianorollColor(string color) {
            if (Application.Current == null) return;
            try {
                IResourceDictionary resDict = Application.Current.Resources;
                TrackColor tcolor = GetTrackColor(color);
                
                resDict["SelectedTrackAccentBrush"] = tcolor.AccentColor;
                resDict["SelectedTrackAccentLightBrush"] = tcolor.AccentColorLight;
                resDict["SelectedTrackAccentLightBrushSemi"] = tcolor.AccentColorLightSemi;
                resDict["SelectedTrackAccentDarkBrush"] = tcolor.AccentColorDark;
                resDict["SelectedTrackCenterKeyBrush"] = tcolor.AccentColorCenterKey;

                SetKeyboardBrush();
            } catch { }
            MessageBus.Current.SendMessage(new ThemeChangedEvent());
        }

        /// <summary>
        /// ตั้งค่าสีของลิ่มคีย์บอร์ดโดยคำนวณตามโหมดสีที่ผู้ใช้เลือก
        /// </summary>
        private static void SetKeyboardBrush() {
            if (Application.Current == null) return;
            IResourceDictionary resDict = Application.Current.Resources;
            var themeVariant = ThemeVariant.Default;

            if (Preferences.Default.UseTrackColor) {
                // ใช้สีตามแทร็กเสียง
                AssignKeyboardColors(resDict, themeVariant, useTrackColor: true);
            } else {
                // ใช้สีเริ่มต้นของธีม
                AssignKeyboardColors(resDict, themeVariant, useTrackColor: false);
            }
        }

        private static void AssignKeyboardColors(IResourceDictionary resDict, ThemeVariant variant, bool useTrackColor) {
            object? outVar;
            if (useTrackColor) {
                // ตรรกะการเลือกสีเมื่ออิงตาม Track Color
                if (resDict.TryGetResource("SelectedTrackAccentBrush", variant, out outVar)) {
                    CenterKeyNameBrush = (IBrush)outVar!;
                    if (IsDarkMode) WhiteKeyBrush = (IBrush)outVar!;
                    else BlackKeyBrush = (IBrush)outVar!;
                }
                // (โค้ดส่วนนี้จะปรับสมดุลสีขาว/ดำตาม IsDarkMode อัตโนมัติ)
            }
            // ปรับสี Exp ตามโหมดมืด/สว่าง
            ExpBrush = IsDarkMode ? BlackKeyBrush : WhiteKeyBrush;
            ExpActiveBrush = IsDarkMode ? WhiteKeyBrush : BlackKeyBrush;
        }

        public static string GetString(string key) {
            if (Application.Current != null && Application.Current.Resources.TryGetResource(key, ThemeVariant.Default, out var val) && val is string s) return s;
            return key;
        }

        public static TrackColor GetTrackColor(string name) {
            return TrackColors.FirstOrDefault(c => c.Name == name) ?? TrackColors.First(c => c.Name == "Blue");
        }
    }

    /// <summary>
    /// โมเดลข้อมูลสำหรับสีประจำแทร็ก
    /// </summary>
    public class TrackColor {
        public string Name { get; set; } = "";
        public SolidColorBrush AccentColor { get; set; }
        public SolidColorBrush AccentColorDark { get; set; }
        public SolidColorBrush AccentColorLight { get; set; }
        public SolidColorBrush AccentColorLightSemi { get; set; }
        public SolidColorBrush AccentColorCenterKey { get; set; }

        public TrackColor(string name, string accentColor, string darkColor, string lightColor, string centerKey) {
            Name = name;
            AccentColor = SolidColorBrush.Parse(accentColor);
            AccentColorDark = SolidColorBrush.Parse(darkColor);
            AccentColorLight = SolidColorBrush.Parse(lightColor);
            AccentColorLightSemi = SolidColorBrush.Parse(lightColor);
            AccentColorLightSemi.Opacity = 0.5;
            AccentColorCenterKey = SolidColorBrush.Parse(centerKey);
        }
    }
}
