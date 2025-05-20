﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using OpenUtau.App.Views;
using OpenUtau.Classic;
using OpenUtau.Core;
using Serilog;
using YamlDotNet.Core.Tokens;

namespace OpenUtau.App {
    public class App : Application {
        public override void Initialize() {
            Log.Information("Initializing application.");
            AvaloniaXamlLoader.Load(this);
            InitializeCulture();
            InitializeTheme();
            Log.Information("Initialized application.");
        }

        public override void OnFrameworkInitializationCompleted() {
            Log.Information("Framework initialization completed.");
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.MainWindow = new SplashWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        public void InitializeCulture() {
            Log.Information("Initializing culture.");
            string sysLang = CultureInfo.InstalledUICulture.Name;
            string prefLang = Core.Util.Preferences.Default.Language;
            var languages = GetLanguages();
            if (languages.ContainsKey(prefLang)) {
                SetLanguage(prefLang);
            } else if (languages.ContainsKey(sysLang)) {
                SetLanguage(sysLang);
                Core.Util.Preferences.Default.Language = sysLang;
                Core.Util.Preferences.Save();
            } else {
                SetLanguage("en-US");
            }

            // Force using InvariantCulture to prevent issues caused by culture dependent string conversion, especially for floating point numbers.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            Log.Information("Initialized culture.");
        }

        public static Dictionary<string, IResourceProvider> GetLanguages() {
            if (Current == null) {
                return new();
            }
            var result = new Dictionary<string, IResourceProvider>();
            foreach (string key in Current.Resources.Keys.OfType<string>()) {
                if (key.StartsWith("strings-") &&
                    Current.Resources.TryGetResource(key, ThemeVariant.Default, out var res) &&
                    res is IResourceProvider rp) {
                    result.Add(key.Replace("strings-", ""), rp);
                }
            }
            return result;
        }

        public static void SetLanguage(string language) {
            if (Current == null) {
                return;
            }
            var languages = GetLanguages();
            foreach (var res in languages.Values) {
                Current.Resources.MergedDictionaries.Remove(res);
            }
            if (language != "en-US") {
                Current.Resources.MergedDictionaries.Add(languages["en-US"]);
            }
            if (languages.TryGetValue(language, out var res1)) {
                Current.Resources.MergedDictionaries.Add(res1);
            }
        }

        static void InitializeTheme() {
            Log.Information("Initializing theme.");
            SetTheme();
            Log.Information("Initialized theme.");
        }

        public static void SetTheme() {
            if (Current == null) {
                return;
            }
            var light = (IResourceProvider)Current.Resources["themes-light"]!;
            var dark = (IResourceProvider)Current.Resources["themes-dark"]!;
            var highContrast = (IResourceProvider)Current.Resources["themes-high-contrast"]!;
            Current.Resources.MergedDictionaries.Remove(light);
            Current.Resources.MergedDictionaries.Remove(dark);
            Current.Resources.MergedDictionaries.Remove(highContrast);
            if (Core.Util.Preferences.Default.Theme == 0) {
                Current.Resources.MergedDictionaries.Add(light);
                Current.RequestedThemeVariant = ThemeVariant.Light;
            } else if (Core.Util.Preferences.Default.Theme == 1) {
                Current.Resources.MergedDictionaries.Add(dark);
                Current.RequestedThemeVariant = ThemeVariant.Dark;
            } else {
                Current.Resources.MergedDictionaries.Add(highContrast);
                Current.RequestedThemeVariant = ThemeVariant.Dark;
            }
            ThemeManager.LoadTheme();
        }
    }
}
