using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenUtau.Classic;
using Serilog;

namespace Classic {
    public partial class Presamp {
        // presamp.ini specification https://delta-kimigatame.hatenablog.jp/entry/ar483589
        // I am not confident in my coding skills! There may be a better way.

        public bool FileExists { get; private set; } = false;
        public Dictionary<string, PresampVowel> Vowels { get; set; } // def: lower of this file
        public Dictionary<string, PresampConsonant> Consonants { get; set; } // def: lower of this file
        public List<string> Priorities { get; set; } = new List<string> { "k", "ky", "g", "gy", "t", "ty", "d", "dy", "ch", "ts", "b", "by", "p", "py", "r", "ry" };
        public Dictionary<string, string> Replace { get; set; } // def: lower of this file
        public PresampAliasRules AliasRules { get; set; } = new PresampAliasRules();
        public List<string> Prefixs { get; set; } = new List<string>(); // def: empty
        public List<string> SuffixOrder { get; set; } = new List<string> { "%num%", "%append%", "%pitch%" };
        public List<string> Nums { get; set; } // def: lower of this file
        public List<string> Appends { get; set; } // def: lower of this file
        public List<string> Pitches { get; set; } // def: lower of this file
        public List<string> AliasPriorityDefault { get; set; } = new List<string> { "VCV", "CVVC", "CROSS_CV", "CV", "BEGINING_CV" };
        public List<string> AliasPriorityDifAppend { get; set; } = new List<string> { "CVVC", "VCV", "CROSS_CV", "CV", "BEGINING_CV" };
        public List<string> AliasPriorityDifPitch { get; set; } = new List<string> { "CVVC", "VCV", "CROSS_CV", "CV", "BEGINING_CV" };
        public bool Split { get; set; } = true;
        public bool MustVC { get; set; } = false;
        public string CFlags { get; set; } = "p0";
        public bool VCLengthFromCV { get; set; } = true;
        /**  
            <summary>
                0: not 1: add ending note 2: convert last note
            </summary>
        */
        public int AddEnding { get; set; } = 1;

        public Dictionary<string, PresampPhoneme> PhonemeList { get; set; } = new Dictionary<string, PresampPhoneme>();

        public Presamp() {
            SetVowels(defVowels);
            SetConsonants(defConsonants);
            Replace = defReplace;
            Nums = defNums;
            Appends = defAppends;
            Pitches = defPitches;

            MakePhonemeList();
        }

