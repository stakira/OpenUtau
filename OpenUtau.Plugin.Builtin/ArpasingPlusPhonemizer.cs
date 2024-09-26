using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Classic;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.G2p;
using OpenUtau.Core.Ustx;
using Serilog;
using YamlDotNet.Core.Tokens;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Arpasing+ Phonemizer", "EN ARPA+", "Cadlaxa", language: "EN")]
    // Custom ARPAsing Phonemizer for OU
    // main focus of this Phonemizer is to bring fallbacks to existing available alias from
    // all ARPAsing banks
    // Help are highly appreciated
    public class ArpasingPlusPhonemizer : SyllableBasedPhonemizer {
        private readonly string[] vowels = {
        "aa", "ax", "ae", "ah", "ao", "aw", "ay", "eh", "er", "ey", "ih", "iy", "ow", "oy", "uh", "uw", "a", "e", "i", "o", "u", "ai", "ei", "oi", "au", "ou", "ix", "ux",
        "aar", "ar", "axr", "aer", "ahr", "aor", "or", "awr", "aur", "ayr", "air", "ehr", "eyr", "eir", "ihr", "iyr", "ir", "owr", "our", "oyr", "oir", "uhr", "uwr", "ur",
        "aal", "al", "axl", "ael", "ahl", "aol", "ol", "awl", "aul", "ayl", "ail", "ehl", "el", "eyl", "eil", "ihl", "iyl", "il", "owl", "oul", "oyl", "oil", "uhl", "uwl", "ul",
        "aan", "an", "axn", "aen", "ahn", "aon", "on", "awn", "aun", "ayn", "ain", "ehn", "en", "eyn", "ein", "ihn", "iyn", "in", "own", "oun", "oyn", "oin", "uhn", "uwn", "un",
        "aang", "ang", "axng", "aeng", "ahng", "aong", "ong", "awng", "aung", "ayng", "aing", "ehng", "eng", "eyng", "eing", "ihng", "iyng", "ing", "owng", "oung", "oyng", "oing", "uhng", "uwng", "ung",
        "aam", "am", "axm", "aem", "ahm", "aom", "om", "awm", "aum", "aym", "aim", "ehm", "em", "eym", "eim", "ihm", "iym", "im", "owm", "oum", "oym", "oim", "uhm", "uwm", "um", "oh",
        "eu", "oe", "yw", "yx", "wx", "ox", "ex", "ea", "ia", "oa", "ua", "ean", "eam", "eang"
        };
        private readonly string[] consonants = "b,ch,d,dh,dr,dx,f,g,hh,jh,k,l,m,n,nx,ng,p,q,r,s,sh,t,th,tr,v,w,y,z,zh".Split(',');
        private readonly string[] affricates = "ch,jh,j".Split(',');
        private readonly string[] tapConsonant = "dx,nx,lx".Split(",");
        private readonly string[] semilongConsonants = "ng,n,m,v,z,q,hh".Split(",");
        private readonly string[] semiVowels = "y,w".Split(",");
        private readonly string[] connectingGlides = "l,r,ll".Split(",");
        private readonly string[] longConsonants = "f,s,sh,th,zh,dr,tr,ts,c".Split(",");
        private readonly string[] normalConsonants = "b,d,dh,g,k,p,t,l,r".Split(',');
        private readonly string[] connectingNormCons = "b,d,g,k,p,t".Split(',');
        private readonly Dictionary<string, string> dictionaryReplacements = ("dx=dx;dr=dr;tr=tr").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);
        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict-0_7b.txt";
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;


        // For banks with missing vowels
        private readonly Dictionary<string, string> missingVphonemes = "ax=ah,aa=ah,ae=ah,iy=ih,uh=uw,ix=ih,ux=uh,oh=ao,eu=uh,oe=ax,uy=uw,yw=uw,yx=iy,wx=uw,ea=eh,ia=iy,oa=ao,ua=uw,R=-".Split(',')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);
        private bool isMissingVPhonemes = false;

        // For banks with missing custom consonants
        private readonly Dictionary<string, string> missingCphonemes = "nx=n,tx=t,dx=d,zh=sh,z=s,ng=n,cl=q,vf=q,dd=d,lx=l".Split(',')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);
        private bool isMissingCPhonemes = false;

        // TIMIT symbols
        private readonly Dictionary<string, string> timitphonemes = "axh=ax,bcl=b,dcl=d,eng=ng,gcl=g,hv=hh,kcl=k,pcl=p,tcl=t".Split(',')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);
        private bool isTimitPhonemes = false;
        private bool vc_FallBack = false;
        private bool cPV_FallBack = false;

        private readonly Dictionary<string, string> vcFallBacks =
            new Dictionary<string, string>() {
                {"aw","uw"},
                {"ow","uw"},
                {"uh","uw"},
                {"ay","iy"},
                {"ey","iy"},
                {"oy","iy"},
                {"aa","ah"},
                {"ae","ah"},
                {"ao","ah"},
                //{"eh","ah"},
                //{"er","ah"},
            };

        private readonly Dictionary<string, string> vvExceptions =
            new Dictionary<string, string>() {
                {"aw","w"},
                {"ow","w"},
                {"uw","w"},
                {"uh","w"},
                {"ay","y"},
                {"ey","y"},
                {"iy","y"},
                {"oy","y"},
                {"ih","y"},
                {"er","r"},
                {"aar","r"},
                {"aen","n"},
                {"aeng","ng"},
                {"aor","r"},
                {"ehr","r"},
                {"ihng","ng"},
                {"ihr","r"},
                {"uwr","r"},
                {"awn","n"},
                {"awng","ng"},
                {"ean","n"},
                {"eam","m"},
                {"eang","ng"},
                // r-colored vowel and l
                {"ar","r"},
                {"or","r"},
                {"air","r"},
                {"ir","r"},
                {"ur","r"},
                {"al","l"},
                {"ol","l"},
                {"il","l"},
                {"el","l"},
                {"ul","l"},
            };
        private readonly Dictionary<string, string> vvDiphthongExceptions =
            new Dictionary<string, string>() {
                {"aw","ah"},
                {"ow","ao"},
                {"uw","uh"},
                {"ay","ah"},
                {"ey","eh"},
                {"oy","ao"},
            };

        private readonly string[] ccvException = { "ch", "dh", "dx", "fh", "gh", "hh", "jh", "kh", "ph", "ng", "sh", "th", "vh", "wh", "zh" };
        private readonly string[] RomajiException = { "a", "e", "i", "o", "u" };
        private string[] tails = "-,R".Split(',');

        protected override string[] GetSymbols(Note note) {
            string[] original = base.GetSymbols(note);
            if (tails.Contains(note.lyric)) {
                return new string[] { note.lyric };
            }
            if (original == null) {
                return null;
            }
            List<string> modified = new List<string>();

            // SPLITS UP DR AND TR
            string[] tr = new[] { "tr" };
            string[] dr = new[] { "dr" };
            string[] wh = new[] { "wh" };
            string[] c_wy = new[] { "by", "dy", "fy", "gy", "hy", "jy", "ky", "ly", "my", "ny", "py", "ry", "sy", "ty", "vy", "zy",
                                    "bw", "chw", "dw", "fw", "gw", "hw", "jw", "kw", "lw", "mw", "nw", "pw", "rw", "sw", "tw", "vw", "zw"};
            string[] av_c = new[] { "al", "am", "an", "ang", "ar" };
            string[] ev_c = new[] { "el", "em", "en", "eng", "err" };
            string[] iv_c = new[] { "il", "im", "in", "ing", "ir" };
            string[] ov_c = new[] { "ol", "om", "on", "ong", "or" };
            string[] uv_c = new[] { "ul", "um", "un", "ung", "ur" };
            var consonatsV1 = new List<string> { "l", "m", "n", "r" };
            var consonatsV2 = new List<string> { "mm", "nn", "ng" };
            // SPLITS UP 2 SYMBOL VOWELS AND 1 SYMBOL CONSONANT
            List<string> vowel3S = new List<string>();
            foreach (string V1 in vowels) {
                foreach (string C1 in consonatsV1) {
                    vowel3S.Add($"{V1}{C1}");
                }
            }
            // SPLITS UP 2 SYMBOL VOWELS AND 2 SYMBOL CONSONANT
            List<string> vowel4S = new List<string>();
            foreach (string V1 in vowels) {
                foreach (string C1 in consonatsV2) {
                    vowel3S.Add($"{V1}{C1}");
                }
            }
            foreach (string s in original) {
                switch (s) {
                    case var str when dr.Contains(str) && !HasOto($"{str} {vowels}", note.tone) && !HasOto($"ay {str}", note.tone):
                        modified.AddRange(new string[] { "jh", s[1].ToString() });
                        break;
                    case var str when tr.Contains(str) && !HasOto($"{str} {vowels}", note.tone) && !HasOto($"ay {str}", note.tone):
                        modified.AddRange(new string[] { "ch", s[1].ToString() });
                        break;
                    case var str when wh.Contains(str) && !HasOto($"{str} {vowels}", note.tone) && !HasOto($"ay {str}", note.tone):
                        modified.AddRange(new string[] { "hh", s[1].ToString() });
                        break;
                    case var str when c_wy.Contains(str) && !HasOto($"{str} {vowels}", note.tone) && !HasOto($"{str}", note.tone):
                        modified.AddRange(new string[] { s[0].ToString(), s[1].ToString() });
                        break;
                    case var str when av_c.Contains(str) && !HasOto($"b {str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { "aa", s[1].ToString() });
                        break;
                    case var str when ev_c.Contains(str) && !HasOto($"b {str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { "eh", s[1].ToString() });
                        break;
                    case var str when iv_c.Contains(str) && !HasOto($"b {str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { "iy", s[1].ToString() });
                        break;
                    case var str when ov_c.Contains(str) && !HasOto($"b {str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { "ao", s[1].ToString() });
                        break;
                    case var str when uv_c.Contains(str) && !HasOto($"b {str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { "uw", s[1].ToString() });
                        break;
                    case var str when vowel3S.Contains(str) && !HasOto($"b {str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { s.Substring(0, 2), s[2].ToString() });
                        break;
                    case var str when vowel4S.Contains(str) && !HasOto($"b {str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { s.Substring(0, 2), s.Substring(2, 2) });
                        break;
                    default:
                        modified.Add(s);
                        break;
                }
            }
            return modified.ToArray();
        }

        protected override IG2p LoadBaseDictionary() {
            var g2ps = new List<IG2p>();
            // LOAD DICTIONARY FROM FOLDER
            string path = Path.Combine(PluginDir, "arpasing.yaml");
            if (!File.Exists(path)) {
                Directory.CreateDirectory(PluginDir);
                File.WriteAllBytes(path, Data.Resources.arpasing_template);
            }
            // LOAD DICTIONARY FROM SINGER FOLDER
            if (singer != null && singer.Found && singer.Loaded) {
                string file = Path.Combine(singer.Location, "arpasing.yaml");
                if (File.Exists(file)) {
                    try {
                        g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(file)).Build());
                    } catch (Exception e) {
                        Log.Error(e, $"Failed to load {file}");
                    }
                }
            }
            g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(path)).Build());
            g2ps.Add(new ArpabetPlusG2p());
            return new G2pFallbacks(g2ps.ToArray());
        }
        protected override List<string> ProcessSyllable(Syllable syllable) {
            syllable.prevV = tails.Contains(syllable.prevV) ? "" : syllable.prevV;
            var prevV = syllable.prevV == "" ? "" : $"{syllable.prevV}";
            //string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;
            string basePhoneme;
            var phonemes = new List<string>();
            var lastC = cc.Length - 1;
            var firstC = 0;

            // Check for missing vowel phonemes
            foreach (var entry in missingVphonemes) {
                if (!HasOto(entry.Key, syllable.tone) && !HasOto(entry.Key, syllable.tone)) {
                    isMissingVPhonemes = true;
                    break;
                }
            }

            // Check for missing consonant phonemes
            foreach (var entry in missingCphonemes) {
                if (!HasOto(entry.Key, syllable.tone) && !HasOto(entry.Value, syllable.tone)) {
                    isMissingCPhonemes = true;
                    break;
                }
            }

            // Check for missing TIMIT phonemes
            foreach (var entry in timitphonemes) {
                if (!HasOto(entry.Key, syllable.tone) && !HasOto(entry.Value, syllable.tone)) {
                    isTimitPhonemes = true;
                    break;
                }
            }

            // For VC Fallback phonemes
            foreach (var entry in vcFallBacks) {
                if (!HasOto($"{entry.Key} {cc}", syllable.tone) || (!HasOto($"ao {cc}", syllable.tone))) {
                    vc_FallBack = true;
                }
            }

            // STARTING V
            if (syllable.IsStartingV) {
                // TRIES - V THEN V
                var rv = $"- {v}";
                if (HasOto(rv, syllable.vowelTone) || HasOto(ValidateAlias(rv), syllable.vowelTone)) {
                    basePhoneme = rv;
                } else {
                    basePhoneme = v;
                }
            }
            // [V V] or [V C][C V]/[V]
            else if (syllable.IsVV) {
                if (!CanMakeAliasExtension(syllable)) {
                    basePhoneme = $"{prevV} {v}";
                    if (!HasOto(basePhoneme, syllable.vowelTone) && !HasOto(ValidateAlias(basePhoneme), syllable.vowelTone) && vvExceptions.ContainsKey(prevV) && prevV != v) {
                        // VV IS NOT PRESENT, CHECKS VVEXCEPTIONS LOGIC
                        var vc = $"{prevV}{vvExceptions[prevV]}";
                        if (!HasOto(vc, syllable.vowelTone) && !HasOto(ValidateAlias(vc), syllable.vowelTone)) {
                            vc = $"{prevV} {vvExceptions[prevV]}";
                        }
                        phonemes.Add(vc);
                        var crv = $"{vvExceptions[prevV]} {v}";
                        var cv = $"{vvExceptions[prevV]}{v}";
                        basePhoneme = cv;
                        if (!HasOto(cv, syllable.vowelTone) && !HasOto(ValidateAlias(cv), syllable.vowelTone)) {
                            basePhoneme = crv;
                        }
                    } else {
                        {
                            if (HasOto($"{prevV} {v}", syllable.vowelTone) || HasOto(ValidateAlias($"{prevV} {v}"), syllable.vowelTone)) {
                                basePhoneme = $"{prevV} {v}";
                            } else if (HasOto(v, syllable.vowelTone) || HasOto(ValidateAlias(v), syllable.vowelTone)) {
                                basePhoneme = v;
                            } else {
                                // MAKE THEM A GLOTTAL STOP INSTEAD
                                basePhoneme = $"q {v}";
                                phonemes.Add($"{prevV} q");
                            }
                        }
                    }
                    // EXTEND AS [V]
                } else if (HasOto($"{v}", syllable.vowelTone) && HasOto(ValidateAlias($"{v}"), syllable.vowelTone) || missingVphonemes.ContainsKey(prevV)) {
                    basePhoneme = v;
                } else if (!HasOto(v, syllable.vowelTone) && !HasOto(ValidateAlias(v), syllable.vowelTone) && vvDiphthongExceptions.ContainsKey(prevV)) {
                    basePhoneme = $"{vvDiphthongExceptions[prevV]} {vvDiphthongExceptions[prevV]}";
                } else {
                    // PREVIOUS ALIAS WILL EXTEND as [V V]
                    basePhoneme = null;
                }

                // [- CV/C V] or [- C][CV/C V]
            } else if (syllable.IsStartingCVWithOneConsonant) {
                var rcv = $"- {cc[0]} {v}";
                var rcv1 = $"- {cc[0]}{v}";
                var crv = $"{cc[0]} {v}";
                var cv = $"{cc[0]}{v}";
                /// - CV
                if ((HasOto(rcv, syllable.vowelTone) || HasOto(ValidateAlias(rcv), syllable.vowelTone))) {
                    basePhoneme = rcv;
                } else if ((HasOto(rcv1, syllable.vowelTone) || HasOto(ValidateAlias(rcv1), syllable.vowelTone)) && (!HasOto(rcv, syllable.vowelTone) && !HasOto(ValidateAlias(rcv), syllable.vowelTone))) {
                    basePhoneme = rcv1;
                    /// CV
                } else if ((HasOto(crv, syllable.vowelTone) || HasOto(ValidateAlias(crv), syllable.vowelTone))) {
                    basePhoneme = crv;
                    TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}", $"-{cc[0]}", ValidateAlias($"- {cc[0]}"), ValidateAlias($"-{cc[0]}"));
                } else if ((HasOto(cv, syllable.vowelTone) || HasOto(ValidateAlias(cv), syllable.vowelTone)) && (!HasOto(crv, syllable.vowelTone) && !HasOto(ValidateAlias(crv), syllable.vowelTone))) {
                    basePhoneme = cv;
                    TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}", $"-{cc[0]}", ValidateAlias($"- {cc[0]}"), ValidateAlias($"-{cc[0]}"));
                    /// - C+V
                } else if ((HasOto(v, syllable.vowelTone) || HasOto(ValidateAlias(v), syllable.vowelTone)) && (!HasOto(cv, syllable.vowelTone) && !HasOto(ValidateAlias(cv), syllable.vowelTone)) && (!HasOto(crv, syllable.vowelTone) && !HasOto(ValidateAlias(crv), syllable.vowelTone))) {
                    basePhoneme = v;
                    TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}", $"-{cc[0]}", ValidateAlias($"- {cc[0]}"), ValidateAlias($"-{cc[0]}"));
                    TryAddPhoneme(phonemes, syllable.tone, $"{cc[0]} -", $"{cc[0]}-", ValidateAlias($"{cc[0]} -"), ValidateAlias($"{cc[0]}-"));
                } else if ((HasOto($"{prevV} {v}", syllable.vowelTone) || HasOto(ValidateAlias($"{prevV} {v}"), syllable.vowelTone) && (!HasOto(v, syllable.vowelTone) && !HasOto(ValidateAlias(v), syllable.vowelTone)) && (!HasOto(cv, syllable.vowelTone) && !HasOto(ValidateAlias(cv), syllable.vowelTone)) && (!HasOto(crv, syllable.vowelTone) && !HasOto(ValidateAlias(crv), syllable.vowelTone)))) {
                    basePhoneme = $"{prevV} {v}";
                    TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}", $"-{cc[0]}", ValidateAlias($"- {cc[0]}"), ValidateAlias($"-{cc[0]}"));
                    TryAddPhoneme(phonemes, syllable.tone, $"{cc[0]} -", $"{cc[0]}-", ValidateAlias($"{cc[0]} -"), ValidateAlias($"{cc[0]}-"));
                } else {
                    basePhoneme = crv;
                    TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}", $"-{cc[0]}", ValidateAlias($"- {cc[0]}"), ValidateAlias($"-{cc[0]}"));
                }
                // [CCV/CC V] or [C C] + [CV/C V]
            } else if (syllable.IsStartingCVWithMoreThanOneConsonant) {
                // TRY [- CCV]/[- CC V] or [- CC][CCV]/[CC V] or [- C][C C][C V]/[CV]
                var rccv = $"- {string.Join("", cc)} {v}";
                var rccv1 = $"- {string.Join("", cc)}{v}";
                var rccv2 = $"-{string.Join("", cc)} {v}";
                var rccv3 = $"-{string.Join("", cc)}{v}";
                var crv = $"{cc.Last()} {v}";
                var ccv = $"{string.Join("", cc)} {v}";
                /// - CCV
                if (HasOto(rccv, syllable.vowelTone) || HasOto(ValidateAlias(rccv), syllable.vowelTone) && !ccvException.Contains(cc[0])) {
                    basePhoneme = rccv;
                    lastC = 0;
                } else if (HasOto(rccv1, syllable.vowelTone) || HasOto(ValidateAlias(rccv1), syllable.vowelTone) && !ccvException.Contains(cc[0])) {
                    basePhoneme = rccv1;
                    lastC = 0;
                } else if (HasOto(rccv2, syllable.vowelTone) || HasOto(ValidateAlias(rccv2), syllable.vowelTone) && !ccvException.Contains(cc[0])) {
                    basePhoneme = rccv2;
                    lastC = 0;
                } else if (HasOto(rccv3, syllable.vowelTone) || HasOto(ValidateAlias(rccv3), syllable.vowelTone) && !ccvException.Contains(cc[0])) {
                    basePhoneme = rccv3;
                    lastC = 0;
                } else {
                    /// CCV and CV
                    if (HasOto(ccv, syllable.vowelTone) || HasOto(ValidateAlias(ccv), syllable.vowelTone) && !ccvException.Contains(cc[0])) {
                        basePhoneme = ccv;
                    } else if (HasOto(crv, syllable.vowelTone) || HasOto(ValidateAlias(crv), syllable.vowelTone)) {
                        basePhoneme = crv;
                        /// C+V
                    } else if ((HasOto(v, syllable.vowelTone) || HasOto(ValidateAlias(v), syllable.vowelTone)) && (!HasOto(crv, syllable.vowelTone) && !HasOto(ValidateAlias(ccv), syllable.vowelTone)) && (!HasOto(ccv, syllable.vowelTone) && !HasOto(ValidateAlias(crv), syllable.vowelTone))) {
                        basePhoneme = v;
                    } else if ((HasOto($"{prevV} {v}", syllable.vowelTone) || HasOto(ValidateAlias($"{prevV} {v}"), syllable.vowelTone) && (!HasOto(v, syllable.vowelTone) && !HasOto(ValidateAlias(v), syllable.vowelTone)) && (!HasOto(ccv, syllable.vowelTone) && !HasOto(ValidateAlias(ccv), syllable.vowelTone)) && (!HasOto(crv, syllable.vowelTone) && !HasOto(ValidateAlias(crv), syllable.vowelTone)))) {
                        basePhoneme = $"{prevV} {v}";
                    
                    } else {
                        basePhoneme = $"{cc.Last()}{v}";
                    }
                    // TRY RCC [- CC]
                    for (var i = cc.Length; i > 1; i--) {
                        if (!ccvException.Contains(cc[0])) {
                            if (TryAddPhoneme(phonemes, syllable.tone, $"- {string.Join("", cc.Take(i))}", $"-{string.Join("", cc.Take(i))}", ValidateAlias($"- {string.Join("", cc.Take(i))}"), ValidateAlias($"-{string.Join("", cc.Take(i))}"))) {
                                firstC = i - 1;
                            }
                        }
                        break;
                    }
                    // [- C]
                    if (phonemes.Count == 0) {
                        TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}", $"-{cc[0]}", ValidateAlias($"- {cc[0]}"), ValidateAlias($"-{cc[0]}"));
                    }
                }
            } else {
                var crv = $"{cc.Last()} {v}";
                var cv = $"{cc.Last()}{v}";
                /// CV
                if (HasOto(crv, syllable.vowelTone) || HasOto(ValidateAlias(crv), syllable.vowelTone)) {
                    basePhoneme = crv;
                } else if ((HasOto(cv, syllable.vowelTone) || HasOto(ValidateAlias(cv), syllable.vowelTone)) && (!HasOto(crv, syllable.vowelTone) && !HasOto(ValidateAlias(crv), syllable.vowelTone))) {
                    basePhoneme = cv;
                    /// C+V
                /*
                } else if ((HasOto(v, syllable.vowelTone) || HasOto(ValidateAlias(v), syllable.vowelTone))) {
                    basePhoneme = v;
                } else if ((HasOto($"{prevV} {v}", syllable.vowelTone) || HasOto(ValidateAlias($"{prevV} {v}"), syllable.vowelTone) && (!HasOto(v, syllable.vowelTone) && !HasOto(ValidateAlias(v), syllable.vowelTone)))) {
                    basePhoneme = $"{prevV} {v}";
                */
                } else {
                    basePhoneme = $"{cc.Last()} {v}";
                }
                // try [CC V]
                for (var i = firstC; i < cc.Length - 1; i++) {
                    var ccv = $"{string.Join("", cc)} {v}";
                    var ccv1 = string.Join("", cc.Skip(i)) + " " + v;
                    /// CCV
                    if (syllable.CurrentWordCc.Length >= 2 && !ccvException.Contains(cc[i])) {
                        if (HasOto(ccv, syllable.vowelTone) || HasOto(ValidateAlias(ccv), syllable.vowelTone)) {
                            basePhoneme = ccv;
                            lastC = i;
                            break;
                        } else if (HasOto(ccv1, syllable.vowelTone) || HasOto(ValidateAlias(ccv1), syllable.vowelTone)) {
                            basePhoneme = ccv1;
                        }
                        break;
                        /// C-Last V
                    } else if (syllable.CurrentWordCc.Length == 1 && syllable.PreviousWordCc.Length == 1) {
                        if ((HasOto(crv, syllable.vowelTone) || HasOto(ValidateAlias(crv), syllable.vowelTone))) {
                            basePhoneme = crv;
                        } else if ((HasOto(cv, syllable.vowelTone) || HasOto(ValidateAlias(cv), syllable.vowelTone)) && !(HasOto(crv, syllable.vowelTone)) || HasOto(ValidateAlias(crv), syllable.vowelTone)) {
                            basePhoneme = cv;
                        } else {
                            basePhoneme = crv;
                        }
                    }
                }
                // try [V C], [V CC], [VC C], [V -][- C]
                for (var i = lastC + 1; i >= 0; i--) {
                    var vr = $"{prevV} -";
                    var vc_c = $"{prevV}{string.Join(" ", cc.Take(2))}";
                    var vcc = $"{prevV} {string.Join("", cc.Take(2))}";
                    var vc = $"{prevV} {cc[0]}";
                    // Boolean Triggers
                    bool CCV = false;
                    if (syllable.CurrentWordCc.Length >= 2 && !ccvException.Contains(cc[0])) {
                        if (HasOto($"{string.Join("", cc)} {v}", syllable.vowelTone) || HasOto(ValidateAlias($"{string.Join("", cc)} {v}"), syllable.vowelTone)) {
                            CCV = true;
                        }
                    }
                    if (i == 0 && (HasOto(vr, syllable.tone) || HasOto(ValidateAlias(vr), syllable.tone)) && !HasOto(vc, syllable.tone)) {
                        phonemes.Add(vr);
                        TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}", $"-{cc[0]}");
                        break;
                    } else if ((HasOto(vcc, syllable.tone) || HasOto(ValidateAlias(vcc), syllable.tone)) && CCV && !affricates.Contains(string.Join("", cc.Take(2)))) {
                        phonemes.Add(vcc);
                        firstC = 1;
                        break;
                        /// temporarily removed vc_c cuz of the arpabet [v] sustain confict on jp vc 😭
                        /*} else if (HasOto(vc_c, syllable.tone) || HasOto(ValidateAlias(vc_c), syllable.tone)) {
                            phonemes.Add(vc_c);
                            firstC = 1;
                            break;
                        */
                    } else if (cPV_FallBack && (!HasOto(crv, syllable.vowelTone) && !HasOto(ValidateAlias(crv), syllable.vowelTone))) {
                        TryAddPhoneme(phonemes, syllable.tone, vc, ValidateAlias(vc));
                        break;
                    } else if (HasOto(vc, syllable.tone) || HasOto(ValidateAlias(vc), syllable.tone)) {
                        phonemes.Add(vc);
                        break;
                    } else {
                        continue;
                    }
                }
            }

            for (var i = firstC; i < lastC; i++) {
                var ccv = $"{string.Join("", cc.Skip(i + 1))} {v}";
                var ccv1 = $"{string.Join("", cc.Skip(i + 1))}{v}";
                var cccv = $"{string.Join("", cc.Skip(i + 1))} {v}";
                var cc1 = $"{string.Join(" ", cc.Skip(i))}";
                var lcv = $"{cc.Last()} {v}";
                var cv = $"{cc.Last()}{v}";
                if (!HasOto(cc1, syllable.tone)) {
                    cc1 = ValidateAlias(cc1);
                }
                // [C1 C2]
                if (!HasOto(cc1, syllable.tone)) {
                    cc1 = $"{cc[i]} {cc[i + 1]}";
                }
                if (!HasOto(cc1, syllable.tone)) {
                    cc1 = ValidateAlias(cc1);
                }
                // CC FALLBACKS
                if (!HasOto(cc1, syllable.tone) || !HasOto(ValidateAlias(cc1), syllable.tone) && !HasOto($"{cc[i]} {cc[i + 1]}", syllable.tone)) {
                    // [C1 -] [- C2]
                    cc1 = $"- {cc[i + 1]}";
                    phonemes.Add($"{cc[i]} -");
                } else if (!HasOto(cc1, syllable.tone) || !HasOto(ValidateAlias(cc1), syllable.tone) && !HasOto($"- {cc[i + 1]}", syllable.tone) && !HasOto($"{cc[i]} -", syllable.tone)) {
                    // [C1-] [-C2]
                    cc1 = $"-{cc[i + 1]}";
                    phonemes.Add($"{cc[i]}-");
                }
                if (!HasOto(cc1, syllable.tone)) {
                    cc1 = ValidateAlias(cc1);
                }
                // CCCV
                if (syllable.CurrentWordCc.Length >= 3) {
                    if (HasOto(cccv, syllable.vowelTone) || HasOto(ValidateAlias(cccv), syllable.vowelTone) && !ccvException.Contains(cc[0])) {
                        basePhoneme = cccv;
                        lastC = i;
                    }
                // CCV
                } else if (syllable.CurrentWordCc.Length == 2) {
                    if (HasOto(ccv, syllable.vowelTone) || HasOto(ValidateAlias(ccv), syllable.vowelTone) && !ccvException.Contains(cc[0])) {
                        basePhoneme = ccv;
                        lastC = i;
                    } else if (HasOto(ccv1, syllable.vowelTone) || HasOto(ValidateAlias(ccv1), syllable.vowelTone) && !ccvException.Contains(cc[0])) {
                        basePhoneme = ccv1;
                        lastC = i;
                    } else if ((HasOto(lcv, syllable.vowelTone) || HasOto(ValidateAlias(lcv), syllable.vowelTone)) && HasOto(cc1, syllable.vowelTone) && !HasOto(ccv, syllable.vowelTone)) {
                        basePhoneme = lcv;
                    } else if ((HasOto(cv, syllable.vowelTone) || HasOto(ValidateAlias(cv), syllable.vowelTone)) && HasOto(cc1, syllable.vowelTone)) {
                        basePhoneme = cv;
                    }
                    // [C1 C2C3]
                    if (HasOto($"{cc[i]} {string.Join("", cc.Skip(i + 1))}", syllable.tone)) {
                        cc1 = $"{cc[i]} {string.Join("", cc.Skip(i + 1))}";
                    }
                // CV
                } else if (syllable.CurrentWordCc.Length == 1 && syllable.PreviousWordCc.Length == 1) {
                    basePhoneme = lcv;
                    if ((HasOto(cv, syllable.vowelTone) || HasOto(ValidateAlias(cv), syllable.vowelTone))) {
                        basePhoneme = cv;
                    }
                    // [C1 C2]
                    if (!HasOto(cc1, syllable.tone)) {
                    cc1 = $"{cc[i]} {cc[i + 1]}";
                    }
                }
                // C+V
                if ((HasOto(v, syllable.vowelTone) || HasOto(ValidateAlias(v), syllable.vowelTone)) && (!HasOto(lcv, syllable.vowelTone) && !HasOto(ValidateAlias(lcv), syllable.vowelTone) && (!HasOto(cv, syllable.vowelTone) && !HasOto(ValidateAlias(cv), syllable.vowelTone)))) {
                    cPV_FallBack = true;
                    basePhoneme = v;
                    cc1 = ValidateAlias(cc1);
                }
                
                if (i + 1 < lastC) {
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = ValidateAlias(cc1);
                    }
                    // [C1 C2]
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = $"{cc[i]} {cc[i + 1]}";
                    }
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = ValidateAlias(cc1);
                    }
                    // CC FALLBACKS
                    if (!HasOto(cc1, syllable.tone) && !HasOto($"{cc[i]} {cc[i + 1]}", syllable.tone)) {
                        // [C1 -] [- C2]
                        cc1 = $"- {cc[i + 1]}";
                        phonemes.Add($"{cc[i]} -");
                    } else if (!HasOto($"- {cc[i + 1]}", syllable.tone) && !HasOto($"{cc[i]} -", syllable.tone)) {
                        // [C1-] [-C2]
                        phonemes.Add($"{cc[0]} -");
                    }
                    // CCCV
                    if (syllable.CurrentWordCc.Length >= 3) {
                        if (HasOto(cccv, syllable.vowelTone) || HasOto(ValidateAlias(cccv), syllable.vowelTone) && !ccvException.Contains(cc[0])) {
                            basePhoneme = cccv;
                            lastC = i;
                        }
                        // CCV
                    } else if (syllable.CurrentWordCc.Length == 2) {
                        if (HasOto(ccv, syllable.vowelTone) || HasOto(ValidateAlias(ccv), syllable.vowelTone) && !ccvException.Contains(cc[0])) {
                            basePhoneme = ccv;
                            lastC = i;
                        } else if (HasOto(ccv1, syllable.vowelTone) || HasOto(ValidateAlias(ccv1), syllable.vowelTone) && !ccvException.Contains(cc[0])) {
                            basePhoneme = ccv1;
                            lastC = i;
                        } else if ((HasOto(lcv, syllable.vowelTone) || HasOto(ValidateAlias(lcv), syllable.vowelTone)) && HasOto(cc1, syllable.vowelTone) && !HasOto(ccv, syllable.vowelTone)) {
                            basePhoneme = lcv;
                        } else if ((HasOto(cv, syllable.vowelTone) || HasOto(ValidateAlias(cv), syllable.vowelTone)) && HasOto(cc1, syllable.vowelTone)) {
                            basePhoneme = cv;
                        }
                        // [C1 C2C3]
                        if (HasOto($"{cc[i]} {string.Join("", cc.Skip(i + 1))}", syllable.tone)) {
                            cc1 = $"{cc[i]} {string.Join("", cc.Skip(i + 1))}";
                        }
                        // CV
                    } else if (syllable.CurrentWordCc.Length == 1 && syllable.PreviousWordCc.Length == 1) {
                        basePhoneme = lcv;
                        if ((HasOto(cv, syllable.vowelTone) || HasOto(ValidateAlias(cv), syllable.vowelTone))) {
                            basePhoneme = cv;
                        }
                        // [C1 C2]
                        if (!HasOto(cc1, syllable.tone)) {
                            cc1 = $"{cc[i]} {cc[i + 1]}";
                        }
                    }
                    // C+V
                    if ((HasOto(v, syllable.vowelTone) || HasOto(ValidateAlias(v), syllable.vowelTone)) && (!HasOto(lcv, syllable.vowelTone) && !HasOto(ValidateAlias(lcv), syllable.vowelTone) && (!HasOto(cv, syllable.vowelTone) && !HasOto(ValidateAlias(cv), syllable.vowelTone)))) {
                        cPV_FallBack = true;
                        basePhoneme = v;
                        cc1 = ValidateAlias(cc1);
                    }
                    
                    if (HasOto(cc1, syllable.tone) && HasOto(cc1, syllable.tone) && !cc1.Contains($"{string.Join("", cc.Skip(i))}")) {
                        // like [V C1] [C1 C2] [C2 C3] [C3 ..]
                        phonemes.Add(cc1);
                    } else if (TryAddPhoneme(phonemes, syllable.tone, cc1)) {
                        // like [V C1] [C1 C2] [C2 ..]
                        if (cc1.Contains($"{string.Join(" ", cc.Skip(i + 1))}")) {
                            i++;
                        }
                    } else {
                        // like [V C1] [C1] [C2 ..]
                        TryAddPhoneme(phonemes, syllable.tone, cc[i], ValidateAlias(cc[i]));
                    }
                } else {
                    TryAddPhoneme(phonemes, syllable.tone, cc1);
                }
            }

            phonemes.Add(basePhoneme);
            return phonemes;
        }

        protected override List<string> ProcessEnding(Ending ending) {
            string prevV = ending.prevV;
            string[] cc = ending.cc;
            string v = ending.prevV;
            var phonemes = new List<string>();
            var lastC = cc.Length - 1;
            var firstC = 0;
            if (tails.Contains(ending.prevV)) {
                return new List<string>();
            }
            if (ending.IsEndingV) {
                var vR = $"{v} -";
                var vR1 = $"{v} R";
                var vR2 = $"{v}-";
                if (HasOto(vR, ending.tone) || HasOto(ValidateAlias(vR), ending.tone)) {
                    phonemes.Add(vR);
                } else if (!HasOto(vR, ending.tone) && !HasOto(ValidateAlias(vR), ending.tone) && (HasOto(vR1, ending.tone) || HasOto(ValidateAlias(vR1), ending.tone))) {
                    phonemes.Add(vR1);
                } else if (!HasOto(vR1, ending.tone) && !HasOto(ValidateAlias(vR1), ending.tone) && (HasOto(vR2, ending.tone) || HasOto(ValidateAlias(vR2), ending.tone))) {
                    phonemes.Add(vR2);
                }
            } else if (ending.IsEndingVCWithOneConsonant) {
                var vc = $"{v} {cc[0]}";
                var vcr = $"{v} {cc[0]}-";
                var vcr2 = $"{v}{cc[0]} -";
                if (!RomajiException.Contains(cc[0])) {
                    if (HasOto(vcr, ending.tone) || HasOto(ValidateAlias(vcr), ending.tone)) {
                        phonemes.Add(vcr);
                    } else if (!HasOto(vcr, ending.tone) && !HasOto(ValidateAlias(vcr), ending.tone) && (HasOto(vcr2, ending.tone) || HasOto(ValidateAlias(vcr2), ending.tone))) {
                        phonemes.Add(vcr2);
                    } else {
                        phonemes.Add(vc);
                        if (vc.Contains(cc[0])) {
                            phonemes.Add($"{cc[0]} -");
                        } else {
                            phonemes.Add($"{cc[0]}-");
                        }
                    }
                }
            } else {
                for (var i = lastC; i >= 0; i--) {
                    var vr = $"{v} -";
                    var vcc = $"{v} {string.Join("", cc.Take(2))}-";
                    var vcc2 = $"{v}{string.Join(" ", cc.Take(2))} -";
                    var vcc3 = $"{v}{string.Join(" ", cc.Take(2))}";
                    var vcc4 = $"{v} {string.Join("", cc.Take(2))}";
                    var vc = $"{v} {cc[0]}";
                    if (!RomajiException.Contains(cc[0])) {
                        if (i == 0) {
                            if (HasOto(vr, ending.tone) || HasOto(ValidateAlias(vr), ending.tone) && !HasOto(vc, ending.tone)) {
                                phonemes.Add(vr);
                            }
                            break;
                        } else if ((HasOto(vcc, ending.tone) || HasOto(ValidateAlias(vcc), ending.tone)) && lastC == 1 && !ccvException.Contains(cc[0])) {
                            phonemes.Add(vcc);
                            firstC = 1;
                            break;
                        } else if ((HasOto(vcc2, ending.tone) || HasOto(ValidateAlias(vcc2), ending.tone)) && lastC == 1 && !ccvException.Contains(cc[0])) {

                            phonemes.Add(vcc2);
                            firstC = 1;
                            break;
                        } else if (HasOto(vcc3, ending.tone) || HasOto(ValidateAlias(vcc3), ending.tone) && !ccvException.Contains(cc[0])) {
                            phonemes.Add(vcc3);
                            if (vcc3.EndsWith(cc.Last()) && lastC == 1) {
                                if (consonants.Contains(cc.Last())) {
                                    TryAddPhoneme(phonemes, ending.tone, $"{cc.Last()} -", $"{cc.Last()}-", ValidateAlias($"{cc.Last()} -"), cc.Last(), ValidateAlias($"{cc.Last()}-"));
                                } else {
                                    TryAddPhoneme(phonemes, ending.tone, $"{cc.Last()} -", ValidateAlias($"{cc.Last()} -"));
                                }
                            }
                            firstC = 1;
                            break;
                        } else if (HasOto(vcc4, ending.tone) || HasOto(ValidateAlias(vcc4), ending.tone) && !ccvException.Contains(cc[0])) {
                            phonemes.Add(vcc4);
                            if (vcc4.EndsWith(cc.Last()) && lastC == 1) {
                                if (consonants.Contains(cc.Last())) {
                                    TryAddPhoneme(phonemes, ending.tone, $"{cc.Last()} -", $"{cc.Last()}-", ValidateAlias($"{cc.Last()} -"), cc.Last(), ValidateAlias($"{cc.Last()}-"));
                                } else {
                                    TryAddPhoneme(phonemes, ending.tone, $"{cc.Last()} -", ValidateAlias($"{cc.Last()} -"));
                                }
                            }
                            firstC = 1;
                            break;
                        } else {
                            phonemes.Add(vc);
                            break;
                        }
                    }
                }
                for (var i = firstC; i < lastC; i++) {
                    var cc1 = $"{cc[i]} {cc[i + 1]}";
                    if (i < cc.Length - 2) {
                        var cc2 = $"{cc[i + 1]} {cc[i + 2]}";
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = ValidateAlias(cc1);
                        }
                        if (!HasOto(cc2, ending.tone)) {
                            cc2 = ValidateAlias(cc2);
                        }
                        if (!HasOto(cc2, ending.tone) && !HasOto($"{cc[i + 1]} {cc[i + 2]}", ending.tone)) {
                            // [C1 -] [- C2]
                            cc2 = $"- {cc[i + 2]}";
                            phonemes.Add($"{cc[i + 1]} -");
                        }
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = ValidateAlias(cc1);
                        }
                        if (HasOto(cc1, ending.tone) && (HasOto(cc2, ending.tone) || HasOto($"{cc[i + 1]} {cc[i + 2]}-", ending.tone) || HasOto(ValidateAlias($"{cc[i + 1]} {cc[i + 2]}-"), ending.tone))) {
                            // like [C1 C2][C2 ...]
                            phonemes.Add(cc1);
                        } else if ((HasOto(cc[i], ending.tone) || HasOto(ValidateAlias(cc[i]), ending.tone) && (HasOto(cc2, ending.tone) || HasOto($"{cc[i + 1]} {cc[i + 2]}-", ending.tone) || HasOto(ValidateAlias($"{cc[i + 1]} {cc[i + 2]}-"), ending.tone)))) {
                            // like [C1 C2-][C3 ...]
                            phonemes.Add(cc[i]);
                        } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} {cc[i + 2]}-", ValidateAlias($"{cc[i + 1]} {cc[i + 2]}-"))) {
                            // like [C1 C2-][C3 ...]
                            i++;
                        } else if (TryAddPhoneme(phonemes, ending.tone, cc1, ValidateAlias(cc1))) {
                            i++;
                        } else if (!HasOto(cc1, ending.tone) && !HasOto($"{cc[i]} {cc[i + 1]}", ending.tone)) {
                            // [C1 -] [- C2]
                            TryAddPhoneme(phonemes, ending.tone, $"- {cc[i + 1]}", ValidateAlias($"- {cc[i + 1]}"));
                            phonemes.Add($"{cc[i]} -");
                            i++;
                        } else {
                            // like [C1][C2 ...]
                            TryAddPhoneme(phonemes, ending.tone, cc[i], ValidateAlias(cc[i]), $"{cc[i]} -", ValidateAlias($"{cc[i]} -"));
                            TryAddPhoneme(phonemes, ending.tone, cc[i + 1], ValidateAlias(cc[i + 1]), $"{cc[i + 1]} -", ValidateAlias($"{cc[i + 1]} -"));
                            i++;
                        }
                    } else {
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = ValidateAlias(cc1);
                        }
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = $"{cc[i]} {cc[i + 1]}";
                        }
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = ValidateAlias(cc1);
                        }
                        // [C1 -] [- C2]
                        if (!HasOto(cc1, ending.tone) || !HasOto(ValidateAlias(cc1), ending.tone) && !HasOto($"{cc[i]} {cc[i + 1]}", ending.tone)) {
                            cc1 = $"- {cc[i + 1]}";
                            phonemes.Add($"{cc[i]} -");
                        } else if (!HasOto(cc1, ending.tone) || !HasOto(ValidateAlias(cc1), ending.tone) && !HasOto($"- {cc[i + 1]}", ending.tone) && !HasOto($"{cc[i]} -", ending.tone)) {
                            // [C1-] [-C2]
                            cc1 = $"-{cc[i + 1]}";
                            phonemes.Add($"{cc[i]}-");
                        }
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = ValidateAlias(cc1);
                        }
                        if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i]} {cc[i + 1]}-", ValidateAlias($"{cc[i]} {cc[i + 1]}-"))) {
                            // like [C1 C2-]
                            i++;
                        } else if (TryAddPhoneme(phonemes, ending.tone, cc1, ValidateAlias(cc1))) {
                            // like [C1 C2][C2 -]
                            TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", ValidateAlias($"{cc[i + 1]} -"), cc[i + 1], ValidateAlias(cc[i + 1]));
                            i++;
                        } else if (!HasOto(cc1, ending.tone) && !HasOto($"{cc[i]} {cc[i + 1]}", ending.tone)) {
                            // [C1 -] [- C2]
                            TryAddPhoneme(phonemes, ending.tone, $"- {cc[i + 1]}", ValidateAlias($"- {cc[i + 1]}"), cc[i + 1], ValidateAlias(cc[i + 1]));
                            phonemes.Add($"{cc[i]} -");
                            i++;
                        }
                    }
                }
            }
            return phonemes;
        }
        protected override string ValidateAlias(string alias) {

            //CV FALLBACKS
            if (alias == "ng ao") {
                return alias.Replace("ao", "ow");
            } else if (alias == "ch ao") {
                return alias.Replace("ch ao", "sh ow");
            } else if (alias == "dh ao") {
                return alias.Replace("ao", "ow");
            } else if (alias == "dh oy") {
                return alias.Replace("oy", "ow");
            } else if (alias == "jh ao") {
                return alias.Replace("ao", "oy");
            } else if (alias == "ao -") {
                return alias.Replace("ao -", "aa -");
            } else if (alias == "v ao") {
                return alias.Replace("v", "b");
            } else if (alias == "z ao") {
                return alias.Replace("z", "s");
            } else if (alias == "ng eh") {
                return alias.Replace("ng", "n");
            } else if (alias == "z eh") {
                return alias.Replace("z", "s");
            } else if (alias == "jh er") {
                return alias.Replace("jh", "z");
            } else if (alias == "ng er") {
                return alias.Replace("ng", "n");
            } else if (alias == "r er") {
                return alias.Replace("r er", "er");
            } else if (alias == "th er") {
                return alias.Replace("th er", "th r");
            } else if (alias == "jh ey") {
                return alias.Replace("ey", "ae");
            } else if (alias == "ng ey") {
                return alias.Replace("ng", "n");
            } else if (alias == "th ey") {
                return alias.Replace("ey", "ae");
            } else if (alias == "zh ey") {
                return alias.Replace("zh ey", "jh ae");
            } else if (alias == "ch ow") {
                return alias.Replace("ch", "sh");
            } else if (alias == "jh ow") {
                return alias.Replace("ow", "oy");
            } else if (alias == "v ow") {
                return alias.Replace("v", "b");
            } else if (alias == "th ow") {
                return alias.Replace("th", "s");
            } else if (alias == "z ow") {
                return alias.Replace("z", "s");
            } else if (alias == "ch oy") {
                return alias.Replace("ch oy", "sh ow");
            } else if (alias == "th oy") {
                return alias.Replace("th oy", "s ao");
            } else if (alias == "v oy") {
                return alias.Replace("v", "b");
            } else if (alias == "w oy") {
                return alias.Replace("oy", "ao");
            } else if (alias == "z oy") {
                return alias.Replace("oy", "aa");
            } else if (alias == "ch uh") {
                return alias.Replace("ch", "sh");
            } else if (alias == "dh uh") {
                return alias.Replace("dh uh", "d uw");
            } else if (alias == "jh uh") {
                return alias.Replace("jh", "sh");
            } else if (alias == "ng uh") {
                return alias.Replace("ng uh", "n uw");
            } else if (alias == "th uh") {
                return alias.Replace("th uh", "f uw");
            } else if (alias == "v uh") {
                return alias.Replace("v", "b");
            } else if (alias == "z uh") {
                return alias.Replace("z", "s");
            } else if (alias == "ch uw") {
                return alias.Replace("ch", "sh");
            } else if (alias == "dh uw") {
                return alias.Replace("dh", "d");
            } else if (alias == "g uw") {
                return alias.Replace("g", "k");
            } else if (alias == "jh uw") {
                return alias.Replace("jh", "sh");
            } else if (alias == "ng uw") {
                return alias.Replace("ng", "n");
            } else if (alias == "th uw") {
                return alias.Replace("th uw", "f uw");
            } else if (alias == "v uw") {
                return alias.Replace("v", "b");
            } else if (alias == "z uw") {
                return alias.Replace("z", "s");
            } else if (alias == "zh aa") {
                return alias.Replace("zh", "sh");
            } else if (alias == "zh ao") {
                return alias.Replace("zh", "sh");
            } else if (alias == "zh ae") {
                return alias.Replace("zh ae", "sh ah");
            } else if (alias == "ng oy") {
                return alias.Replace("oy", "ow");
            } else if (alias == "sh ao") {
                return alias.Replace("ao", "ow");
            } else if (alias == "z uh") {
                return alias.Replace("z uh", "s uw");
            } else if (alias == "r uh") {
                return alias.Replace("uh", "uw");
            } else if (alias == "sh oy") {
                return alias.Replace("oy", "ow");
            }

            // VALIDATE ALIAS DEPENDING ON METHOD
            if (isMissingVPhonemes || isMissingCPhonemes || isTimitPhonemes) {
                foreach (var phoneme in missingVphonemes.Concat(missingCphonemes).Concat(timitphonemes)) {
                    alias = alias.Replace(phoneme.Key, phoneme.Value);
                }
            }

            var CVMappings = new Dictionary<string, string[]> {
                { "ao", new[] { "ow" } },
                { "oy", new[] { "ow" } },
                { "aw", new[] { "ah" } },
                { "ay", new[] { "ah" } },
                { "eh", new[] { "ae" } },
                { "ey", new[] { "eh" } },
                { "ow", new[] { "ao" } },
                { "uh", new[] { "uw" } },
            };
            foreach (var kvp in CVMappings) {
                var v1 = kvp.Key;
                var vfallbacks = kvp.Value;
                foreach (var vfallback in vfallbacks) {
                    foreach (var c1 in consonants) {
                        alias = alias.Replace(c1 + " " + v1, c1 + " " + vfallback);
                    }
                }
            }

            //VV (diphthongs) some
            var vvReplacements = new Dictionary<string, List<string>> {
                { "ay ay", new List<string> { "y ah" } },
                { "ey ey", new List<string> { "iy ey" } },
                { "oy oy", new List<string> { "y ow" } },
                { "er er", new List<string> { "er" } },
                { "aw aw", new List<string> { "w ae" } },
                { "ow ow", new List<string> { "w ao" } },
                { "uw uw", new List<string> { "w uw" } }
            };

            // Apply VV replacements
            foreach (var (originalValue, replacementOptions) in vvReplacements) {
                foreach (var replacementOption in replacementOptions) {
                    alias = alias.Replace(originalValue, replacementOption);
                }
            }
            //VC (diphthongs)
            //VC (aw specific)
            bool vcSpecific = true;
            if (vcSpecific) {
                if (alias == "aw ch") {
                    return alias.Replace("ch", "t");
                }
                if (alias == "aw jh") {
                    return alias.Replace("jh", "d");
                }
                if (alias == "aw ng") {
                    return alias.Replace("aw ng", "uh ng");
                }
                if (alias == "aw q") {
                    return alias.Replace("q", "t");
                }
                if (alias == "aw zh") {
                    return alias.Replace("zh", "d");
                }
                if (alias == "aw w") {
                    return alias.Replace("aw", "ah");
                }

                //VC (ay specific)
                if (alias == "ay ng") {
                    return alias.Replace("ay", "ih");
                }
                if (alias == "ay q") {
                    return alias.Replace("q", "t");
                }
                if (alias == "ay zh") {
                    return alias.Replace("zh", "jh");
                }
                //VC (ey specific)
                if (alias == "ey ng") {
                    return alias.Replace("ey", "ih");
                }
                if (alias == "ey q") {
                    return alias.Replace("q", "t");
                }
                if (alias == "ey zh") {
                    return alias.Replace("zh", "jh");
                }
                //VC (ow specific)
                if (alias == "ow ch") {
                    return alias.Replace("ch", "t");
                }
                if (alias == "ow jh") {
                    return alias.Replace("jh", "d");
                }
                if (alias == "ow ng") {
                    return alias.Replace("ow", "uh");
                }
                if (alias == "ow q") {
                    return alias.Replace("q", "t");
                }
                if (alias == "ow zh") {
                    return alias.Replace("zh", "z");
                }
                //VC (oy specific)
                if (alias == "- oy") {
                    return alias.Replace("oy", "ow");
                }
                if (alias == "oy f") {
                    return alias.Replace("oy", "ih");
                }
                if (alias == "oy ng") {
                    return alias.Replace("iy", "ih");
                }
                if (alias == "oy q") {
                    return alias.Replace("oy q", "iy t");
                }
                if (alias == "oy zh") {
                    return alias.Replace("oy zh", "iy jh");
                }

                //VC (aa)
                //VC (aa specific)
                if (alias == "aa b") {
                    return alias.Replace("aa b", "aa d");
                }
                if (alias == "aa dx") {
                    return alias.Replace("aa dx", "aa d");
                }
                if (alias == "aa q") {
                    return alias.Replace("aa q", "aa t");
                }
                if (alias == "aa y") {
                    return alias.Replace("aa y", "ah iy");
                }
                if (alias == "aa zh") {
                    return alias.Replace("aa zh", "aa z");
                }

                //VC (ae specific)
                if (alias == "ae b") {
                    return alias.Replace("ae b", "ah d");
                }
                if (alias == "ae dx") {
                    return alias.Replace("ae dx", "ah d");
                }
                if (alias == "ae q") {
                    return alias.Replace("ae q", "ah t");
                }
                if (alias == "ae y") {
                    return alias.Replace("ae y", "ah iy");
                }
                if (alias == "ae zh") {
                    return alias.Replace("ae zh", "ah z");
                }

                //VC (ah specific)
                if (alias == "ah b") {
                    return alias.Replace("ah b", "ah d");
                }
                if (alias == "ah dx") {
                    return alias.Replace("ah dx", "ah d");
                }
                if (alias == "ah q") {
                    return alias.Replace("ah q", "ah t");
                }
                if (alias == "ah y") {
                    return alias.Replace("ah y", "ah iy");
                }
                if (alias == "ah zh") {
                    return alias.Replace("ah zh", "ah z");
                }

                //VC (ao)
                //VC (ao specific)
                if (alias == "ao b") {
                    return alias.Replace("ao b", "ah d");
                }
                if (alias == "ao dx") {
                    return alias.Replace("ao dx", "ah d");
                }
                if (alias == "ao q") {
                    return alias.Replace("ao q", "ao t");
                }
                if (alias == "ao y") {
                    return alias.Replace("ao y", "ow y");
                }
                if (alias == "ao zh") {
                    return alias.Replace("ao zh", "ah z");
                }

                //VC (ax)
                //VC (ax specific)
                if (alias == "ax b") {
                    return alias.Replace("ax b", "ah d");
                }
                if (alias == "ax dx") {
                    return alias.Replace("ax dx", "ah d");
                }
                if (alias == "ax q") {
                    return alias.Replace("ax q", "ah t");
                }
                if (alias == "ax y") {
                    return alias.Replace("ax y", "ah iy");
                }
                if (alias == "ax zh") {
                    return alias.Replace("ax zh", "ah z");
                }

                //VC (eh)
                //VC (eh specific)
                if (alias == "eh b") {
                    return alias.Replace("eh b", "eh d");
                }
                if (alias == "eh ch") {
                    return alias.Replace("eh ch", "eh t");
                }
                if (alias == "eh dh") {
                    return alias.Replace("eh dh", "eh d");
                }
                if (alias == "eh dx") {
                    return alias.Replace("eh dx", "eh d");
                }
                if (alias == "eh ng") {
                    return alias.Replace("eh ng", "eh n");
                }
                if (alias == "eh q") {
                    return alias.Replace("eh q", "eh t");
                }
                if (alias == "eh y") {
                    return alias.Replace("eh y", "ey");
                }
                if (alias == "eh zh") {
                    return alias.Replace("eh zh", "eh s");
                }

                //VC (er specific)
                if (alias == "er ch") {
                    return alias.Replace("er ch", "er t");
                }
                if (alias == "er dx") {
                    return alias.Replace("er dx", "er d");
                }
                if (alias == "er jh") {
                    return alias.Replace("er jh", "er d");
                }
                if (alias == "er ng") {
                    return alias.Replace("er ng", "er n");
                }
                if (alias == "er q") {
                    return alias.Replace("er q", "er t");
                }
                if (alias == "er r") {
                    return alias.Replace("er r", "er");
                }
                if (alias == "er sh") {
                    return alias.Replace("er sh", "er s");
                }
                if (alias == "er zh") {
                    return alias.Replace("er zh", "er z");
                }

                //VC (ih specific)
                if (alias == "ih b") {
                    return alias.Replace("ih b", "ih d");
                }
                if (alias == "ih dx") {
                    return alias.Replace("ih dx", "ih d");
                }
                if (alias == "ih hh") {
                    return alias.Replace("ih hh", "iy hh");
                }
                if (alias == "ih q") {
                    return alias.Replace("ih q", "ih t");
                }
                if (alias == "ih w") {
                    return alias.Replace("ih w", "iy w");
                }
                if (alias == "ih y") {
                    return alias.Replace("ih y", "iy y");
                }
                if (alias == "ih zh") {
                    return alias.Replace("ih zh", "ih z");
                }

                //VC (iy specific)
                if (alias == "iy dx") {
                    return alias.Replace("iy dx", "iy d");
                }
                if (alias == "iy f") {
                    return alias.Replace("iy f", "iy hh");
                }
                if (alias == "iy n") {
                    return alias.Replace("iy n", "iy m");
                }
                if (alias == "iy ng") {
                    return alias.Replace("iy ng", "ih ng");
                }
                if (alias == "iy q") {
                    return alias.Replace("iy q", "iy t");
                }
                if (alias == "iy tr") {
                    return alias.Replace("iy tr", "iy t");
                }
                if (alias == "iy zh") {
                    return alias.Replace("iy zh", "iy z");
                }

                //VC (uh)
                //VC (uh specific)
                if (alias == "uh ch") {
                    return alias.Replace("uh ch", "uh t");
                }
                if (alias == "uh dx") {
                    return alias.Replace("uh dx", "uh d");
                }
                if (alias == "uh jh") {
                    return alias.Replace("uh jh", "uw d");
                }
                if (alias == "uh q") {
                    return alias.Replace("uh q", "uh t");
                }
                if (alias == "uh zh") {
                    return alias.Replace("uh zh", "uw z");
                }

                //VC (uw specific)
                if (alias == "uw ch") {
                    return alias.Replace("uw ch", "uw t");
                }
                if (alias == "uw dx") {
                    return alias.Replace("uw dx", "uw d");
                }
                if (alias == "uw jh") {
                    return alias.Replace("uw jh", "uw d");
                }
                if (alias == "uw ng") {
                    return alias.Replace("uw ng", "uw n");
                }
                if (alias == "uw q") {
                    return alias.Replace("uw q", "uw t");
                }
                if (alias == "uw zh") {
                    return alias.Replace("uw zh", "uw sh");
                }
            }

            bool ccSpecific = true;
            if (ccSpecific) {
                //CC (b)
                //CC (b specific)
                if (alias == "b dh") {
                    return alias.Replace("b ch", "p dh");
                }
                if (alias == "b ng") {
                    return alias.Replace("b ng", "ng");
                }
                if (alias == "b zh") {
                    return alias.Replace("zh", "z");
                }

                //CC (ch specific)
                if (alias == "ch r") {
                    return alias.Replace("ch r", "ch er");
                }
                if (alias == "ch w") {
                    return alias.Replace("ch w", "ch ah");
                }
                if (alias == "ch y") {
                    return alias.Replace("ch y", "ch iy");
                }
                if (alias == "ch -") {
                    return alias.Replace("ch", "jh");
                }
                if (alias == "- ch") {
                    return alias.Replace("ch", "jh");
                }

                //CC (d specific)
                if (alias == "d ng") {
                    return alias.Replace("ng", "n");
                }
                if (alias == "d zh") {
                    return alias.Replace("zh", "z");
                }

                //CC (dh specific)
                if (alias == "dh dh") {
                    return alias.Replace("dh dh", "dh d");
                }
                if (alias == "dh ng") {
                    return alias.Replace("dh ng", "d n");
                }
                if (alias == "dh zh") {
                    return alias.Replace("zh", "z");
                }


                //CC (f specific)
                if (alias == "f sh") {
                    return alias.Replace("sh", "s");
                }
                if (alias == "f z") {
                    return alias.Replace("z", "s");
                }
                if (alias == "f zh") {
                    return alias.Replace("zh", "s");
                }
                if (alias == "f -") {
                    return alias.Replace("f", "th");
                }

                //CC (g specific)
                if (alias == "g dh") {
                    return alias.Replace("g", "d");
                }
                if (alias == "g ng") {
                    return alias.Replace("g ng", "ng");
                }
                if (alias == "g zh") {
                    return alias.Replace("zh", "z");
                }

                //CC (hh specific)
                if (alias == "hh y") {
                    return alias.Replace("hh", "f");
                }
                

                //CC (jh specific)
                if (alias == "jh r") {
                    return alias.Replace("jh r", "jh ah");
                }
                if (alias == "jh w") {
                    return alias.Replace("jh w", "jh ah");
                }
                if (alias == "jh y") {
                    return alias.Replace("y", "iy");
                }

                //CC (k specific)
                if (alias == "k z") {
                    return alias.Replace("z", "s");
                }
                if (alias == "k zh") {
                    return alias.Replace("zh", "s");
                }

                //CC (l specific)
                if (alias == "l ch") {
                    return alias.Replace("ch", "t");
                }
                if (alias == "l b") {
                    return alias.Replace("b", "d");
                }
                if (alias == "l jh") {
                    return alias.Replace("jh", "d");
                }
                if (alias == "l ng") {
                    return alias.Replace("ng", "n");
                }
                if (alias == "l sh") {
                    return alias.Replace("sh", "s");
                }
                if (alias == "l th") {
                    return alias.Replace("l th", "l s");
                }
                if (alias == "l zh") {
                    return alias.Replace("zh", "z");
                }

                //CC (m specific)
                if (alias == "m ch") {
                    return alias.Replace("m", "n");
                }
                if (alias == "m jh") {
                    return alias.Replace("jh", "d");
                }
                if (alias == "m ng") {
                    return alias.Replace("ng", "n");
                }
                if (alias == "m n") {
                    return alias.Replace("m n", "n");
                }
                if (alias == "m m") {
                    return alias.Replace("m m", "n");
                }
                if (alias == "m r") {
                    return alias.Replace("m", "n");
                }
                if (alias == "m s") {
                    return alias.Replace("m", "n");
                }
                if (alias == "m sh") {
                    return alias.Replace("m", "n");
                }
                if (alias == "m v") {
                    return alias.Replace("m v", "m m");
                }
                if (alias == "m zh") {
                    return alias.Replace("zh", "z");
                }

                //CC (n specific)
                if (alias == "n ng") {
                    return alias.Replace("ng", "n");
                }
                if (alias == "n n") {
                    return alias.Replace("n n", "n");
                }
                if (alias == "n m") {
                    return alias.Replace("n m", "n");
                }
                if (alias == "n v") {
                    return alias.Replace("n v", "n m");
                }
                if (alias == "n zh") {
                    return alias.Replace("zh", "z");
                }

                //CC (ng)
                foreach (var c1 in new[] { "ng" }) {
                    foreach (var c2 in consonants) {
                        alias = alias.Replace(c1 + " " + c2, "n" + " " + c2);
                    }
                }

                //CC (ng specific)
                if (alias == "ng ch") {
                    return alias.Replace("ch", "t");
                }
                if (alias == "ng ng") {
                    return alias.Replace("ng", "n");
                }
                if (alias == "ng zh") {
                    return alias.Replace("zh", "z");
                }

                //CC (p specific)
                if (alias == "p z") {
                    return alias.Replace("z", "s");
                }
                if (alias == "p zh") {
                    return alias.Replace("zh", "s");
                }

                //CC (r specific)
                if (alias == "r ch") {
                    return alias.Replace("ch", "t");
                }
                if (alias == "r dx") {
                    return alias.Replace("dx", "d");
                }
                if (alias == "r ng") {
                    return alias.Replace("ng", "n");
                }
                if (alias == "r sh") {
                    return alias.Replace("sh", "s");
                }
                if (alias == "r zh") {
                    return alias.Replace("zh", "z");
                }

                //CC (s specific)
                if (alias == "s ch") {
                    return alias.Replace("ch", "t");
                }
                if (alias == "s ng") {
                    return alias.Replace("ng", "n");
                }
                if (alias == "s sh") {
                    return alias.Replace("sh", "s");
                }
                if (alias == "s th") {
                    return alias.Replace("s", "z");
                }
                if (alias == "s v") {
                    return alias.Replace("s", "z");
                }
                if (alias == "s zh") {
                    return alias.Replace("zh", "s");
                }

                //CC (sh specific)
                if (alias == "sh l") {
                    return alias.Replace("sh", "s");
                }
                if (alias == "sh m") {
                    return alias.Replace("sh", "s");
                }
                if (alias == "sh n") {
                    return alias.Replace("sh", "s");
                }
                if (alias == "sh r") {
                    return alias.Replace("sh", "s");
                }
                if (alias == "sh s") {
                    return alias.Replace("sh", "s");
                }
                if (alias == "sh sh") {
                    return alias.Replace("sh sh", "s s");
                }

                //CC (t specific)
                if (alias == "t zh") {
                    return alias.Replace("t zh", "g z");
                }

                //CC (th specific)
                if (alias == "th y") {
                    return alias.Replace("th y", "th ih");
                }
                if (alias == "th zh") {
                    return alias.Replace("zh", "s");
                }

                //CC (v specific)
                if (alias == "v dh") {
                    return alias.Replace("dh", "d");
                }
                if (alias == "v th") {
                    return alias.Replace("v th", "th");
                }
                // CC (w C)
                foreach (var c2 in consonants) {
                    if (!(alias.Contains($"aw {c2}") || alias.Contains($"ew {c2}") || alias.Contains($"iw {c2}") || alias.Contains($"ow {c2}") || alias.Contains($"uw {c2}"))) {
                        alias = alias.Replace($"w {c2}", $"uw {c2}");
                    }
                }
                // CC (C w)
                foreach (var c2 in consonants) {
                    if (!(alias.Contains($"aw {c2}") || alias.Contains($"ew {c2}") || alias.Contains($"iw {c2}") || alias.Contains($"ow {c2}") || alias.Contains($"uw {c2}"))) {
                        alias = alias.Replace($"{c2} w", $"{c2} uw");
                    }
                }
                if (alias == "w -") {
                    return alias.Replace("w", "uw");
                }

                //CC (y C)
                foreach (var c2 in consonants) {
                    if (!(alias.Contains($"ay {c2}") || alias.Contains($"ey {c2}") || alias.Contains($"iy {c2}") || alias.Contains($"oy {c2}"))) {
                        alias = alias.Replace($"y {c2}", $"iy {c2}");
                    }
                }
                //CC (C y)
                foreach (var c2 in consonants) {
                    if (!(alias.Contains($"ay {c2}") || alias.Contains($"ey {c2}") || alias.Contains($"iy {c2}") || alias.Contains($"oy {c2}"))) {
                        alias = alias.Replace($"{c2} y", $"{c2} y");
                    }
                }
                if (alias == "y -") {
                    return alias.Replace("y", "iy");
                }

                //CC (z specific)
                if (alias == "z ch") {
                    return alias.Replace("ch", "t");
                }
                if (alias == "z dx") {
                    return alias.Replace("dx", "d");
                }
                if (alias == "z ng") {
                    return alias.Replace("ng", "n");
                }
                if (alias == "z z") {
                    return alias.Replace("z z", "z s");
                }
                if (alias == "z zh") {
                    return alias.Replace("z zh", "z s");
                }
                //CC (zh)
                //CC (zh specific)
                if (alias == "zh ch") {
                    return alias.Replace("ch", "t");
                }
                if (alias == "zh dx") {
                    return alias.Replace("dx", "d");
                }
                if (alias == "zh ng") {
                    return alias.Replace("ng", "n");
                }
                if (alias == "zh z") {
                    return alias.Replace("zh z", "z s");
                }
                if (alias == "zh zh") {
                    return alias.Replace("z zh", "z s");
                }
                if (alias == "- zh") {
                    return alias.Replace("zh", "jh");
                }
            }

            //VC's
            foreach (var v1 in vcFallBacks) {
                foreach (var c1 in consonants) {
                    if (vc_FallBack && isMissingVPhonemes) {
                        alias = alias.Replace(v1.Key + " " + c1, v1.Value + " " + c1);
                    }
                }
            }
            
            // glottal
            foreach (var v1 in vowels) {
                if (!alias.Contains("cl " + v1) || !alias.Contains("q " + v1)) {
                    alias = alias.Replace("q " + v1, "- " + v1);
                }
            }
            foreach (var c2 in consonants) {
                if (!alias.Contains(c2 + " cl") || !alias.Contains(c2 + " q")) {
                    alias = alias.Replace(c2 + " q", $"{c2} -");
                }
            }
            foreach (var c2 in consonants) {
                if (!alias.Contains("cl " + c2) || !alias.Contains("q " + c2)) {
                    alias = alias.Replace("q " + c2, "- " + c2);
                }
            }

            // C -'s
            foreach (var c1 in new[] { "d", "dh", "g", "p", "jh", "b", "s", "ch", "t", "r", "n", "l", "ng", "sh", "zh", "th", "z", "f", "k", "s", "hh" }) {
                foreach (var s in new[] { "-" }) {
                    var str = c1 + " " + s;
                    if (alias.Contains(str) && !alias.Contains($"{c1} -")) {
                        switch (c1) {
                            case "b" when c1 == "b":
                                alias = alias.Replace(str, "d" + " " + s);
                                break;
                            case "d" when c1 == "d" || c1 == "dh" || c1 == "g" || c1 == "p":
                                alias = alias.Replace(str, "b" + " " + s);
                                break;
                            case "ch" when c1 == "ch":
                                alias = alias.Replace(str, "jh" + " " + s);
                                break;
                            case "jh" when c1 == "jh":
                                alias = alias.Replace(str, "ch" + " " + s);
                                break;
                            case "s" when c1 == "s":
                                alias = alias.Replace(str, "f" + " " + s);
                                break;
                            case "ch" when c1 == "ch":
                                alias = alias.Replace(str, "jh" + " " + s);
                                break;
                            case "t" when c1 == "t":
                                alias = alias.Replace(str, "k" + " " + s);
                                break;
                            case "r" when c1 == "r":
                                alias = alias.Replace(str, "er" + " " + s);
                                break;
                            case "n" when c1 == "n":
                                alias = alias.Replace(str, "m" + " " + s);
                                break;
                            case "ng" when c1 == "ng" || c1 == "m":
                                alias = alias.Replace(str, "n" + " " + s);
                                break;
                            case "sh" when c1 == "sh" || c1 == "zh" || c1 == "th" || c1 == "z" || c1 == "f":
                                alias = alias.Replace(str, "s" + " " + s);
                                break;
                            case "k" when c1 == "k":
                                alias = alias.Replace(str, "t" + " " + s);
                                break;
                            case "s" when c1 == "s":
                                alias = alias.Replace(str, "z" + " " + s);
                                break;
                            case "hh" when c1 == "hh":
                                alias = alias.Replace(str, str);
                                break;
                        }
                    }
                }
            }
            // CC's
            foreach (var c1 in new[] { "f", "z", "k", "p", "d", "dh", "g", "b", "m", "r" }) {
                foreach (var c2 in consonants) {
                    var str = c1 + " " + c2;
                    if (alias.Contains(str)) {
                        if (ccSpecific) {
                            switch (c1) {
                                case "f" when c1 == "f" || c1 == "z":
                                    alias = alias.Replace(str, "s" + " " + c2);
                                    break;
                                case "k" when c1 == "k" || c1 == "p" || c1 == "d":
                                    alias = alias.Replace(str, "t" + " " + c2);
                                    break;
                                case "dh" when c1 == "dh" || c1 == "g" || c1 == "b":
                                    alias = alias.Replace(str, "d" + " " + c2);
                                    break;
                                case "m" when c1 == "m":
                                    alias = alias.Replace(str, "n" + " " + c2);
                                    break;
                                case "r" when c1 == "r":
                                    alias = alias.Replace(str, "er" + " " + c2);
                                    break;
                            }
                        }
                    }
                }
            }
            return base.ValidateAlias(alias);
        }

        protected override double GetTransitionBasicLengthMs(string alias = "") {
            //I wish these were automated instead :')
            double transitionMultiplier = 1.0; // Default multiplier
            bool isEndingConsonant = false;
            bool isEndingVowel = false;
            bool hasCons = false;
            bool haslr = false;
            var excludedVowels = new List<string> { "a", "e", "i", "o", "u" };
            var GlideVCCons = new List<string> { $"{excludedVowels} {connectingGlides}" };
            var NormVCCons = new List<string> { $"{excludedVowels} {connectingNormCons}" };
            var arpabetFirstVDiphthong = new List<string> { "a", "e", "i", "o", "u" };
            var excludedEndings = new List<string> { $"{arpabetFirstVDiphthong}y -", $"{arpabetFirstVDiphthong}w -", $"{arpabetFirstVDiphthong}r -", };
            var numbers = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

            foreach (var c in longConsonants) {
                if (alias.Contains(c) && !alias.StartsWith(c) && !alias.Contains($"{c} -")) {
                    return base.GetTransitionBasicLengthMs() * 2.5;
                }
            }

            foreach (var c in normalConsonants) {
                foreach (var v in normalConsonants.Except(GlideVCCons)) {
                    foreach (var b in normalConsonants.Except(NormVCCons)) {
                        if (alias.Contains(c) && !alias.StartsWith(c) &&
                        !alias.Contains("dx") && !alias.Contains($"{c} -")) {
                            if ("b,d,g,k,p,t".Split(',').Contains(c)) {
                                hasCons = true;
                            } else if ("l,r".Split(',').Contains(c)) {
                                haslr = true;
                            } else {
                                return base.GetTransitionBasicLengthMs() * 1.3;
                            }
                        }
                    }
                }
            }

            foreach (var c in connectingNormCons) {
                foreach (var v in vowels.Except(excludedVowels)) {
                    if (alias.Contains(c) && !alias.Contains("- ") && alias.Contains($"{v} {c}")
                       && !alias.Contains("dx")) {
                        return base.GetTransitionBasicLengthMs() * 2.0;
                    }
                }
            }

            foreach (var c in tapConsonant) {
                foreach (var v in vowels) {
                    if (alias.Contains($"{v} {c}") || alias.Contains(c)) {
                        return base.GetTransitionBasicLengthMs() * 0.5;
                    }
                }
            }

            foreach (var c in affricates) {
                if (alias.Contains(c) && !alias.StartsWith(c)) {
                    return base.GetTransitionBasicLengthMs() * 1.5;
                }
            }

            foreach (var c in connectingGlides) {
                foreach (var v in vowels.Except(excludedVowels)) {
                    if (alias.Contains($"{v} {c}") && !alias.Contains($"{c} -") && !alias.Contains($"{v} -")) {
                        return base.GetTransitionBasicLengthMs() * 2.2;
                    }
                }
            }

            foreach (var c in connectingGlides) {
                foreach (var v in vowels.Where(v => excludedVowels.Contains(v))) {
                    if (alias.Contains($"{v} r")) {
                        return base.GetTransitionBasicLengthMs() * 0.6;

                    }
                }
            }

            foreach (var c in semilongConsonants) {
                foreach (var v in semilongConsonants.Except(excludedEndings)) {
                    if (alias.Contains(c) && !alias.StartsWith(c) && !alias.Contains($"{c} -") && !alias.Contains($"- q")) {
                        return base.GetTransitionBasicLengthMs() * 1.5;
                    }
                }
            }

            foreach (var c in semiVowels) {
                foreach (var v in semilongConsonants.Except(excludedEndings)) {
                    if (alias.Contains(c) && !alias.StartsWith(c) && !alias.Contains($"{c} -")) {
                        return base.GetTransitionBasicLengthMs() * 1.5;
                    }
                }
            }

            if (hasCons) {
                return base.GetTransitionBasicLengthMs() * 1.3; // Value for 'cons'
            } else if (haslr) {
                return base.GetTransitionBasicLengthMs() * 1.2; // Value for 'cons'
            }

            // Check if the alias ends with a consonant or vowel
            foreach (var c in consonants) {
                if (alias.Contains(c) && alias.Contains('-') && alias.Contains($"{c} -")) {
                    isEndingConsonant = true;
                    break;
                }
            }

            foreach (var v in vowels) {
                if (alias.Contains(v) && alias.Contains('-') && alias.Contains($"{v} -")) {
                    isEndingVowel = true;
                    break;
                }
            }

            // If the alias ends with a consonant or vowel, return 0.5 ms
            if (isEndingConsonant || isEndingVowel) {
                return base.GetTransitionBasicLengthMs() * 0.5;
            }

            return base.GetTransitionBasicLengthMs() * transitionMultiplier;
        }
    }
}
