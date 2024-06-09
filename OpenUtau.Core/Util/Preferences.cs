using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using OpenUtau.Core.Render;
using Serilog;

namespace OpenUtau.Core.Util {

    public static class Preferences {
        public static SerializablePreferences Default;

        static Preferences() {
            Load();
        }

        public static void Save() {
            try {
                File.WriteAllText(PathManager.Inst.PrefsFilePath,
                    JsonConvert.SerializeObject(Default, Formatting.Indented),
                    Encoding.UTF8);
            } catch (Exception e) {
                Log.Error(e, "Failed to save prefs.");
            }
        }

        public static void Reset() {
            Default = new SerializablePreferences();
            Save();
        }

        public static List<string> GetSingerSearchPaths() {
            return new List<string>(Default.SingerSearchPaths);
        }

        public static void SetSingerSearchPaths(List<string> paths) {
            Default.SingerSearchPaths = new List<string>(paths);
            Save();
        }

        public static void AddRecentFileIfEnabled(string filePath){
            //Users can choose adding .ust, .vsqx and .mid files to recent files or not
            string ext = Path.GetExtension(filePath);
            switch(ext){
                case ".ustx":
                    AddRecentFile(filePath);
                    break;
                case ".mid":
                case ".midi":
                    if(Preferences.Default.RememberMid){
                        AddRecentFile(filePath);
                    }
                    break;
                case ".ust":
                    if(Preferences.Default.RememberUst){
                        AddRecentFile(filePath);
                    }
                    break;
                case ".vsqx":
                    if(Preferences.Default.RememberVsqx){
                        AddRecentFile(filePath);
                    }
                    break;
                default:
                    break;
            }
        }

        private static void AddRecentFile(string filePath) {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
                return;
            }
            var recent = Default.RecentFiles;
            recent.RemoveAll(f => f == filePath);
            recent.Insert(0, filePath);
            recent.RemoveAll(f => string.IsNullOrEmpty(f)
                || !File.Exists(f)
                || f.Contains(PathManager.Inst.TemplatesPath));
            if (recent.Count > 16) {
                recent.RemoveRange(16, recent.Count - 16);
            }
            Save();
        }

        private static void Load() {
            try {
                if (File.Exists(PathManager.Inst.PrefsFilePath)) {
                    Default = JsonConvert.DeserializeObject<SerializablePreferences>(
                        File.ReadAllText(PathManager.Inst.PrefsFilePath, Encoding.UTF8));
                    if(Default == null) {
                        Reset();
                        return;
                    }

                    if (!ValidString(new Action(() => CultureInfo.GetCultureInfo(Default.Language)))) Default.Language = string.Empty;
                    if (!ValidString(new Action(() => CultureInfo.GetCultureInfo(Default.SortingOrder)))) Default.SortingOrder = string.Empty;
                    if (!Renderers.getRendererOptions().Contains(Default.DefaultRenderer)) Default.DefaultRenderer = string.Empty;
                    if (!Onnx.getRunnerOptions().Contains(Default.OnnxRunner)) Default.OnnxRunner = string.Empty;
                } else {
                    Reset();
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to load prefs.");
                Default = new SerializablePreferences();
            }
        }

        private static bool ValidString(Action action) {
            try {
                action();
                return true;
            } catch {
                return false;
            }
        }

        [Serializable]
        public class SerializablePreferences {
            public const int MidiWidth = 1024;
            public const int MidiHeight = 768;
            public int MainWidth = 1024;
            public int MainHeight = 768;
            public bool MainMaximized;
            public bool MidiMaximized;
            public int UndoLimit = 100;
            public List<string> SingerSearchPaths = new List<string>();
            public string PlaybackDevice = string.Empty;
            public int PlaybackDeviceNumber;
            public int? PlaybackDeviceIndex;
            public bool ShowPrefs = true;
            public bool ShowTips = true;
            public int Theme;
            public bool PenPlusDefault = false;
            public int DegreeStyle;
            public bool UseTrackColor = false;
            public bool ClearCacheOnQuit = false;
            public bool PreRender = true;
            public int NumRenderThreads = 2;
            public string DefaultRenderer = string.Empty;
            public int WorldlineR = 0;
            public string OnnxRunner = string.Empty;
            public int OnnxGpu = 0;
            public double DiffSingerDepth = 1.0;
            public int DiffSingerSteps = 20;
            public bool DiffSingerTensorCache = true;
            public bool SkipRenderingMutedTracks = false;
            public string Language = string.Empty;
            public string? SortingOrder = null;
            public List<string> RecentFiles = new List<string>();
            public string SkipUpdate = string.Empty;
            public string AdditionalSingerPath = string.Empty;
            public bool InstallToAdditionalSingersPath = true;
            public bool LoadDeepFolderSinger = true;
            public bool PreferCommaSeparator = false;
            public bool ResamplerLogging = false;
            public List<string> RecentSingers = new List<string>();
            public List<string> FavoriteSingers = new List<string>();
            public Dictionary<string, string> SingerPhonemizers = new Dictionary<string, string>();
            public List<string> RecentPhonemizers = new List<string>();
            public bool PreferPortAudio = false;
            public double PlayPosMarkerMargin = 0.9;
            public int LockStartTime = 0;
            public int PlaybackAutoScroll = 2;
            public bool ReverseLogOrder = true;
            public bool ShowPortrait = true;
            public bool ShowIcon = true;
            public bool ShowGhostNotes = true;
            public bool PlayTone = true;
            public bool ShowVibrato = true;
            public bool ShowPitch = true;
            public bool ShowFinalPitch = true;
            public bool ShowWaveform = true;
            public bool ShowPhoneme = true;
            public bool ShowNoteParams = false;
            public Dictionary<string, string> DefaultResamplers = new Dictionary<string, string>();
            public Dictionary<string, string> DefaultWavtools = new Dictionary<string, string>();
            public string LyricHelper = string.Empty;
            public bool LyricsHelperBrackets = false;
            public int OtoEditor = 0;
            public string VLabelerPath = string.Empty;
            public string SetParamPath = string.Empty;
            public bool Beta = false;
            public bool RememberMid = false;
            public bool RememberUst = true;
            public bool RememberVsqx = true;
            public int ImportTempo = 0;
            public string PhoneticAssistant = string.Empty;
            public string RecentOpenSingerDirectory = string.Empty;
            public string RecentOpenProjectDirectory = string.Empty;
            public bool LockUnselectedNotesPitch = true;
            public bool LockUnselectedNotesVibrato = true;
            public bool LockUnselectedNotesExpressions = true;

            public bool VoicebankPublishUseIgnore = true;
            public string VoicebankPublishIgnores = "#Adobe Audition\n*.pkf\n\n#UTAU Engines\n*.ctspec\n*.d4c\n*.dio\n*.frc\n*.frt\n#*.frq\n*.harvest\n*.lessaudio\n*.llsm\n*.mrq\n*.pitchtier\n*.pkf\n*.platinum\n*.pmk\n*.star\n*.uspec\n*.vs4ufrq\n\n#UTAU related tools\n$read\n*.setParam-Scache\n*.lbp\n*.lbp.caches/*\n\n#OpenUtau\nerrors.txt\n*.sc.npz";
        }
    }
}
