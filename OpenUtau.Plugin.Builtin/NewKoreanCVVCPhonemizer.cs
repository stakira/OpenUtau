using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("New Korean CVVC Phonemizer", "KO CVVC", "RYUUSEI")]
    public class NewKoreanCVVCPhonemizer : Phonemizer {
        static readonly string initialConsonantsTable = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
        static readonly string vowelsTable = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡ";
        static readonly string lastConsonantsTable = "　ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";
        static readonly ushort unicodeKoreanBase = 0xAC00;
        static readonly ushort unicodeKoreanLast = 0xD79F;

        private char[] SeparateHangul(char letter) {
            if (letter == 0) return new char[] {'　', '　' , '　'};
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
            "=ㅇ",
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
            "ue=ㅞ,ㅙ",
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
            "ㅇㄴ=ㄱㄴ",
            "ㅇㄴ=ㄱㄹ",
            "ㅇㄱ=ㄱㅁ",
            "ㄴㄴ=ㄷㄴ",
            "ㄴㅁ=ㄷㅁ",
            "ㅁㄴ=ㅂㄴ",
            "ㅁㄴ=ㅂㄹ",
            "ㅁㅁ=ㅂㅁ",
            "ㅇㄴ=ㅇㄹ",
            "ㅁㄴ=ㅁㄹ",
            "ㄹㄹ=ㄴㄹ",

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
            "ㄱㄲ=ㄱㄱ,ㄲㄱ",
            "ㄱㄸ=ㄱㄷ,ㄺㄷ,ㄺㅌ,ㄺㄸ",
            "ㄱㅃ=ㄱㅂ",
            "ㄱㅆ=ㄱㅅ",
            "ㄱㅉ=ㄱㅈ",
            "ㄴㄸ=ㄵㄷ,ㄵㄸ",
            "ㄷㄲ=ㄷㄱ",
            "ㄷㄸ=ㄷㄷ",
            "ㄷㅃ=ㄷㅂ",
            "ㄷㅆ=ㄷㅅ",
            "ㄷㅉ=ㅈㅈ",
            "ㅁㄸ=ㄻㄷ,ㄻㅌ,ㄻㄸ",
            "ㅂㄲ=ㅂㄱ,ㄼㄱ,ㄼㅋ,ㄼㄲ",
            "ㅂㄸ=ㅂㄷ",
            "ㅂㅃ=ㅂㅂ",
            "ㅂㅆ=ㄼㅅ,ㄼㅆ,ㅂㅅ",
            "ㅂㅉ=ㅂㅈ",
            "ㅅㄲ=ㅅㄱ",
            "ㅅㄸ=ㅅㄷ",
            "ㅅㅃ=ㅅㅂ",
            "ㅅㅆ=ㅅㅅ",
            "ㅅㅉ=ㅅㅈ",
            "ㅈㄲ=ㅈㄱ",
            "ㅈㄸ=ㅈㄷ",
            "ㅈㅃ=ㅈㅂ",
            "ㅈㅆ=ㅈㅅ",

            // 자음 축약
            "　ㅋ=ㄱㅎ",
            "　ㅌ=ㄷㅎ",
            "　ㅍ=ㅂㅎ",
            "　ㅊ=ㅈㅎ",

            // 탈락
            "ㄴㅌ=ㄶㄷ",
            "ㄴㄸ=ㄶㅌ,ㄶㄸ",
            "　ㄴ=ㄶㅇ",
            "ㄹㅌ=ㅀㄷ",
            "ㄹㄸ=ㅀㅌ,ㅀㄸ",
            "　ㄹ=ㅀㅇ",

            // 연음
            "　ㄱ=ㄱㅇ",
            "　ㄲ=ㄲㅇ",
            "　ㄴ=ㄴㅇ",
            "ㄴㅈ=ㄵㅇ",
            "　ㄹ=ㄹㅇ",
            "ㄹㄱ=ㄺㅇ",
            "ㄹㅁ=ㄻㅇ",
            "ㄹㅂ=ㄼㅇ",
            "ㄹㅅ=ㄽㅇ",
            "ㄹㅌ=ㄾㅇ",
            "ㄹㅍ=ㄿㅇ",
            "　ㅁ=ㅁㅇ",
            "　ㅂ=ㅂㅇ",
            "　ㅅ=ㅅㅇ",
            "　ㅈ=ㅈㅇ",
            "　ㅊ=ㅊㅇ",
            "　ㅋ=ㅋㅇ",
            "　ㅌ=ㅌㅇ",
            "　ㅎ=ㅎㅇ",
        };


        static readonly Dictionary<string, string> initialConsonantLookup;
        static readonly Dictionary<string, string> vowelLookup;
        static readonly Dictionary<string, string> subsequentVowelsLookup;
        static readonly Dictionary<string, string> lastConsonantsLookup;
        static readonly Dictionary<string, string> ruleOfConsonantsLookup;


        static KoreanCVVCPhonemizer() {
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
            var note = notes[0];
            var currentUnicode = ToUnicodeElements(note.lyric);
            var prevLyric = prevNeighbour?.lyric;
            var prevKoreanLyrics = SeparateHangul(prevLyric != null ? prevLyric[0] : '\0');
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

            return new Phoneme[] {
                new Phoneme {
                    phoneme = $"- {currentLyric}",
                    },
            };
        }
    }
}
