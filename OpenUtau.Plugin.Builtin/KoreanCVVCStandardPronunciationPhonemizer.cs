using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Korean CVVC Phonemizer (standard pronunciation) ", "KO CVVC", "RYUUSEI")]
    public class KoreanCVVCStandardPronunciationPhonemizer : Phonemizer {
        static readonly string initialConsonantsTable = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
        static readonly string vowelsTable = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
        static readonly string lastConsonantsTable = "　ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";
        static readonly ushort unicodeKoreanBase = 0xAC00;
        static readonly ushort unicodeKoreanLast = 0xD79F;

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
        static readonly string[] initialConsonants = new string[] {
            "g=ㄱ",
            "k=ㄲ,ㅋ",
            "n=ㄴ",
            "d=ㄷ",
            "t=ㄸ,ㅌ",
            "r=ㄹ",
            "m=ㅁ",
            "b=ㅂ",
            "p=ㅃ",
            "s=ㅅ",
            "ss=ㅆ",
            "=ㅇ,　",
            "j=ㅈ",
            "zh=ㅉ",
            "ch=ㅊ",
            "p=ㅍ",
            "h=ㅎ",
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
            "eui=ㅢ",
            "we=ㅞ,ㅙ",
            "uo=ㅝ",
            "wa=ㅘ",
            "wi=ㅟ",
        };

        // V-V의 경우 이전 모음으로 대체
        static readonly string[] subsequentVowels = new string[] {
            "a=ㅏ,ㅑ,ㅘ",
            "eo=ㅓ,ㅕ,ㅝ",
            "o=ㅗ,ㅛ",
            "u=ㅜ,ㅠ",
            "eu=ㅡ",
            "e=ㅔ,ㅐ,ㅞ,ㅙ",
            "i=ㅣ,ㅢ,ㅟ",
        };

        // 끝소리일 경우에만 동작
        static readonly string[] lastConsonants = new string[] {
            "k=ㄱ,ㅋ,ㄲ,ㄳ,ㄺ",
            "n=ㄴ,ㄵ,ㄶ",
            "t=ㄷ,ㅅ,ㅈ,ㅊ,ㅌ,ㅆ,ㅎ",
            "l=ㄹ,ㄼ,ㄽ,ㄾ,ㄿ,ㅀ",
            "m=ㅁ,ㄻ",
            "b=ㅂ,ㅍ,ㅄ",
            "ng=ㅇ",
        };

        // 표준발음법 적용
        static readonly string[] ruleOfConsonants = new string[] {
            // 자음동화: (비음화, 유음화)
            "ㅇㄴ=ㅇㄴ,ㄱㄴ,ㄱㄹ,ㅇㄹ",
            "ㅇㄱ=ㅇㄱ,ㄱㅁ",
            "ㄴㄴ=ㄴㄴ,ㄷㄴ",
            "ㄴㅁ=ㄴㅁ,ㄷㅁ",
            "ㅁㄴ=ㅁㄴ,ㅂㄴ,ㅂㄹ,ㅁㄹ",
            "ㅁㅁ=ㅁㅁ,ㅂㅁ",
            "ㄹㄹ=ㄹㄹ,ㄴㄹ",

            // 구개음화
            "　ㅈㅣ=ㄷㅇㅣ",
            "　ㅈㅓ=ㄷㅇㅓ",
            "　ㅈㅓ=ㄷㅇㅕ",
            "　ㅊㅣ=ㄷㅎㅣ",
            "　ㅊㅓ=ㄷㅎㅓ",
            "　ㅊㅓ=ㄷㅎㅕ",
            "　ㅊㅣ=ㅌㅇㅣ",
            "　ㅊㅓ=ㅌㅇㅓ",
            "　ㅊㅓ=ㅌㅇㅕ",
            "　ㅊㅣ=ㅌㅎㅣ",
            "　ㅊㅓ=ㅌㅎㅓ",
            "　ㅊㅓ=ㅌㅎㅕ",
            "ㄹㅊㅣ=ㄾㅇㅣ",
            "ㄹㅊㅓ=ㄾㅇㅓ",
            "ㄹㅊㅓ=ㄾㅇㅕ",
            "ㄹㅊㅣ=ㄾㅎㅣ",
            "ㄹㅊㅓ=ㄾㅎㅓ",
            "ㄹㅊㅓ=ㄾㅎㅕ",

            // 경음화
            "ㄱㄲ=ㄱㄲ,ㄱㄱ,ㄲㄱ",
            "ㄱㄸ=ㄱㄸ,ㄱㄷ,ㄺㄷ,ㄺㅌ,ㄺㄸ",
            "ㄱㅃ=ㄱㅃ,ㄱㅂ",
            "ㄱㅆ=ㄱㅆ,ㄱㅅ",
            "ㄱㅉ=ㄱㅉ,ㄱㅈ",
            "ㄴㄸ=ㄴㄸ,ㄵㄷ,ㄵㄸ, ㄶㅌ,ㄶㄸ",
            "ㄷㄲ=ㄷㄲ,ㄷㄱ",
            "ㄷㄸ=ㄷㄸ,ㄷㄷ",
            "ㄷㅃ=ㄷㅃ,ㄷㅂ",
            "ㄷㅆ=ㄷㅆ,ㄷㅅ",
            "ㄷㅉ=ㄷㅉ,ㅈㅈ",
            "ㅁㄸ=ㅁㄸ,ㄻㄷ,ㄻㅌ,ㄻㄸ",
            "ㅂㄲ=ㅂㄲ,ㅂㄱ,ㄼㄱ,ㄼㅋ,ㄼㄲ",
            "ㅂㄸ=ㅂㄸ,ㅂㄷ",
            "ㅂㅃ=ㅂㅃ,ㅂㅂ",
            "ㅂㅆ=ㅂㅆ,ㄼㅅ,ㄼㅆ,ㅂㅅ",
            "ㅂㅉ=ㅂㅉ,ㅂㅈ",
            "ㅅㄲ=ㅅㄲ,ㅅㄱ",
            "ㅅㄸ=ㅅㄸ,ㅅㄷ",
            "ㅅㅃ=ㅅㅃ,ㅅㅂ",
            "ㅅㅆ=ㅅㅆ,ㅅㅅ",
            "ㅅㅉ=ㅅㅉ,ㅅㅈ",
            "ㅈㄲ=ㅈㄲ,ㅈㄱ",
            "ㅈㄸ=ㅈㄸ,ㅈㄷ",
            "ㅈㅃ=ㅈㅃ,ㅈㅂ",
            "ㅈㅆ=ㅈㅆ,ㅈㅅ",

            // 자음 축약
            "　ㅋ=ㄱㅎ",
            "　ㅌ=ㄷㅎ",
            "　ㅍ=ㅂㅎ",
            "　ㅊ=ㅈㅎ",
            "ㄴㅊ=ㄵㅎ",

            // 탈락
            "ㄴㅌ=ㄴㅌ,ㄶㄷ",
            "　ㄴ=ㄶㅇ",
            "ㄹㅌ=ㄹㅌ,ㅀㄷ,ㄾㅇ",
            "ㄹㄸ=ㄹㄸ,ㅀㅌ,ㅀㄸ",
            "　ㄹ=ㅀㅇ",

            // 연음
            "　ㄱ=ㄱㅇ",
            "　ㄲ=ㄲㅇ",
            "　ㄴ=ㄴㅇ",
            "ㄴㅈ=ㄴㅈ,ㄵㅇ",
            "　ㄹ=ㄹㅇ",
            "ㄹㄱ=ㄹㄱ,ㄺㅇ",
            "ㄹㅁ=ㄹㅁ,ㄻㅇ",
            "ㄹㅂ=ㄹㅂ,ㄼㅇ",
            "ㄹㅅ=ㄹㅅ,ㄽㅇ",
            "ㄹㅍ=ㄹㅍ,ㄿㅇ",
            "　ㅁ=ㅁㅇ",
            "　ㅂ=ㅂㅇ",
            "　ㅅ=ㅅㅇ",
            "　ㅈ=ㅈㅇ",
            "　ㅊ=ㅊㅇ",
            "　ㅋ=ㅋㅇ",
            "　ㅌ=ㅌㅇ",
            "　ㅍ=ㅍㅇ",
            "　ㅎ=ㅎㅇ",
        };


        static readonly Dictionary<string, string> initialConsonantLookup;
        static readonly Dictionary<string, string> vowelLookup;
        static readonly Dictionary<string, string> subsequentVowelsLookup;
        static readonly Dictionary<string, string> lastConsonantsLookup;
        static readonly Dictionary<string, string> ruleOfConsonantsLookup;


        static KoreanCVVCStandardPronunciationPhonemizer() {
            initialConsonantLookup = initialConsonants.ToList()
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
            subsequentVowelsLookup = subsequentVowels.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
            lastConsonantsLookup = lastConsonants.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
            ruleOfConsonantsLookup = ruleOfConsonants.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);

        }

        // Store singer in field, will try reading presamp.ini later
        private USinger singer;
        public override void SetSinger(USinger singer) => this.singer = singer;

        public override Phoneme[] Process(Note[] notes, Note? prevNeighbour, Note? nextNeighbour) {
            var prevLyric = prevNeighbour?.lyric;
            char[] prevKoreanLyrics = { '　', '　', '　' };
            bool isPrevEndV = true;
            if (prevLyric != null && prevLyric[0] >= '가' && prevLyric[0] <= '힣') {
                prevKoreanLyrics = SeparateHangul(prevLyric != null ? prevLyric[0] : '\0');
                isPrevEndV = prevKoreanLyrics[2] == '　' && prevKoreanLyrics[0] != '　';
            }
            
            var currentLyric = notes[0].lyric;
            if (!(currentLyric[0] >= '가' && currentLyric[0] <= '힣')) {
                return new Phoneme[] {
                    new Phoneme {
                        phoneme = $"{currentLyric}",
                    }};
            }
            var currentKoreanLyrics = SeparateHangul(currentLyric[0]);
            var isCurrentEndV = currentKoreanLyrics[2] == '　' && currentKoreanLyrics[0] != '　';

            var nextLyric = nextNeighbour?.lyric;
            char[] nextKoreanLyrics  = { '　', '　', '　' };
            if (nextLyric != null && nextLyric[0] >= '가' && nextLyric[0] <= '힣') {
                nextKoreanLyrics = SeparateHangul(nextLyric != null ? nextLyric[0] : '\0');
            }

            int totalDuration = notes.Sum(n => n.duration);
            int vcLength = 60;

            string CV = "";
            if(prevNeighbour != null) {
                // 앞문자 존재
                if (!isPrevEndV) {
                    // 앞문자 종결이 C
                    ruleOfConsonantsLookup.TryGetValue(prevKoreanLyrics[2].ToString() + currentKoreanLyrics[0].ToString(), out var CCConsonants);
                    vowelLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentVowel);
                    initialConsonantLookup.TryGetValue(CCConsonants[1].ToString(), out var changedCurrentConsonants);
                    CV = $"{changedCurrentConsonants}{currentVowel}";
                    
                } else {
                    // 앞문자 종결이 V
                    initialConsonantLookup.TryGetValue(currentKoreanLyrics[0].ToString(), out var currentInitialConsonants);
                    vowelLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentVowel);

                    CV = $"{currentInitialConsonants}{currentVowel}";
                }
            } else {
                // 앞문자 없음
                initialConsonantLookup.TryGetValue(currentKoreanLyrics[0].ToString(), out var currentInitialConsonants);
                vowelLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentVowel);

                CV = $"{currentInitialConsonants}{currentVowel}";
            }
            System.Diagnostics.Debug.WriteLine(CV);

            string VC = "";
            // 뒷문자 존재
            if (nextNeighbour != null) {
                if(isCurrentEndV) {
                    // 현재 문자 종결이 V
                    initialConsonantLookup.TryGetValue(nextLyrics[0].ToString(), out var nextInitialConsonants);
                    subsequentVowelsLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentSubsequentVowel);
                    if (nextInitialConsonants == "") {
                        // 다음 문자 시작이 V(VV 형태)
                        vowelLookup.TryGetValue(nextKoreanLyrics[1].ToString(), out var nextVowel);
                        VC = $"{currentSubsequentVowel} {nextVowel}";
                    } else {
                        // 다음 문자 시작이 C(VC 형태)
                        VC = $"{currentSubsequentVowel} {nextInitialConsonants}";
                    }
                } else {
                    // 현재 문자 종결이 C
                    ruleOfConsonantsLookup.TryGetValue(currentKoreanLyrics[2].ToString() + nextKoreanLyrics[0].ToString(), out var VCConsonants);       
                    initialConsonantLookup.TryGetValue(VCConsonants[1].ToString(), out var changedNextConsonants);
                    if (changedNextConsonants == "") {
                        // 다음 문자 시작이 V(CV 형태)
                        vowelLookup.TryGetValue(nextKoreanLyrics[1].ToString(), out var nextVowel);
                        if (VCConsonants[0] == "　") {
                            // 현재 문자 종결이 V로 바뀌는 경우(VV)
                            subsequentVowelsLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentSubsequentVowel);
                            VC = $"{currentSubsequentVowel} {nextVowel}";
                        } else {
                            // 현재 문자 종결이 C가 유지되는 경우(CV)
                            lastConsonantsLookup.TryGetValue(currentKoreanLyrics[2].ToString(), out var currentLastConsonants);
                            VC = $"{currentLastConsonants} {nextVowel}";
                        }
                    } else {
                        // 다음 문자 시작이 C(CC 형태)
                        lastConsonantsLookup.TryGetValue(VCConsonants[1].ToString(), out var currentLastConsonants);
                        VC = $"{currentLastConsonants} {changedNextConsonants[0]}";
                    }
                }




            }


            // 레거시
            /*
            var note = notes[0];
            var currentUnicode = ToUnicodeElements(note.lyric);
            var isPrevCV = prevKoreanLyrics[2] == '　' && prevKoreanLyrics[0] != '　';

            var currentLyric = note.lyric;
            var currentKoreanLyrics = SeparateHangul(currentLyric[0]);
            var isCurrentCV = currentKoreanLyrics[2] == '　';

            var nextLyric = nextNeighbour?.lyric;
            var nextKoreanLyrics = SeparateHangul(nextLyric != null ? nextLyric[0] : '\0');
            var isNextCV = nextKoreanLyrics[2] == '　' && nextKoreanLyrics[0] != '　';

            initialConsonantLookup.TryGetValue(currentKoreanLyrics[0].ToString(), out var iCon);
            vowelLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var fVow);
            subsequentVowelsLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var cSVow);

            int totalDuration = notes.Sum(n => n.duration);
            int vcLength = 60;

            // 이전 노트가 없는 경우
            if (prevNeighbour == null) {

                var initial = $"- {currentLyric}";
                // 보이스 뱅크에 있으면 "- V" / "- CV"를 사용
                if (singer.TryGetMappedOto(initial, note.tone, out var _)) {
                    currentLyric = initial;

                } else {
                    // 받침이 없는 경우
                    if (isCurrentCV == true) {

                        return new Phoneme[] {
                            new Phoneme {
                                phoneme = $"{iCon}{fVow}",
                            },
                        };
                    } else {
                        // 받침이 있는 경우
                        lastConsonantsLookup.TryGetValue(currentKoreanLyrics[2].ToString(), out var lCon);

                        return new Phoneme[] {
                            new Phoneme {
                                phoneme = $"{iCon}{fVow}",
                            },
                            new Phoneme {
                                phoneme = $"{cSVow} {lCon}",
                                position = totalDuration - vcLength,
                            },
                        };
                    }
                }
            } else {
                subsequentVowelsLookup.TryGetValue(prevKoreanLyrics[1].ToString(), out var pVow);

                if (isPrevCV) {
                    // 이전 노트가 CV 인 경우
                    if (isCurrentCV == true) {
                        // 현재 노트에 받침이 없는 경우
                        return new Phoneme[] {
                        new Phoneme {
                                phoneme = $"{iCon}{fVow}",
                            },
                        };

                    } else {
                        // 현재 노트에 받침이 있는 경우

                        lastConsonantsLookup.TryGetValue(currentKoreanLyrics[2].ToString(), out var lCon);

                        return new Phoneme[] {
                            new Phoneme {
                                phoneme = $"{iCon}{fVow}",
                            },
                            new Phoneme {
                                phoneme = $"{cSVow} {lCon}",
                                position = totalDuration - vcLength,
                            },
                        };
                    }
                } else {
                    // 이전 노트가 CVC인 경우
                    ruleOfConsonantsLookup.TryGetValue(prevKoreanLyrics[2].ToString() + currentKoreanLyrics[0].ToString(), out var pCon);
                    initialConsonantLookup.TryGetValue(pCon == null ? currentKoreanLyrics[0].ToString() : pCon[1].ToString(), out var iSCon);

                    if (isCurrentCV == true) {
                        // 현재 노트에 받침이 없는 경우
                        return new Phoneme[] {
                            new Phoneme {
                                phoneme = $"{iSCon}{fVow}",
                            },
                        };

                    } else {
                        // 현재 노트에 받침이 있는 경우
                        lastConsonantsLookup.TryGetValue(currentKoreanLyrics[2].ToString(), out var lCon);

                        return new Phoneme[] {
                            new Phoneme {
                                phoneme = $"{iSCon}{fVow}",
                            },
                            new Phoneme {
                                phoneme = $"{cSVow} {lCon}",
                                position = totalDuration - vcLength,
                            },
                        };

                    }
                }
            }
            */
            return new Phoneme[] {
                new Phoneme {
                    phoneme = CV,
                },
                new Phoneme {
                    phoneme = VC,
                    position = totalDuration - vcLength,
                },
            };
        }
    }
}
