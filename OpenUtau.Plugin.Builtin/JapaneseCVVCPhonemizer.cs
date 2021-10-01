using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Japanese CVVC Phonemizer", "JA CVVC", "TUBS")]
    public class JapaneseCVVCPhonemizer : Phonemizer {
        static readonly string[] plainVowels = new string[] { "あ","い","う","え","お","ん"};

        // presamp plain vowels
        private static List<string> presampPlainVowels = new List<string>();

        private static string[] vowels = new string[] {
            "a=ぁ,あ,か,が,さ,ざ,た,だ,な,は,ば,ぱ,ま,ゃ,や,ら,わ,ァ,ア,カ,ガ,サ,ザ,タ,ダ,ナ,ハ,バ,パ,マ,ャ,ヤ,ラ,ワ",
            "e=ぇ,え,け,げ,せ,ぜ,て,で,ね,へ,べ,ぺ,め,れ,ゑ,ェ,エ,ケ,ゲ,セ,ゼ,テ,デ,ネ,ヘ,ベ,ペ,メ,レ,ヱ",
            "i=ぃ,い,き,ぎ,し,じ,ち,ぢ,に,ひ,び,ぴ,み,り,ゐ,ィ,イ,キ,ギ,シ,ジ,チ,ヂ,ニ,ヒ,ビ,ピ,ミ,リ,ヰ",
            "o=ぉ,お,こ,ご,そ,ぞ,と,ど,の,ほ,ぼ,ぽ,も,ょ,よ,ろ,を,ォ,オ,コ,ゴ,ソ,ゾ,ト,ド,ノ,ホ,ボ,ポ,モ,ョ,ヨ,ロ,ヲ",
            "n=ん",
            "u=ぅ,う,く,ぐ,す,ず,つ,づ,ぬ,ふ,ぶ,ぷ,む,ゅ,ゆ,る,ゥ,ウ,ク,グ,ス,ズ,ツ,ヅ,ヌ,フ,ブ,プ,ム,ュ,ユ,ル,ヴ",
            "N=ン",
        };

        private static string[] consonants = new string[] {
            "ch=ch,ち,ちぇ,ちゃ,ちゅ,ちょ",
            "gy=gy,ぎ,ぎぇ,ぎゃ,ぎゅ,ぎょ",
            "ts=ts,つ,つぁ,つぃ,つぇ,つぉ",
            "ty=ty,てぃ,てぇ,てゃ,てゅ,てょ",
            "py=py,ぴ,ぴぇ,ぴゃ,ぴゅ,ぴょ",
            "ry=ry,り,りぇ,りゃ,りゅ,りょ",
            "ny=ny,に,にぇ,にゃ,にゅ,にょ",
            "r=r,ら,る,れ,ろ",
            "hy=hy,ひ,ひぇ,ひゃ,ひゅ,ひょ",
            "dy=dy,でぃ,でぇ,でゃ,でゅ,でょ",
            "by=by,び,びぇ,びゃ,びゅ,びょ",
            "b=b,ば,ぶ,べ,ぼ",
            "d=d,だ,で,ど,どぅ",
            "g=g,が,ぐ,げ,ご",
            "f=f,ふ,ふぁ,ふぃ,ふぇ,ふぉ",
            "h=h,は,へ,ほ",
            "k=k,か,く,け,こ",
            "j=j,じ,じぇ,じゃ,じゅ,じょ",
            "m=m,ま,む,め,も",
            "n=n,な,ぬ,ね,の",
            "p=p,ぱ,ぷ,ぺ,ぽ",
            "s=s,さ,す,すぃ,せ,そ",
            "sh=sh,し,しぇ,しゃ,しゅ,しょ",
            "t=t,た,て,と,とぅ",
            "w=w,うぃ,うぅ,うぇ,うぉ,わ,を",
            "v=v,ヴ,ヴぁ,ヴぃ,ヴぅ,ヴぇ,ヴぉ",
            "y=y,いぃ,いぇ,や,ゆ,よ,ゐ,ゑ",
            "ky=ky,き,きぇ,きゃ,きゅ,きょ",
            "z=z,ざ,ず,ずぃ,ぜ,ぞ",
            "my=my,み,みぇ,みゃ,みゅ,みょ",
            "R=R",
            "息=息",
            "吸=吸",
            "-=-"
        };

        private static Dictionary<string, string> vowelLookup;
        private static Dictionary<string, string> consonantLookup;

        // Dictionaries for presamp data
        private static Dictionary<string, string> presampConsonants;
        private static Dictionary<string, string> presampVowels;


        // Store singer in field
        private USinger singer;
        public override void SetSinger(USinger singer) {
            this.singer = singer;

            // load presamp config from singer
            LoadPresampConfig(Path.Combine(singer.Location, "presamp.ini"));
        }

        public JapaneseCVVCPhonemizer() {
            Initialize();
        }

        private void LoadPresampConfig(string presampIni) {
            // Lists for presamp data
            List<string> presampVowList = new List<string>();
            List<string> presampConsList = new List<string>();

            // read presamp ini if it exists
            if (File.Exists(presampIni)) {
                try {
                    var Header = "";
                    foreach (string line in File.ReadLines(presampIni, Encoding.UTF8).ToList()) {
                        if (line.StartsWith(@"[") && line.EndsWith(@"]")) {
                            Header = line;
                            // clear consonants and vowels in case loading from a voicebank to prevent conflicts
                            if (Header == "[CONSONANT]") { presampConsList.Clear(); }
                            if (Header == "[VOWEL]") { presampVowList.Clear(); presampPlainVowels.Clear(); }
                            continue;
                        }
                        switch (Header) {
                            case "[CONSONANT]":
                                // If the consonant is not already pesent add it
                                if (!presampConsList.Contains(line.Split("=")[0] + "=" + line.Split("=")[1])) {
                                    presampConsList.Add(line.Split("=")[0] + "=" + line.Split("=")[1]);
                                }
                                break;
                            case "[VOWEL]":
                                // check the vowels don't already exist before adding them
                                if (!presampVowList.Contains(line.Split("=")[0] + "=" + line.Split("=")[2])) {
                                    presampVowList.Add(line.Split("=")[0] + "=" + line.Split("=")[2]);
                                }
                                if (!presampPlainVowels.Contains(line.Split("=")[1])) {
                                    presampPlainVowels.Add(line.Split("=")[1]);
                                }
                                break;
                        }
                    }
                } catch (Exception e) {
                    Log.Error(e, "Failed to read \"" + presampIni + "\".");
                }
            }

            // Create consonant dictionary from presamp if possible
            presampConsonants = presampConsList.SelectMany(line => {
                var parts = line.Split('=');
                return parts[1].Split(',').Select(cv => (cv, parts[0]));
            }).ToDictionary(t => t.Item1, t => t.Item2);

            // and create vowel dictionary from presamp if possible
            presampVowels = presampVowList.SelectMany(line => {
                var parts = line.Split('=');
                return parts[1].Split(',').Select(cv => (cv, parts[0]));
            }).ToDictionary(t => t.Item1, t => t.Item2);
        }

        private void Initialize() {
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

            // find global presamp.ini
            string dir = Path.GetDirectoryName(typeof(JapaneseCVVCPhonemizer).Assembly.Location);
            var presampIni = Path.Combine(dir, "presamp.ini");

            // load presamp ini
            LoadPresampConfig(presampIni);
        }

        public override Phoneme[] Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour) {
            var note = notes[0];
            var currentUnicode = ToUnicodeElements(note.lyric);
            var currentLyric = note.lyric;

            if (prevNeighbour == null) {
                // Use "- V" or "- CV" if present in voicebank
                var initial = $"- {currentLyric}";
                if (singer.TryGetMappedOto(initial, note.tone, out var _)) {
                    currentLyric = initial;
                }
            } else if (plainVowels.Contains(currentLyric) || presampPlainVowels.Contains(currentLyric)){
                var prevUnicode = ToUnicodeElements(prevNeighbour?.lyric);

                // Current note is VV
                if (presampVowels.TryGetValue(prevUnicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    currentLyric = $"{vow} {currentLyric}";
                }
                else if (vowelLookup.TryGetValue(prevUnicode.LastOrDefault() ?? string.Empty, out vow)) {
                    currentLyric = $"{vow} {currentLyric}";
                }
            }

            if (nextNeighbour != null) {
                var nextUnicode = ToUnicodeElements(nextNeighbour?.lyric);
                var nextLyric = string.Join("", nextUnicode);

                // Check if next note is a vowel and does not require VC
                if (plainVowels.Contains(nextUnicode.FirstOrDefault() ?? string.Empty) || presampPlainVowels.Contains(nextUnicode.FirstOrDefault() ?? string.Empty)) {
                    return new Phoneme[] {
                        new Phoneme() {
                            phoneme = currentLyric,
                        }
                    };
                }

                // Insert VC before next neighbor
                // Get vowel from current note
                var vowel = "";
                string vow;
                if (presampVowels.TryGetValue(currentUnicode.LastOrDefault() ?? string.Empty, out vow)) {
                    vowel = vow;
                } else if (vowelLookup.TryGetValue(currentUnicode.LastOrDefault() ?? string.Empty, out vow)) {
                    vowel = vow;
                }

                // Get consonant from next note
                var consonant = "";
                string con;
                if (presampConsonants.TryGetValue(nextUnicode.FirstOrDefault() ?? string.Empty, out con)) {
                    consonant = con;
                } else if (consonantLookup.TryGetValue(nextUnicode.FirstOrDefault() ?? string.Empty, out con)) {
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
                if (!singer.TryGetMappedOto(vcPhoneme, note.tone, out var _)) {
                    return new Phoneme[] {
                        new Phoneme() {
                            phoneme = currentLyric,
                        }
                    };
                }

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
            
            // No next neighbor
            return new Phoneme[] {
                new Phoneme {
                    phoneme = currentLyric,
                }
            };
        }
    }
}
