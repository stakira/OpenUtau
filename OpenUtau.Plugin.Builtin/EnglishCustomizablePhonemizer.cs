using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Classic;
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

        private string[] vowels;
        private string[] diphthongs;
        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => new string[] { }; // All non-vowel symbols are consonants by default. No need to define
        private Dictionary<string, string> replacements;
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => replacements;

        private string[] tails;
        private CombinedPhoneme[] combinations;

        public override void SetSinger(USinger singer) {
            if (this.singer != singer) {
                string file = "";
                if (singer.Location != null) {
                    file = Path.Combine(singer.Location, "en-custom.yaml");
                }

                if (File.Exists(file)) {
                    var data = Core.Yaml.DefaultDeserializer.Deserialize<EnglishCustomConfigData>(File.ReadAllText(file));

                    tails = data.tails;

                    var loadVowels = new List<string>();
                    var loadDiphthongs = new List<string>();
                    var loadReplacements = new Dictionary<string, string>();
                    foreach (var symbol in data.symbols) { 
                        var rename = symbol.rename ?? symbol.name;
                        loadReplacements.Add(symbol.name, rename);
                        if ((symbol.type == "vowel" || symbol.type == "diphthong") && !loadVowels.Contains(rename)) {
                            loadVowels.Add(rename);
                        }
                        if (symbol.type == "diphthong" && !loadDiphthongs.Contains(rename)) {
                            loadDiphthongs.Add(rename);
                        }
                    }
                    vowels = loadVowels.ToArray();
                    diphthongs = loadDiphthongs.ToArray();
                    replacements = loadReplacements;

                    if (data.combinations != null) {
                        //combinations = new Dictionary<string, string>();
                        var loadCombinations = new List<CombinedPhoneme>();
                        foreach (var combo in data.combinations) {
                            loadCombinations.Add(new CombinedPhoneme {
                                before = combo.before,
                                after = combo.after,
                                prefix = combo.prefix ?? combo.after
                            });
                        }
                        combinations = loadCombinations.ToArray();
                    }
                } else {
                    File.WriteAllBytes(file, Data.Resources.en_custom_template);
                }

                ReadDictionaryAndInit();
                this.singer = singer;
            }
        }

        protected override string[] GetSymbols(Note note) {
            if (tails.Contains(note.lyric)) {
                return new string[] { note.lyric };
            }

            var symbols = base.GetSymbols(note);

            if (combinations != null) {
                var symbolString = string.Join(" ", symbols);
                foreach (var combo in combinations) {
                    symbolString = symbolString.Replace(combo.before, combo.after);
                }
                symbols = symbolString.Split();
            }

            return symbols;
        }

        protected override List<string> ProcessSyllable(Syllable syllable) {
            if (CanMakeAliasExtension(syllable) && !diphthongs.Contains(syllable.prevV)) {
                return new List<string>();
            }

            var phonemes = new List<string>();
            var symbols = new List<string>();

            syllable.prevV = tails.Contains(syllable.prevV) ? "" : syllable.prevV;
            symbols.Add(syllable.prevV == "" ? "-" : syllable.prevV);
            symbols.AddRange(syllable.cc);
            if (syllable.cc.Length == 0) {
                symbols.Add(syllable.v);
            }

            for (int i = 0; i < symbols.Count - 1; i++) {
                var second = symbols[i + 1];
                if (combinations != null && combinations.Any(c => c.after == second)) {
                    second = combinations.Where(c => c.after == second).First().prefix;
                }
                phonemes.Add($"{symbols[i]} {second}");
            }

            // TODO: make explicit config option for [CV] or [C V] notation. Never check the OTO
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
            if (tails.Contains(ending.prevV)) {
                return new List<string>();
            }

            var phonemes = new List<string>();
            var symbols = new List<string>();
            symbols.Add(ending.prevV);
            symbols.AddRange(ending.cc);
            symbols.Add("-");

            for (int i = 0; i < symbols.Count - 1; i++) {
                var second = symbols[i + 1];
                if (combinations != null && combinations.Any(c => c.after == second)) {
                    second = combinations.Where(c => c.after == second).First().prefix;
                }
                phonemes.Add($"{symbols[i]} {second}");
            }
            return phonemes;
        }   
        struct CombinedPhoneme {
            public string before;
            public string after;
            public string prefix;
        }
    }

    public class EnglishCustomConfigData {
        public struct SymbolData {
            public string name;
            public string type;
            public string? rename;
        }

        public SymbolData[] symbols;
        public string[] tails;

        public struct CombinePhonemeData {
            public string before;
            public string after;
            public string? prefix;
        }

        public CombinePhonemeData[]? combinations;
    }
}
