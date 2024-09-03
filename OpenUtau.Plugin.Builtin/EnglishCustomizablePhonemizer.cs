using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Core.G2p;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("English Customizable Phonemizer", "EN CUSTOM", "TUBS", language: "EN")]
    public class EnglishCustomizablePhonemizer : SyllableBasedPhonemizer {
        public EnglishCustomizablePhonemizer () {
            vowels = new string[] { "aa", "ae", "ah", "ao", "eh", "er", "ih", "iy", "uh", "uw", "ay", "ey", "oy", "ow", "aw", "ax" };
            replacements = new Dictionary<string, string> { };
        }
        
        protected override string GetDictionaryName() => "";
        protected override IG2p LoadBaseDictionary() => new ArpabetG2p();
        
        private string[] vowels { get; set; }
        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => new string[] { }; // All non-vowel symbols are consonants by default. No need to define
        private Dictionary<string, string> replacements { get; set; }
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => replacements;

        public override void SetSinger(USinger singer) {
            if (this.singer != singer) {
                string file = "";
                if (singer.Location != null) {
                    file = Path.Combine(singer.Location, "en-custom.yaml");
                }

                if (File.Exists(file)) {
                    var data = Core.Yaml.DefaultDeserializer.Deserialize<EnglishCustomConfigData>(File.ReadAllText(file));
                    var loadVowels = new List<string>();
                    var loadReplacements = new Dictionary<string, string>();

                    foreach (var symbol in data.symbols) { 
                        var rename = symbol.rename ?? symbol.name;
                        loadReplacements.Add(symbol.name, rename);
                        if (symbol.type == "vowel") {
                            loadVowels.Add(rename);
                        }
                    }
                    vowels = loadVowels.ToArray();
                    replacements = loadReplacements;
                } else {
                    File.WriteAllBytes(file, Data.Resources.en_custom_template);
                }

                ReadDictionaryAndInit();
                this.singer = singer;
            }
        }

        protected override List<string> ProcessSyllable(Syllable syllable) {
            if (CanMakeAliasExtension(syllable)) {
                return new List<string> { "null" };
            }

            var phonemes = new List<string>();
            var symbols = new List<string>();
            symbols.Add(syllable.prevV == "" ? "-" : syllable.prevV);
            symbols.AddRange(syllable.cc);
            if (syllable.cc.Length == 0) {
                symbols.Add(syllable.v);
            }

            for (int i = 0; i < symbols.Count - 1; i++) {
                phonemes.Add($"{symbols[i]} {symbols[i + 1]}");
            }

            if (syllable.cc.Length > 0) {
                var cv = new[] { $"{syllable.cc.Last()}{syllable.v}",
                $"{syllable.cc.Last()} {syllable.v}"};
                if (!TryAddPhoneme(phonemes, syllable.vowelTone, cv)) {
                    phonemes.Add(cv[1]);
                }
            }

            return phonemes;
        }

        protected override List<string> ProcessEnding(Ending ending) {
            var phonemes = new List<string>();
            var symbols = new List<string>();
            symbols.Add(ending.prevV);
            symbols.AddRange(ending.cc);
            symbols.Add("-");

            for (int i = 0; i < symbols.Count - 1; i++) {
                phonemes.Add($"{symbols[i]} {symbols[i + 1]}");
            }
            return phonemes;
        }
    }

    public class EnglishCustomConfigData {
        public struct SymbolData {
            public string name;
            public string type;
            public string? rename;
        }

        public SymbolData[] symbols;
    }
}
