using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Markup.Xaml.Converters;
using OpenUtau.Core;
using OpenUtau.App.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using YamlDotNet.Serialization;
using Org.BouncyCastle.Crypto.Operators;

namespace OpenUtau.App {
    class ThemeChangedEvent { }

    public class Theme {
        public string Name { get; set; }
        public static bool Dark { get; set; }
        public string ForgroundBrush { get; set; }
        public string BackgroundBrush { get; set; }
        public string NeutralAccentBrush { get; set; }
        public string NeutralAccentBrushSemi { get; set; }
        public string AccentBrush1 { get; set; }
        public string AccentBrush1Semi { get; set; }
        public string AccentBrush2 { get; set; }
        public string AccentBrush2Semi { get; set; }
        public string AccentBrush3 { get; set; }
        public string AccentBrush3Semi { get; set; }
        public string TickLineBrushLow { get; set; }
        public string BarNumberBrush { get; set; }
        public string BarNumberPen { get; set; }
        public string FinalPitchBrush { get; set; }
        public string WhiteKeyBrush { get; set; }
        public string WhiteKeyNameBrush { get; set; }
        public string CenterKeyBrush { get; set; }
        public string CenterKeyNameBrush { get; set; }
        public string BlackKeyBrush { get; set; }
        public string BlackKeyNameBrush { get; set; }
        public string ExpBrush { get; set; }
        public string ExpNameBrush { get; set; }
        public string ExpShadowBrush { get; set; }
        public string ExpShadowNameBrush { get; set; }
        public string ExpActiveBrush { get; set; }
        public string ExpActiveNameBrush { get; set; }

    } 

