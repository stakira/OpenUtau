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
            "b ch d dh f g h j k l m n ng p r s sh t th v w y z zh".Split();
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

        protected Dictionary<string, string> StartingConsonant => new Dictionary<string, string> {
            { "b", "b" },
            { "ch", "ch" },
            { "d", "d" },
            { "dh", "d" },
            { "f", "f" },
            { "g", "g" },
            { "h", "h" },
            { "j", "j" },
            { "k", "k" },
            { "l", "r" },
            { "m", "m" },
            { "n", "n" },
            { "ng", "n" },
            { "p", "p" },
            { "r", "rr" },
            { "s", "s" },
            { "sh", "sh" },
            { "t", "t" },
            { "th", "s" },
            { "v", "f" },
            { "w", "w" },
            { "y", "y" },
            { "z", "z" },
            { "zh", "sh" },
        };

        protected Dictionary<string, string> SoloConsonant => new Dictionary<string, string> {
            { "b", "ぶ" },
            { "ch", "ちゅ" },
            { "d", "ど" },
            { "dh", "ず" },
            { "f", "ふ" },
            { "g", "ぐ" },
            { "h", "ほ" },
            { "j", "じゅ" },
            { "k", "く" },
            { "l", "う" },
            { "m", "む" },
            { "n", "ん" },
            { "ng", "ん" },
            { "p", "ぷ" },
            { "r", "う" },
            { "s", "す" },
            { "sh", "しゅ" },
            { "t", "と" },
            { "th", "す" },
            { "v", "ふ" },
            { "w", "う" },
            { "y", "い" },
            { "z", "ず" },
            { "zh", "しゅ" },
        };

        protected override string ReadDictionary(string filename) {
            return Core.Api.Resources.cmudict_0_7b;
        }

        protected override List<string> ProcessEnding(Ending ending) {
            var prevV = ending.prevV;
            var cc = ending.cc;
            var phonemes = new List<string>();

            if (prevV.Length == 2) {
                var newCC = new List<string>();
                newCC.Add(prevV[1].ToString());
                newCC.AddRange(cc);
                cc = newCC.ToArray();
            }

            foreach (var symbol in cc) {
                phonemes.Add(SoloConsonant[symbol]);
            }
            return phonemes;
        }

        protected override List<string> ProcessSyllable(Syllable syllable) {
            var prevV = syllable.prevV;
            var cc = syllable.cc;
            var v = syllable.v;
            var phonemes = new List<string>();

            if (prevV.Length == 2) {
                var newCC = new List<string>();
                newCC.Add(prevV[1].ToString());
                newCC.AddRange(cc);
                cc = newCC.ToArray();
            }
            
            if (cc.Length > 0) {
                var finalCons = cc[cc.Length - 1];
                if (cc.Length > 1) {
                    for (var i = 0; i < cc.Length - 1; i++) {
                        phonemes.Add(SoloConsonant[cc[i]]);
                    }
                }
                phonemes.AddRange(ConvertPhoneme(StartingConsonant[finalCons], v));
            }
            else {
                phonemes.AddRange(ConvertPhoneme("",v));
            }

            return phonemes;
        }

        protected Dictionary<string, string> AltCv => new Dictionary<string, string> {
            {"yi", "i" },
            {"wu", "u" },
            {"wo", "ulo" }, //use うぉ instead of を
            {"rra", "wa" },
            {"rri", "wi" },
            {"rru", "ru" },
            {"rre", "we" },
            {"rro", "wo" }, //use うぉ instead of を
        };
        protected Dictionary<string, string[]> ExtraCv => new Dictionary<string, string[]> {
            {"si", new [] { "se", "i" } },
            {"she", new [] { "si", "e" } },
            {"zi", new [] { "ze", "i" } },
            {"je", new [] { "ji", "e" } },
            {"ti", new [] { "te", "i" } },
            {"tu", new [] { "to", "u" } },
            {"che", new [] { "chi", "e" } },
            {"tsa", new [] { "tsu", "a" } },
            {"tsi", new [] { "tsu", "i" } },
            {"tse", new [] { "tsu", "e" } },
            {"tso", new [] { "tsu", "o" } },
            {"di", new [] { "de", "i" } },
            {"du", new [] { "do", "u" } },
            {"hu", new [] { "ho", "u" } },
            {"fa", new [] { "fu", "a" } },
            {"fi", new [] { "fu", "i" } },
            {"fe", new [] { "fu", "e" } },
            {"fo", new [] { "fu", "o" } },
            {"ye", new [] { "i", "e" } },
            {"wi", new [] { "u", "i" } },
            {"we", new [] { "u", "e" } },
        };

        protected List<string> ConvertPhoneme(string consonant, string vowel) {
            var romaji = new List<string>();

            if (vowel.Length == 2) {
                vowel = vowel[0].ToString();
            }

            var cv = $"{consonant}{vowel}";
            if (AltCv.ContainsKey(cv)) {
                cv = AltCv[cv];
            }
            if (ExtraCv.ContainsKey(cv)) {
                romaji.AddRange(ExtraCv[cv]);
            } else {
                romaji.Add(cv);
            }

            var hiragana = new List<string>();
            foreach (var item in romaji) {
                hiragana.Add(WanaKana.ToHiragana(item));
            }

            return hiragana;
        }
    }
}