        public void ReadPresampIni(string dirPath, Encoding textFileEncoding) {
            try {
                string iniPath = Path.Combine(dirPath, "presamp.ini");
                if (!File.Exists(iniPath)) {
                    FileExists = false;
                } else {
                    FileExists = true;

                    List<IniBlock> blocks;
                    using (var reader = new StreamReader(iniPath, textFileEncoding)) {
                        blocks = Ini.ReadBlocks(reader, iniPath, @"\[\w+\]", false);
                    }

                    if (Ini.TryGetLines(blocks, "[VOWEL]", out List<IniLine> vowelLines)) {
                        SetVowels(vowelLines.Select(l => l.line).ToList());
                    }

                    if (Ini.TryGetLines(blocks, "[CONSONANT]", out List<IniLine> consonantLines)) {
                        SetConsonants(consonantLines.Select(l => l.line).ToList());
                    }

                    if (Ini.TryGetLines(blocks, "[PRIORITY]", out List<IniLine> priority)) {
                        Priorities.Clear();
                        if (priority.Count > 0) {
                            Priorities.AddRange(priority[0].line.Split(','));
                        }
                    }

                    if (Ini.TryGetLines(blocks, "[REPLACE]", out List<IniLine> replace)) {
                        Replace.Clear();
                        replace.ForEach(r => {
                            var s = r.line.Split('=');
                            Replace.Add(s[0], s[1]);
                        });
                    }

                    // AliasRules
                    if (Ini.TryGetLines(blocks, "[ALIAS]", out List<IniLine> alias)) {
                        foreach (IniLine line in alias) {
                            var parts = line.line.Split('=');
                            switch (parts[0]) {
                                case "VCV":
                                    AliasRules.VCV = parts[1];
                                    break;
                                case "BEGINING_CV":
                                    AliasRules.BEGINING_CV = parts[1];
                                    break;
                                case "CROSS_CV":
                                    AliasRules.CROSS_CV = parts[1];
                                    break;
                                case "VC":
                                    AliasRules.VC = parts[1];
                                    break;
                                case "CV":
                                    AliasRules.CV = parts[1];
                                    break;
                                case "C":
                                    AliasRules.C = parts[1];
                                    break;
                                case "LONG_V":
                                    AliasRules.LONG_V = parts[1];
                                    break;
                                case "VCPAD":
                                    AliasRules.VCPAD = parts[1];
                                    break;
                                case "VCVPAD":
                                    AliasRules.VCVPAD = parts[1];
                                    break;
                                case "ENDING1":
                                    AliasRules.ENDING1 = parts[1];
                                    break;
                                case "ENDING2":
                                    AliasRules.ENDING2 = parts[1];
                                    break;
                            }
                        }
                    }
                    if (Ini.TryGetLines(blocks, "[ENDTYPE]", out List<IniLine> endtype)) {
                        AliasRules.ENDING1 = endtype[0].line;
                    }
                    if (Ini.TryGetLines(blocks, "[ENDTYPE1]", out List<IniLine> endtype1)) {
                        AliasRules.ENDING1 = endtype1[0].line;
                    }
                    if (Ini.TryGetLines(blocks, "[ENDTYPE2]", out List<IniLine> endtype2)) {
                        AliasRules.ENDING2 = endtype2[0].line;
                    }
                    // Ignore [VCPAD] block as it was not used.


                    if (Ini.TryGetLines(blocks, "[PRE]", out List<IniLine> pre)) {
                        Prefixs.Clear();
                        pre.ForEach(p => { Prefixs.Add(p.line); });
                    }
                    if (Ini.TryGetLines(blocks, "[SU]", out List<IniLine> su)) {
                        SuffixOrder.Clear();
                        string str = su[0].line;
                        for (int i = 1; i < 3 && str != ""; i++) {
                            if (str.StartsWith("%num%")) {
                                SuffixOrder.Add("%num%");
                                str = str.Replace("%num%", "");
                            }
                            if (str.StartsWith("%append%")) {
                                SuffixOrder.Add("%append%");
                                str = str.Replace("%append%", "");
                            }
                            if (str.StartsWith("%pitch%")) {
                                SuffixOrder.Add("%pitch%");
                                str = str.Replace("%pitch%", "");
                            }
                        }
                    }

                    // think about @UNDERBAR@ @NOREPEAT@ when you need it.
                    if (Ini.TryGetLines(blocks, "[NUM]", out List<IniLine> num)) {
                        Nums.Clear();
                        foreach (var n in num) {
                            if (!Regex.IsMatch(n.line, "^@.+@$")) {
                                Nums.Add(n.line);
                            }
                        }
                    }
                    if (Ini.TryGetLines(blocks, "[APPEND]", out List<IniLine> append)) {
                        Appends.Clear();
                        foreach (var a in append) {
                            if (!Regex.IsMatch(a.line, "^@.+@$")) {
                                Appends.Add(a.line);
                            }
                        }
                    }
                    if (Ini.TryGetLines(blocks, "[PITCH]", out List<IniLine> pitch)) {
                        Pitches.Clear();
                        foreach (var p in pitch) {
                            if (!Regex.IsMatch(p.line, "^@.+@$")) {
                                Pitches.Add(p.line);
                            }
                        }
                    }


                    // ALIAS_PRIORITY
                    if (Ini.TryGetLines(blocks, "[ALIAS_PRIORITY]", out List<IniLine> ap1)) {
                        AliasPriorityDefault.Clear();
                        ap1.ForEach(a => AliasPriorityDefault.Add(a.line));
                    }
                    if (Ini.TryGetLines(blocks, "[ALIAS_PRIORITY_DIFAPPEND]", out List<IniLine> ap2)) {
                        AliasPriorityDifAppend.Clear();
                        ap2.ForEach(a => AliasPriorityDifAppend.Add(a.line));
                    }
                    if (Ini.TryGetLines(blocks, "[ALIAS_PRIORITY_DIFPITCH]", out List<IniLine> ap3)) {
                        AliasPriorityDifPitch.Clear();
                        ap3.ForEach(a => AliasPriorityDifPitch.Add(a.line));
                    }


                    if (Ini.TryGetLines(blocks, "[SPLIT]", out List<IniLine> split)) {
                        if (split[0].line == "0") {
                            Split = false;
                        } else if (split[0].line == "1") {
                            Split = true;
                        }
                    }
                    if (Ini.TryGetLines(blocks, "[MUSTVC]", out List<IniLine> mustVC)) {
                        if (mustVC[0].line == "0") {
                            MustVC = false;
                        } else if (mustVC[0].line == "1") {
                            MustVC = true;
                        }
                    }
                    if (Ini.TryGetLines(blocks, "[CFLAGS]", out List<IniLine> cflags)) {
                        CFlags = cflags[0].line;
                    }
                    if (Ini.TryGetLines(blocks, "[VCLENGTH]", out List<IniLine> vcLength)) {
                        if (vcLength[0].line == "0") {
                            VCLengthFromCV = true;
                        } else if (vcLength[0].line == "1") {
                            VCLengthFromCV = false;
                        }
                    }

                    if (Ini.TryGetLines(blocks, "[ENDFLAG]", out List<IniLine> endflag)) {
                        if (int.TryParse(endflag[0].line, out int result)) {
                            AddEnding = result;
                        }
                    }
                }
                
            } catch (Exception e) {
                Log.Error(e, "failed to load presamp.ini");
            }

            MakePhonemeList();
        }

