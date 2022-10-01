using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Korean CVCCV Phonemizer", "KO CVCCV", "RYUUSEI", language:"KO")]
    public class KoreanCVCCVPhonemizer : Phonemizer {
        static readonly string initialConsonantsTable = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
        static readonly string vowelsTable = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
        static readonly string lastConsonantsTable = "　ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";

        static readonly ushort unicodeKoreanBase = 0xAC00;
        static readonly ushort unicodeKoreanLast = 0xD79F;

        static readonly string continuousinitialConsonantsTable = "ㄱㄴㄷㄹㅁㅂㅅㅇㅈㅎ";

        static readonly string cvvcInitialConsonantsTable = "ㅋㅌㅍㅊㄲㄸㅃㅉㅆ";

        static readonly string vccLastConsonantsTable = "ㄴㄵㅁㄹㄺㄻㄼㄽㄾㄿㅀㅇ";
        static readonly string vccSubInitialConsonantsTable = "ㄱㅋㄲㅅㅈㅌㄷㅆㅎㅊㅂㅍ";

        static readonly string vcc2LastConsonantsTable = "ㄱㄲㄳㅂㅄㅍㅌㅆ";
        static readonly string vcc2SubInitialConsonantsTable = "ㅅㅆ";
        

        private char[] SeparateHangul(char letter) {
            if (letter == 0) return new char[] { '　', '　', '　' };
            var u16 = Convert.ToUInt16(letter);

            if (u16 < unicodeKoreanBase || u16 > unicodeKoreanLast)
                return new char[] { letter };

            u16 -= unicodeKoreanBase;

            var i = u16 / (21 * 28);
            u16 %= 21 * 28;
            var v = u16 / 28;
            u16 %= 28;
            var l = u16;

            return new char[] { initialConsonantsTable[i], vowelsTable[v], lastConsonantsTable[l] };
        }

        // 초성
        static readonly string[] continuousinitialConsonants = new string[] {
            "g=ㄱ",
            "n=ㄴ",
            "d=ㄷ",
            "r=ㄹ",
            "m=ㅁ",
            "b=ㅂ",
            "s=ㅅ",
            "=ㅇ",
            "j=ㅈ",
            "ch=ㅊ",
            "k=ㅋ",
            "t=ㅌ",
            "p=ㅍ",
            "h=ㅎ",
            "gg=ㄲ",
            "dd=ㄸ",
            "bb=ㅃ",
            "ss=ㅆ",
            "jj=ㅉ",
        };

        // 일반 모음
        static readonly string[] vowels = new string[] {
            "a=ㅏ",
            "ya=ㅑ",
            "eo=ㅓ",
            "yeo=ㅕ",
            "o=ㅗ",
            "yo=ㅛ",
            "u=ㅜ",
            "yu=ㅠ",
            "eu=ㅡ",
            "i=ㅣ",
            "e=ㅔ,ㅐ",
            "ye=ㅖ,ㅒ",
            "eui=ㅢ",
            "wa=ㅘ",
            "weo=ㅝ",
            "we=ㅞ,ㅙ,ㅚ",
            "wi=ㅟ",
        };

        static readonly string[] subsequentVowels = new string[] {
            "a=ㅏ,ㅑ,ㅘ",
            "eo=ㅓ,ㅕ,ㅝ",
            "o=ㅗ,ㅛ",
            "u=ㅜ,ㅠ",
            "eu=ㅡ",
            "e=ㅔ,ㅐ,ㅞ,ㅙ,ㅚ,ㅖ,ㅒ",
            "i=ㅣ,ㅢ,ㅟ",
        };

        static readonly string[] noNextLastConsonants = new string[] {
            "K=ㄱ,ㅋ,ㄲ",
            "T=ㅅ,ㅈ,ㅌ,ㄷ,ㅆ,ㅎ,ㅊ",
            "P=ㅂ,ㅍ",
            "m=ㅁ",
            "n=ㄴ,ㄵ",
            "ng=ㅇ",
            "l=ㄹ,ㄺ,ㄻ,ㄼ,ㄽ,ㄾ,ㄿ,ㅀ"
        };

        static readonly string[] nextExistLastConsonants = new string[] {
            "k=ㅋ,ㄲ",
            "t=ㅌ,ㄸ,ㅊ,ㅉ",
            "p=ㅍ,ㅃ",
            "ss=ㅆ"
        };

        static readonly string[] nextExistSpecialLastConsonants = new string[] {
            "k=ㅋ,ㄲ",
            "t=ㅌ,ㄸ,ㅊ,ㅉ",
            "p=ㅍ,ㅃ",
            "s=ㅆ"
        };

        static readonly string[] nextExistVCCLastconsonants = new string[] {
            "k=ㄱ,ㄲ,ㄳ",
            "ss=ㅌ",
            "p=ㅂ,ㅄ,ㅍ"
        };

        static readonly string[] prevLastConsonantsExists = new string[] {
            "n=ㄴ,ㄵ",
            "m=ㅁ",
            "l=ㄹ,ㄺ,ㄻ,ㄼ,ㄽ,ㄾ,ㄿ,ㅀ",
            "ng=ㅇ"
        };

        static readonly Dictionary<string, string> initialConsonantLookup;
        static readonly Dictionary<string, string> vowelLookup;
        static readonly Dictionary<string, string> subsequentVowelLookup;
        static readonly Dictionary<string, string> noNextLastConsonantsLookup;
        static readonly Dictionary<string, string> nextExistLastLastConsonantsLookup;
        static readonly Dictionary<string, string> nextExistSpecialLastConsonantsLookup;
        static readonly Dictionary<string, string> nextExistSpecialCurrentLastConsonantsLookup;
        static readonly Dictionary<string, string> prevLastConsonantsExistsLookup;


        static KoreanCVCCVPhonemizer() {
            initialConsonantLookup = continuousinitialConsonants.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
            vowelLookup = vowels.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
            subsequentVowelLookup = subsequentVowels.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
            noNextLastConsonantsLookup = noNextLastConsonants.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
            nextExistLastLastConsonantsLookup = nextExistLastConsonants.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
            nextExistSpecialLastConsonantsLookup = nextExistSpecialLastConsonants.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
            nextExistSpecialCurrentLastConsonantsLookup = nextExistVCCLastconsonants.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
            prevLastConsonantsExistsLookup = prevLastConsonantsExists.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
        }

        // Store singer in field, will try reading presamp.ini later
        private USinger singer;
        public override void SetSinger(USinger singer) => this.singer = singer;
        
        // Legacy mapping. Might adjust later to new mapping style.
		public override bool LegacyMapping => true;
        
        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs) {
            var prevLyric = prevNeighbour?.lyric;
            var nextLyric = nextNeighbour?.lyric;
            char[] prevKoreanLyrics = { '　', '　', '　' };
            char[] currentKoreanLyrics = { '　', '　', '　' };
            char[] nextKoreanLyrics = { '　', '　', '　' };

            int totalDuration = notes.Sum(n => n.duration);
            int vcLength = 120;

            List<Phoneme> phonemesArr = new List<Phoneme>();

            var currentLyric = notes[0].lyric;
            currentKoreanLyrics = SeparateHangul(currentLyric != null ? currentLyric[0] : '\0');


            // 글자 앞 부분
            initialConsonantLookup.TryGetValue(currentKoreanLyrics[0].ToString(), out var currentInitialConsonants);
            vowelLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentVowel);
            if (prevLyric == null) {

                phonemesArr.Add(
                    new Phoneme {
                        phoneme = $"- {currentInitialConsonants}{currentVowel}",
                    }
                );
            } else {
                if (prevLyric[0] >= '가' && prevLyric[0] <= '힣') {
                    prevKoreanLyrics = SeparateHangul(prevLyric != null ? prevLyric[0] : '\0');
                }

                if (continuousinitialConsonantsTable.Contains(currentKoreanLyrics[0].ToString())) {
                    if(lastConsonantsTable.Contains(prevKoreanLyrics[2]) && prevKoreanLyrics[2] != '　') {
                        // 이전 글자에서 받침이 있음

                        if (prevKoreanLyrics[2] == 'ㄹ' && currentKoreanLyrics[0] == 'ㄹ') {
                            phonemesArr.Add(
                                new Phoneme {
                                    phoneme = $"{"l"} {"l"}{currentVowel}",
                                }
                            );
                        } else if(vccSubInitialConsonantsTable.Contains(prevKoreanLyrics[2])) {
                            // 받침 ㄱㅂ 뒤에 ㅅ ㅆ있으면 VCC(3) 조건
                            if (vcc2LastConsonantsTable.Contains(prevKoreanLyrics[2]) && (currentKoreanLyrics[0] == 'ㅆ')) {
                                phonemesArr.Add(
                                    new Phoneme {
                                        phoneme = $"{currentInitialConsonants}{currentVowel}",
                                    }
                                );
                            } else {
                                phonemesArr.Add(
                                    new Phoneme {
                                        phoneme = $"- {currentInitialConsonants}{currentVowel}",
                                    }
                                );
                            }
                        } else if (vccLastConsonantsTable.Contains(prevKoreanLyrics[2])) {
                        prevLastConsonantsExistsLookup.TryGetValue(prevKoreanLyrics[2].ToString(), out var prevLastConsonants);
                        phonemesArr.Add(
                            new Phoneme {
                                phoneme = $"{prevLastConsonants} {currentInitialConsonants}{currentVowel}",
                            }
                        ); 
                        } else {
                            phonemesArr.Add(
                                new Phoneme {
                                    phoneme = $"{currentInitialConsonants}{currentVowel}",
                                }
                            );
                        }

                    } else {
                        // ㄱ ㄴ ㄷ ㄹ ㅁ ㅂ ㅅ ㅇ ㅈ ㅎ은 연속음으로 진행(1) 조건

                        subsequentVowelLookup.TryGetValue(prevKoreanLyrics[1].ToString(), out var prevSubsequentVowel);
                        phonemesArr.Add(
                            new Phoneme {
                                phoneme = $"{prevSubsequentVowel} {currentInitialConsonants}{currentVowel}",
                            }
                        );
                    }
                } else {
                    // ㅋ ㅌ ㅍ ㅊ ㄲ ㄸ ㅃ ㅉ ㅆ는 CVVC로 진행(2) 조건
                    if (cvvcInitialConsonantsTable.Contains(currentKoreanLyrics[0])) {
                        phonemesArr.Add(
                            new Phoneme {
                                phoneme = $"{currentInitialConsonants}{currentVowel}",
                            }
                        );
                    } else {
                        subsequentVowelLookup.TryGetValue(prevKoreanLyrics[1].ToString(), out var prevSubsequentVowel);
                        phonemesArr.Add(
                            new Phoneme {
                                phoneme = $"{prevSubsequentVowel} {currentInitialConsonants}{currentVowel}",
                            }
                        );
                    }
                }
            }

            // 글자 뒷 부분
            subsequentVowelLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentSubsequentVowel);
            if (nextLyric == null) {
                // 다음 글자 없음
                if (currentKoreanLyrics[2] == '　') {
                    // 현재 글자 종성 없음

                    phonemesArr.Add(
                        new Phoneme {
                            phoneme = $"{currentSubsequentVowel} {(currentLyric.Length == 2 ? currentLyric[1].ToString() : "-")}",
                            position = totalDuration - 15,
                        }
                    );
                } else {
                    // 현재 글자 종성 있음
                    noNextLastConsonantsLookup.TryGetValue(currentKoreanLyrics[2].ToString(), out var currentLastConsonants);

                    if (vccLastConsonantsTable.Contains(currentKoreanLyrics[2])) {
                        // 받침 ㄴ ㅇ ㄹ ㅁ 뒤에 ㅋ ㅌ ㅍ ㅊ ㄲ ㄸ ㅃ ㅉ ㅆ오면 VCC(3) 조건

                        phonemesArr.Add(
                            new Phoneme {
                                phoneme = $"{currentSubsequentVowel}{currentLastConsonants} {(currentLyric.Length == 2 ? currentLyric[1].ToString() : "-")}",
                                position = totalDuration - 15,
                            }
                        );
                    } else {

                        phonemesArr.Add(
                            new Phoneme {
                                phoneme = $"{currentSubsequentVowel} {currentLastConsonants}",
                                position = totalDuration - vcLength,
                            }
                        );
                    }
                }

            } else {
                // 다음 글자 있음
                if (nextLyric[0] >= '가' && nextLyric[0] <= '힣') {
                    nextKoreanLyrics = SeparateHangul(nextLyric != null ? nextLyric[0] : '\0');
                }

                if (currentKoreanLyrics[2] == '　') {
                    // 현재 글자 종성 없음
                    // ㅋ ㅌ ㅍ ㅊ ㄲ ㄸ ㅃ ㅉ ㅆ는 CVVC로 진행(2) 조건
                    if(cvvcInitialConsonantsTable.Contains(nextKoreanLyrics[0])) {
                        nextExistLastLastConsonantsLookup.TryGetValue(nextKoreanLyrics[0].ToString(), out var currentLastConsonants);

                        phonemesArr.Add(
                            new Phoneme {
                                phoneme = $"{currentSubsequentVowel} {currentLastConsonants}",
                                position = totalDuration - vcLength,
                            }
                        );
                    }
                } else {
                    // 현재 글자 종성 있음
                    if (vccLastConsonantsTable.Contains(currentKoreanLyrics[2]) && cvvcInitialConsonantsTable.Contains(nextKoreanLyrics[0])) {
                        // 받침 ㄴ ㅇ ㄹ ㅁ 뒤에 ㅋ ㅌ ㅍ ㅊ ㄲ ㄸ ㅃ ㅉ ㅆ오면 VCC(3) 조건
                        nextExistSpecialLastConsonantsLookup.TryGetValue(nextKoreanLyrics[0].ToString(), out var currentLastConsonants);
                        prevLastConsonantsExistsLookup.TryGetValue(currentKoreanLyrics[2].ToString(), out var currentSpecialLastConsonants);
                        phonemesArr.Add(
                            new Phoneme {
                                phoneme = $"{currentSubsequentVowel}{currentSpecialLastConsonants} {currentLastConsonants}",
                                position = totalDuration - vcLength,
                            }
                        );
                    } else if (vcc2LastConsonantsTable.Contains(currentKoreanLyrics[2]) && vcc2SubInitialConsonantsTable.Contains(nextKoreanLyrics[0])) {
                        // 받침 ㄱㅂ 뒤에 ㅅ ㅆ있으면 VCC(4) 조건
                        nextExistSpecialCurrentLastConsonantsLookup.TryGetValue(currentKoreanLyrics[2].ToString(), out var currentLastConsonants);

                        if (currentKoreanLyrics[2] == 'ㅆ') {
                            phonemesArr.Add(
                                new Phoneme {
                                    phoneme = $"{currentSubsequentVowel} {"ss"}",
                                    position = totalDuration - vcLength,
                                }
                            );
                        } else {
                            phonemesArr.Add(
                                new Phoneme {
                                    phoneme = $"{currentSubsequentVowel}{currentLastConsonants} {"s"}",
                                    position = totalDuration - vcLength,
                                }
                            );
                        }
                        
                    } else {
                        noNextLastConsonantsLookup.TryGetValue(currentKoreanLyrics[2].ToString(), out var currentLastConsonants);
                        phonemesArr.Add(
                            new Phoneme {
                                phoneme = $"{currentSubsequentVowel} {currentLastConsonants}",
                                position = totalDuration - vcLength,
                            }
                        );
                    }
                }
            }

            // 여기서 phonemes 바꿔줘야함
            Phoneme[] phonemes = phonemesArr.ToArray();

            return new Result {
                phonemes = phonemes
            };
        }

    }
}
