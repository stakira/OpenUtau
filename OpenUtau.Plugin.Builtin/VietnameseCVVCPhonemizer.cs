using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Vietnamese CVVC Phonemizer", "VIE CVVC", "Jani Tran")]
    public class VietnameseCVVCPhonemizer : Phonemizer {
        static readonly string[] plainVowels = new string[] { "a", "A", "@", "i", "e", "E", "o", "O", "u", "U","m","nh","n","ng","R","-"};

        static readonly string[] vowels = new string[] {
            "a=a",
            "A=A",
            "@=@",
            "i=i",
            "e=e",
            "E=E",
            "o=o",
            "O=O",
            "u=u",
            "U=U",
            "m=m",
            "n=n",
            "ng=g",
            "-=h,t,k,p",
        };

        static readonly string[] consonants = new string[] {
            "b=b",
            "ch=ch",
            "d=d",
            "f=f",
            "g=g",
            "h=h",
            "k=k",
            "kh=kh",
            "l=l",
            "r=r",
            "s=s",
            "t=t",
            "n=n",
            "p=p",
            "sh=sh",
            "m=m",
            "nh=nh",
            "ng=ng",
            "th=th",
            "tr=tr",
            "w=w",
            "v=v",
            "y=y",
            "z=z",
        };

        static readonly Dictionary<string, string> vowelLookup;
        static readonly Dictionary<string, string> consonantLookup;

        static VietnameseCVVCPhonemizer() {
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

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var note = notes[0];
            var currentUnicode = ToUnicodeElements(note.lyric);
            var currentLyric = note.lyric;
            var attr0 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
            var attr1 = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 1) ?? default;

            if (prevNeighbour == null) {
                // Use "- V" or "- CV" if present in voicebank
                var initial = $"-{currentLyric}";
                if (singer.TryGetMappedOto(initial, note.tone + attr0.toneShift, attr0.voiceColor, out var oto)) {
                    currentLyric = oto.Alias;
                }
            } else if (plainVowels.Contains(currentLyric)) {
                var prevUnicode = ToUnicodeElements(prevNeighbour?.lyric);
                // Current note is VV
                var vowel = "";
                    if (vowelLookup.TryGetValue(prevUnicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    currentLyric = $"{vow}{currentLyric}";
                        if (singer.TryGetMappedOto(currentLyric, note.tone + attr0.toneShift, attr0.voiceColor, out var oto)) {
                        currentLyric = oto.Alias;
                    }
                }
            } else if (singer.TryGetMappedOto(currentLyric, note.tone + attr0.toneShift, attr0.voiceColor, out var oto)) {
                currentLyric = oto.Alias;
            }

            if (nextNeighbour != null) {

                var nextUnicode = ToUnicodeElements(nextNeighbour?.lyric);
                var nextLyric = string.Join("", nextUnicode);

                // Check if next note is a vowel and does not require VC
                if (nextUnicode.Count < 3 && plainVowels.Contains(nextUnicode.FirstOrDefault() ?? string.Empty)) {
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
                if (singer.TryGetMappedOto(vcPhoneme, note.tone + attr1.toneShift, attr1.voiceColor, out var oto1)) {
                    vcPhoneme = oto1.Alias;
                } else {
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
                var nextAttr = nextNeighbour.Value.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
                if (singer.TryGetMappedOto(nextLyric, nextNeighbour.Value.tone + nextAttr.toneShift, nextAttr.voiceColor, out var oto)) {
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
