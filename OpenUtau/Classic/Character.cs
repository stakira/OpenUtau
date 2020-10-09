using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenUtau.SimpleHelpers;
using Serilog;

namespace OpenUtau.Classic {

    /// <summary>
    /// A character. May contains multiple voice banks.
    /// </summary>
    public class Character {

        private Character() {
        }

        public string Name { private set; get; }
        public string DisplayName => Loaded ? Name : $"{Name}[Unloaded]";
        public string BasePath { private set; get; }
        public string CharacterFile { private set; get; }
        public string Author { private set; get; }
        public string Website { private set; get; }
        public string Language { private set; get; }
        public List<Voicebank> Voicebanks { private set; get; }
        public bool Loaded { private set; get; }

        public static List<Character> SearchAndLoadCharacters(List<string> searchPaths) {
            const string CharacterFileName = "character.txt";
            var characterFiles = new HashSet<string>();
            foreach (var searchPath in searchPaths) {
                if (!Directory.Exists(searchPath)) {
                    continue;
                }
                var files = Directory.GetFiles(searchPath, CharacterFileName,
                    SearchOption.AllDirectories);
                foreach (var file in files) {
                    characterFiles.Add(file);
                }
            }
            var result = new List<Character>();
            foreach (var file in characterFiles) {
                var character = new Character {
                    BasePath = Path.GetDirectoryName(file),
                    CharacterFile = file,
                };
                character.LoadVoicebanks();
                result.Add(character);
            }
            return result;
        }

        public void LoadVoicebanks() {
            const string OtoFileName = "oto.ini";
            var otoFiles = Directory.GetFiles(BasePath, OtoFileName, SearchOption.AllDirectories);
            var voicebanks = new List<Voicebank>();
            foreach (var otoFile in otoFiles) {
                try {
                    var voicebank = new Voicebank.Builder(otoFile)
                        .SetOtoEncoding(FileEncoding.DetectFileEncoding(
                            otoFile, Encoding.GetEncoding("shift_jis")))
                        .SetPathEncoding(Encoding.Default)
                        .Build();
                    voicebanks.Add(voicebank);
                } catch (Exception e) {
                    Log.Error(e, "Failed to load {0}", otoFile);
                }
            }
            Log.Information("Loaded {0} voicebanks from character {1}", voicebanks.Count, BasePath);
            Voicebanks = voicebanks;
            Loaded = true;
        }
    }
}
