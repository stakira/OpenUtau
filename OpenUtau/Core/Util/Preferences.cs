using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using JsonFx.Json;

namespace OpenUtau.Core.Util
{
    static class Preferences
    {
        private const string filename = "prefs.json";

        public static dynamic Default;
        private static JsonWriter writer;
        private static JsonReader reader;

        static Preferences() {
            writer = new JsonWriter();
            writer.Settings.PrettyPrint = true;
            reader = new JsonReader();
            Load();
        }

        private static void Load()
        {
            if (File.Exists(filename)) Default = reader.Read(File.ReadAllText(filename));
            else Reset();
        }

        public static void Save()
        {
            File.WriteAllText(filename, writer.Write(Default));
        }

        public static void Reset()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var stream = new StreamReader(assembly.GetManifestResourceStream("OpenUtau.Resources.prefs.json"));
            Default = reader.Read(stream.ReadToEnd());
            Save();
        }
    }
}