        private void MakePhonemeList() {
            PhonemeList.Clear();
            foreach (PresampConsonant pc in Consonants.Values) {
                if (!PhonemeList.ContainsKey(pc.Consonant)) {
                    PresampPhoneme pp = new PresampPhoneme();
                    pp.Phoneme = pc.Consonant;
                    pp.Consonant = pc.Consonant;
                    pp.NotClossfade = pc.NotClossfade;
                    PhonemeList.Add(pc.Consonant, pp);
                }

                foreach (string phoneme in pc.Phonemes) {
                    if (!PhonemeList.ContainsKey(phoneme)) {
                        PresampPhoneme pp = new PresampPhoneme();
                        pp.Phoneme = phoneme;
                        pp.Consonant = pc.Consonant;
                        pp.NotClossfade = pc.NotClossfade;
                        PhonemeList.Add(phoneme, pp);
                    }
                }
            }
            foreach (PresampVowel pv in Vowels.Values) {
                foreach (string phoneme in pv.Phonemes) {
                    if (PhonemeList.TryGetValue(phoneme, out PresampPhoneme pp)) {
                        pp.Vowel = pv.VowelLower;
                        pp.VowelVol = pv.Vol;
                    } else {
                        pp = new PresampPhoneme();
                        pp.Phoneme = phoneme;
                        pp.Vowel = pv.VowelLower;
                        pp.VowelVol = pv.Vol;
                        PhonemeList.Add(phoneme, pp);
                    }
                }
            }
            foreach (PresampPhoneme pp in PhonemeList.Values) { // 拗音の母音情報をゃゅょから取得する Vowel completion
                if (!pp.HasVowel) {
                    if (PhonemeList.TryGetValue(pp.Phoneme.Substring(pp.Phoneme.Length - 1), out PresampPhoneme vowel)) {
                        pp.Vowel = vowel.Vowel;
                        pp.VowelVol = vowel.VowelVol;
                    }
                }
            }
            foreach (string p in Priorities) {
                if (PhonemeList.TryGetValue(p, out PresampPhoneme phoneme)) {
                    phoneme.IsPriority = true;
                }
            }
        }


