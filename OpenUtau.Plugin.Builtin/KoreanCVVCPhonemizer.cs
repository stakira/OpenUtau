using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Korean CVVC Phonemizer", "KO CVVC", "RYUUSEI")]
    public class KoreanCVVCPhonemizer : Phonemizer {
        static readonly string initialConsonantsTable = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
        static readonly string vowelsTable = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡ";
        static readonly string lastConsonantsTable = " ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";
        static readonly ushort unicodeKoreanBase = 0xAC00;
        static readonly ushort unicodeKoreanLast = 0xD79F;

        private char[] SeparateHangul(char letter) {
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

        static readonly string[] initialConsonants = new string[] {
            "g=ㄱ",
            "k=ㄲ",
            "n=ㄴ",
            "d=ㄷ",
            "t=ㄸ",
            "r=ㄹ",
            "m=ㅁ",
            "b=ㅂ",
            "p=ㅃ",
            "s=ㅅ",
            "ss=ㅆ",
            "j=ㅈ",
            "zh=ㅉ",
            "ch=ㅊ",
            "k=ㅋ",
            "t=ㅌ",
            "p=ㅍ",
            "h=ㅎ",
        };

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

        static readonly string[] lastConsonants = new string[] {
            "g=ㄱ",
            "k=ㄲ",
            "n=ㄴ",
            "d=ㄷ",
            "r=ㄹ",
            "m=ㅁ",
            "b=ㅂ",
            "s=ㅅ",
            "j=ㅈ",
            "zh=ㅉ",
            "ch=ㅊ",
            "k=ㅋ",
            "t=ㅌ",
            "p=ㅍ",
            "h=ㅎ",
        };

        static readonly Dictionary<string, string> initialConsonantLookup;
        static readonly Dictionary<string, string> vowelLookup;
        static readonly Dictionary<string, string> lastConsonantLookup;

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
            lastConsonantLookup = lastConsonants.ToList()
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
            var currentLyric = note.lyric;
            var koreanLyrics = SeparateHangul(currentLyric[0]);


            // 이전 노트가 없는 경우
            if (prevNeighbour == null) {
                // 보이스 뱅크에 있는 경우 "- V" 또는 "- CV"를 사용
                var initial = $"- {currentLyric}";
                if (singer.TryGetMappedOto(initial, note.tone, out var _)) {
                    currentLyric = initial;
                }
            } else if (vowels.Contains(currentLyric)){
                var prevUnicode = ToUnicodeElements(prevNeighbour?.lyric);

                // 지금 노트가 VV 일때
                if (vowelLookup.TryGetValue(prevUnicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    currentLyric = $"{vow} {currentLyric}";
                }
            }

            if (nextNeighbour != null) {
                var nextUnicode = ToUnicodeElements(nextNeighbour?.lyric);
                var nextLyric = string.Join("", nextUnicode);

                // Check if next note is a vowel and does not require VC
                if (vowels.Contains(nextUnicode.FirstOrDefault() ?? string.Empty)) {
                    return new Phoneme[] {
                        new Phoneme() {
                            phoneme = currentLyric,
                        }
                    };
                }

                // 다음 노트 앞에 VC 삽입
                // 현재 노트에서 모음 가져오기
                var vowel = "";
                if (vowelLookup.TryGetValue(currentUnicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    vowel = vow;
                }

                // 다음 노트에서 자음 가져오기
                var consonant = "";
                if (lastConsonantLookup.TryGetValue(nextUnicode.FirstOrDefault() ?? string.Empty, out var con)) {
                    consonant = con;
                }

                if (consonant == "") {
                    return new Phoneme[] {
                        new Phoneme() {
                            phoneme = currentLyric,
                        }
                    };
                }

                var vcPhoneme = $"{vowel} {consonant}";

                int totalDuration = notes.Sum(n => n.duration);
                int vcLength = 120;
                if (singer.TryGetMappedOto(nextLyric, note.tone, out var oto)) {
                    vcLength = MsToTick(oto.Preutter);
                }
                vcLength = Math.Min(totalDuration / 2, vcLength);
                
                return new Phoneme[] {
                    new Phoneme() {
                        phoneme = currentLyric,
                    },
                    new Phoneme() {
                        phoneme = vcPhoneme,
                        position = totalDuration - vcLength,
                    }
                };
            } 
            
            // 다음 노트가 없는 경우
            return new Phoneme[] {
                new Phoneme {
                    phoneme = currentLyric,
                }
            };
        }
    }
}
