using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Korean C+V Phonemizer", "KO C+V", "SODAsoo07", language: "KO")]
    public class KoreanCPlusVPhonemizer : BaseKoreanPhonemizer {
        private const int HangulSyllableStart = 0xAC00;
        private const int HangulSyllableEnd = 0xD7A3;

        private static readonly char[] InitialJamoOrder = {
            'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ',
            'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ',
        };

        private static readonly char[] MedialJamoOrder = {
            'ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ',
            'ㅙ', 'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ',
        };

        private static readonly string[] FinalJamoOrder = {
            "ㄱ", "ㄲ", "ㄳ", "ㄴ", "ㄵ", "ㄶ", "ㄷ", "ㄹ", "ㄺ",
            "ㄻ", "ㄼ", "ㄽ", "ㄾ", "ㄿ", "ㅀ", "ㅁ", "ㅂ", "ㅄ",
            "ㅅ", "ㅆ", "ㅇ", "ㅈ", "ㅊ", "ㅋ", "ㅌ", "ㅍ", "ㅎ",
        };

        private static readonly Dictionary<char, string> InitialConsonants = new() {
            ['ㄱ'] = "- g", ['ㄴ'] = "- n", ['ㄷ'] = "- d", ['ㄹ'] = "- r", ['ㅁ'] = "- m",
            ['ㅂ'] = "- b", ['ㅅ'] = "- s", ['ㅇ'] = "-", ['ㅈ'] = "- j", ['ㅊ'] = "- ch",
            ['ㅋ'] = "- k", ['ㅌ'] = "- t", ['ㅍ'] = "- p", ['ㅎ'] = "- h",
            ['ㄲ'] = "- kk", ['ㄸ'] = "- tt", ['ㅃ'] = "- pp", ['ㅆ'] = "- ss", ['ㅉ'] = "- jj",
        };

        private static readonly Dictionary<char, string> Vowels = new() {
            ['ㅏ'] = "- a", ['ㅓ'] = "- eo", ['ㅗ'] = "- o", ['ㅜ'] = "- u",
            ['ㅡ'] = "- eu", ['ㅣ'] = "- i", ['ㅔ'] = "- e", ['ㅐ'] = "- e",
        };

        private static readonly Dictionary<char, string> Diphthongs = new() {
            ['ㅑ'] = "- ya", ['ㅕ'] = "- yeo", ['ㅛ'] = "- yo", ['ㅠ'] = "- yu",
            ['ㅒ'] = "- yae", ['ㅖ'] = "- ye", ['ㅘ'] = "- wa", ['ㅙ'] = "- we",
            ['ㅚ'] = "- we", ['ㅝ'] = "- wo", ['ㅞ'] = "- we", ['ㅟ'] = "- wi", ['ㅢ'] = "- ui",
        };

        // Includes complex final consonants (겹받침).
        private static readonly Dictionary<string, string> FinalConsonantsOnly = new(StringComparer.Ordinal) {
            ["ㄱ"] = "K", ["ㄲ"] = "K", ["ㄳ"] = "K", ["ㅋ"] = "K", ["ㄺ"] = "K",
            ["ㄴ"] = "N", ["ㄵ"] = "N", ["ㄶ"] = "N",
            ["ㄷ"] = "T", ["ㅅ"] = "T", ["ㅆ"] = "T", ["ㅈ"] = "T", ["ㅊ"] = "T", ["ㅌ"] = "T", ["ㅎ"] = "T",
            ["ㄹ"] = "L", ["ㄼ"] = "L", ["ㄽ"] = "L", ["ㄾ"] = "L", ["ㅀ"] = "L",
            ["ㅁ"] = "M", ["ㄻ"] = "M",
            ["ㅂ"] = "P", ["ㅄ"] = "P", ["ㅍ"] = "P", ["ㄿ"] = "P",
            ["ㅇ"] = "NG",
        };

        public override Result ConvertPhonemes(
            Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            if (notes.Length == 0) {
                return new Result { phonemes = Array.Empty<Phoneme>() };
            }

            var note = notes[0];
            var lyric = note.lyric?.Trim() ?? string.Empty;
            int totalDuration = notes.Sum(n => n.duration);

            if (lyric.StartsWith("+", StringComparison.Ordinal)) {
                return new Result { phonemes = Array.Empty<Phoneme>() };
            }

            var decomposed = DecomposeHangul(lyric);
            if (decomposed.Count == 0) {
                return new Result {
                    phonemes = new[] {
                        new Phoneme { index = 0, phoneme = ResolveMappedAlias(lyric, note, 0), position = 0 },
                    },
                };
            }

            var rawPhonemes = new List<Phoneme>();
            int syllableCount = decomposed.Count;
            for (int i = 0; i < syllableCount; i++) {
                var syllable = decomposed[i];
                int syllableStart = totalDuration * i / syllableCount;
                int syllableEnd = totalDuration * (i + 1) / syllableCount;
                int syllableDuration = Math.Max(1, syllableEnd - syllableStart);
                rawPhonemes.AddRange(ProcessSyllable(syllable, syllableDuration, syllableStart));
            }

            var mappedPhonemes = new List<Phoneme>(rawPhonemes.Count);
            for (int i = 0; i < rawPhonemes.Count; i++) {
                var phoneme = rawPhonemes[i];
                mappedPhonemes.Add(new Phoneme {
                    index = i,
                    phoneme = ResolveMappedAlias(phoneme.phoneme, note, i),
                    position = phoneme.position,
                });
            }

            return new Result { phonemes = mappedPhonemes.ToArray() };
        }

        public override Result GenerateEndSound(
            Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            return ConvertPhonemes(notes, prev, next, prevNeighbour, nextNeighbour, prevNeighbours);
        }

        private List<HangulSyllable> DecomposeHangul(string text) {
            var result = new List<HangulSyllable>();
            foreach (char c in text) {
                if (c < HangulSyllableStart || c > HangulSyllableEnd) {
                    continue;
                }
                int unicode = c - HangulSyllableStart;
                int initial = unicode / (MedialJamoOrder.Length * 28);
                int medial = (unicode % (MedialJamoOrder.Length * 28)) / 28;
                int final = unicode % 28;

                result.Add(new HangulSyllable {
                    Initial = GetInitialConsonant(initial),
                    Medial = GetMedialVowel(medial),
                    Final = final > 0 ? GetFinalConsonant(final - 1) : null,
                });
            }
            return result;
        }

        private List<Phoneme> ProcessSyllable(HangulSyllable syllable, int syllableDuration, int syllableOffset) {
            var phonemes = new List<Phoneme>();

            int consonantDuration = 25;
            if (syllable.Initial is "- n" or "- r" or "- m") {
                consonantDuration = 20;
            }

            if (!string.IsNullOrEmpty(syllable.Initial) && syllable.Initial != "-") {
                phonemes.Add(new Phoneme { phoneme = syllable.Initial, position = syllableOffset });
                if (!string.IsNullOrEmpty(syllable.Medial)) {
                    var vowelOnly = syllable.Medial.Replace("- ", "");
                    int vowelPosition = syllableOffset + Math.Min(consonantDuration, Math.Max(0, syllableDuration - 1));
                    phonemes.Add(new Phoneme { phoneme = vowelOnly, position = vowelPosition });
                }
            } else if (!string.IsNullOrEmpty(syllable.Medial)) {
                phonemes.Add(new Phoneme { phoneme = syllable.Medial, position = syllableOffset });
            }

            if (!string.IsNullOrEmpty(syllable.Final)) {
                string finalPhoneme = FinalConsonantsOnly.TryGetValue(syllable.Final, out var mappedFinal)
                    ? mappedFinal
                    : syllable.Final;
                int finalDuration = finalPhoneme switch {
                    "M" or "L" or "NG" => 20,
                    "N" => 13,
                    _ => 40,
                };
                int finalPosition = syllableOffset + Math.Max(0, syllableDuration - finalDuration);
                phonemes.Add(new Phoneme { phoneme = finalPhoneme, position = finalPosition });
            }

            return phonemes;
        }

        private static string GetInitialConsonant(int index) {
            if (index < 0 || index >= InitialJamoOrder.Length) {
                return "";
            }
            char initial = InitialJamoOrder[index];
            if (initial == 'ㅇ') {
                return "";
            }
            return InitialConsonants.TryGetValue(initial, out var value) ? value : "";
        }

        private static string GetMedialVowel(int index) {
            if (index < 0 || index >= MedialJamoOrder.Length) {
                return "";
            }
            char vowel = MedialJamoOrder[index];
            if (Diphthongs.TryGetValue(vowel, out var diphthong)) {
                return diphthong;
            }
            return Vowels.TryGetValue(vowel, out var plainVowel) ? plainVowel : "";
        }

        private static string? GetFinalConsonant(int index) {
            if (index < 0 || index >= FinalJamoOrder.Length) {
                return null;
            }
            return FinalJamoOrder[index];
        }

        // Applies alt / color / tone-shift and prefix.map (prefix/suffix) through TryGetMappedOto.
        private string ResolveMappedAlias(string phoneme, Note note, int phonemeIndex) {
            if (string.IsNullOrWhiteSpace(phoneme) || singer == null) {
                return phoneme;
            }

            var attr = note.phonemeAttributes?.FirstOrDefault(a => a.index == phonemeIndex) ?? default;
            string alt = attr.alternate?.ToString() ?? string.Empty;
            string color = attr.voiceColor ?? string.Empty;
            int shiftedTone = note.tone + attr.toneShift;

            if (singer.TryGetMappedOto(phoneme + alt, shiftedTone, color, out var mappedWithAlt)) {
                return mappedWithAlt.Alias;
            }
            if (singer.TryGetMappedOto(phoneme, shiftedTone, color, out var mapped)) {
                return mapped.Alias;
            }

            if (attr.toneShift != 0) {
                if (singer.TryGetMappedOto(phoneme + alt, note.tone, color, out mappedWithAlt)) {
                    return mappedWithAlt.Alias;
                }
                if (singer.TryGetMappedOto(phoneme, note.tone, color, out mapped)) {
                    return mapped.Alias;
                }
            }

            if (singer.TryGetMappedOto(phoneme + alt, shiftedTone, out mappedWithAlt)) {
                return mappedWithAlt.Alias;
            }
            if (singer.TryGetMappedOto(phoneme, shiftedTone, out mapped)) {
                return mapped.Alias;
            }

            return phoneme;
        }

        private sealed class HangulSyllable {
            public string Initial { get; set; } = string.Empty;
            public string Medial { get; set; } = string.Empty;
            public string? Final { get; set; }
        }
    }
}
