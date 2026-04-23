using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Classic;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
//using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Japanese presamp Phonemizer", "JA VCV & CVVC", "Maiko", language: "JA")]
    public class JapanesePresampPhonemizer : Phonemizer {

        // CV, VCV, CVVCを含むすべての日本語VBをサポートする予定です。Will support all Japanese VBs including CV, VCV, CVVC
        // 基本的な仕様はpresampに準拠します。Basic behavior conforms to presamp
        // 歌詞に書かれた"強"などの表情は非対応。VoiceColorでやってもらいます。Append suffixes such as "強" written in the lyrics are not supported
        // 喉切り"・"はpresamp.iniに書いてなくても動くようなんとかします。I'll try to make it work even if "・" are not written in presamp.ini
        // Supporting: [VOWEL][CONSONANT][PRIORITY][REPLACE][ALIAS(VCPAD,VCVPAD)]
        // Partial supporting: [NUM][APPEND][PITCH] -> Using to exclude useless characters in lyrics

        private USinger singer;
        private Presamp presamp;
        private UProject project;
        private UTrack track;

        // in case voicebank is missing certain symbols
        static readonly string[] substitution = new string[] {
            "ty,ch,ts=t", "j,dy=d", "gy=g", "ky=k", "py=p", "ny=n", "ry=r", "my=m", "hy,f=h", "by,v=b", "dz=z", "l=r", "ly=l"
        };

        static readonly Dictionary<string, string> substituteLookup;

        static JapanesePresampPhonemizer() {
            substituteLookup = substitution.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[0].Split(',').Select(orig => (orig, parts[1]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
        }

        public override void SetUp(Note[][] groups, UProject project, UTrack track) {
            this.project = project;
            this.track = track;
        }

        public override void SetSinger(USinger singer) {
            if (this.singer == singer) {
                return;
            }
            this.singer = singer;
            if (this.singer == null) {
                return;
            }

            presamp = new Presamp();
            presamp.ReadPresampIni(singer.Location, singer.TextFileEncoding);
        }

        // Extracts the raw text (Hint takes priority over Lyric)
        private string GetRawLyric(Note note) {
            if (!string.IsNullOrEmpty(note.phoneticHint)) {
                return note.phoneticHint.Normalize();
            }
            return note.lyric.Normalize();
        }

        // Helper method to multisyllable lyrics into raw syllables
        private string[] SplitSyllables(string lyric) {
            if (lyric.Contains(" ") || lyric == "+") {
                return lyric.Split(new char[] { ' ', '+' }, StringSplitOptions.RemoveEmptyEntries);
            }

            // Group prefixes (like っ, ー, -) directly with the following syllable
            var pattern = @"[ーっッ・-]*[ぁ-んァ-ン][ゃゅょャュョぁぃぅぇぉァィゥェォ]?|[ーっッ・-]*[bcdfghjklmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ]*[aeiouAEIOU]|[nN](?![aeiouAEIOU])|[ーっッ・-]";
            var matches = Regex.Matches(lyric, pattern);
            
            if (matches.Count > 0) {
                return matches.Cast<Match>().Select(m => m.Value).ToArray();
            }

            return new string[] { lyric };
        }

        // Helper method to combine raw dictionary output (e.g., "g", "a", "k", "u") into proper syllables ("ga", "ku")
        private string[] CombineRomajiPhonemes(string[] phonemes) {
            var result = new List<string>();
            int i = 0;
            while (i < phonemes.Length) {
                string current = phonemes[i];
                
                // If it is a consonant or 'n'
                if (Regex.IsMatch(current, "^[bcdfghjklmpqrstvwxyzBCDFGHJKLMPQRSTVWXYZ]+$") || current.ToLower() == "n") {
                    string combined = current;
                    int j = i + 1;
                    
                    if (current.ToLower() == "n") {
                        if (j < phonemes.Length && Regex.IsMatch(phonemes[j], "^[aiueoAIUEOyY]$")) {
                            // Valid follow-up for n (e.g., n + a = na, n + y = nya), continue combining
                        } else {
                            // Standalone 'n'
                            result.Add(current);
                            i++;
                            continue;
                        }
                    }

                    // Look ahead and accumulate trailing consonants (like 'y' in 'kya')
                    while (j < phonemes.Length && Regex.IsMatch(phonemes[j], "^[bcdfghjklmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ]+$")) {
                        combined += phonemes[j];
                        j++;
                    }
                    // Grab the final vowel to complete the syllable
                    if (j < phonemes.Length && Regex.IsMatch(phonemes[j], "^[aiueoAIUEO]$")) {
                        combined += phonemes[j];
                        j++;
                    }
                    result.Add(combined);
                    i = j;
                } else {
                    result.Add(current);
                    i++;
                }
            }
            return result.ToArray();
        }

        // Applies presamp.ini [REPLACE] rules on a per-syllable basis
        private string ApplyPresampReplace(string symbol) {
            if (presamp.Replace != null) {
                foreach (var pair in presamp.Replace) {
                    if (pair.Key == symbol) {
                        return pair.Value;
                    }
                }
            }
            return symbol;
        }

        // Checks if a phonetic symbol actually has a matching recording in the voicebank
        private bool IsSymbolSupported(string symbol, Note note) {
            if (singer == null) return false;
            var attr = note.phonemeAttributes?.FirstOrDefault(a => a.index == 0) ?? default;
            string color = attr.voiceColor ?? string.Empty;
            int tone = note.tone + attr.toneShift;
            string alt = attr.alternate?.ToString() ?? "";

            var vcvpad = string.IsNullOrEmpty(presamp.AliasRules?.VCVPAD) ? " " : presamp.AliasRules.VCVPAD;
            var vcpad = string.IsNullOrEmpty(presamp.AliasRules?.VCPAD) ? "" : presamp.AliasRules.VCPAD;

            var tests = new List<string> {
                symbol,
                $"-{vcvpad}{symbol}",
                $"-{vcpad}{symbol}",
                symbol + "・",
                $"-{vcvpad}{symbol}・"
            };

            foreach (var t in tests) {
                if (singer.TryGetMappedOto(t + alt, tone, color, out _) ||
                    singer.TryGetMappedOto(t, tone, color, out _)) {
                    return true;
                }
            }
            return false;
        }

        private string[] GetSymbols(Note note) {
            var rawLyric = GetRawLyric(note);
            if (string.IsNullOrEmpty(rawLyric)) return new string[0];

            string[] rawSymbols = SplitSyllables(rawLyric);
            rawSymbols = CombineRomajiPhonemes(rawSymbols);
            
            var symbols = new List<string>();
            foreach (var sym in rawSymbols) {
                string replaced = ApplyPresampReplace(sym);
                
                // Smart Fallback: If Hiragana conversion happened, check if the voicebank actually supports it
                if (replaced != sym) {
                    if (!IsSymbolSupported(replaced, note) && IsSymbolSupported(sym, note)) {
                        symbols.Add(sym); // VB has no Hiragana oto, fall back to Romaji
                        continue;
                    }
                }
                symbols.Add(replaced);
            }

            var symbolsArr = symbols.ToArray();

            // Sokuon Prefix Fix
            if (symbolsArr.Length > 1 && symbolsArr[0] == "っ") {
                symbolsArr = symbolsArr.Skip(1).ToArray();
            }

            return symbolsArr;
        }

        // Helper method to dynamically apply P flag to consonant phonemes (- C, C, C -)
        private Phoneme ApplyCFlags(Phoneme phoneme, string alias) {
            bool isConsonant = false;
            
            // Identify if the alias is a standalone consonant
            string baseAlias = alias.TrimStart('-', '*', '・', ' ');
            if (presamp.PhonemeList.TryGetValue(baseAlias, out var ph)) {
                isConsonant = !ph.HasVowel && ph.HasConsonant;
            } else if (presamp.PhonemeList.TryGetValue(alias, out var ph2)) {
                isConsonant = !ph2.HasVowel && ph2.HasConsonant;
            } else {
                // Fallback check: if it contains no vowels, it acts as a consonant phoneme
                isConsonant = !Regex.IsMatch(alias, "[aiueoAIUEOあいうえおぁぃぅぇぉんンNn]");
            }

            if (isConsonant) {
                int pValue = 0; // Default P flag value is 0
                
                // Override default if explicitly set in presamp.ini
                if (!string.IsNullOrEmpty(presamp.CFlags)) {
                    var match = Regex.Match(presamp.CFlags, @"[pP](\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int val)) {
                        pValue = val;
                    }
                }

                if (phoneme.expressions == null) {
                    phoneme.expressions = new List<PhonemeExpression>();
                }
                
                // Apply OpenUtau's built-in NORM expression
                phoneme.expressions.Add(new PhonemeExpression() { abbr = Core.Format.Ustx.NORM, value = pValue });
            }

            return phoneme;
        }

        // Extracts the merged attribute context from the specific note the syllable lands on
        private PhonemeAttributes GetMergedAttribute(Note[] notes, int noteIndex, int pIndex) {
            Note mainNote = notes[0];
            Note currentNote = notes[noteIndex];

            var attr = mainNote.phonemeAttributes?.FirstOrDefault(a => a.index == pIndex) ?? default;
            var attrCurr = currentNote.phonemeAttributes?.FirstOrDefault(a => a.index == 0) ?? default;
            var attrMain = mainNote.phonemeAttributes?.FirstOrDefault(a => a.index == 0) ?? default;

            return new PhonemeAttributes {
                index = pIndex,
                voiceColor = attr.voiceColor ?? attrCurr.voiceColor ?? attrMain.voiceColor,
                alternate = attr.alternate ?? attrCurr.alternate ?? attrMain.alternate,
                toneShift = attr.toneShift != 0 ? attr.toneShift : (attrCurr.toneShift != 0 ? attrCurr.toneShift : attrMain.toneShift),
                consonantStretchRatio = attr.consonantStretchRatio ?? attrCurr.consonantStretchRatio ?? attrMain.consonantStretchRatio
            };
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var result = new List<Phoneme>();
            var note = notes[0];
            
            string[] symbols = GetSymbols(note);
            if (symbols.Length == 0) {
                return new Result { phonemes = new Phoneme[0] };
            }

            // Fallback for presamp.ini values
            var vcvpad = string.IsNullOrEmpty(presamp.AliasRules?.VCVPAD) ? " " : presamp.AliasRules.VCVPAD;
            var vcpad = string.IsNullOrEmpty(presamp.AliasRules?.VCPAD) ? "" : presamp.AliasRules.VCPAD;

            int totalDuration = notes.Sum(n => n.duration);

            string prevLyric = "";
            string prevAlias = "";
            if (prevNeighbour != null) {
                var prevSymbols = GetSymbols(prevNeighbour.Value);
                if (prevSymbols.Length > 0) {
                    prevLyric = prevSymbols[^1];
                    prevAlias = ParseAliasFromLyric(prevLyric);
                }
            }

            int lastSyllablePosition = 0; 

            for (int i = 0; i < symbols.Length; i++) {
                // Avoid OpenUtau's reserved VC (1) and -C (2) index slots for subsequent syllables
                int pIndex = i == 0 ? 0 : i + 2;
                int noteIndex = Math.Min(i, notes.Length - 1); // Map the syllable to the correct tied note

                string currentSymbol = symbols[i];
                
                // Extract the base phonetic target for CVVC fallback (e.g., "っと" -> "と")
                string baseSymbol = currentSymbol.TrimStart('っ', 'ッ', 'ー', '-', '・');
                if (string.IsNullOrEmpty(baseSymbol)) baseSymbol = currentSymbol;

                string currentAlias = ParseAliasFromLyric(baseSymbol); 
                
                string initial = $"-{vcvpad}{currentSymbol}";
                string initialBase = $"-{vcvpad}{baseSymbol}";
                string cfLyric = $"*{vcpad}{currentSymbol}";
                string cfLyricBase = $"*{vcpad}{baseSymbol}";

                // Smart Vowel Extractor: Supports both Japanese characters and Romaji arrays natively via presamp definitions
                string vowelUpper = Regex.Match(baseSymbol, "[あいうえおんン]").Value;
                if (string.IsNullOrEmpty(vowelUpper)) {
                    if (presamp.PhonemeList.TryGetValue(currentAlias, out var ph) && ph.HasVowel) {
                        vowelUpper = ph.Vowel;
                    } else {
                        vowelUpper = baseSymbol;
                    }
                }

                var glottalCVtests = new List<string> { 
                    $"・{vcpad}{vowelUpper}", $"・{vowelUpper}", $"{vowelUpper}・", 
                    $"-{vcvpad}{vowelUpper}・", $"-{vcvpad}{vowelUpper}", 
                    initial, currentSymbol, initialBase, baseSymbol, vowelUpper 
                };

                bool preCFlag = false;
                string convertedLyric = currentSymbol;

                // Convert phoneme checking attributes relative to dynamic index
                if (i == 0 && !string.IsNullOrEmpty(note.phoneticHint) && symbols.Length == 1) { 
                    var tests = new List<string> { currentSymbol, baseSymbol };
                    if (checkOtoUntilHit(tests, notes, noteIndex, pIndex, out var oto)) {
                        convertedLyric = oto.Alias;
                    }
                } else if (i == 0 && prevNeighbour == null) { // beginning of phrase
                    preCFlag = true;
                    if (currentSymbol.Contains("・")) {
                        var tests = new List<string> { $"-{vcvpad}{vowelUpper}・", $"・{vcpad}{vowelUpper}", $"{vowelUpper}・", $"・{vowelUpper}", $"-{vcvpad}{vowelUpper}", initial, currentSymbol, initialBase, baseSymbol };
                        if (checkOtoUntilHit(tests, notes, noteIndex, pIndex, out var oto1)) {
                            convertedLyric = oto1.Alias;
                        }
                    } else {
                        var tests = new List<string> { initial, currentSymbol, initialBase, baseSymbol };
                        if (checkOtoUntilHit(tests, notes, noteIndex, pIndex, out var oto)) {
                            convertedLyric = oto.Alias;
                        }
                    }
                } else { // middle of phrase
                    string vcGlottalStop = "[aiueonN]" + vcpad + "・$"; // [a ・]
                    if (prevLyric == "・" || Regex.IsMatch(prevLyric, vcGlottalStop)) {
                        if (checkOtoUntilHit(glottalCVtests, notes, noteIndex, pIndex, out var oto)) {
                            convertedLyric = oto.Alias;
                        }
                    } else if (prevLyric.Contains("っ")) {
                        var tests = new List<string> { currentSymbol, initial, baseSymbol, initialBase };
                        if (checkOtoUntilHit(tests, notes, noteIndex, pIndex, out var oto)) {
                            convertedLyric = oto.Alias;
                        }
                    } else if (presamp.PhonemeList.TryGetValue(prevAlias, out PresampPhoneme prevPhoneme)) {
                        if (currentSymbol.Contains("・")) {
                            var tests = new List<string>();
                            UOto oto;

                            if (Regex.IsMatch(currentSymbol, vcGlottalStop)) { 
                                tests = new List<string> { currentSymbol, baseSymbol };
                                if (checkOtoUntilHit(tests, notes, noteIndex, pIndex, out oto)) {
                                    convertedLyric = oto.Alias;
                                }
                            } else if ((currentSymbol == "・" || baseSymbol == "・") && prevPhoneme.HasVowel) { 
                                var vc = $"{prevPhoneme.Vowel}{vcpad}{currentSymbol}";
                                var vcBase = $"{prevPhoneme.Vowel}{vcpad}{baseSymbol}";
                                tests = new List<string> { vc, currentSymbol, vcBase, baseSymbol };
                                if (checkOtoUntilHit(tests, notes, noteIndex, pIndex, out oto)) {
                                    convertedLyric = oto.Alias;
                                }
                            } else if (prevPhoneme.HasVowel) { 
                                tests.Add($"{prevPhoneme.Vowel}{vcvpad}{currentSymbol}");
                                if (currentSymbol != baseSymbol) {
                                    tests.Add($"{prevPhoneme.Vowel}{vcvpad}{baseSymbol}");
                                }
                                tests.Add($"{prevPhoneme.Vowel}{vcvpad}{vowelUpper}・");
                                tests.Add($"{prevPhoneme.Vowel}{vcvpad}・{vowelUpper}");
                            }
                            tests.AddRange(glottalCVtests); 
                            if (checkOtoUntilHit(tests, notes, noteIndex, pIndex, out oto)) { 
                                convertedLyric = oto.Alias;
                            }
                        } else if (presamp.PhonemeList.TryGetValue(currentAlias, out PresampPhoneme currentPhoneme) && currentPhoneme.IsPriority) {
                            var tests = new List<string> { currentSymbol, initial, baseSymbol, initialBase };
                            if (checkOtoUntilHit(tests, notes, noteIndex, pIndex, out var oto)) {
                                convertedLyric = oto.Alias;
                            }
                        } else if (prevPhoneme.HasVowel) {
                            string prevVow = prevPhoneme.Vowel;

                            if (currentSymbol == "っ" && (i < symbols.Length - 1 || nextNeighbour != null)) { 
                                string nextLyricForTu = "";
                                if (i < symbols.Length - 1) {
                                    nextLyricForTu = symbols[i + 1];
                                } else if (nextNeighbour != null) {
                                    var nextTuSymbols = GetSymbols(nextNeighbour.Value);
                                    if (nextTuSymbols.Length > 0) {
                                        nextLyricForTu = nextTuSymbols[0];
                                    }
                                }
                                string nextBaseForTu = nextLyricForTu.TrimStart('っ', 'ッ', 'ー', '-', '・');
                                if (string.IsNullOrEmpty(nextBaseForTu)) nextBaseForTu = nextLyricForTu;

                                string nextAliasForTu = ParseAliasFromLyric(nextBaseForTu);

                                var axtu1 = $"{prevVow}{vcvpad}{currentSymbol}"; 
                                var axtu2 = $"{prevVow}{vcpad}{currentSymbol}"; 
                                var tests2 = new List<string> { axtu1, axtu2, currentSymbol };
                                if (presamp.PhonemeList.TryGetValue(nextAliasForTu, out PresampPhoneme nextPhoneme) && nextPhoneme.HasConsonant) {
                                    tests2.Insert(2, $"{prevVow}{vcpad}{nextPhoneme.Consonant}"); // VC
                                }
                                if (checkOtoUntilHit(tests2, notes, noteIndex, pIndex, out var oto2)) {
                                    convertedLyric = oto2.Alias;
                                }
                            } else { 
                                var vcv = $"{prevVow}{vcvpad}{currentSymbol}";
                                var vc = $"{prevVow}{vcpad}{currentSymbol}";
                                var vcvBase = $"{prevVow}{vcvpad}{baseSymbol}";
                                var vcBase = $"{prevVow}{vcpad}{baseSymbol}";
                                var tests = new List<string> { vcv, vc, cfLyric, currentSymbol, vcvBase, vcBase, cfLyricBase, baseSymbol };
                                if (checkOtoUntilHit(tests, notes, noteIndex, pIndex, out var oto)) {
                                    convertedLyric = oto.Alias;
                                }
                            }
                        } else {
                            var tests = new List<string> { currentSymbol, initial, baseSymbol, initialBase };
                            if (checkOtoUntilHit(tests, notes, noteIndex, pIndex, out var oto)) {
                                convertedLyric = oto.Alias;
                            }
                        }
                    } else {
                        preCFlag = true;
                        var tests = new List<string> { initial, currentSymbol, initialBase, baseSymbol };
                        if (checkOtoUntilHit(tests, notes, noteIndex, pIndex, out var oto)) {
                            convertedLyric = oto.Alias;
                        }
                    }
                }

                int position;
                if (i < notes.Length) {
                    position = notes[i].position - notes[0].position;
                } else {
                    int lastNoteIndex = notes.Length - 1;
                    int remainingSyllables = symbols.Length - lastNoteIndex;
                    int durationPerExtra = notes[lastNoteIndex].duration / remainingSyllables;
                    position = (notes[lastNoteIndex].position - notes[0].position) + ((i - lastNoteIndex) * durationPerExtra);
                }

                // CVVC Internal VC Generation
                if (i > 0) {
                    bool needsInternalVC = false;
                    string internalVCPhoneme = null;
                    int? internalVCColor = null;

                    if (presamp.PhonemeList.TryGetValue(prevAlias, out PresampPhoneme prevPh) && prevPh.HasVowel) {
                        string prevVow = prevPh.Vowel;
                        
                        if (presamp.PhonemeList.TryGetValue(currentAlias, out PresampPhoneme currPh)) {
                            if (currPh.IsPriority) {
                                needsInternalVC = true;
                            } else {
                                bool isVCV = convertedLyric.StartsWith($"{prevVow}{vcvpad}") || convertedLyric.StartsWith($"{prevVow}{vcpad}");
                                if (!isVCV) {
                                    needsInternalVC = true;
                                }
                            }

                            if (needsInternalVC && currPh.HasConsonant) {
                                var vcPhonemes = new List<string>();
                                
                                // Prioritize explicitly mapping an "a っ" if the user supplied it
                                if (currentSymbol.StartsWith("っ") || currentSymbol.StartsWith("ッ")) {
                                    vcPhonemes.Add($"{prevVow}{vcpad}っ");
                                }
                                
                                vcPhonemes.Add($"{prevVow}{vcpad}{currPh.Consonant}");
                                if (substituteLookup.TryGetValue(currPh.Consonant ?? "", out var con)) {
                                    vcPhonemes.Add($"{prevVow}{vcpad}{con}");
                                }
                                
                                if (checkOtoUntilHit(vcPhonemes, notes, noteIndex, pIndex + 100, out var vcOto, out var color)) {
                                    internalVCPhoneme = vcOto.Alias;
                                    internalVCColor = color;
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(internalVCPhoneme)) {
                        int vcLength = 120;
                        int endTick = notes[0].position + position; 
                        var attr = GetMergedAttribute(notes, noteIndex, pIndex);
                        
                        if (singer.TryGetMappedOto(convertedLyric, notes[noteIndex].tone + attr.toneShift, attr.voiceColor, out var nextOto)) {
                            if (nextOto.Overlap < 0) {
                                vcLength = -timeAxis.MsToTickAt(-(nextOto.Preutter - nextOto.Overlap), endTick);
                            } else {
                                vcLength = -timeAxis.MsToTickAt(-nextOto.Preutter, endTick);
                            }
                        }
                        
                        int prevSyllableDuration = position - lastSyllablePosition;
                        vcLength = Convert.ToInt32(Math.Min(prevSyllableDuration / 2, Math.Max(30, vcLength * (attr.consonantStretchRatio ?? 1))));

                        var expressions = new List<PhonemeExpression>();
                        if (internalVCColor != null) {
                            expressions.Add(new PhonemeExpression() { abbr = Core.Format.Ustx.CLR, value = (int)internalVCColor });
                        }

                        var internalVC = new Phoneme() {
                            phoneme = internalVCPhoneme,
                            position = position - vcLength,
                            index = pIndex + 100, 
                            expressions = expressions.Count > 0 ? expressions : null
                        };
                        
                        internalVC = ApplyCFlags(internalVC, internalVCPhoneme);
                        result.Add(internalVC);
                    }
                }
                
                lastSyllablePosition = position;

                var currentPhonemeObj = new Phoneme() { phoneme = convertedLyric, position = position, index = pIndex };
                
                currentPhonemeObj = ApplyCFlags(currentPhonemeObj, convertedLyric);
                result.Add(currentPhonemeObj);

                prevLyric = currentSymbol;
                prevAlias = currentAlias;

                if (i == 0 && string.IsNullOrEmpty(note.phoneticHint)
                    && preCFlag
                    && !convertedLyric.Contains(vcvpad)
                    && presamp.PhonemeList.TryGetValue(currentAlias, out PresampPhoneme phoneme)
                    && phoneme.HasConsonant
                    && (presamp.Priorities == null || !presamp.Priorities.Contains(phoneme.Consonant))) {
                    if (checkOtoUntilHit(new List<string> { $"-{vcvpad}{phoneme.Consonant}" }, notes, 0, 2, out var cOto, out var color)
                        && checkOtoUntilHit(new List<string> { convertedLyric }, notes, 0, pIndex, out var oto)) {
                        int endTick = notes[^1].position + notes[^1].duration;
                        var attr = GetMergedAttribute(notes, 0, 0);
                        var cLength = Math.Max(30, -timeAxis.MsToTickAt(-oto.Preutter, endTick) * (attr.consonantStretchRatio ?? 1));

                        if (prevNeighbour != null) {
                            cLength = Math.Min(prevNeighbour.Value.duration / 2, cLength);
                        } else if (prev != null) {
                            cLength = Math.Min(note.position - prev.Value.position - prev.Value.duration, cLength);
                        }

                        var initC = new Phoneme() {
                            phoneme = cOto.Alias,
                            position = Convert.ToInt32(- cLength),
                            index = 2,
                            expressions = new List<PhonemeExpression>()
                        };
                        if (color != null) {
                            initC.expressions.Add(new PhonemeExpression() { abbr = Core.Format.Ustx.CLR, value = (int)color });
                        }
                        
                        initC = ApplyCFlags(initC, cOto.Alias);
                        result.Insert(0, initC);
                    }
                }
            }

            // Use the last syllable processed for the End-of-Phrase VC calculation
            string lastSymbol = symbols[^1];
            string lastBase = lastSymbol.TrimStart('っ', 'ッ', 'ー', '-', '・');
            if (string.IsNullOrEmpty(lastBase)) lastBase = lastSymbol;
            string lastAlias = ParseAliasFromLyric(lastBase);

            if (nextNeighbour != null && string.IsNullOrEmpty(nextNeighbour.Value.phoneticHint)) {
                if (TickToMs(totalDuration) < 100 && presamp.MustVC == false) {
                    return new Result { phonemes = result.ToArray() };
                }

                string nextLyric = "";
                string[] nextSymbols = GetSymbols(nextNeighbour.Value);
                
                foreach (var sym in nextSymbols) {
                    string baseSym = sym.TrimStart('っ', 'ッ', 'ー', '-', '・');
                    if (string.IsNullOrEmpty(baseSym)) baseSym = sym;

                    var nextPhAlias = ParseAliasFromLyric(baseSym);
                    if (presamp.PhonemeList.TryGetValue(nextPhAlias, out var p) && (p.HasVowel || p.HasConsonant || p.IsPriority)) {
                        nextLyric = sym;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(nextLyric) && nextSymbols.Length > 0) {
                    nextLyric = nextSymbols[0];
                }

                string nextBase = nextLyric.TrimStart('っ', 'ッ', 'ー', '-', '・');
                if (string.IsNullOrEmpty(nextBase)) nextBase = nextLyric;
                var nextAlias = ParseAliasFromLyric(nextBase); 

                string vcPhoneme = null;
                int? vcColorIndex = null;

                if (!presamp.PhonemeList.TryGetValue(lastAlias, out PresampPhoneme currentPhoneme) || !currentPhoneme.HasVowel) {
                    return new Result { phonemes = result.ToArray() };
                }
                var vowel = currentPhoneme.Vowel;

                // Smart Vowel Extractor for Final VC targets
                string vowelUpperNext = Regex.Match(nextBase, "[あいうえおんン]").Value;
                if (string.IsNullOrEmpty(vowelUpperNext)) {
                    if (presamp.PhonemeList.TryGetValue(nextAlias, out var ph) && ph.HasVowel) {
                        vowelUpperNext = ph.Vowel;
                    } else {
                        vowelUpperNext = nextBase;
                    }
                }

                if (Regex.IsMatch(nextBase, "[aiueonN]" + vcvpad) || Regex.IsMatch(nextBase, "[aiueonN]" + vcpad)) {
                    return new Result { phonemes = result.ToArray() };
                } else {
                    int lastNoteIndex = notes.Length - 1;
                    if (nextLyric.Contains("・")) { 
                        if (nextBase == "・") { 
                            return new Result { phonemes = result.ToArray() };
                        } else {
                            if (string.IsNullOrEmpty(vowelUpperNext)) {
                                return new Result { phonemes = result.ToArray() };
                            }

                            var tests = new List<string>();
                            tests.Add($"{vowel}{vcvpad}{nextLyric}");
                            tests.Add($"{vowel}{vcvpad}{nextBase}");
                            tests.Add($"{vowel}{vcvpad}{vowelUpperNext}・");
                            tests.Add($"{vowel}{vcvpad}・{vowelUpperNext}");
                            var nextGlottalCVtests = new List<string> { $"・{vcpad}{vowelUpperNext}", $"・{vowelUpperNext}", $"{vowelUpperNext}・", $"-{vcvpad}{vowelUpperNext}・", $"-{vcvpad}{vowelUpperNext}", $"-{vcvpad}{nextLyric}", nextLyric, nextBase, vowelUpperNext };
                            tests.AddRange(nextGlottalCVtests);
                            
                            if (checkOtoUntilHit(tests, new Note[] { nextNeighbour.Value }, 0, 0, out var oto1) && oto1.Alias.Contains($"{vowel}{vcvpad}")) {
                                return new Result { phonemes = result.ToArray() };
                            }

                            tests = new List<string> { $"{vowel}{vcpad}・" };
                            if (checkOtoUntilHit(tests, notes, lastNoteIndex, 1, out oto1, out var color)) {
                                vcPhoneme = oto1.Alias;
                                vcColorIndex = color;
                            } else {
                                return new Result { phonemes = result.ToArray() };
                            }
                        }
                    } else {

                        if (!presamp.PhonemeList.TryGetValue(nextAlias, out PresampPhoneme nextPhoneme) || !nextPhoneme.HasConsonant) {
                            return new Result { phonemes = result.ToArray() };
                        }
                        var consonant = nextPhoneme.Consonant;

                        if (!nextPhoneme.IsPriority) {
                            var nextVCV = $"{vowel}{vcvpad}{nextLyric}";
                            var nextVC = $"{vowel}{vcpad}{nextLyric}";
                            var nextVCVBase = $"{vowel}{vcvpad}{nextBase}";
                            var nextVCBase = $"{vowel}{vcpad}{nextBase}";
                            var tests = new List<string> { nextVCV, nextVC, nextLyric, nextVCVBase, nextVCBase, nextBase };
                            
                            if (checkOtoUntilHit(tests, new Note[] { nextNeighbour.Value }, 0, 0, out var oto1)
                                && (Regex.IsMatch(oto1.Alias, "[aiueonN]" + vcvpad) || Regex.IsMatch(oto1.Alias, "[aiueonN]" + vcpad))) {
                                return new Result { phonemes = result.ToArray() };
                            }
                        }

                        var vcPhonemes = new List<string>();
                        if (nextLyric.StartsWith("っ") || nextLyric.StartsWith("ッ")) {
                            vcPhonemes.Add($"{vowel}{vcpad}っ");
                        }
                        vcPhonemes.Add($"{vowel}{vcpad}{consonant}");
                        
                        if (substituteLookup.TryGetValue(consonant ?? string.Empty, out var con)) {
                            vcPhonemes.Add($"{vowel}{vcpad}{con}");
                        }
                        
                        if (checkOtoUntilHit(vcPhonemes, notes, lastNoteIndex, 1, out var oto, out var color)) {
                            vcPhoneme = oto.Alias;
                            vcColorIndex = color;
                        } else {
                            return new Result { phonemes = result.ToArray() };
                        }
                    }
                }
                if (!string.IsNullOrEmpty(vcPhoneme)) {
                    int vcLength = 120;
                    int endTick = notes[^1].position + notes[^1].duration;
                    var nextAttr = nextNeighbour.Value.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
                    
                    if (singer.TryGetMappedOto(nextLyric, nextNeighbour.Value.tone + nextAttr.toneShift, nextAttr.voiceColor, out var nextOto)) {
                        if (nextOto.Overlap < 0) {
                            vcLength = -timeAxis.MsToTickAt(-(nextOto.Preutter - nextOto.Overlap), endTick);
                        } else {
                            vcLength = -timeAxis.MsToTickAt(-nextOto.Preutter, endTick);
                        }
                    }
                    
                    int lastSyllableDuration = totalDuration - lastSyllablePosition;
                    vcLength = Convert.ToInt32(Math.Min(lastSyllableDuration / 2, Math.Max(30, vcLength * (nextAttr.consonantStretchRatio ?? 1))));

                    var finalVC = new Phoneme() {
                        phoneme = vcPhoneme,
                        position = totalDuration - vcLength,
                        index = 1,
                        expressions = new List<PhonemeExpression>()
                    };
                    if (vcColorIndex != null) {
                        finalVC.expressions.Add(new PhonemeExpression() { abbr = Core.Format.Ustx.CLR, value = (int)vcColorIndex });
                    }
                    
                    finalVC = ApplyCFlags(finalVC, vcPhoneme);
                    result.Add(finalVC);
                }
            }

            return new Result { phonemes = result.ToArray() };
        }

        private bool checkOtoUntilHit(List<string> input, Note[] notes, int noteIndex, int pIndex, out UOto oto) {
            oto = default;
            var attr = GetMergedAttribute(notes, noteIndex, pIndex);
            string color = attr.voiceColor ?? string.Empty;
            string alt = attr.alternate?.ToString() ?? string.Empty; // Fixed string cast
            int tone = notes[noteIndex].tone + attr.toneShift;

            var otos = new List<UOto>();
            foreach (string test in input) {
                if (singer.TryGetMappedOto(test + alt, tone, color, out var otoAlt)) {
                    otos.Add(otoAlt);
                } else if (singer.TryGetMappedOto(test, tone, color, out var otoCandidacy)) {
                    otos.Add(otoCandidacy);
                }
            }

            if (otos.Count > 0) {
                oto = otos.FirstOrDefault(o => o.IsColorMatch(color));
                if (oto == null) {
                    oto = otos.First();
                }
                return true;
            }
            return false;
        }
        
        private bool checkOtoUntilHit(List<string> input, Note[] notes, int noteIndex, int pIndex, out UOto oto, out int? colorIndex) {
            oto = default;
            colorIndex = null;
            var attr = GetMergedAttribute(notes, noteIndex, pIndex);
            string color = attr.voiceColor ?? string.Empty;
            string alt = attr.alternate?.ToString() ?? string.Empty;
            int tone = notes[noteIndex].tone + attr.toneShift;

            var otos = new List<UOto>();
            foreach (string test in input) {
                if (singer.TryGetMappedOto(test + alt, tone, color, out var otoAlt)) {
                    otos.Add(otoAlt);
                } else if (singer.TryGetMappedOto(test, tone, color, out var otoCandidacy)) {
                    otos.Add(otoCandidacy);
                }
            }

            if (otos.Count > 0) {
                oto = otos.FirstOrDefault(o => o.IsColorMatch(color));
                if (oto != null) {
                    if (track.VoiceColorExp.options.Contains(color)) {
                        colorIndex = Array.IndexOf(track.VoiceColorExp.options, color);
                    }
                    return true;
                } else if (pIndex != 1 && pIndex != 2) {
                    oto = otos.First();
                    return true;
                }
            }
            return false;
        }

        private string ParseAliasFromLyric(string lyric) {
            string alias = presamp.ParseAlias(lyric)[1]; 
            if (alias != "・" && alias.Contains("・")) {
                alias = alias.Replace("・", "");
            }
            return alias;
        }
    }
}