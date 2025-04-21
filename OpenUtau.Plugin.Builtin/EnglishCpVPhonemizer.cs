using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Classic;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.G2p;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("English C+V Phonemizer", "EN C+V", "Cadlaxa", language: "EN")]
    // Custom C+V Phonemizer for OU
    // Arpabet only but in the future update, it can be customize the phoneme set
    public class EnglishCpVPhonemizer : SyllableBasedPhonemizer {
        private string[] vowels = {
        "aa", "ax", "ae", "ah", "ao", "aw", "ay", "eh", "er", "ey", "ih", "iy", "ow", "oy", "uh", "uw", "a", "e", "i", "o", "u", "ai", "ei", "oi", "au", "ou", "ix", "ux",
        "aar", "ar", "axr", "aer", "ahr", "aor", "or", "awr", "aur", "ayr", "air", "ehr", "eyr", "eir", "ihr", "iyr", "ir", "owr", "our", "oyr", "oir", "uhr", "uwr", "ur",
        "aal", "al", "axl", "ael", "ahl", "aol", "ol", "awl", "aul", "ayl", "ail", "ehl", "el", "eyl", "eil", "ihl", "iyl", "il", "owl", "oul", "oyl", "oil", "uhl", "uwl", "ul",
        "aan", "an", "axn", "aen", "ahn", "aon", "on", "awn", "aun", "ayn", "ain", "ehn", "en", "eyn", "ein", "ihn", "iyn", "in", "own", "oun", "oyn", "oin", "uhn", "uwn", "un",
        "aang", "ang", "axng", "aeng", "ahng", "aong", "ong", "awng", "aung", "ayng", "aing", "ehng", "eng", "eyng", "eing", "ihng", "iyng", "ing", "owng", "oung", "oyng", "oing", "uhng", "uwng", "ung",
        "aam", "am", "axm", "aem", "ahm", "aom", "om", "awm", "aum", "aym", "aim", "ehm", "em", "eym", "eim", "ihm", "iym", "im", "owm", "oum", "oym", "oim", "uhm", "uwm", "um", "oh",
        "eu", "oe", "yw", "yx", "wx", "ox", "ex", "ea", "ia", "oa", "ua", "ean", "eam", "eang", "N", "nn", "mm", "ll"
        };
        private readonly string[] consonants = "b,ch,d,dh,dr,dx,f,g,hh,jh,k,l,m,n,nx,ng,p,q,r,s,sh,t,th,tr,v,w,y,z,zh".Split(',');
        private readonly string[] affricates = "ch,jh,j".Split(',');
        private readonly string[] tapConsonant = "dx,nx,lx".Split(",");
        private readonly string[] semilongConsonants = "ng,n,m,v,z,q,hh".Split(",");
        private readonly string[] semiVowels = "y,w".Split(",");
        private readonly string[] connectingGlides = "l,r,ll".Split(",");
        private readonly string[] longConsonants = "f,s,sh,th,zh,dr,tr,ts,c,vf".Split(",");
        private readonly string[] normalConsonants = "b,d,dh,g,k,p,t,l,r".Split(',');
        private readonly string[] connectingNormCons = "b,d,g,k,p,t".Split(',');
        private Dictionary<string, string> dictionaryReplacements;
        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "";
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;


        // For banks with missing vowels
        private readonly Dictionary<string, string> missingVphonemes = "ax=ah,aa=ah,ae=ah,iy=ih,uh=uw,ix=ih,ux=uh,oh=ao,eu=uh,oe=ax,uy=uw,yw=uw,yx=iy,wx=uw,ea=eh,ia=iy,oa=ao,ua=uw,R=-,N=n,mm=m,ll=l".Split(',')
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

        private static readonly Dictionary<string, string> VVManualOverrides = new Dictionary<string, string>() {
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
            {"ea", "ea"},
            {"ia", "ia"},
            {"oa", "oa"},
            {"ua", "ua"}
        };
            
        // Final consonants
        private static readonly string[] FinalConsonants = { "w", "y", "r", "l", "m", "n", "ng" };

        // Manually defined exceptions (Overrides auto-generated values)
        private static readonly Dictionary<string, string> ManualOverrides = new Dictionary<string, string>() {
            {"ea", "ea"},
            {"ia", "ia"},
            {"oa", "oa"},
            {"ua", "ua"}
        };

        // Dictionary initialized dynamically
        private Dictionary<string, string> _diphthongExceptions;
        private readonly object diphthongLock = new object();
        private Dictionary<string, string> _vvExceptions;
        private readonly object vvLock = new object();
        public Dictionary<string, string> DiphthongExceptions {
            get {
                // Lazy initialization: only initialize once when it's accessed
                if (_diphthongExceptions == null) {
                    lock (diphthongLock) {
                        if (_diphthongExceptions == null) {
                            // Create an instance of the phonemizer to access instance-specific data
                            var phonemizerInstance = new EnglishCpVPhonemizer();
                            phonemizerInstance.SetSinger(singer);
                            _diphthongExceptions = phonemizerInstance.GenerateDiphthongExceptions();
                        }
                    }
                }
                return _diphthongExceptions;
            }
        }

        private Dictionary<string, string> GenerateDiphthongExceptions() {
            var diphthongExceptions = new Dictionary<string, string>();

            // Access instance-specific vowels here
            foreach (string vowel in GetVowels()) {
                foreach (string consonant in FinalConsonants) {
                    if (vowel.EndsWith(consonant)) {
                        diphthongExceptions[vowel] = consonant;
                        break;
                    }
                }
            }

            // Apply manual overrides
            foreach (var entry in ManualOverrides) {
                diphthongExceptions[entry.Key] = entry.Value;
            }
            return diphthongExceptions;
        }

        public Dictionary<string, string> vvExceptions {
            get {
                // Lazy initialization: only initialize once when it's accessed
                if (_vvExceptions == null) {
                    lock (vvLock) {
                        if (_vvExceptions == null) {
                            // Create an instance of the phonemizer to access instance-specific data
                            var phonemizerInstance = new EnglishCpVPhonemizer();
                            phonemizerInstance.SetSinger(singer);
                            _vvExceptions = phonemizerInstance.GenerateVVExceptions();
                        }
                    }
                }
                return _vvExceptions;
            }
        }

        private Dictionary<string, string> GenerateVVExceptions() {
            var vvExceptions = new Dictionary<string, string>();

            // Access instance-specific vowels here
            foreach (string vowel in GetVowels()) {
                foreach (string consonant in FinalConsonants) {
                    if (vowel.EndsWith(consonant)) {
                        // only the final consonants are used
                        vvExceptions[vowel] = consonant;
                        break;
                    }
                }
            }
            // Apply manual overrides
            foreach (var entry in VVManualOverrides) {
                vvExceptions[entry.Key] = entry.Value;
            }
            return vvExceptions;
        }

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
            string[] av_c = new[] { "al", "am", "an", "ang", "ar" };
            string[] ev_c = new[] { "el", "em", "en", "eng", "err" };
            string[] iv_c = new[] { "il", "im", "in", "ing", "ir" };
            string[] ov_c = new[] { "ol", "om", "on", "ong", "or" };
            string[] uv_c = new[] { "ul", "um", "un", "ung", "ur" };
            string[] ccv = new[] { "bw", "by", "dw", "dy", "fw", "fy", "gw", "gy", "hw", "hy", "kw", "ky"
                                   , "lw", "ly", "mw", "my", "nw", "ny", "pw", "py", "rw", "ry", "sw", "sy"
                                    , "tw", "ty", "ts", "vw", "vy", "zw", "zy"};
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
                    case var str when dr.Contains(str) && !HasOto($"{str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { "jh", s[1].ToString() });
                        break;
                    case var str when tr.Contains(str) && !HasOto($"{str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { "ch", s[1].ToString() });
                        break;
                    case var str when wh.Contains(str) && !HasOto($"{str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { "hh", s[1].ToString() });
                        break;
                    case var str when av_c.Contains(str) && !HasOto($"{str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { "aa", s[1].ToString() });
                        break;
                    case var str when ev_c.Contains(str) && !HasOto($"{str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { "eh", s[1].ToString() });
                        break;
                    case var str when iv_c.Contains(str) && !HasOto($"{str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { "iy", s[1].ToString() });
                        break;
                    case var str when ov_c.Contains(str) && !HasOto($"{str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { "ao", s[1].ToString() });
                        break;
                    case var str when uv_c.Contains(str) && !HasOto($"{str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { "uw", s[1].ToString() });
                        break;
                    case var str when vowel3S.Contains(str) && !HasOto($"{str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { s.Substring(0, 2), s[2].ToString() });
                        break;
                    case var str when vowel4S.Contains(str) && !HasOto($"{str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { s.Substring(0, 2), s.Substring(2, 2) });
                        break;
                    case var str when ccv.Contains(str) && !HasOto($"{str}", note.tone) && !HasOto(ValidateAlias(str), note.tone):
                        modified.AddRange(new string[] { s[0].ToString(), s[1].ToString() });
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
            string path = Path.Combine(PluginDir, "en-cPv.yaml");
            if (!File.Exists(path)) {
                Directory.CreateDirectory(PluginDir);
                File.WriteAllBytes(path, Data.Resources.en_cPv_template);
            }
            // LOAD DICTIONARY FROM SINGER FOLDER
            if (singer != null || singer.Found || singer.Loaded) {
                string file = Path.Combine(singer.Location, "en-cPv.yaml");
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

    public override void SetSinger(USinger singer) {
            if (this.singer != singer) {
                string file;
                if (singer != null && singer.Found && singer.Loaded && !string.IsNullOrEmpty(singer.Location)) {
                    file = Path.Combine(singer.Location, "en-cPv.yaml");
                    if (!File.Exists(file)) {
                        try {
                            File.WriteAllBytes(file, Data.Resources.en_cPv_template);
                        } catch (Exception e) {
                            Log.Error(e, $"Failed to write 'en-cPv.yaml' to singer folder at {file}");
                        }
                    }
                } else if (!string.IsNullOrEmpty(PluginDir)) {
                    file = Path.Combine(PluginDir, "en-cPv.yaml");
                } else {
                    Log.Error("Singer location and PluginDir are both null or empty. Cannot locate 'en-cPv.yaml'.");
                    return; // Exit early to avoid null file issues
                }

                if (File.Exists(file)) {
                    try {
                        var data = Core.Yaml.DefaultDeserializer.Deserialize<ArpabetYAMLData>(File.ReadAllText(file));

                        // Load vowels
                        try {
                            var loadVowels = data.symbols
                                ?.Where(s => s.type == "vowel")
                                .Select(s => s.symbol)
                                .ToList() ?? new List<string>();

                            vowels = vowels.Concat(loadVowels).Distinct().ToArray();
                        } catch (Exception ex) {
                            Log.Error($"Failed to load vowels from en-cPv.yaml: {ex.Message}");
                        }
                        // Load replacements
                        try {
                            if (data?.replacements?.Any() == true) {
                                dictionaryReplacements = data.replacements
                                    .Where(r => !string.IsNullOrEmpty(r.from) && !string.IsNullOrEmpty(r.to))
                                    .ToDictionary(r => r.from, r => r.to);
                            } else {
                                dictionaryReplacements = new Dictionary<string, string>(); // Prevent null reference
                            }
                        } catch (Exception ex) {
                            Log.Error($"Failed to load replacements from en-cPv.yaml: {ex.Message}");
                        }
                        // load fallbacks
                        try {
                            if (data?.fallbacks?.Any() == true) {
                                foreach (var df in data.fallbacks) {
                                    if (!string.IsNullOrEmpty(df.from) && !string.IsNullOrEmpty(df.to)) {
                                        if (!missingVphonemes.ContainsKey(df.from)) {
                                            missingVphonemes[df.from] = df.to;
                                        } else {
                                            Log.Error($"Skipped fallback '{df.from}' from YAML because it already exists.");
                                        }
                                    }
                                }
                            }
                        } catch (Exception ex) {
                            Log.Error($"Failed to load fallbacks from en-cPv.yaml: {ex.Message}");
                        }
                    } catch (Exception ex) {
                       Log.Error($"Failed to parse en-cPv.yaml: {ex.Message}");
                    }
                }
                ReadDictionaryAndInit();
                this.singer = singer;
            }
        }

        public class ArpabetYAMLData {
            public SymbolData[] symbols { get; set; } = Array.Empty<SymbolData>();
            public Replacement[] replacements { get; set; } = Array.Empty<Replacement>();
            public Replacement[] fallbacks { get; set; } = Array.Empty<Replacement>();

            public struct SymbolData {
                public string symbol { get; set; }
                public string type { get; set; }
            }
            public struct Replacement {
                public string from { get; set; }
                public string to { get; set; }
            }
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

            // STARTING V
            if (syllable.IsStartingV) {
                // TRIES - V THEN -V AND SO ON
                basePhoneme = AliasFormat(v, "startingV", syllable.vowelTone, "");
            }
            // [V V] or [V C][- C/C][V]/[V]
            else if (syllable.IsVV) {
                if (!CanMakeAliasExtension(syllable)) {
                    basePhoneme = $"{prevV} {v}";
                    if (!HasOto(basePhoneme, syllable.vowelTone) && !HasOto(ValidateAlias(basePhoneme), syllable.vowelTone) && vvExceptions.ContainsKey(prevV)) {
                        // VV IS NOT PRESENT, CHECKS VVEXCEPTIONS LOGIC
                        var vc = $"{prevV} {vvExceptions[prevV]}";
                        if (!HasOto(vc, syllable.vowelTone) && !HasOto(ValidateAlias(vc), syllable.vowelTone)) {
                            vc = $"{prevV}-";
                        }
                        TryAddPhoneme(phonemes, syllable.tone, vc, $"{vvExceptions[prevV]}", $"{prevV} -", $"_{prevV}", $"- {vvExceptions[prevV]}", $"-{vvExceptions[prevV]}", ValidateAlias(vc), ValidateAlias($"{prevV}-"), ValidateAlias($"_{prevV}"), ValidateAlias($"{vvExceptions[prevV]}"), ValidateAlias($"-{vvExceptions[prevV]}"), ValidateAlias($"- {vvExceptions[prevV]}"));
                        basePhoneme = AliasFormat(v, "vv", syllable.vowelTone, "");
                    } else {
                        {
                            if (!HasOto($"{prevV} {v}", syllable.vowelTone) || !HasOto(ValidateAlias($"{prevV} {v}"), syllable.vowelTone)) {
                                basePhoneme = AliasFormat(v, "vv", syllable.vowelTone, "");
                            } else {
                                basePhoneme = $"{prevV} {v}";
                            }
                        }
                    }
                    // EXTEND AS [V]
                } else if (!HasOto($"{prevV} {v}", syllable.vowelTone) || !HasOto(ValidateAlias($"{prevV} {v}"), syllable.vowelTone) || missingVphonemes.ContainsKey(prevV)) {
                    basePhoneme = AliasFormat(v, "vv", syllable.vowelTone, "");
                } else if (HasOto($"{prevV} {v}", syllable.vowelTone) || HasOto(ValidateAlias($"{prevV} {v}"), syllable.vowelTone) || missingVphonemes.ContainsKey(prevV)) {
                    basePhoneme = AliasFormat(v, "vv", syllable.vowelTone, "");
                } else {
                    // PREVIOUS ALIAS WILL EXTEND as [V V]
                    basePhoneme = null;
                }

            } else if (syllable.IsStartingCVWithOneConsonant) {
                /// [- C/-C/C]
                basePhoneme = AliasFormat(v, "cv", syllable.vowelTone, "");
                TryAddPhoneme(phonemes, syllable.tone, AliasFormat($"{cc[0]}", "cc_start", syllable.tone, ""));

            } else if (syllable.IsStartingCVWithMoreThanOneConsonant) {
                //// CCV with multiple starting consonants with cc's support
                basePhoneme = AliasFormat(v, "cv", syllable.vowelTone, "");
                // TRY RCC [- CC] [-CC] [CC]
                for (var i = cc.Length; i > 1; i--) {
                    if (TryAddPhoneme(phonemes, syllable.tone, AliasFormat($"{string.Join("", cc.Take(i))}", "cc_start", syllable.tone, ""))) {
                        firstC = i - 1;
                    }
                    break;
                }
                // [- C] [-C] [C]
                if (phonemes.Count == 0) {
                    TryAddPhoneme(phonemes, syllable.tone, AliasFormat($"{cc[0]}", "cc_start", syllable.tone, ""));
                }
                /// VCV PART (not starting but in the middle phrase)
            } else {
                // [V] to [-V] to [- V]
                basePhoneme = AliasFormat(v, "cv", syllable.vowelTone, "");

                // try [V C], [V CC], [VC C], [V -][- C]
                for (var i = lastC + 1; i >= 0; i--) {
                    var vr = $"_{prevV}";
                    var vr1 = $"{prevV}-";
                    var vc = $"{prevV} {cc[0]}";
                    bool CCV = false;
                    if (syllable.CurrentWordCc.Length >= 2 && !ccvException.Contains(cc[0])) {
                        if (HasOto($"{string.Join("", cc)}", syllable.vowelTone) || HasOto($"- {string.Join("", cc)}", syllable.vowelTone) || HasOto($"-{string.Join("", cc)}", syllable.vowelTone)) {
                            CCV = true;
                        }
                    }
                    if (i == 0 && !HasOto(vc, syllable.tone)) {
                        TryAddPhoneme(phonemes, syllable.tone, AliasFormat($"{cc[0]}", "cc", syllable.tone, ""));
                        break;
                        /// use vowel ending
                    } else if (DiphthongExceptions.ContainsKey(prevV) && ((HasOto(vr, syllable.tone) || HasOto(ValidateAlias(vr), syllable.tone) || (HasOto(vr1, syllable.tone) || HasOto(ValidateAlias(vr1), syllable.tone)) && !HasOto(vc, syllable.tone)))) {
                        TryAddPhoneme(phonemes, syllable.tone, vr1, vr);
                        TryAddPhoneme(phonemes, syllable.tone, AliasFormat($"{cc[0]}", "cc", syllable.tone, ""));
                        break;
                        /// use consonants for diphthongs if the vb doesn't have vowel endings
                    } else if (DiphthongExceptions.ContainsKey(prevV) && (!(HasOto(vr, syllable.tone) || HasOto(ValidateAlias(vr), syllable.tone) || (HasOto(vr1, syllable.tone) || HasOto(ValidateAlias(vr1), syllable.tone)) && !HasOto(vc, syllable.tone)))) {
                        TryAddPhoneme(phonemes, syllable.tone, $"{DiphthongExceptions[prevV]}", $"- {DiphthongExceptions[prevV]}", $"-{DiphthongExceptions[prevV]}");
                        TryAddPhoneme(phonemes, syllable.tone, AliasFormat($"{cc[0]}", "cc", syllable.tone, ""));

                        break;
                    } else if (HasOto(vc, syllable.tone) || HasOto(ValidateAlias(vc), syllable.tone)) {
                        TryAddPhoneme(phonemes, syllable.tone, vc, ValidateAlias(vc));
                        TryAddPhoneme(phonemes, syllable.tone, AliasFormat($"{cc[0]}", "cc", syllable.tone, ""));
                        break;
                        /// CC+V
                    } else if (CCV) {
                        TryAddPhoneme(phonemes, syllable.tone, vc, ValidateAlias(vc));
                        TryAddPhoneme(phonemes, syllable.tone, AliasFormat($"{string.Join("", cc)}", "cc", syllable.tone, ""));
                        firstC = 1;
                        break;
                    } else {
                        continue;
                    }
                }
            }

            for (var i = firstC; i < lastC; i++) {
                var cc1 = $"{cc.Skip(i)}";
                if (!HasOto(cc1, syllable.tone)) {
                    cc1 = ValidateAlias(cc1);
                }
                // [C1][C2]
                if (!HasOto(cc1, syllable.tone) || !HasOto(ValidateAlias(cc1), syllable.tone)) {
                    cc1 = AliasFormat($"{cc[i + 1]}", "cc", syllable.tone, "");
                }
                if (!HasOto(cc1, syllable.tone)) {
                    cc1 = ValidateAlias(cc1);
                }
                // CCV
                if (syllable.CurrentWordCc.Length >= 2) {
                    if ((HasOto($"{v}", syllable.vowelTone) || HasOto($"- {v}", syllable.vowelTone) || HasOto($"-{v}", syllable.vowelTone)) && HasOto(AliasFormat($"{string.Join("", cc.Skip(i + 1))}", "cc", syllable.tone, ""), syllable.vowelTone)) {
                        basePhoneme = AliasFormat(v, "cv", syllable.vowelTone, "");
                        lastC = i;
                    } else if ((HasOto(AliasFormat(v, "cv", syllable.tone, ""), syllable.vowelTone)) && HasOto(cc1, syllable.vowelTone)) {
                        basePhoneme = AliasFormat(v, "cv", syllable.vowelTone, "");
                    }
                    // [C2C3]
                    if (HasOto(AliasFormat($"{string.Join("", cc.Skip(i + 1))}", "cc", syllable.tone, ""), syllable.vowelTone)) {
                        cc1 = AliasFormat($"{string.Join("", cc.Skip(i + 1))}", "cc", syllable.tone, "");
                    }
                    // CV
                } else if (syllable.CurrentWordCc.Length == 1 && syllable.PreviousWordCc.Length == 1) {
                    basePhoneme = AliasFormat(v, "cv", syllable.vowelTone, "");
                    // [C1] [C2]
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = AliasFormat($"{cc[i + 1]}", "cc", syllable.tone, "");

                    }
                }
                if (i + 1 < lastC) {
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = ValidateAlias(cc1);
                    }
                    // [C1][C2]
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = AliasFormat($"{cc[i + 1]}", "cc", syllable.tone, "");
                    }
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = ValidateAlias(cc1);
                    }
                    // CCV
                    if (syllable.CurrentWordCc.Length >= 2) {
                        if ((HasOto($"{v}", syllable.vowelTone) || HasOto($"- {v}", syllable.vowelTone) || HasOto($"-{v}", syllable.vowelTone)) && HasOto(AliasFormat($"{string.Join("", cc.Skip(i + 1))}", "cc", syllable.tone, ""), syllable.vowelTone)) {
                            basePhoneme = AliasFormat(v, "cv", syllable.vowelTone, "");
                            lastC = i;
                        } else if ((HasOto(AliasFormat(v, "cv", syllable.tone, ""), syllable.vowelTone)) && HasOto(cc1, syllable.vowelTone)) {
                            basePhoneme = AliasFormat(v, "cv", syllable.vowelTone, "");
                        }
                        // [C2C3]
                        if (HasOto(AliasFormat($"{string.Join("", cc.Skip(i + 1))}", "cc", syllable.tone, ""), syllable.vowelTone)) {
                            cc1 = AliasFormat($"{string.Join("", cc.Skip(i + 1))}", "cc", syllable.tone, "");
                        }
                        // CV
                    } else if (syllable.CurrentWordCc.Length == 1 && syllable.PreviousWordCc.Length == 1) {
                        basePhoneme = AliasFormat(v, "cv", syllable.vowelTone, "");
                        // [C1] [C2]
                        if (!HasOto(cc1, syllable.tone)) {
                            cc1 = AliasFormat($"{cc[i + 1]}", "cc", syllable.tone, "");

                        }
                    }
                    if (TryAddPhoneme(phonemes, syllable.tone, $"{cc[i]}{cc[i + 1]}{cc[i + 2]} -")) {
                        // if it exists, use [C1][C2][C3] -
                        i += 2;
                    } else if (HasOto(cc1, syllable.tone) && HasOto(cc1, syllable.tone)) {
                        // like [V C1] [C1 C2] [C2 C3] [C3 ..]
                        phonemes.Add(cc1);
                    } else if (TryAddPhoneme(phonemes, syllable.tone, cc1)) {
                        // like [V C1] [C1 C2] [C2 ..]
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
                var endV = AliasFormat(v, "ending", ending.tone, "");
                if (HasOto(vR, ending.tone) || HasOto(ValidateAlias(vR), ending.tone) || (HasOto(vR1, ending.tone) || HasOto(ValidateAlias(vR1), ending.tone) || (HasOto(vR2, ending.tone) || HasOto(ValidateAlias(vR2), ending.tone)))) {
                    TryAddPhoneme(phonemes, ending.tone, AliasFormat(v, "ending", ending.tone, ""));
                    /// split diphthong vowels
                } else if (vvExceptions.ContainsKey(prevV) && !(HasOto(vR, ending.tone) && HasOto(ValidateAlias(vR), ending.tone) && (HasOto(vR2, ending.tone) || HasOto(ValidateAlias(vR2), ending.tone)))) {
                    TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{vvExceptions[prevV]}", "cv", ending.tone, ""));
                }
            } else if (ending.IsEndingVCWithOneConsonant) {
                var vc = $"{v} {cc[0]}";
                var vcr = $"{v} {cc[0]}-";
                var vcr2 = $"{v}{cc[0]} -";
                var vr = $"_{v}";
                var vr1 = $"{v}-";
                var c_cR = new List<string> { "f", "hh", "h", "s", "sh", "th", "v", "z", "zh", "r", "l", "w", "y", "n", "m", "ng" };
                if (!RomajiException.Contains(cc[0])) {
                    if (HasOto(vcr, ending.tone) || HasOto(ValidateAlias(vcr), ending.tone)) {
                        TryAddPhoneme(phonemes, ending.tone, vcr);
                    } else if (!HasOto(vcr, ending.tone) && !HasOto(ValidateAlias(vcr), ending.tone) && (HasOto(vcr2, ending.tone) || HasOto(ValidateAlias(vcr2), ending.tone))) {
                        TryAddPhoneme(phonemes, ending.tone, vcr2);
                        // double the consonants if has [C -]/[C-]
                    } else if (DiphthongExceptions.ContainsKey(prevV) && ((HasOto(AliasFormat(v, "ending_mix", ending.tone, ""), ending.tone) && (HasOto($"{c_cR[0]} -", ending.tone) || (HasOto($"{c_cR[0]}-", ending.tone)))))) {
                        TryAddPhoneme(phonemes, ending.tone, AliasFormat(v, "ending_mix", ending.tone, ""));
                        TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc1_mix", ending.tone, ""));
                        TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc_mix", ending.tone, ""));
                    } else if (DiphthongExceptions.ContainsKey(prevV) && ((HasOto(AliasFormat(v, "ending_mix", ending.tone, ""), ending.tone)) && !HasOto(vc, ending.tone))) {
                        TryAddPhoneme(phonemes, ending.tone, AliasFormat(v, "ending_mix", ending.tone, ""));
                        TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc_mix", ending.tone, ""));
                        /// use consonants for diphthongs if the vb doesn't have vowel endings
                    } else if (DiphthongExceptions.ContainsKey(prevV) && (!(HasOto(AliasFormat(v, "ending_mix", ending.tone, ""), ending.tone) && !HasOto(vc, ending.tone)))) {
                        TryAddPhoneme(phonemes, ending.tone, $"{DiphthongExceptions[prevV]}", $"- {DiphthongExceptions[prevV]}", $"-{DiphthongExceptions[prevV]}");
                        if (c_cR.Contains(cc.Last())) {
                            if (HasOto(AliasFormat($"{c_cR[0]}", "cc_mix", ending.tone, ""), ending.tone)) {
                                TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc1_mix", ending.tone, ""));
                                TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc_mix", ending.tone, ""));
                            } else if (!(HasOto(AliasFormat($"{c_cR[0]}", "cc_mix", ending.tone, ""), ending.tone))) {
                                TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc1_mix", ending.tone, ""));
                            } else {
                                TryAddPhoneme(phonemes, ending.tone, $"{cc[0]} -", $"{cc[0]}-");
                            }
                        } else if (!c_cR.Contains(cc.Last())) {
                            if (HasOto(AliasFormat($"{c_cR[0]}", "cc_mix", ending.tone, ""), ending.tone)) {
                                TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc_mix", ending.tone, ""));
                            } else if (!(HasOto(AliasFormat($"{c_cR[0]}", "cc_mix", ending.tone, ""), ending.tone))) {
                                TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc1_mix", ending.tone, ""));
                            } else {
                                TryAddPhoneme(phonemes, ending.tone, $"{cc[0]} -", $"{cc[0]}-");
                            }
                        }
                        /// add additional c to those consonants on the top
                    } else if (c_cR.Contains(cc.Last())) {
                        if (HasOto(vc, ending.tone) || HasOto(ValidateAlias(vc), ending.tone)) { 
                            TryAddPhoneme(phonemes, ending.tone, vc);
                            //TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc1_mix", ending.tone, ""));
                            TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc_mix", ending.tone, ""));
                        } else if (HasOto($"{c_cR[0]} -", ending.tone) || HasOto(ValidateAlias($"{c_cR[0]} -"), ending.tone) || (HasOto($"{c_cR[0]}-", ending.tone) || HasOto(ValidateAlias($"{c_cR[0]}-"), ending.tone))) {
                            TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc1_mix", ending.tone, ""));
                            TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc_mix", ending.tone, ""));
                        } else if (!(HasOto($"{c_cR[0]} -", ending.tone) || HasOto(ValidateAlias($"{c_cR[0]} -"), ending.tone) || (HasOto($"{c_cR[0]}-", ending.tone) || HasOto(ValidateAlias($"{c_cR[0]}-"), ending.tone)))) {
                            TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc_mix", ending.tone, ""));
                        } else {
                            TryAddPhoneme(phonemes, ending.tone, $"{cc[0]} -", $"{cc[0]}-");
                        }
                    } else {
                        TryAddPhoneme(phonemes, ending.tone, vc);
                        if (vc.Contains(cc[0])) {
                            TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc_mix", ending.tone, ""));
                        } else {
                            TryAddPhoneme(phonemes, ending.tone, $"{cc[0]} -", $"{cc[0]}-");
                        }
                    }
                }
            } else {
                for (var i = lastC; i >= 0; i--) {
                    var vr = $"_{v}";
                    var vr1 = $"{v}-";
                    var vcc = $"{v} {string.Join("", cc.Take(2))}-";
                    var vcc2 = $"{v}{string.Join(" ", cc.Take(2))} -";
                    var vcc3 = $"{v}{string.Join(" ", cc.Take(2))}";
                    var vcc4 = $"{v} {string.Join("", cc.Take(2))}";
                    var vc = $"{v} {cc[0]}";
                    if (!RomajiException.Contains(cc[0])) {
                        if (i == 0) {
                            if (HasOto(vr, ending.tone) || HasOto(ValidateAlias(vr), ending.tone) && !HasOto(vc, ending.tone)) {
                                TryAddPhoneme(phonemes, ending.tone, vr);
                            }
                            break;
                        } else if ((HasOto(vcc, ending.tone) || HasOto(ValidateAlias(vcc), ending.tone)) && lastC == 1 && !ccvException.Contains(cc[0])) {
                            TryAddPhoneme(phonemes, ending.tone, vcc);
                            firstC = 1;
                            break;
                        } else if ((HasOto(vcc2, ending.tone) || HasOto(ValidateAlias(vcc2), ending.tone)) && lastC == 1 && !ccvException.Contains(cc[0])) {
                            TryAddPhoneme(phonemes, ending.tone, vcc2);
                            firstC = 1;
                            break;
                        } else if (HasOto(vcc3, ending.tone) || HasOto(ValidateAlias(vcc3), ending.tone) && !ccvException.Contains(cc[0])) {
                            TryAddPhoneme(phonemes, ending.tone, vcc3);
                            if (vcc3.EndsWith(cc.Last()) && lastC == 1) {
                                if (consonants.Contains(cc.Last())) {
                                    TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc_mix", ending.tone, ""));
                                }
                            }
                            firstC = 1;
                            break;
                        } else if (HasOto(vcc4, ending.tone) || HasOto(ValidateAlias(vcc4), ending.tone) && !ccvException.Contains(cc[0])) {
                            TryAddPhoneme(phonemes, ending.tone, vcc4);
                            if (vcc4.EndsWith(cc.Last()) && lastC == 1) {
                                if (consonants.Contains(cc.Last())) {
                                    TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[0]}", "cc_mix", ending.tone, ""));
                                }
                            }
                            firstC = 1;
                            break;
                        } else if (DiphthongExceptions.ContainsKey(prevV) && ((HasOto(vr, ending.tone) || HasOto(ValidateAlias(vr), ending.tone) || (HasOto(vr1, ending.tone) || HasOto(ValidateAlias(vr1), ending.tone)) && !HasOto(vc, ending.tone)))) {
                            TryAddPhoneme(phonemes, ending.tone, vr1, vr);
                            /// use consonants for diphthongs if the vb doesn't have vowel endings
                        } else if (DiphthongExceptions.ContainsKey(prevV) && (!(HasOto(vr, ending.tone) || HasOto(ValidateAlias(vr), ending.tone) || (HasOto(vr1, ending.tone) || HasOto(ValidateAlias(vr1), ending.tone)) && !HasOto(vc, ending.tone)))) {
                            TryAddPhoneme(phonemes, ending.tone, $"{DiphthongExceptions[prevV]}", $"- {DiphthongExceptions[prevV]}", $"-{DiphthongExceptions[prevV]}");
                            break;
                        } else {
                            TryAddPhoneme(phonemes, ending.tone, vc);
                            break;
                        }
                    }
                }
                for (var i = firstC; i < lastC; i++) {
                    var cc1 = $"{cc[i]}";
                    var c_cR = new List<string> { "f", "hh", "h", "s", "sh", "th", "v", "z", "zh", "r", "l", "w", "y", "n", "m", "ng" };
                    if (i < cc.Length - 2) {
                        var cc2 = $"{cc[i + 1]}";
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = ValidateAlias(cc1);
                        }
                        // CC FALLBACKS
                        if (!HasOto(cc1, ending.tone) || !HasOto(ValidateAlias(cc1), ending.tone) && !HasOto($"{cc[i]} {cc[i + 1]}", ending.tone)) {
                            // [C1] [C2]
                            cc1 = $"{cc[i + 1]}";
                        } else if (!HasOto(cc1, ending.tone) || !HasOto(ValidateAlias(cc1), ending.tone) && !HasOto($"{cc[i + 1]}", ending.tone)) {
                            // [- C1] [- C2]
                            cc1 = $"- {cc[i + 1]}";
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
                        } else {
                            // like [C1][C2 ...]
                            TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[i]}", "cc1_mix", ending.tone, ""));
                            TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[i + 1]}", "cc1_mix", ending.tone, ""));
                            i++;
                        }
                    } else {
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = ValidateAlias(cc1);
                        }
                        // CC FALLBACKS
                        if (!HasOto(cc1, ending.tone) || !HasOto(ValidateAlias(cc1), ending.tone)) {
                            // [C1] [C2]
                            cc1 = AliasFormat($"{cc[i + 1]}", "cc_end", ending.tone, "");
                        }
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = ValidateAlias(cc1);
                        }
                        // CC FALLBACKS
                        if (!HasOto(cc1, ending.tone) || !HasOto(ValidateAlias(cc1), ending.tone) && !HasOto($"{cc[i]} {cc[i + 1]}", ending.tone)) {
                            // [C1] [C2]
                            cc1 = AliasFormat($"{cc[i + 1]}", "cc1_mix", ending.tone, ""); ;
                        }
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = ValidateAlias(cc1);
                        }
                        if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i]} {cc[i + 1]}-", ValidateAlias($"{cc[i]} {cc[i + 1]}-"))) {
                            // like [C1 C2-]
                            i++;
                        } else if (c_cR.Contains(cc.Last())) {
                            if (HasOto($"{c_cR[0]} -", ending.tone) || HasOto(ValidateAlias($"{c_cR[0]} -"), ending.tone) || (HasOto($"{c_cR[0]}-", ending.tone) || HasOto(ValidateAlias($"{c_cR[0]}-"), ending.tone))) {
                                TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[i]}", "cc1_mix", ending.tone, ""));
                                TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[i + 1]}", "cc1_mix", ending.tone, ""));
                                TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[i + 1]}", "cc_mix", ending.tone, ""));
                                i++;
                            } else if (!(HasOto($"{c_cR[0]} -", ending.tone) || HasOto(ValidateAlias($"{c_cR[0]} -"), ending.tone) || (HasOto($"{c_cR[0]}-", ending.tone) || HasOto(ValidateAlias($"{c_cR[0]}-"), ending.tone)))) {
                                TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[i]}", "cc1_mix", ending.tone, ""));
                                TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[i + 1]}", "cc_mix", ending.tone, ""));
                                i++;
                            }
                        } else if (TryAddPhoneme(phonemes, ending.tone, cc1, ValidateAlias(cc1))) {
                            // like [C1 C2][C2 -]
                            TryAddPhoneme(phonemes, ending.tone, AliasFormat($"{cc[i + 1]}", "cc_mix", ending.tone, ""));
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
        private string AliasFormat(string alias, string type, int tone, string prevV) {
            var aliasFormats = new Dictionary<string, string[]> {
                // Define alias formats for different types
                { "startingV", new string[] { "-", "- ", "_", "" } },
                { "vv", new string[] { "-", "", "_", "- " } },
                { "vvExtend", new string[] { "", "_", "-", "- " } },
                { "cv", new string[] { "-", "", "- ", "_" } },
                { "ending", new string[] { " -", "-", " R" } },
                { "ending_mix", new string[] { "-", " -", "R", " R", "_", "--" } },
                { "cc", new string[] { "", "-", "- ", "_" } },
                { "cc_start", new string[] { "- ", "-", "", "_" } },
                { "cc_end", new string[] { " -", "-", "" } },
                { "cc_mix", new string[] { " -", " R", "-", "", "_", "- ", "-" } },
                { "cc1_mix", new string[] { "", " -", "-", " R", "_", "- ", "-" } },

            };

            // Check if the given type exists in the aliasFormats dictionary
            if (!aliasFormats.ContainsKey(type)) {
                return alias;
            }
            // Get the array of possible alias formats for the specified type
            var formatsToTry = aliasFormats[type];
            int counter = 0;
            foreach (var format in formatsToTry) {
                string aliasFormat;
                if (type.Contains("mix") && counter < 4) {
                    // Alternate between alias + format and format + alias for the first 4 iterations
                    aliasFormat = (counter % 2 == 0) ? alias + format : format + alias;
                    counter++;
                } else if (type.Contains("end")) {
                    aliasFormat = alias + format;
                } else {
                    aliasFormat = format + alias;
                }
                // Check if the formatted alias exists using HasOto and ValidateAlias
                if (HasOto(aliasFormat, tone) || HasOto(ValidateAlias(aliasFormat), tone)) {
                    alias = aliasFormat;
                    return alias;
                }
            }
            return alias;
        }

        protected override string ValidateAlias(string alias) {

            // VALIDATE ALIAS DEPENDING ON METHOD
            if (isMissingVPhonemes || isMissingCPhonemes || isTimitPhonemes) {
                foreach (var phoneme in missingVphonemes.Concat(missingCphonemes).Concat(timitphonemes)) {
                    alias = alias.Replace(phoneme.Key, phoneme.Value);
                }
            }
            return alias;
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

            foreach (var c in longConsonants) {
                if (alias.Contains(c) && !alias.Contains($"{c} -") && !alias.Contains("h -")) {
                    return base.GetTransitionBasicLengthMs() * 2.5;
                }
            }

            foreach (var c in normalConsonants) {
                foreach (var v in normalConsonants.Except(GlideVCCons)) {
                    foreach (var b in normalConsonants.Except(NormVCCons)) {
                        if (alias.Contains(c) && alias.StartsWith(c) &&
                        !alias.Contains("dx") && !alias.Contains($"{c} -") && !alias.Contains($"- {c}") && !alias.Contains("h -")) {
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
                        return base.GetTransitionBasicLengthMs() * 1.8;
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
                if (alias.Contains(c) && alias.StartsWith(c) && !alias.Contains("h -")) {
                    return base.GetTransitionBasicLengthMs() * 1.4;
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
                    if (alias.Contains(c) && !alias.Contains($"{c} -") && !alias.Contains($"- q") && !alias.Contains("ng -") && !alias.Contains("h -")) {
                        return base.GetTransitionBasicLengthMs() * 1.7;
                    }
                }
            }

            foreach (var c in semiVowels) {
                foreach (var v in semilongConsonants.Except(excludedEndings)) {
                    if (alias.Contains(c) && !alias.Contains($"{c} -")) {
                        return base.GetTransitionBasicLengthMs() * 1.4;
                    }
                }
            }

            if (hasCons) {
                return base.GetTransitionBasicLengthMs() * 1.2; // Value for 'cons'
            } else if (haslr) {
                return base.GetTransitionBasicLengthMs() * 1.3; // Value for 'cons'
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