        /**  
            <summary>
                Break down lyric.
            </summary>
            <returns>
                string[] containing 0:preVowel, 1:phoneme, 2:suffix
            </returns>
        */
        public string[] ParseAlias(string lyric) {
            string preVowel = "";
            string phoneme = lyric;
            string suffix = "";

            if (phoneme.Contains(AliasRules.VCPAD)) {
                var split = phoneme.Split(AliasRules.VCPAD);
                preVowel = split[0];
                phoneme = split[1];
            } else if (phoneme.Contains(AliasRules.VCVPAD)) {
                var split = phoneme.Split(AliasRules.VCVPAD);
                preVowel = split[0];
                phoneme = split[1];
            }
            if(phoneme == "") {
                return new string[] { preVowel, phoneme, suffix };
            }

            string saving = phoneme.Substring(0, 1);
            phoneme = phoneme.Substring(1);

            Nums.ForEach(n => {
                if (phoneme.Contains(n)) {
                    var split = phoneme.Split(n);
                    suffix = new Regex(split[0]).Replace(phoneme, "", 1) + suffix;
                    phoneme = split[0];
                };
            });
            Appends.ForEach(a => {
                if (phoneme.Contains(a)) {
                    var split = phoneme.Split(a);
                    suffix = new Regex(split[0]).Replace(phoneme, "", 1) + suffix;
                    phoneme = split[0];
                };
            });
            Pitches.ForEach(p => {
                if (phoneme.Contains(p)) {
                    var split = phoneme.Split(p);
                    suffix = new Regex(split[0]).Replace(phoneme, "", 1) + suffix;
                    phoneme = split[0];
                };
            });
            if (phoneme.Contains("_")) {
                var split = phoneme.Split("_");
                suffix = new Regex(split[0]).Replace(phoneme, "", 1) + suffix;
                phoneme = split[0];
            }

            return new string[] { preVowel, saving + phoneme, suffix };
        }

        public void SetVowels(List<string> list) {
            var dict = new Dictionary<string, PresampVowel>();
            foreach (var line in list) {
                var parts = line.Split('=');

                if (parts.Length >= 4) {
                    var vowel = new PresampVowel();
                    vowel.VowelLower = parts[0];
                    vowel.VowelUpper = parts[1];
                    string[] sounds = parts[2].Split(',');
                    foreach (var sound in sounds) {
                        vowel.Phonemes.Add(sound);
                    }
                    if (int.TryParse(parts[3], out var vol)) {
                        vowel.Vol = vol;
                    }
                    dict.Add(vowel.VowelLower, vowel);
                }
            }
            Vowels = dict;
        }
        public void SetVowels(Dictionary<string, PresampVowel> dict) {
            Vowels = dict;
        }
        public void SetConsonants(List<string> list) {
            var dict = new Dictionary<string, PresampConsonant>();
            foreach (var line in list) {
                var parts = line.Split('=');

                if (parts.Length >= 3) {
                    var consonant = new PresampConsonant();
                    consonant.Consonant = parts[0];
                    string[] sounds = parts[1].Split(',');
                    foreach (var sound in sounds) {
                        consonant.Phonemes.Add(sound);
                    }
                    if (parts[2] == "1") {
                        consonant.NotClossfade = true;
                    }
                    dict.Add(consonant.Consonant, consonant);
                }
            }
            Consonants = dict;
        }
        public void SetConsonants(Dictionary<string, PresampConsonant> dict) {
            Consonants = dict;
        }

    }


    public class PresampPhoneme {
        public string Phoneme { get; set; } = "";
        public string Vowel { get; set; } = "";
        public int VowelVol { get; set; } = 100;
        public string Consonant { get; set; } = "";
        public bool NotClossfade { get; set; } = false;
        public bool IsPriority = false;

        public bool HasVowel { get => Vowel != ""; }
        public bool HasConsonant { get => Consonant != ""; }
    }
    public class PresampVowel {
        public string VowelLower { get; set; } = "";
        public string VowelUpper { get; set; } = "";
        public List<string> Phonemes { get; set; } = new List<string>();
        public int Vol = 100;
    }
    public class PresampConsonant {
        public string Consonant { get; set; } = "";
        public List<string> Phonemes { get; set; } = new List<string>();
        public bool NotClossfade { get; set; } = false;
    }
    public class PresampAliasRules {
        public string VCPAD = " ";
        public string VCVPAD = " ";

