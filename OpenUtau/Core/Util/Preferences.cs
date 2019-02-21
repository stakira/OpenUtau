using System;
using System.Collections.Generic;
using System.IO;

using JsonFx.Json;

namespace OpenUtau.Core.Util {

    internal static class Preferences {
        public static SerializablePreferences Default;
        private const string filename = "prefs.json";
        private static JsonWriter writer;
        private static JsonReader reader;

        static Preferences() {
            writer = new JsonWriter();
            writer.Settings.PrettyPrint = true;
            reader = new JsonReader();
            Load();
        }

        public static void Save() {
            File.WriteAllText(filename, writer.Write(Default));
        }

        public static void Reset() {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var stream = new StreamReader(assembly.GetManifestResourceStream("OpenUtau.Resources.prefs.json"));
            Default = reader.Read<SerializablePreferences>(stream.ReadToEnd());
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
            if (File.Exists(filename)) {
                Default = reader.Read<SerializablePreferences>(File.ReadAllText(filename));
            } else {
                Reset();
            }
        }

        [Serializable]
        public class SerializablePreferences {
            public const int MidiWidth = 1024;
            public const int MidiHeight = 768;
            public int MainWidth = 1024;
            public int MainHeight = 768;
            public bool MainMaximized = false;
            public bool MidiMaximized;
            public int UndoLimit = 100;
            public List<string> SingerSearchPaths;
            public string ExternalPreviewEngine;
            public string ExternalExportEngine;
        }
    }
}
