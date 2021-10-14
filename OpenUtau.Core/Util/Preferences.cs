using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Serilog;

namespace OpenUtau.Core.Util {

    public static class Preferences {
        public static SerializablePreferences Default;
        private const string filename = "prefs.json";

        static Preferences() {
            Load();
        }

        public static void Save() {
            try {
                File.WriteAllText(filename, JsonConvert.SerializeObject(Default, Formatting.Indented));
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

        private static void Load() {
            try {
                if (File.Exists(filename)) {
                    Default = JsonConvert.DeserializeObject<SerializablePreferences>(File.ReadAllText(filename));
                } else {
                    Reset();
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to load prefs.");
                Default = new SerializablePreferences();
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
            public string ExternalPreviewEngine = string.Empty;
            public string ExternalExportEngine = string.Empty;
            public string PlaybackDevice = string.Empty;
            public int PlaybackDeviceNumber;
            public bool ShowPrefs = true;
            public bool ShowTips = true;
            public int Theme;
            public int Beta = 0;
            public int PrerenderThreads = 8;
            public string Language = string.Empty;
            public List<string> RecentFiles = new List<string>();
        }
    }
}
