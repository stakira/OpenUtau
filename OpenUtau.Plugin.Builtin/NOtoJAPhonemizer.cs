using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.G2p;
using WanaKanaNet;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Norwegian to Japanese Phonemizer", "NO to JA", "LoaBeepo", language: "NO")]
    public class NOtoJAPhonemizer : SyllableBasedPhonemizer {
        protected override string[] GetVowels() => vowels;
        private static readonly string[] vowels =
            "a i u e o ai au ei oi".Split();

        protected override string[] GetConsonants() => consonants;
        private static readonly string[] consonants =
            "b ch d f g h j k kj l m n ng p r rs s sj skj t v w z".Split();

        protected override string GetDictionaryName() => null;

        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryPhonemesReplacement;
        private static readonly Dictionary<string, string> dictionaryPhonemesReplacement = new Dictionary<string, string>();

        protected override IG2p LoadBaseDictionary() => null;

        private Dictionary<string, string> StartingConsonant => startingConsonant;
        private static readonly Dictionary<string, string> startingConsonant = new Dictionary<string, string> {
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
            { "j", "y" },
            { "k", "k" },
            { "kj", "hy" },
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
            { "r", "r" },
            { "ry", "ry" },
            { "rs", "sh" },
            { "s", "s" },
            { "sh", "sh" },
            { "sj", "sh" },
            { "skj", "sh" },
            { "t", "t" },
            { "ts", "ts" },
            { "th", "s" },
            { "v", "v" },
            { "w", "w" },
            { "y", "y" },
            { "z", "z" },
            { "zh", "sh" },
        };

        private Dictionary<string, string> SoloConsonant => soloConsonant;
        private static readonly Dictionary<string, string> soloConsonant = new Dictionary<string, string> {
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
            { "j", "い" },
            { "k", "く" },
            { "kj", "ひ" },
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
            { "rs", "しゅ" },
            { "s", "す" },
            { "sh", "しゅ" },
            { "sj", "しゅ" },
            { "skj", "しゅ" },
            { "t", "と" },
            { "ts", "つ" },
            { "th", "す" },
            { "v", "ヴ" },
            { "w", "う" },
            { "y", "い" },
            { "z", "ず" },
            { "zh", "しゅ" },
        };

        private string[] SpecialClusters = "ky gy ny hy by py my ry ly".Split();

        private Dictionary<string, string> AltCv => altCv;
        private static readonly Dictionary<string, string> altCv = new Dictionary<string, string> {
            {"si", "shi" },
            {"zi", "ji" },
            {"ti", "chi" },
            {"tu", "tsu" },
            {"di", "ji" },
            {"du", "zu" },
            {"hu", "fu" },
            {"yi", "i" },
            {"wu", "u" },
            {"wo", "ulo" },
            {"rra", "wa" },
            {"rri", "wi" },
            {"rru", "ru" },
            {"rre", "we" },
            {"rro", "ulo" },
        };

        private Dictionary<string, string> ConditionalAlt => conditionalAlt;
        private static readonly Dictionary<string, string> conditionalAlt = new Dictionary<string, string> {
            {"ulo", "wo"},
            {"va", "fa"},
            {"vi", "fi"},
            {"vu", "fu"},
            {"ヴ", "ふ"},
            {"ve", "fe"},
            {"vo", "fo"},
        };

        private Dictionary<string, string[]> ExtraCv => extraCv;
        private static readonly Dictionary<string, string[]> extraCv = new Dictionary<string, string[]> {
            {"kye", new [] { "ki", "e" } },
            {"gye", new [] { "gi", "e" } },
            {"she", new [] { "shi", "e" } },
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

        private string[] affricates = "ch j".Split();

        protected override string[] GetSymbols(Note note) {
            string[] original = base.GetSymbols(note);
            string lyric = note.lyric;

            // Pass through tails / special symbols unchanged
            if (lyric == "R" || lyric == "-" || lyric == "br" || lyric == "Br" || lyric == "?" || lyric.StartsWith("<<")) {
                return original ?? new string[] { lyric };
            }

            string lowerLyric = lyric.ToLowerInvariant();
            var phonemes = new List<string>();
            int i = 0;

            while (i < lowerLyric.Length) {
                if (!char.IsLetter(lowerLyric[i])) {
                    i++;
                    continue;
                }

                // 3-letter clusters
                if (i + 2 < lowerLyric.Length) {
                    string tri = lowerLyric.Substring(i, 3);
                    if (tri == "skj") {
                        phonemes.Add("skj");
                        i += 3;
                        continue;
                    }
                }

                // 2-letter sequences
                if (i + 1 < lowerLyric.Length) {
                    string di = lowerLyric.Substring(i, 2);
                    if (di == "kj") { phonemes.Add("kj"); i += 2; continue; }
                    if (di == "sj") { phonemes.Add("sj"); i += 2; continue; }
                    if (di == "rs") { phonemes.Add("rs"); i += 2; continue; }
                    if (di == "ng") { phonemes.Add("ng"); i += 2; continue; }
                    if (di == "qu") { phonemes.Add("k"); phonemes.Add("v"); i += 2; continue; }
                    if (di == "ch") { phonemes.Add("ch"); i += 2; continue; }
                    if (di == "hj") { phonemes.Add("j"); i += 2; continue; }
                    // sk + front vowel -> sj (Norwegian retroflex rule)
                    if (di == "sk" && i + 2 < lowerLyric.Length && "eiæøy".Contains(lowerLyric[i + 2])) {
                        phonemes.Add("sj");
                        i += 2;
                        continue;
                    }
                    // diphthongs
                    if (di == "ai") { phonemes.Add("ai"); i += 2; continue; }
                    if (di == "au") { phonemes.Add("au"); i += 2; continue; }
                    if (di == "ei") { phonemes.Add("ei"); i += 2; continue; }
                    if (di == "oi") { phonemes.Add("oi"); i += 2; continue; }
                    if (di == "øy") { phonemes.Add("oi"); i += 2; continue; }
                    if (di == "oy") { phonemes.Add("oi"); i += 2; continue; }
                }

                char c = lowerLyric[i];
                char? next = i + 1 < lowerLyric.Length ? lowerLyric[i + 1] : (char?)null;

                // soft c before front vowels
                if (c == 'c' && next != null && "eiæøy".Contains(next.Value)) {
                    phonemes.Add("s");
                    i++;
                    continue;
                }
                // soft g before front vowels
                if (c == 'g' && next != null && "eiæøy".Contains(next.Value)) {
                    phonemes.Add("j");
                    i++;
                    continue;
                }

                switch (c) {
                    case 'a': phonemes.Add("a"); break;
                    case 'e': phonemes.Add("e"); break;
                    case 'i': phonemes.Add("i"); break;
                    case 'o': phonemes.Add("o"); break;
                    case 'u': phonemes.Add("u"); break;
                    case 'y': phonemes.Add("u"); break; // Norwegian y -> Japanese u (rounded approx)
                    case 'æ': phonemes.Add("a"); break;
                    case 'ø': phonemes.Add("o"); break;
                    case 'å': phonemes.Add("o"); break;
                    case 'b': phonemes.Add("b"); break;
                    case 'c': phonemes.Add("k"); break;
                    case 'd': phonemes.Add("d"); break;
                    case 'f': phonemes.Add("f"); break;
                    case 'g': phonemes.Add("g"); break;
                    case 'h': phonemes.Add("h"); break;
                    case 'j': phonemes.Add("j"); break;
                    case 'k': phonemes.Add("k"); break;
                    case 'l': phonemes.Add("l"); break;
                    case 'm': phonemes.Add("m"); break;
                    case 'n': phonemes.Add("n"); break;
                    case 'p': phonemes.Add("p"); break;
                    case 'q': phonemes.Add("k"); break;
                    case 'r': phonemes.Add("r"); break;
                    case 's': phonemes.Add("s"); break;
                    case 't': phonemes.Add("t"); break;
                    case 'v': phonemes.Add("v"); break;
                    case 'w': phonemes.Add("v"); break; // Norwegian w -> v
                    case 'x': phonemes.Add("k"); phonemes.Add("s"); break;
                    case 'z': phonemes.Add("z"); break;
                    default: break;
                }
                i++;
            }

            if (phonemes.Count == 0) {
                return original ?? new string[] { lyric };
            }
            return phonemes.ToArray();
        }

        protected override List<string> ProcessSyllable(Syllable syllable) {
            if (CanMakeAliasExtension(syllable)) {
                return new List<string> { null };
            }

            var prevV = syllable.prevV;
            var cc = syllable.cc;
            var v = syllable.v;
            var phonemes = new List<string>();
            var usingVC = false;

            if (prevV.Length == 0) {
                prevV = "-";
            }

            var adjustedCC = new List<string>();
            for (var i = 0; i < cc.Length; i++) {
                if (i == cc.Length - 1) {
                    adjustedCC.Add(cc[i]);
                } else {
                    if (cc[i] == cc[i + 1]) {
                        adjustedCC.Add(cc[i]);
                        i++;
                        continue;
                    }
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

            var finalCons = "";
            if (cc.Length > 0) {
                finalCons = cc[cc.Length - 1];

                var start = 0;
                (var hasVc, var vcPhonemes) = HasVc(prevV, cc[0], syllable.tone, cc.Length);
                usingVC = hasVc;
                phonemes.AddRange(vcPhonemes);

                if (usingVC) {
                    start = 1;
                }

                for (var i = start; i < cc.Length - 1; i++) {
                    var cons = SoloConsonant[cc[i]];
                    if (!usingVC) {
                        cons = TryVcv(prevV, cons, syllable.tone);
                    } else {
                        usingVC = false;
                    }
                    if (HasOto(cons, syllable.tone)) {
                        phonemes.Add(cons);
                    } else if (ConditionalAlt.ContainsKey(cons)) {
                        cons = ConditionalAlt[cons];
                        phonemes.Add(TryVcv(prevV, cons, syllable.tone));
                    }
                    prevV = WanaKana.ToRomaji(cons).Last<char>().ToString();
                }
            }

            var cv = $"{StartingConsonant[finalCons]}{v}";
            cv = AltCv.ContainsKey(cv) ? AltCv[cv] : cv;
            var hiragana = ToHiragana(cv);
            if (!usingVC) {
                hiragana = TryVcv(prevV, hiragana, syllable.vowelTone);
            } else {
                hiragana = FixCv(hiragana, syllable.vowelTone);
            }

            var split = false;
            if (HasOto(hiragana, syllable.vowelTone)) {
                phonemes.Add(hiragana);
            } else if (ConditionalAlt.ContainsKey(cv)) {
                cv = ConditionalAlt[cv];
                hiragana = TryVcv(prevV, ToHiragana(cv), syllable.vowelTone);
                if (HasOto(hiragana, syllable.vowelTone)) {
                    phonemes.Add(hiragana);
                } else {
                    split = true;
                }
            } else {
                split = true;
            }

            if (split && ExtraCv.ContainsKey(cv)) {
                var splitCv = ExtraCv[cv];
                for (var i = 0; i < splitCv.Length; i++) {
                    if (splitCv[i] != prevV) {
                        var converted = ToHiragana(splitCv[i]);
                        phonemes.Add(TryVcv(prevV, converted, syllable.vowelTone));
                        prevV = splitCv[i].Last<char>().ToString();
                    }
                }
            }

            return phonemes;
        }

        protected override List<string> ProcessEnding(Ending ending) {
            var prevV = ending.prevV;
            var cc = ending.cc;
            var phonemes = new List<string>();

            var adjustedCC = new List<string>();
            for (var i = 0; i < cc.Length; i++) {
                if (i == cc.Length - 1) {
                    adjustedCC.Add(cc[i]);
                } else {
                    if (cc[i] == cc[i + 1]) {
                        adjustedCC.Add(cc[i]);
                        i++;
                        continue;
                    }
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

            var usingVC = false;
            for (var i = 0; i < cc.Length; i++) {
                var symbol = cc[i];

                if (i == 0) {
                    (var hasVc, var vcPhonemes) = HasVc(prevV, symbol, ending.tone, cc.Length + 1);
                    usingVC = hasVc;
                    phonemes.AddRange(vcPhonemes);
                    if (usingVC) {
                        continue;
                    }
                }

                var solo = SoloConsonant[symbol];
                if (!usingVC) {
                    solo = TryVcv(prevV, solo, ending.tone);
                } else {
                    usingVC = false;
                    solo = FixCv(solo, ending.tone);
                }

                if (HasOto(solo, ending.tone)) {
                    phonemes.Add(solo);
                } else if (ConditionalAlt.ContainsKey(solo)) {
                    solo = ConditionalAlt[solo];
                    if (!usingVC) {
                        solo = TryVcv(prevV, solo, ending.tone);
                    } else {
                        solo = FixCv(solo, ending.tone);
                    }
                    phonemes.Add(solo);
                }

                if (solo.Contains("ん")) {
                    if (ending.IsEndingVCWithOneConsonant) {
                        TryAddPhoneme(phonemes, ending.tone, $"n R", $"n -", $"n-");
                    } else if (ending.IsEndingVCWithMoreThanOneConsonant && cc.Last() == "n" || cc.Last() == "ng") {
                        TryAddPhoneme(phonemes, ending.tone, $"n R", $"n -", $"n-");
                    }
                }

                prevV = WanaKana.ToRomaji(solo).Last<char>().ToString();
            }
            
            if (ending.IsEndingV) {
                TryAddPhoneme(phonemes, ending.tone, $"{prevV} R", $"{prevV} -", $"{prevV}-");
            }

            return phonemes;
        }

        private (bool, string[]) HasVc(string vowel, string cons, int tone, int cc) {
            if (vowel == "" || vowel == "-") {
                return (false, new string[0]);
            }

            var phonemes = new List<string>();
            cons = StartingConsonant.ContainsKey(cons) ? StartingConsonant[cons] : cons;

            var vc = $"{vowel} {cons}";
            var altVc = $"{vowel} {cons[0]}";
            
            if (HasOto(vc, tone)) {
                phonemes.Add(vc);
            } else if (HasOto(altVc, tone)) {
                phonemes.Add(altVc);
            } else {
                return (false, new string[0]);
            }

            if (affricates.Contains(cons) && cc > 1) {
                phonemes.Add(FixCv(SoloConsonant[cons], tone));
            }

            return (phonemes.Count > 0, phonemes.ToArray());
        }

        private string TryVcv(string vowel, string cv, int tone) {
            var vcv = $"{vowel} {cv}";
            return HasOto(vcv, tone) ? vcv : FixCv(cv, tone);
        }

        private string FixCv(string cv, int tone) {
            var alt = $"- {cv}";
            return HasOto(cv, tone) ? cv : HasOto(alt, tone) ? alt : cv;
        }

        private string ToHiragana(string romaji) {
            var hiragana = WanaKana.ToHiragana(romaji);
            hiragana = hiragana.Replace("ゔ", "ヴ");
            return hiragana;
        }
    }
}