        private string vcv = "%v%%VCVPAD%%CV%";
        public string VCV {
            get => Replacer(vcv);
            set => vcv = value;
        }
        // write eventually
        public string BEGINING_CV = "-%VCVPAD%%CV%";
        public string CROSS_CV = "*%VCVPAD%%CV%";
        public string VC = "%v%%vcpad%%c%,%c%%vcpad%%c%";
        public string CV = "%CV%,%c%%V%";
        public string C = "%c%";
        public string LONG_V = "%V%ー";
        public string ENDING1 = "%v%%VCPAD%R";
        public string ENDING2 = "-";

        private string Replacer(string str) {
            return str.Replace("%CVPAD%", VCPAD).Replace("%VCVPAD%", VCVPAD);
        }
    }


    public partial class Presamp {
        // default value of presamp.ini (from https://delta-kimigatame.hatenablog.jp/entry/ar483589 )

        private readonly static List<string> defVowels = new List<string> {
            { "a=あ=ぁ,あ,か,が,さ,ざ,た,だ,な,は,ば,ぱ,ま,ゃ,や,ら,わ,ァ,ア,カ,ガ,サ,ザ,タ,ダ,ナ,ハ,バ,パ,マ,ャ,ヤ,ラ,ワ=100" },
            { "i=い=ぃ,い,き,ぎ,し,じ,ち,ぢ,に,ひ,び,ぴ,み,り,ゐ,ィ,イ,キ,ギ,シ,ジ,チ,ヂ,ニ,ヒ,ビ,ピ,ミ,リ,ヰ=100" },
            { "u=う=ぅ,う,く,ぐ,す,ず,つ,づ,ぬ,ふ,ぶ,ぷ,む,ゅ,ゆ,る,ゥ,ウ,ク,グ,ス,ズ,ツ,ヅ,ヌ,フ,ブ,プ,ム,ュ,ユ,ル,ヴ=100" },
            { "e=え=ぇ,え,け,げ,せ,ぜ,て,で,ね,へ,べ,ぺ,め,れ,ゑ,ェ,エ,ケ,ゲ,セ,ゼ,テ,デ,ネ,ヘ,ベ,ペ,メ,レ,ヱ=100" },
            { "o=お=ぉ,お,こ,ご,そ,ぞ,と,ど,の,ほ,ぼ,ぽ,も,ょ,よ,ろ,を,ォ,オ,コ,ゴ,ソ,ゾ,ト,ド,ノ,ホ,ボ,ポ,モ,ョ,ヨ,ロ,ヲ=100" },
            { "n=ん=ん=100" },
            { "N=ン=ン=100" }
        };
        private readonly static List<string> defConsonants = new List<string> {
            { "ch=ch,ち,ちぇ,ちゃ,ちゅ,ちょ=1" },
            { "gy=gy,ぎ,ぎぇ,ぎゃ,ぎゅ,ぎょ=1" },
            { "ts=ts,つ,つぁ,つぃ,つぇ,つぉ=1" },
            { "ty=ty,てぃ,てぇ,てゃ,てゅ,てょ=1" },
            { "py=py,ぴ,ぴぇ,ぴゃ,ぴゅ,ぴょ=1" },
            { "ry=ry,り,りぇ,りゃ,りゅ,りょ=1" },
            { "ny=ny,に,にぇ,にゃ,にゅ,にょ=0" },
            { "r=r,ら,る,れ,ろ=1" },
            { "hy=hy,ひ,ひぇ,ひゃ,ひゅ,ひょ=0" },
            { "dy=dy,でぃ,でぇ,でゃ,でゅ,でょ=1" },
            { "by=by,び,びぇ,びゃ,びゅ,びょ=1" },
            { "b=b,ば,ぶ,べ,ぼ=1" },
            { "d=d,だ,で,ど,どぅ=1" },
            { "g=g,が,ぐ,げ,ご=1" },
            { "f=f,ふ,ふぁ,ふぃ,ふぇ,ふぉ=0" },
            { "h=h,は,へ,ほ=0" },
            { "k=k,か,く,け,こ=1" },
            { "j=j,じ,じぇ,じゃ,じゅ,じょ=0" },
            { "m=m,ま,む,め,も=0" },
            { "n=n,な,ぬ,ね,の=0" },
            { "p=p,ぱ,ぷ,ぺ,ぽ=1" },
            { "s=s,さ,す,すぃ,せ,そ=0" },
            { "sh=sh,し,しぇ,しゃ,しゅ,しょ=0" },
            { "t=t,た,て,と,とぅ=1" },
            { "w=w,うぃ,うぅ,うぇ,うぉ,わ,を=0" },
            { "v=v,ヴ,ヴぁ,ヴぃ,ヴぅ,ヴぇ,ヴぉ=0" },
            { "y=y,いぃ,いぇ,や,ゆ,よ,ゐ,ゑ=0" },
            { "ky=ky,き,きぇ,きゃ,きゅ,きょ=1" },
            { "z=z,ざ,ず,ずぃ,ぜ,ぞ=0" },
            { "my=my,み,みぇ,みゃ,みゅ,みょ=0" }
        };

