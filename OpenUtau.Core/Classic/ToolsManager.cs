using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using OpenUtau;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace OpenUtau.Classic {
    public class ToolsManager : SingletonBase<ToolsManager> {
        static object _locker = new object();

        private readonly List<IResampler> resamplers = new List<IResampler>();
        private readonly List<IWavtool> wavtools = new List<IWavtool>();
        private readonly List<ITheme> themes = new List<ITheme>();
        private readonly Dictionary<string, IResampler> resamplersMap
            = new Dictionary<string, IResampler>();
        private readonly Dictionary<string, IWavtool> wavtoolsMap
            = new Dictionary<string, IWavtool>();
        private readonly Dictionary<string, ITheme> themeCollection
            = new Dictionary<string, ITheme>();

        public List<IResampler> Resamplers {
            get {
                lock (_locker) {
                    return resamplers.ToList();
                }
            }
        }

        public List<IWavtool> Wavtools {
            get {
                lock (_locker) {
                    return wavtools.ToList();
                }
            }
        }

        public List<ITheme> Themes {
            get { 
                lock (_locker) {
                    return themes.ToList();
                } 
            }
        }

        IResampler LoadResampler(string filePath, string basePath) {
            if (!File.Exists(filePath)) {
                return null;
            }
            string ext = Path.GetExtension(filePath).ToLower();
            if (OS.IsWindows()) {
                if (ext == ".exe" || ext == ".bat") {
                    return new ExeResampler(filePath, basePath);
                }
            } else {
                if (ext == ".sh" || string.IsNullOrEmpty(ext)) {
                    return new ExeResampler(filePath, basePath);
                }
            }
            return null;
        }

        IWavtool LoadWavtool(string filePath, string basePath) {
            if (!File.Exists(filePath)) {
                return null;
            }
            string ext = Path.GetExtension(filePath).ToLower();
            if (OS.IsWindows()) {
                if (ext == ".exe" || ext == ".bat") {
                    return new ExeWavtool(filePath, basePath);
                }
            } else {
                if (ext == ".sh" || string.IsNullOrEmpty(ext)) {
                    return new ExeWavtool(filePath, basePath);
                }
            }
            return null;
        }

        public void Initialize() {
            lock (_locker) {
                SearchResamplers();
                SearchWavtools();
            }
        }

        public void SearchResamplers() {
            resamplers.Clear();
            resamplersMap.Clear();
            resamplers.Add(new WorldlineResampler());
            string basePath = PathManager.Inst.ResamplersPath;
            try {
                Directory.CreateDirectory(basePath);
                foreach (var file in Directory.EnumerateFiles(basePath, "*", new EnumerationOptions() {
                    RecurseSubdirectories = true
                })) {
                    var driver = LoadResampler(file, basePath);
                    if (driver != null) {
                        resamplers.Add(driver);
                    }
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to search resamplers.");
                resamplers.Clear();
            }
            foreach (var resampler in resamplers) {
                resamplersMap[resampler.ToString()] = resampler;
            }
        }

        public void SearchWavtools() {
            wavtools.Clear();
            wavtoolsMap.Clear();
            wavtools.Add(new SharpWavtool(true));
            wavtools.Add(new SharpWavtool(false));
            string basePath = PathManager.Inst.WavtoolsPath;
            try {
                Directory.CreateDirectory(basePath);
                foreach (var file in Directory.EnumerateFiles(basePath, "*", new EnumerationOptions() {
                    RecurseSubdirectories = true
                })) {
                    var driver = LoadWavtool(file, basePath);
                    if (driver != null) {
                        wavtools.Add(driver);
                    }
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to search wavtools.");
                wavtools.Clear();
            }
            foreach (var wavtool in wavtools) {
                wavtoolsMap[wavtool.ToString()] = wavtool;
            }
        }

        public IResampler GetResampler(string name) {
            lock (_locker) {
                if (name != null && resamplersMap.TryGetValue(name, out var resampler)) {
                    return resampler;
                } else {
                    return resamplersMap[WorldlineResampler.name];
                }
            }
        }

        public IWavtool GetWavtool(string name) {
            lock (_locker) {
                if (name != null && wavtoolsMap.TryGetValue(name, out var wavtool)) {
                    return wavtool;
                } else {
                    return wavtoolsMap[SharpWavtool.nameConvergence];
                }
            }
        }
        public ITheme GetName() { 
            lock (_locker) {
                Themes.Clear();
                themeCollection.Clear();
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
                foreach (var file in files) {
                    string text = File.ReadAllText(file);
                    var n = deserializer.Deserialize<Theme>(text);
                    themeCollection[n.Name.ToString()] = n.Name;
                    
                }
            }
        }
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
    }
}
