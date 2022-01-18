using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenUtau.Api;
using WanaKanaNet;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("English to Japanese Phonemizer", "EN to JA", "TUBS")]
    public class ENtoJAPhonemizer : SyllableBasedPhonemizer {
        protected override string[] GetVowels() => "a i u e o ay ey oy ow aw".Split();

        protected override string[] GetConsonants() =>
            "b by ch d dh f g gy h hy j k ky l ly m my n ny ng p py r ry s sh t ts th v w y z zh".Split();
        protected override string GetDictionaryName() => "cmudict-0_7b.txt";
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => new Dictionary<string, string> {
            { "AA", "a" },
            { "AA0", "a" },
            { "AA1", "a" },
            { "AA2", "a" },
            { "AE", "e" },
            { "AE0", "e" },
            { "AE1", "e" },
            { "AE2", "e" },
            { "AH", "a" },
            { "AH0", "a" },
            { "AH1", "a" },
            { "AH2", "a" },
            { "AO", "o" },
            { "AO0", "o" },
            { "AO1", "o" },
            { "AO2", "o" },
            { "AW", "aw" },
            { "AW0", "aw" },
            { "AW1", "aw" },
            { "AW2", "aw" },
            { "AY", "ay" },
            { "AY0", "ay" },
            { "AY1", "ay" },
            { "AY2", "ay" },
            { "B", "b" },
            { "CH", "ch" },
            { "D", "d" },
            { "DH", "dh" },
            { "EH", "e" },
            { "EH0", "e" },
            { "EH1", "e" },
            { "EH2", "e" },
            { "ER", "o" },
            { "ER0", "o" },
            { "ER1", "o" },
            { "ER2", "o" },
            { "EY", "ey" },
            { "EY0", "ey" },
            { "EY1", "ey" },
            { "EY2", "ey" },
            { "F", "f" },
            { "G", "g" },
            { "HH", "h" },
            { "IH", "e" },
            { "IH0", "e" },
            { "IH1", "e" },
            { "IH2", "e" },
            { "IY", "i" },
            { "IY0", "i" },
            { "IY1", "i" },
            { "IY2", "i" },
            { "JH", "j" },
            { "K", "k" },
            { "L", "l" },
            { "M", "m" },
            { "N", "n" },
            { "NG", "ng" },
            { "OW", "ow" },
            { "OW0", "ow" },
            { "OW1", "ow" },
            { "OW2", "ow" },
            { "OY", "oy" },
            { "OY0", "oy" },
            { "OY1", "oy" },
            { "OY2", "oy" },
            { "P", "p" },
            { "R", "r" },
            { "S", "s" },
            { "SH", "sh" },
            { "T", "t" },
            { "TH", "th" },
            { "UH", "o" },
            { "UH0", "o" },
            { "UH1", "o" },
            { "UH2", "o" },
            { "UW", "u" },
            { "UW0", "u" },
            { "UW1", "u" },
            { "UW2", "u" },
            { "V", "v" },
            { "W", "w" },
            { "Y", "y" },
            { "Z", "z" },
            { "ZH", "zh" },
        };

        protected override string ReadDictionary(string filename) {
            return Core.Api.Resources.cmudict_0_7b;
        }

        private Dictionary<string, string> StartingConsonant => new Dictionary<string, string> {
            { "", "" },
            { "b", "b" },
            { "by", "by" },
            { "ch", "ch" },
            { "d", "d" },
            { "dh", "d" },
            { "f", "f" },
            { "g", "g" },
            { "gy", "gy" },
            { "h", "h" },
            { "hy", "hy" },
            { "j", "j" },
            { "k", "k" },
            { "ky", "ky" },
            { "l", "r" },
            { "ly", "ry" },
            { "m", "m" },
            { "my", "my" },
            { "n", "n" },
            { "ny", "ny" },
            { "ng", "n" },
            { "p", "p" },
            { "py", "py" },
            { "r", "rr" },
            { "ry", "ry" },
            { "s", "s" },
            { "sh", "sh" },
            { "t", "t" },
            { "ts", "ts" },
            { "th", "s" },
            { "v", "f" },
            { "w", "w" },
            { "y", "y" },
            { "z", "z" },
            { "zh", "sh" },
        };

        private Dictionary<string, string> SoloConsonant => new Dictionary<string, string> {
            { "b", "ぶ" },
            { "by", "び" },
            { "ch", "ちゅ" },
            { "d", "ど" },
            { "dh", "ず" },
            { "f", "ふ" },
            { "g", "ぐ" },
            { "gy", "ぎ" },
            { "h", "ほ" },
            { "hy", "ひ" },
            { "j", "じゅ" },
            { "k", "く" },
            { "ky", "き" },
            { "l", "う" },
            { "ly", "り" },
            { "m", "む" },
            { "my", "み" },
            { "n", "ん" },
            { "ny", "に" },
            { "ng", "ん" },
            { "p", "ぷ" },
            { "py", "ぴ" },
            { "r", "う" },
            { "ry", "り" },
            { "s", "す" },
            { "sh", "しゅ" },
            { "t", "と" },
            { "ts", "つ" },
            { "th", "す" },
            { "v", "ふ" },
            { "w", "う" },
            { "y", "い" },
            { "z", "ず" },
            { "zh", "しゅ" },
        };

        private string[] SpecialClusters = "ky gy ts ny hy by py my ry ly".Split();

        private Dictionary<string, string> AltCv => new Dictionary<string, string> {
            {"si", "suli" },
            {"zi", "zuli" },
            {"ti", "teli" },
            {"tu", "tolu" },
            {"di", "deli" },
            {"du", "dolu" },
            {"hu", "holu" },
            {"yi", "i" },
            {"wu", "u" },
            {"wo", "ulo" },
            {"rra", "wa" },
            {"rri", "wi" },
            {"rru", "ru" },
            {"rre", "we" },
            {"rro", "ulo" },
        };

        private Dictionary<string, string[]> ExtraCv => new Dictionary<string, string[]> {
            {"kye", new [] { "ki", "e" } },
            {"gye", new [] { "gi", "e" } },
            {"suli", new [] { "se", "i" } },
            {"she", new [] { "si", "e" } },
            {"zuli", new [] { "ze", "i" } },
            {"je", new [] { "ji", "e" } },
            {"teli", new [] { "te", "i" } },
            {"tolu", new [] { "to", "u" } },
            {"che", new [] { "chi", "e" } },
            {"tsa", new [] { "tsu", "a" } },
            {"tsi", new [] { "tsu", "i" } },
            {"tse", new [] { "tsu", "e" } },
            {"tso", new [] { "tsu", "o" } },
            {"deli", new [] { "de", "i" } },
            {"dolu", new [] { "do", "u" } },
            {"nye", new [] { "ni", "e" } },
            {"hye", new [] { "hi", "e" } },
            {"holu", new [] { "ho", "u" } },
            {"fa", new [] { "fu", "a" } },
            {"fi", new [] { "fu", "i" } },
            {"fe", new [] { "fu", "e" } },
            {"fo", new [] { "fu", "o" } },
            {"bye", new [] { "bi", "e" } },
            {"pye", new [] { "pi", "e" } },
            {"mye", new [] { "mi", "e" } },
            {"ye", new [] { "i", "e" } },
            {"rye", new [] { "ri", "e" } },
            {"wi", new [] { "u", "i" } },
            {"we", new [] { "u", "e" } },
            {"ulo", new [] { "u", "o" } },
        };

        protected override List<string> ProcessEnding(Ending ending) {
            var prevV = ending.prevV;
            var cc = ending.cc;
            var phonemes = new List<string>();

            if (prevV.Length == 2) {
                var newCC = new List<string>();
                newCC.Add(prevV[1].ToString());
                newCC.AddRange(cc);
                cc = newCC.ToArray();
                prevV = prevV[0].ToString();
            }

            foreach (var symbol in cc) {
                phonemes.Add(SoloConsonant[symbol]);
            }

            var phonemesVcv = new List<string>();
            foreach (var phoneme in phonemes) {
                phonemesVcv.Add(TryVcv(prevV, phoneme, ending.tone));
                prevV = WanaKana.ToRomaji(phoneme).Last<char>().ToString();
            }

            return phonemesVcv;
        }

        protected override List<string> ProcessSyllable(Syllable syllable) {
            // Skip processing if this note extends the prevous syllable
            if (CanMakeAliasExtension(syllable)) {
                return new List<string> { null };
            }

            var prevV = syllable.prevV;
            var cc = syllable.cc;
            var v = syllable.v;
            var phonemes = new List<string>();

            // Handle diphthongs
            if (prevV.Length == 2) {
                var newCC = new List<string>();
                newCC.Add(prevV[1].ToString());
                newCC.AddRange(cc);
                cc = newCC.ToArray();
                prevV = prevV[0].ToString();
            } else if (prevV.Length == 0) {
                prevV = "-";
            }
            if (v.Length == 2) {
                v = v[0].ToString();
            }

            // Check CCs for special clusters
            var adjustedCC = new List<string>();
            for (var i = 0; i < cc.Length; i++) {
                if (i == cc.Length - 1) {
                    adjustedCC.Add(cc[i]);
                } else {
                    var diphone = $"{cc[i]}{cc[i + 1]}";
                    if (SpecialClusters.Contains(diphone)) {
                        adjustedCC.Add(diphone);
                        i++;
                    } else {
                        adjustedCC.Add(cc[i]);
                    }
                }
            }
            cc = adjustedCC.ToArray();

            // Separate CCs and main CV
            var finalCons = "";
            if (cc.Length > 0) {
                finalCons = cc[cc.Length - 1];
                for (var i = 0; i < cc.Length - 1; i++) {
                    var cons = SoloConsonant[cc[i]];
                    phonemes.Add(TryVcv(prevV, cons, syllable.tone));
                    prevV = WanaKana.ToRomaji(cons).Last<char>().ToString();
                }
            }

            // Convert to hiragana
            var cv = $"{StartingConsonant[finalCons]}{v}";
            cv = AltCv.ContainsKey(cv) ? AltCv[cv] : cv;
            var hiragana = WanaKana.ToHiragana(cv);
            hiragana = TryVcv(prevV, hiragana, syllable.vowelTone);

            // Check for nonstandard CV
            var split = false;
            if (HasOto(hiragana, syllable.vowelTone)) {
                phonemes.Add(hiragana);
            } else if (cv == "ulo") {
                hiragana = TryVcv(prevV, "を", syllable.vowelTone);
                if (HasOto(hiragana, syllable.vowelTone)) {
                    phonemes.Add(hiragana);
                } else {
                    split = true;
                }
            } else {
                split = true;
            }
            
            // Handle nonstandard CV
            if (split && ExtraCv.ContainsKey(cv)) {
                var splitCv = ExtraCv[cv];
                for (var i = 0; i < splitCv.Length; i++) {
                    if (splitCv[i] != prevV) {
                        var converted = WanaKana.ToHiragana(splitCv[i]);
                        phonemes.Add(TryVcv(prevV, converted, syllable.vowelTone));
                        prevV = splitCv[i].Last<char>().ToString();
                    }
                }
            }

            return phonemes;
        }

        private string TryVcv(string vowel, string cv, int tone) {
            var vcv = $"{vowel} {cv}";
            return HasOto(vcv, tone) ? vcv : cv;
        }
    }
}