        private readonly static Dictionary<string, string> defReplace = new Dictionary<string, string> {
            { "N", "ン" },
            { "a", "あ" },
            { "ba", "ば" },
            { "be", "べ" },
            { "bi", "び" },
            { "bo", "ぼ" },
            { "bu", "ぶ" },
            { "bya", "びゃ" },
            { "bye", "びぇ" },
            { "byi", "び" },
            { "byo", "びょ" },
            { "byu", "びゅ" },
            { "cha", "ちゃ" },
            { "che", "ちぇ" },
            { "chi", "ち" },
            { "cho", "ちょ" },
            { "chu", "ちゅ" },
            { "da", "だ" },
            { "de", "で" },
            { "dha", "でゃ" },
            { "dhe", "でぇ" },
            { "dhi", "でぃ" },
            { "dho", "でょ" },
            { "dhu", "でゅ" },
            { "di", "でぃ" },
            { "do", "ど" },
            { "du", "どぅ" },
            { "e", "え" },
            { "fa", "ふぁ" },
            { "fe", "ふぇ" },
            { "fi", "ふぃ" },
            { "fo", "ふぉ" },
            { "fu", "ふ" },
            { "fyu", "ふゅ" },
            { "ga", "が" },
            { "ge", "げ" },
            { "gi", "ぎ" },
            { "go", "ご" },
            { "gu", "ぐ" },
            { "gya", "ぎゃ" },
            { "gye", "ぎぇ" },
            { "gyi", "ぎ" },
            { "gyo", "ぎょ" },
            { "gyu", "ぎゅ" },
            { "ha", "は" },
            { "he", "へ" },
            { "hi", "ひ" },
            { "ho", "ほ" },
            { "hu", "ふ" },
            { "hya", "ひゃ" },
            { "hye", "ひぇ" },
            { "hyi", "ひ" },
            { "hyo", "ひょ" },
            { "hyu", "ひゅ" },
            { "i", "い" },
            { "ja", "じゃ" },
            { "je", "じぇ" },
            { "ji", "じ" },
            { "jo", "じょ" },
            { "ju", "じゅ" },
            { "ka", "か" },
            { "ke", "け" },
            { "ki", "き" },
            { "ko", "こ" },
            { "ku", "く" },
            { "kya", "きゃ" },
            { "kye", "きぇ" },
            { "kyi", "き" },
            { "kyo", "きょ" },
            { "kyu", "きゅ" },
            { "ma", "ま" },
            { "me", "め" },
            { "mi", "み" },
            { "mo", "も" },
            { "mu", "む" },
            { "mya", "みゃ" },
            { "mye", "みぇ" },
            { "myi", "み" },
            { "myo", "みょ" },
            { "myu", "みゅ" },
            { "n", "ん" },
            { "na", "な" },
            { "ne", "ね" },
            { "ni", "に" },
            { "no", "の" },
            { "nu", "ぬ" },
            { "nya", "にゃ" },
            { "nye", "にぇ" },
            { "nyi", "に" },
            { "nyo", "にょ" },
            { "nyu", "にゅ" },
            { "o", "お" },
            { "pa", "ぱ" },
            { "pe", "ぺ" },
            { "pi", "ぴ" },
            { "po", "ぽ" },
            { "pu", "ぷ" },
            { "pya", "ぴゃ" },
            { "pye", "ぴぇ" },
            { "pyi", "ぴ" },
            { "pyo", "ぴょ" },
            { "pyu", "ぴゅ" },
            { "ra", "ら" },
            { "re", "れ" },
            { "ri", "り" },
            { "ro", "ろ" },
            { "ru", "る" },
            { "rya", "りゃ" },
            { "rye", "りぇ" },
            { "ryi", "り" },
            { "ryo", "りょ" },
            { "ryu", "りゅ" },
            { "sa", "さ" },
            { "se", "せ" },
            { "sha", "しゃ" },
            { "she", "しぇ" },
            { "shi", "し" },
            { "sho", "しょ" },
            { "shu", "しゅ" },
            { "si", "すぃ" },
            { "so", "そ" },
            { "su", "す" },
            { "ta", "た" },
            { "te", "て" },
            { "ti", "てぃ" },
            { "to", "と" },
            { "tsa", "つぁ" },
            { "tse", "つぇ" },
            { "tsi", "つぃ" },
            { "tso", "つぉ" },
            { "tsu", "つ" },
            { "tu", "とぅ" },
            { "tya", "てゃ" },
            { "tye", "てぇ" },
            { "tyi", "てぃ" },
            { "tyo", "てょ" },
            { "tyu", "てゅ" },
            { "u", "う" },
            { "va", "ヴぁ" },
            { "ve", "ヴぇ" },
            { "vi", "ヴぃ" },
            { "vo", "ヴぉ" },
            { "vu", "ヴ" },
            { "vyu", "ヴゅ" },
            { "wa", "わ" },
            { "we", "うぇ" },
            { "wi", "うぃ" },
            { "wo", "うぉ" },
            { "wu", "うぅ" },
            { "ya", "や" },
            { "ye", "いぇ" },
            { "yi", "い" },
            { "yo", "よ" },
            { "yu", "ゆ" },
            { "za", "ざ" },
            { "ze", "ぜ" },
            { "zi", "ずぃ" },
            { "zo", "ぞ" },
            { "zu", "ず" },
            { "zya", "じゃ" },
            { "zye", "じぇ" },
            { "zyi", "じ" },
            { "zyo", "じょ" },
            { "zyu", "じゅ" },
            { "を", "お" },
            { "ぢ", "じ" },
            { "づ", "ず" }
        };

