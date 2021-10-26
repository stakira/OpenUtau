using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Japanese CVVC Phonemizer", "JA CVVC", "TUBS")]
    public class JapaneseCVVCPhonemizer : Phonemizer {
        static readonly string[] plainVowels = new string[] { "あ", "い", "う", "え", "お", "ん" };

        static readonly string[] vowels = new string[] {
            "a=ぁ,あ,か,が,さ,ざ,た,だ,な,は,ば,ぱ,ま,ゃ,や,ら,わ,ァ,ア,カ,ガ,サ,ザ,タ,ダ,ナ,ハ,バ,パ,マ,ャ,ヤ,ラ,ワ",
            "e=ぇ,え,け,げ,せ,ぜ,て,で,ね,へ,べ,ぺ,め,れ,ゑ,ェ,エ,ケ,ゲ,セ,ゼ,テ,デ,ネ,ヘ,ベ,ペ,メ,レ,ヱ",
            "i=ぃ,い,き,ぎ,し,じ,ち,ぢ,に,ひ,び,ぴ,み,り,ゐ,ィ,イ,キ,ギ,シ,ジ,チ,ヂ,ニ,ヒ,ビ,ピ,ミ,リ,ヰ",
            "o=ぉ,お,こ,ご,そ,ぞ,と,ど,の,ほ,ぼ,ぽ,も,ょ,よ,ろ,を,ォ,オ,コ,ゴ,ソ,ゾ,ト,ド,ノ,ホ,ボ,ポ,モ,ョ,ヨ,ロ,ヲ",
            "n=ん",
            "u=ぅ,う,く,ぐ,す,ず,つ,づ,ぬ,ふ,ぶ,ぷ,む,ゅ,ゆ,る,ゥ,ウ,ク,グ,ス,ズ,ツ,ヅ,ヌ,フ,ブ,プ,ム,ュ,ユ,ル,ヴ",
            "N=ン",
        };

        static readonly string[] consonants = new string[] {
            "ch=ch,ち,ちぇ,ちゃ,ちゅ,ちょ",
            "gy=gy,ぎ,ぎぇ,ぎゃ,ぎゅ,ぎょ",
            "ts=ts,つ,つぁ,つぃ,つぇ,つぉ",
            "ty=ty,てぃ,てぇ,てゃ,てゅ,てょ",
            "py=py,ぴ,ぴぇ,ぴゃ,ぴゅ,ぴょ",
            "ry=ry,り,りぇ,りゃ,りゅ,りょ",
            "ny=ny,に,にぇ,にゃ,にゅ,にょ",
            "r=r,4,ら,る,るぃ,れ,ろ",
            "hy=hy,ひ,ひぇ,ひゃ,ひゅ,ひょ",
            "dy=dy,でぃ,でぇ,でゃ,でゅ,でょ",
            "by=by,び,びぇ,びゃ,びゅ,びょ",
            "b=b,ば,ぶ,ぶぃ,べ,ぼ",
            "d=d,だ,で,ど,どぃ,どぅ",
            "g=g,が,ぐ,ぐぃ,げ,ご",
            "f=f,ふ,ふぁ,ふぃ,ふぇ,ふぉ",
            "h=h,は,はぃ,へ,ほ,ほぅ",
            "k=k,か,く,くぃ,け,こ",
            "j=j,じ,じぇ,じゃ,じゅ,じょ",
            "m=m,ま,む,むぃ,め,も",
            "n=n,な,ぬ,ぬぃ,ね,の",
            "p=p,ぱ,ぷ,ぷぃ,ぺ,ぽ",
            "s=s,さ,す,すぃ,せ,そ",
            "sh=sh,し,しぇ,しゃ,しゅ,しょ",
            "t=t,た,て,と,とぃ,とぅ",
            "v=v,ヴ,ヴぁ,ヴぃ,ヴぅ,ヴぇ,ヴぉ",
            "ky=ky,き,きぇ,きゃ,きゅ,きょ",
            "w=w,うぃ,うぅ,うぇ,うぉ,わ,ゐ,ゑ,を,ヰ,ヱ",
            "y=y,いぃ,いぇ,や,ゆ,よ",
            "z=z,ざ,ず,ずぃ,ぜ,ぞ",
            "my=my,み,みぇ,みゃ,みゅ,みょ",
            "ng=ng,ガ,ギ,グ,ゲ,ゴ",
            "R=R",
            "息=息",
            "吸=吸",
            "-=-"
        };

        static readonly Dictionary<string, string> vowelLookup;
        static readonly Dictionary<string, string> consonantLookup;

        static JapaneseCVVCPhonemizer() {
            vowelLookup = vowels.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
            consonantLookup = consonants.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
        }

        // Store singer in field, will try reading presamp.ini later
        private USinger singer;
        public override void SetSinger(USinger singer) => this.singer = singer;

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour) {
            var note = notes[0];
            var currentUnicode = ToUnicodeElements(note.lyric);
            var currentLyric = note.lyric;

            if (prevNeighbour == null) {
                // Use "- V" or "- CV" if present in voicebank
                var initial = $"- {currentLyric}";
                if (singer.TryGetMappedOto(initial, note.tone, out var _)) {
                    currentLyric = initial;
                }
            } else if (plainVowels.Contains(currentLyric)) {
                var prevUnicode = ToUnicodeElements(prevNeighbour?.lyric);
                // Current note is VV
                if (vowelLookup.TryGetValue(prevUnicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    currentLyric = $"{vow} {currentLyric}";
                }
            }

            if (nextNeighbour != null) {

                var nextUnicode = ToUnicodeElements(nextNeighbour?.lyric);
                var nextLyric = string.Join("", nextUnicode);

                // Check if next note is a vowel and does not require VC
                if (nextUnicode.Count < 2 && plainVowels.Contains(nextUnicode.FirstOrDefault() ?? string.Empty)) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme() {
                                phoneme = currentLyric,
                            }
                        },
                    };
                }

                // Insert VC before next neighbor
                // Get vowel from current note

                var vowel = "";
                if (vowelLookup.TryGetValue(currentUnicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    vowel = vow;
                }

                // Get consonant from next note
                var consonant = "";
                if (consonantLookup.TryGetValue(nextUnicode.FirstOrDefault() ?? string.Empty, out var con)
                    || nextUnicode.Count >= 2 && consonantLookup.TryGetValue(string.Join("", nextUnicode.Take(2)), out con)) {
                    consonant = con;
                }


                if (consonant == "") {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme() {
                                phoneme = currentLyric,
                            }
                        },
                    };
                }

                var vcPhoneme = $"{vowel} {consonant}";
                if (!singer.TryGetMappedOto(vcPhoneme, note.tone, out var _)) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme() {
                                phoneme = currentLyric,
                            }
                        },
                    };
                }

                int totalDuration = notes.Sum(n => n.duration);
                int vcLength = 120;
                if (singer.TryGetMappedOto(nextLyric, note.tone, out var oto)) {
                    vcLength = MsToTick(oto.Preutter);
                }
                vcLength = Math.Min(totalDuration / 2, vcLength);

                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme() {
                            phoneme = currentLyric,
                        },
                        new Phoneme() {
                            phoneme = vcPhoneme,
                            position = totalDuration - vcLength,
                        }
                    },
                };
            }

            // No next neighbor
            return new Result {
                phonemes = new Phoneme[] {
                    new Phoneme {
                        phoneme = currentLyric,
                    }
                },
            };
        }
    }
}