    class ThemeManager {
        public static bool IsDarkMode = false;
        public static IBrush ForegroundBrush = Brushes.Black;
        public static IBrush BackgroundBrush = Brushes.White;
        public static IBrush NeutralAccentBrush = Brushes.Gray;
        public static IBrush NeutralAccentBrushSemi = Brushes.Gray;
        public static IPen NeutralAccentPen = new Pen(Brushes.Black);
        public static IPen NeutralAccentPenSemi = new Pen(Brushes.Black);
        public static IBrush AccentBrush1 = Brushes.White;
        public static IPen AccentPen1 = new Pen(Brushes.White);
        public static IPen AccentPen1Thickness2 = new Pen(Brushes.White);
        public static IPen AccentPen1Thickness3 = new Pen(Brushes.White);
        public static IBrush AccentBrush1Semi = Brushes.Gray;
        public static IBrush AccentBrush2 = Brushes.Gray;
        public static IPen AccentPen2 = new Pen(Brushes.White);
        public static IPen AccentPen2Thickness2 = new Pen(Brushes.White);
        public static IPen AccentPen2Thickness3 = new Pen(Brushes.White);
        public static IBrush AccentBrush2Semi = Brushes.Gray;
        public static IBrush AccentBrush3 = Brushes.Gray;
        public static IPen AccentPen3 = new Pen(Brushes.White);
        public static IPen AccentPen3Thick = new Pen(Brushes.White);
        public static IBrush AccentBrush3Semi = Brushes.Gray;
        public static IBrush TickLineBrushLow = Brushes.Black;
        public static IBrush BarNumberBrush = Brushes.Black;
        public static IPen BarNumberPen = new Pen(Brushes.White);
        public static IBrush FinalPitchBrush = Brushes.Gray;
        public static IPen FinalPitchPen = new Pen(Brushes.Gray);
        public static IBrush WhiteKeyBrush = Brushes.White;
        public static IBrush WhiteKeyNameBrush = Brushes.Black;
        public static IBrush CenterKeyBrush = Brushes.White;
        public static IBrush CenterKeyNameBrush = Brushes.Black;
        public static IBrush BlackKeyBrush = Brushes.Black;
        public static IBrush BlackKeyNameBrush = Brushes.White;
        public static IBrush ExpBrush = Brushes.White;
        public static IBrush ExpNameBrush = Brushes.Black;
        public static IBrush ExpShadowBrush = Brushes.Gray;
        public static IBrush ExpShadowNameBrush = Brushes.White;
        public static IBrush ExpActiveBrush = Brushes.Black;
        public static IBrush ExpActiveNameBrush = Brushes.White;
        public static void LoadTheme() {
            IResourceDictionary resDict = Application.Current.Resources;
            object? outVar;
            IsDarkMode = false;
            if (resDict.TryGetResource("IsDarkMode", out outVar)) {
                if (outVar is bool b) {
                    IsDarkMode = b;
                }
            }
            if (resDict.TryGetResource("SystemControlForegroundBaseHighBrush", out outVar)) {
                ForegroundBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("SystemControlBackgroundAltHighBrush", out outVar)) {
                BackgroundBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("NeutralAccentBrush", out outVar)) {
                NeutralAccentBrush = (IBrush)outVar!;
                NeutralAccentPen = new Pen(NeutralAccentBrush, 1);
            }
            if (resDict.TryGetResource("NeutralAccentBrushSemi", out outVar)) {
                NeutralAccentBrushSemi = (IBrush)outVar!;
                NeutralAccentPenSemi = new Pen(NeutralAccentBrushSemi, 1);
            }
            if (resDict.TryGetResource("AccentBrush1", out outVar)) {
                AccentBrush1 = (IBrush)outVar!;
                AccentPen1 = new Pen(AccentBrush1);
                AccentPen1Thickness2 = new Pen(AccentBrush1, 2);
                AccentPen1Thickness3 = new Pen(AccentBrush1, 3);
            }
            if (resDict.TryGetResource("AccentBrush1Semi", out outVar)) {
                AccentBrush1Semi = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("AccentBrush2", out outVar)) {
                AccentBrush2 = (IBrush)outVar!;
                AccentPen2 = new Pen(AccentBrush2, 1);
                AccentPen2Thickness2 = new Pen(AccentBrush2, 2);
                AccentPen2Thickness3 = new Pen(AccentBrush2, 3);
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
            if (resDict.TryGetResource("FinalPitchBrush", out outVar)) {
                FinalPitchBrush = (IBrush)outVar!;
                FinalPitchPen = new Pen(FinalPitchBrush, 1);
            }
            if (resDict.TryGetResource("WhiteKeyBrush", out outVar)) {
                WhiteKeyBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("WhiteKeyNameBrush", out outVar)) {
                WhiteKeyNameBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("CenterKeyBrush", out outVar)) {
                CenterKeyBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("CenterKeyNameBrush", out outVar)) {
                CenterKeyNameBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("BlackKeyBrush", out outVar)) {
                BlackKeyBrush = (IBrush)outVar!;
            }
            if (resDict.TryGetResource("BlackKeyNameBrush", out outVar)) {
                BlackKeyNameBrush = (IBrush)outVar!;
            }
            if (!IsDarkMode) {
                ExpBrush = WhiteKeyBrush;
                ExpNameBrush = WhiteKeyNameBrush;
                ExpActiveBrush = BlackKeyBrush;
                ExpActiveNameBrush = BlackKeyNameBrush;
                ExpShadowBrush = CenterKeyBrush;
                ExpShadowNameBrush = CenterKeyNameBrush;
            } else {
                ExpBrush = BlackKeyBrush;
                ExpNameBrush = BlackKeyNameBrush;
                ExpActiveBrush = WhiteKeyBrush;
                ExpActiveNameBrush = WhiteKeyNameBrush;
                ExpShadowBrush = CenterKeyBrush;
                ExpShadowNameBrush = CenterKeyNameBrush;
            }
            TextLayoutCache.Clear();
            MessageBus.Current.SendMessage(new ThemeChangedEvent());
        }

        public static void GetColor(string theme) {
            var deserializer = new DeserializerBuilder()
                .Build();
            string text = File.ReadAllText(theme);
            var t = deserializer.Deserialize<Theme>(text);
            ForegroundBrush = (IBrush)ColorToBrushConverter.Convert(t.ForgroundBrush, typeof(IBrush));
            BackgroundBrush = (IBrush)ColorToBrushConverter.Convert(t.BackgroundBrush, typeof(IBrush));
            NeutralAccentBrush = (IBrush)ColorToBrushConverter.Convert(t.NeutralAccentBrush, typeof(IBrush));
            NeutralAccentPen = new Pen(NeutralAccentBrush, 1);
            NeutralAccentBrushSemi = (IBrush)ColorToBrushConverter.Convert(t.NeutralAccentBrushSemi, typeof(IBrush));
            NeutralAccentPenSemi = new Pen(NeutralAccentBrushSemi, 1);
            AccentBrush1 = (IBrush)ColorToBrushConverter.Convert(t.AccentBrush1, typeof(IBrush));
            AccentPen1 = new Pen(AccentBrush1);
            AccentPen1Thickness2 = new Pen(AccentBrush1, 2);
            AccentPen1Thickness3 = new Pen(AccentBrush1, 3);
            AccentBrush1Semi = (IBrush)ColorToBrushConverter.Convert(t.AccentBrush1Semi, typeof(IBrush));
            AccentBrush2 = (IBrush)ColorToBrushConverter.Convert(t.AccentBrush2, typeof(IBrush));
            AccentPen2 = new Pen(AccentBrush2, 1);
            AccentPen2Thickness2 = new Pen(AccentBrush2, 2);
            AccentPen2Thickness3 = new Pen(AccentBrush2, 3);
            AccentBrush2Semi = (IBrush)ColorToBrushConverter.Convert(t.AccentBrush2Semi, typeof(IBrush));
            AccentBrush3 = (IBrush)ColorToBrushConverter.Convert(t.AccentBrush3, typeof(IBrush));
            AccentPen3 = new Pen(AccentBrush3, 1);
            AccentPen3Thick = new Pen(AccentBrush3, 3);
            AccentBrush3Semi = (IBrush)ColorToBrushConverter.Convert(t.AccentBrush3Semi, typeof(IBrush));
            TickLineBrushLow = (IBrush)ColorToBrushConverter.Convert(t.TickLineBrushLow, typeof(IBrush));
            BarNumberBrush = (IBrush)ColorToBrushConverter.Convert(t.BarNumberBrush, typeof(IBrush));
            BarNumberPen = new Pen(BarNumberBrush, 1);
            FinalPitchBrush = (IBrush)ColorToBrushConverter.Convert(t.FinalPitchBrush, typeof(IBrush));
            FinalPitchPen = new Pen(FinalPitchBrush, 1);
            WhiteKeyBrush = (IBrush)ColorToBrushConverter.Convert(t.FinalPitchBrush, typeof(IBrush));
            WhiteKeyNameBrush = (IBrush)ColorToBrushConverter.Convert(t.WhiteKeyNameBrush, typeof(IBrush));
            CenterKeyBrush = (IBrush)ColorToBrushConverter.Convert(t.CenterKeyBrush, typeof(IBrush));
            BlackKeyBrush = (IBrush)ColorToBrushConverter.Convert(t.BlackKeyBrush, typeof(IBrush));
            BlackKeyNameBrush = (IBrush)ColorToBrushConverter.Convert(t.BlackKeyNameBrush, typeof(IBrush));
            if (!Theme.Dark) {
                ExpBrush = WhiteKeyBrush;
                ExpNameBrush = WhiteKeyNameBrush;
                ExpActiveBrush = BlackKeyBrush;
                ExpActiveNameBrush = BlackKeyNameBrush;
                ExpShadowBrush = CenterKeyBrush;
                ExpShadowNameBrush = CenterKeyNameBrush;
            } else {
                ExpBrush = BlackKeyBrush;
                ExpNameBrush = BlackKeyNameBrush;
                ExpActiveBrush = WhiteKeyBrush;
                ExpActiveNameBrush = WhiteKeyNameBrush;
                ExpShadowBrush = CenterKeyBrush;
                ExpShadowNameBrush = CenterKeyNameBrush;
            }
        }

        public static void LoadExternalTheme(string theme) {
            var deserializer = new DeserializerBuilder()
                .Build();
            var files = new List<string>();
            var names = new List<string>();
            try {
                Directory.CreateDirectory(PathManager.Inst.ThemesPath);
                files.AddRange(Directory.EnumerateFiles(PathManager.Inst.ThemesPath, "*.yaml", SearchOption.AllDirectories));
            } catch (Exception e) {
                Log.Error(e, "Failed to search themes.");
            };
            foreach(var file in files) {
                string text = File.ReadAllText(file);
                var n = deserializer.Deserialize<Theme>(text);
                if (n.Name == theme) {
                    GetColor(file);
                    break;
                }
            }
        }
        public static string GetString(string key) {
            IResourceDictionary resDict = Application.Current.Resources;
            if (resDict.TryGetResource(key, out var outVar) && outVar is string s) {
                return s;
            }
            return key;
        }
    }
}