        private readonly static List<string> defNums = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        private readonly static List<string> defAppends = new List<string> {
            "強", "弱", "暗", "明", "囁", "叫", "超", "幼", "甘", "笑", "瞭", "憂", "優", "感", "張", "抗", "泣叫", "癖", "元気", "艶" };
        private readonly static List<string> defPitches = new List<string> {
            "C1","C#1","Db1","D1","D#1","Eb1","E1","F1","F#1","Gb1","G1","G#1","Ab1","A1","A#1","Bb1","B1",
            "C2","C#2","Db2","D2","D#2","EB2","E2","F2","F#2","Gb2","G2","G#2","Ab2","A2","A#2","Bb2","B2",
            "C3","C#3","Db3","D3","D#3","EB3","E3","F3","F#3","Gb3","G3","G#3","Ab3","A3","A#3","Bb3","B3",
            "C4","C#4","Db4","D4","D#4","EB4","E4","F4","F#4","Gb4","G4","G#4","Ab4","A4","A#4","Bb4","B4",
            "C5","C#5","Db5","D5","D#5","EB5","E5","F5","F#5","Gb5","G5","G#5","Ab5","A5","A#5","Bb5","B5",
            "C6","C#6","Db6","D6","D#6","EB6","E6","F6","F#6","Gb6","G6","G#6","Ab6","A6","A#6","Bb6","B6",
            "C7","C#7","Db7","D7","D#7","EB7","E7","F7","F#7","Gb7","G7","G#7","Ab7","A7","A#7","Bb7","B7",
            "↑","↓","→","←","high","low","mid" };

    }
}
