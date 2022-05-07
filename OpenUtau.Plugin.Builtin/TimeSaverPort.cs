using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    /// <summary>
    /// The simplest Phonemizer possible. Simply pass the lyric as phoneme.
    /// </summary>
    [Phonemizer("Customise Phonemizer", "Multi", "MrCookies")]
    public class TimeSaverPort : Phonemizer {

        private USinger singer;
        private List<Key_ph> phonemes;
        private IniManager conf;
        private GetInfoNote infoNote;
        private NoteInfo tempNote, lastNote = null;
        private ConsData cons;
        private int l = 0;
        private bool reading = false;

        public override void SetSinger(USinger singer) {
            this.singer = singer;
            LoadConf();
            
        }

        private void LoadConf() {
            if (singer != null && singer.Found && singer.Loaded) {
                if (File.Exists(Path.Combine(singer.Location, "config.ini"))) {
                    //OnAsyncInitStarted();
                    Log.Information("Reading " + Path.Combine(singer.Location, "config.ini"));

                    conf = new IniManager(Path.Combine(singer.Location, "config.ini"));
                    Dictionary<string, string> singerDic;
                    if (conf.getSection("Settings").GetKey("DicCaseSense") == null || !bool.Parse(conf.getSection("Settings").GetKey("DicCaseSense").value)) {
                        if (conf.getSection("Replace") == null) conf.addSection("Replace");
                        conf.getSection("Replace").keys = new Dictionary<string, string>(conf.getSection("Replace").keys, StringComparer.OrdinalIgnoreCase);
                    }
                    if (conf.getSection("Replace") != null) {
                        singerDic = new Dictionary<string, string>(conf.getSection("Replace").keys);
                    } else {
                        singerDic = new Dictionary<string, string>();
                    }
                    cons = new ConsData(conf.getSection("Consonants"));
                    infoNote = new GetInfoNote(new VowelsData(conf.getSection("Vowels")));
                    Task.Run(() => {
                        reading = true;
                        List<string> rex = new List<string>();
                        if (conf.getSection("Settings").GetKeyLow("regex") != null) {
                            rex.AddRange(conf.getSection("Settings").GetKeyLow("regex").value.Split(","));
                        }
                        if (conf.getSection("Settings").GetKeyLow("g2p") != null) {
                            var path = conf.getSection("Settings").GetKeyLow("g2p").value.Split(",")[0];
                            if (conf.getSection("Settings").GetKeyLow("g2p").value.Split(",").Length > 1) {
                                string opt = conf.getSection("Settings").GetKeyLow("g2p").value.Split(",")[1].ToLower();

                                if (opt == "resources") {
                                    Log.Information("Reading dictionary at " + path);
                                    conf.AddBuildInFile(path, "Replace", rex: rex.ToArray());
                                } else {
                                    switch (opt) {
                                        case "local":
                                            path = Path.Combine(singer.Location, path);
                                            break;
                                        case "plugin":
                                            path = Path.Combine(PluginDir, path);
                                            break;
                                    }
                                    Log.Information("Reading dictionary at " + path);
                                    conf.Add(path, "Replace", rex: rex.ToArray());
                                }
                            }
                        }
                        if (singerDic != null) {
                            foreach (var p in singerDic) {
                                conf.getSection("Replace").ModKey(p.Key, p.Value);
                            }
                        }
                        if (conf.getSection("Settings").GetKeyLow("dic_file") != null) {
                            var path = conf.getSection("Settings").GetKeyLow("dic_file").value.Split(",")[0];
                            if (conf.getSection("Settings").GetKeyLow("dic_file").value.Split(",").Length > 1) {
                                switch (conf.getSection("Settings").GetKeyLow("dic_file").value.Split(",")[1].ToLower()) {
                                    case "global":
                                        break;
                                    case "local":
                                        path = Path.Combine(singer.Location, path);
                                        break;
                                    default:
                                        path = Path.Combine(PluginDir, path);
                                        break;
                                }
                            } else {
                                path = Path.Combine(PluginDir, path);
                            }
                            Log.Information("Reading dictionary at " + path);
                            conf.Add(path, "Replace");
                        }
                    }).ContinueWith((task) => {
                        //OnAsyncInitFinished();
                        reading = false;
                        Log.Information("End reading " + Path.Combine(singer.Location, "config.ini"));
                    });
                } else {
                    Log.Error("config.ini not find at " + Path.Combine(singer.Location, "config.ini"));
                    conf = null;
                }
            }
        }
        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            // Note that even when input has multiple notes, only the leading note is used to produce phoneme.
            // This is because the 2nd+ notes will always be extender notes, i.e., with lyric "+" or "+<number>".
            // For this simple phonemizer, all these notes maps to a single phoneme.


            //if start with ? return lyric with out process
            if (conf != null) {
                if (notes[0].lyric.StartsWith("?")) {
                    return new Result {
                        phonemes = new Phoneme[] { new Phoneme {
                        phoneme = notes[0].lyric[1..]
                    }}
                    };
                }

                phonemes = new List<Key_ph>();
                lastNote = null;
                bool ignoreLastNote = false;
                if (prevNeighbour != null)
                    if (prevNeighbour.Value.lyric.StartsWith("?")) {
                        ignoreLastNote = true;
                    } else {
                        lastNote = infoNote.GetInfo(DicReplace(prevNeighbour.Value.lyric, prevNeighbour.Value.phoneticHint).Split(",", StringSplitOptions.RemoveEmptyEntries)[^1]);
                    }

                //if ignoreLastNote = true don't do V C only CC and CV

                if (notes[0].lyric == "-") {
                    if (prevNeighbour != null && prevNeighbour.Value.lyric != "-") {
                        string pre = "", suf = "";
                        if (DicReplace(prevNeighbour.Value.lyric, prevNeighbour.Value.phoneticHint) != "") {
                            if (prevNeighbour.Value.lyric[(prevNeighbour.Value.lyric.StartsWith("+")? 1: 0)..].StartsWith("(")) {
                                pre = prevNeighbour.Value.lyric[(prevNeighbour.Value.lyric.StartsWith("+") ? 1 : 0)..(prevNeighbour.Value.lyric.IndexOf(")") + 1)];
                            }
                            if (prevNeighbour.Value.lyric[(prevNeighbour.Value.lyric.StartsWith("+") ? 1 : 0)..].EndsWith(")")) {
                                suf = prevNeighbour.Value.lyric[prevNeighbour.Value.lyric.LastIndexOf("(")..];
                            }
                            tempNote = infoNote.GetInfo(pre + DicReplace(prevNeighbour.Value.lyric, prevNeighbour.Value.phoneticHint).Split(",", StringSplitOptions.RemoveEmptyEntries)[^1] + suf);
                            final(0, prevNeighbour.Value.tone, prevNeighbour.Value.duration, true, null, true);
                        }
                    }
                } else {

                    notes = FixNotes(notes);
                    List<string> word = DicReplace(notes[0].lyric, notes[0].phoneticHint).Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();

                    if (word[0].StartsWith("%") && word[0].EndsWith("%")) {
                        return new Result {
                            phonemes = new Phoneme[] {
                                new Phoneme {
                                    phoneme = word[0][1..^1]
                            }}
                        };
                    }

                    int p = 0;
                    string pre, suf;
                    for (int i = 0; i < notes.Length; i++) {
                        if (i == notes.Length - 1) {
                            if (notes[i].lyric.StartsWith("+?")) {
                                phonemes.Add(new Key_ph() {
                                    key = notes[i].tone, value = new Phoneme {
                                        phoneme = notes[i].lyric[2..],
                                        position = p
                                    }
                                });
                                p += notes[i].duration;
                                ignoreLastNote = true;
                                continue;
                            }
                            pre = "";
                            suf = "";
                            if (notes[i].lyric.Length > 1) {
                                if (notes[i].lyric[1..].StartsWith("(")) {
                                    pre = notes[i].lyric[1..(notes[i].lyric.IndexOf(")") + 1)];
                                }
                                if (notes[i].lyric[1..].EndsWith(")")) {
                                    suf = notes[i].lyric[notes[i].lyric.LastIndexOf("(")..];
                                }
                            }

                            //last note

                            l = notes[^1].duration / word.Count;
                            for (int ii = 0; ii < word.Count; ii++) {
                                tempNote = infoNote.GetInfo(pre + ((lastNote != null && prevNeighbour != null) ? lastNote.Final + word[ii] : word[ii]) + suf);
                                if (lastNote == null) lastNote = tempNote;
                                if ((prevNeighbour == null || prevNeighbour.Value.lyric == "-") && i == 0 && ii == 0) {
                                    start(p, notes[i].duration, notes[i].tone, null);
                                } else {
                                    middle(p, notes[i].duration, notes[i].tone, (ii == 0) ? (i == 0 ? prevNeighbour.Value.tone : notes[i - 1].tone) : notes[i].tone, (ii == 0) ? (i == 0 ? prevNeighbour.Value.duration : notes[i - 1].duration) : l, ignoreLastNote, null);
                                }
                                lastNote = tempNote;

                                p += l;
                            }

                            break;
                        }
                        if (notes[i].lyric.StartsWith("+?")) {
                            phonemes.Add(new Key_ph() {
                                key = notes[i].tone, value = new Phoneme {
                                    phoneme = notes[i].lyric[2..],
                                    position = p
                                }
                            });
                            p += notes[i].duration;
                            ignoreLastNote = true;
                            continue;
                        }
                        pre = "";
                        suf = "";
                        if (notes[i].lyric.Length > 1) {
                            if (notes[i].lyric[1..].StartsWith("(")) {
                                pre = notes[i].lyric[1..(notes[i].lyric.IndexOf(")") + 1)];
                            }
                            if (notes[i].lyric[1..].EndsWith(")")) {
                                suf = notes[i].lyric[notes[i].lyric.LastIndexOf("(")..];
                            }
                        }

                        tempNote = infoNote.GetInfo(pre + ((lastNote != null && prevNeighbour != null) ? lastNote.Final + word[0] : word[0]) + suf);
                        if (lastNote == null) lastNote = tempNote;
                        if ((prevNeighbour == null || prevNeighbour.Value.lyric == "-") && i == 0) {
                            start(p, notes[i].duration, notes[i].tone, null);
                        } else {
                            middle(p, notes[i].duration, notes[i].tone, (i == 0) ? prevNeighbour.Value.tone : notes[i - 1].tone, (i == 0) ? prevNeighbour.Value.duration : notes[i - 1].duration, ignoreLastNote, null);
                        }
                        lastNote = tempNote;
                        word.RemoveAt(0);
                        if (word.Count == 0) break;
                        p += notes[i].duration;
                        ignoreLastNote = false;
                    }
                    if (nextNeighbour == null || nextNeighbour.Value.lyric.StartsWith("?")) {
                        final(p, notes[^1].tone, l, nextNeighbour == null ? true : !nextNeighbour.Value.lyric.StartsWith("?"), null);
                    }
                    lastNote = null;
                }
                phonemes = phonemes.OrderBy(i => i.value.position).ToList();
                AddDettais(notes[0]);


                return new Result {
                    phonemes = GetPh()
                };
            } else {
                return new Result {
                    phonemes = new Phoneme[] {
                    new Phoneme {
                        phoneme = notes[0].lyric
                    }
                }
                };
            }
        }

        Phoneme[] GetPh() {
            List<Phoneme> temp = new List<Phoneme>();
            foreach (var ph in phonemes) temp.Add(ph.value);
            return temp.ToArray();
        }


        void start(int position, int duration, int tone, double? vel) {
            if (tempNote.Cons != "") {
                if (tempNote.Init.Count == 0) {
                    //-CV
                    string cv = GetRightOne(TransformPattern(conf.getSection("-cv").GetKey("pattern").value, "s", c2: GetReplaceC(GetSmartC(tempNote.Cons, tempNote.vowel.k)), v2: tempNote.vowel), tone, "", "");
                    string[] tempString = cv.Split("/");

                    if (conf.getSection("ConsonantsTime") != null) {
                        if (conf.getSection("ConsonantsTime").GetKey(tempNote.Cons) != null) {
                            if (new Regex("S|A").IsMatch(conf.getSection("ConsonantsTime").GetKey(tempNote.Cons).value.Split(",")[^1].ToUpper())) {
                                if (float.TryParse(conf.getSection("ConsonantsTime").GetKey(tempNote.Cons).value.Split(",")[1], out float offset)) {
                                    if (conf.getSection("ConsonantsTime").GetKey("IsMs") != null && bool.Parse(conf.getSection("ConsonantsTime").GetKey("IsMs").value)) {
                                        offset = MsToTick(offset);
                                    }
                                    if (conf.getSection("ConsonantsTime").GetKey(tempNote._final[^1]).value.Split(",")[0] == "+") {
                                        position += (int)offset;
                                    } else {
                                        position -= (int)offset;
                                    }
                                } else if (conf.getSection("ConsonantsTime").GetKey(tempNote.Cons).value.Split(",")[1].ToLower() == "false") {
                                    if (conf.getSection("ConsonantsTime").GetKey(tempNote.Cons).value.Split(",")[0] == "+") {
                                        position += GetCLenght(GetPreOto(tempString[^1], tone, "", ""), vel);
                                    } else {
                                        position -= GetCLenght(GetPreOto(tempString[^1], tone, "", ""), vel);
                                    }
                                }
                            }
                        }
                    }

                    phonemes.Add(new Key_ph() {
                        key = tone, value = new Phoneme() {
                            phoneme = tempString[^1],
                            position = position
                        }
                    });
                    if (cv.Contains("/")) {
                        phonemes.Add(new Key_ph() {
                            key = tone, value = new Phoneme() {
                                phoneme = tempString[0],
                                position = position - GetCLenght(GetPreOto(tempString[^1], tone, "", ""), vel)
                            }
                        });
                    }
                    if (conf.getSection("v").GetKey("use_always") != null && bool.Parse(conf.getSection("v").GetKey("use_always").value)) {
                        phonemes.Add(new Key_ph() {
                            key = tone, value = new Phoneme() {
                                phoneme = GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""),
                                position = position + Math.Min(duration / 4, GetCLenght(GetPreOto(GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""), tone, "", ""), vel))
                            }
                        });
                    }
                } else {
                    //-C...CV
                    string cv = GetRightOne(TransformPattern(conf.getSection("cv").GetKey("pattern").value, "s", c2: GetReplaceC(GetSmartC(tempNote.Cons, tempNote.vowel.k)), v2: tempNote.vowel), tone, "", "");
                    int l = GetCLenght(GetPreOto(cv, tone, "", ""), vel);


                    if (conf.getSection("ConsonantsTime") != null) {
                        if (conf.getSection("ConsonantsTime").GetKey(tempNote.Cons) != null) {
                            if (new Regex("S|A").IsMatch(conf.getSection("ConsonantsTime").GetKey(tempNote.Cons).value.Split(",")[^1].ToUpper())) {
                                if (float.TryParse(conf.getSection("ConsonantsTime").GetKey(tempNote.Cons).value.Split(",")[1], out float offset)) {
                                    if (conf.getSection("ConsonantsTime").GetKey("IsMs") != null && bool.Parse(conf.getSection("ConsonantsTime").GetKey("IsMs").value)) {
                                        offset = MsToTick(offset);
                                    }
                                    if (conf.getSection("ConsonantsTime").GetKey(tempNote._final[^1]).value.Split(",")[0] == "+") {
                                        position += (int)offset;
                                    } else {
                                        position -= (int)offset;
                                    }
                                } else if (conf.getSection("ConsonantsTime").GetKey(tempNote.Cons).value.Split(",")[1].ToLower() == "false") {
                                    if (conf.getSection("ConsonantsTime").GetKey(tempNote.Cons).value.Split(",")[0] == "+") {
                                        position += l;
                                    } else {
                                        position -= l;
                                    }
                                }
                            }
                        }
                    }

                    List<string> cc = new List<string>();
                    string Scc = GetRightOne(TransformPattern(conf.getSection("-cc").GetKey("pattern").value, "s", c2: GetReplaceC(GetSmartC((tempNote.Init.Count > 1) ? tempNote.Init[1] : tempNote.Cons, tempNote.Init.Count == 1 ? tempNote.vowel.k : tempNote.Init.Count == 2 ? tempNote.Cons : tempNote.Init[2])), c1: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Init.Count == 1 ? tempNote.Cons : tempNote.Init[1]))), tone, tempNote.Prefix, tempNote.Sufix);

                    cc.Add(Scc.Split("/")[0]);

                    if (Scc.Contains("/")) {
                        cc.Add(Scc.Split("/")[1]);
                    }

                    for (int x = 2; x < tempNote.Init.Count; x++) {
                        //maybe to add CC F 
                        /*if (x < tempNote.Init.Count - 1) {
                            if (!AliaExist(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "s", c1: GetReplaceC(GetSmartC(tempNote.Init[x], tempNote.Init[x + 1])), c2: GetReplaceC(GetSmartC(tempNote.Init[x + 1], x + 1 < tempNote.Init.Count - 1 ? tempNote.Init[x + 2] : tempNote.Cons))), tone, "", ""), tone, "", "")){
                                cc.Add(GetRightOne(TransformPattern(conf.getSection("cc F").GetKey("pattern").value, "s", c1: GetReplaceC(GetSmartC(tempNote.Init[x - 1], tempNote.Init[x])), c2: GetReplaceC(GetSmartC(tempNote.Init[x], x < tempNote.Init.Count - 1 ? tempNote.Init[x + 1] : tempNote.Cons))), tone, "", ""));
                                continue;
                            }
                        }*/
                        cc.Add(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "s", c1: GetReplaceC(GetSmartC(tempNote.Init[x - 1], tempNote.Init[x])), c2: GetReplaceC(GetSmartC(tempNote.Init[x], x < tempNote.Init.Count - 1 ? tempNote.Init[x + 1] : tempNote.Cons))), tone, "", ""));
                    }

                    if (tempNote.Init.Count > 1)
                        cc.Add(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "s", c1: GetReplaceC(GetSmartC(tempNote.Init[^1], tempNote.Cons)), c2: GetReplaceC(GetSmartC(tempNote.Cons, tempNote.vowel.k))), tone, tempNote.Prefix, tempNote.Sufix));

                    for (int x = 0; x < cc.Count; x++) {
                        if (AliaExist2(cc[x], tone, "", "", out bool use)) {
                            if (use) { cc[x] = cc[x]; }
                        } else {
                            cc.RemoveAt(x--);
                        }
                    }

                    for (int x = cc.Count - 1; x > -1; x--) {
                        phonemes.Add(new Key_ph() {
                            key = tone, value = new Phoneme() {
                                phoneme = cc[x],
                                position = position - l
                            }
                        });
                        l += GetCLenght(GetPreOto(cc[x], tone, "", ""), vel);
                    }
                    phonemes.Add(new Key_ph() { key = tone, value = new Phoneme() { phoneme = cv, position = position } });
                    if (conf.getSection("v").GetKey("use_always") != null && bool.Parse(conf.getSection("v").GetKey("use_always").value)) {
                        phonemes.Add(new Key_ph() {
                            key = tone, value = new Phoneme() {
                                phoneme = GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""),
                                position = position + Math.Min(duration / 4, GetCLenght(GetPreOto(GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""), tone, "", ""), vel))
                            }
                        });
                    }
                }
            } else {
                //-V
                phonemes.Add(new Key_ph() {
                    key = tone, value = new Phoneme() {
                        phoneme = GetRightOne(TransformPattern(conf.getSection("-v").GetKey("pattern").value, "s", v2: tempNote.vowel), tone, "", ""),
                        position = position
                    }
                });
                if (conf.getSection("v").GetKey("use_always") != null && bool.Parse(conf.getSection("v").GetKey("use_always").value)) {
                    phonemes.Add(new Key_ph() {
                        key = tone, value = new Phoneme() {
                            phoneme = GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""),
                            position = position + Math.Min(duration / 4, GetCLenght(GetPreOto(GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""), tone, "", ""), vel))
                        }
                    });
                }
            }
        }

        void middle(int position, int duration, int tone, int lastTone, int LastDuration, bool IgnoreVC, double? vel) {
            if ((!IgnoreVC) && bool.Parse(conf.getSection("vcv").GetKey("use").value) && AliaExist(GetRightOne(TransformPattern(conf.getSection("vcv").GetKey("pattern").value, "m", v1: lastNote.vowel, v2: tempNote.vowel, c2: GetReplaceC( GetSmartC( tempNote.Cons, tempNote.vowel.k ))), tone, "", ""), tone, "", "") && tempNote.Init.Count == 0) {
                phonemes.Add(new Key_ph() {
                    key = tone, value = new Phoneme() {
                        phoneme = GetRightOne(TransformPattern(conf.getSection("vcv").GetKey("pattern").value, "m", v1: lastNote.vowel, v2: tempNote.vowel, c2: GetReplaceC(GetSmartC(tempNote.Cons, tempNote.vowel.k))), tone, "", ""),
                        position = position
                    }
                });
                if (conf.getSection("v").GetKey("use_always") != null && bool.Parse(conf.getSection("v").GetKey("use_always").value)) {
                    phonemes.Add(new Key_ph() {
                        key = tone, value = new Phoneme() {
                            phoneme = GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""),
                            position = position + Math.Min(duration / 4, GetCLenght(GetPreOto(GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""), tone, "", ""), vel))
                        }
                    });
                }
            } else {
                if(tempNote.Cons == "") {
                    //vv                    
                    phonemes.Add(new Key_ph() {
                        key = tone, value = new Phoneme() {
                            phoneme = GetRightOne(TransformPattern(conf.getSection((!IgnoreVC) ? "v v" : "v").GetKey("pattern").value, "m", v1: (!IgnoreVC) ? lastNote.vowel : null, v2: tempNote.vowel), tone, "", ""),
                            position = position
                        }
                    });
                    if (conf.getSection("v").GetKey("use_always") != null && bool.Parse(conf.getSection("v").GetKey("use_always").value)) {
                        phonemes.Add(new Key_ph() {
                            key = tone, value = new Phoneme() {
                                phoneme = GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""),
                                position = position + Math.Min(duration / 4, GetCLenght(GetPreOto(GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""), tone, "", ""), vel))
                            }
                        });
                    }
                } else {
                    string cv, vc;
                    int l;
                    float FixCC;
                    cv = GetRightOne(TransformPattern(conf.getSection("cv").GetKey("pattern").value, "m", c2: GetReplaceC(GetSmartC(tempNote.Cons, tempNote.vowel.k)), v2: tempNote.vowel), tone, "", "");
                    l = GetCLenght(GetPreOto(cv, tone, "", ""), vel);

                    if (conf.getSection("ConsonantsTime") != null) {
                        if (conf.getSection("ConsonantsTime").GetKey(tempNote.Cons) != null) {
                            if (new Regex("M|A").IsMatch(conf.getSection("ConsonantsTime").GetKey(tempNote.Cons).value.Split(",")[^1].ToUpper())) {
                                if (float.TryParse(conf.getSection("ConsonantsTime").GetKey(tempNote.Cons).value.Split(",")[1], out float offset)) {
                                    if (conf.getSection("ConsonantsTime").GetKey("IsMs") != null && bool.Parse(conf.getSection("ConsonantsTime").GetKey("IsMs").value)) {
                                        offset = MsToTick(offset);
                                    }
                                    if (conf.getSection("ConsonantsTime").GetKey(tempNote._final[^1]).value.Split(",")[0] == "+") {
                                        position += (int)offset;
                                    } else {
                                        position -= (int)Math.Min(offset, lastTone / 2);
                                    }
                                } else if (conf.getSection("ConsonantsTime").GetKey(tempNote.Cons).value.Split(",")[1].ToLower() == "false") {
                                    if (conf.getSection("ConsonantsTime").GetKey(tempNote.Cons).value.Split(",")[0] == "+") {
                                        position += l;
                                    } else {
                                        position -= Math.Min(l , LastDuration / 2);
                                    }
                                }
                            }
                        }
                    }
                    switch (tempNote.Init.Count) {
                        case 0:
                            phonemes.Add(new Key_ph() { key = tone, value = new Phoneme() { phoneme = cv, position = position } });
                            if (conf.getSection("v").GetKey("use_always") != null && bool.Parse(conf.getSection("v").GetKey("use_always").value)) {
                                phonemes.Add(new Key_ph() {
                                    key = tone, value = new Phoneme() {
                                        phoneme = GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""),
                                        position = position + Math.Min(duration / 4, GetCLenght(GetPreOto(GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""), tone, "", ""), vel))
                                    }
                                });
                            }
                            if ((!IgnoreVC) && !(conf.getSection("v c").GetKey("use") != null && !bool.Parse(conf.getSection("v c").GetKey("use").value))) {
                                vc = GetRightOne(TransformPattern(conf.getSection("v c").GetKey("pattern").value, "m", c2: GetReplaceC(GetSmartC(tempNote.Cons, tempNote.vowel.k)), v1: lastNote.vowel), lastTone, "", "");
                                if (AliaExist2(vc, lastTone, "", "", out bool f)) {
                                    phonemes.Add(new Key_ph() {
                                        key = lastTone, value = new Phoneme() {
                                            phoneme = vc,
                                            position = position - Math.Min(LastDuration / 2, l)
                                        }
                                    });
                                }
                            }
                            break;
                        case 1:
                                phonemes.Add(new Key_ph() {
                                    key = tone, value = new Phoneme() {
                                        phoneme = cv,
                                        position = position
                                    }
                                });
                            if (conf.getSection("v").GetKey("use_always") != null && bool.Parse(conf.getSection("v").GetKey("use_always").value)) {
                                phonemes.Add(new Key_ph() {
                                    key = tone, value = new Phoneme() {
                                        phoneme = GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""),
                                        position = position + Math.Min(duration / 4, GetCLenght(GetPreOto(GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""), tone, "", ""), vel))
                                    }
                                });
                            }
                            //vcc cv
                            if ((!IgnoreVC) && bool.Parse(conf.getSection("vcc").GetKey("use").value) && AliaExist(GetRightOne(TransformPattern(conf.getSection("vcc").GetKey("pattern").value, "m", v1: lastNote.vowel, c1: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Cons)), c2: GetReplaceC(GetSmartC(tempNote.Cons, tempNote.vowel.k))), lastTone, "", ""), lastTone, "", "")) {
                                phonemes.Add(new Key_ph() {
                                    key = lastTone, value = new Phoneme() {
                                        phoneme = GetRightOne(TransformPattern(conf.getSection("vcc").GetKey("pattern").value, "m", v1: lastNote.vowel, c1: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Cons)), c2: GetReplaceC(GetSmartC(tempNote.Cons, tempNote.vowel.k))), lastTone, "", ""),
                                        position = Math.Max(position - (LastDuration / 2), position - l)
                                    }
                                });
                            } else {
                                if ((!IgnoreVC) && !AliaExist(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "m", c2: GetReplaceC(GetSmartC(tempNote.Cons, tempNote.vowel.k)), c1: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Cons))), tone, "", ""), tone, "", "") && bool.Parse(conf.getSection("vc").GetKey("use").value)) {
                                    //vc cv
                                    phonemes.Add(new Key_ph() {
                                        key = lastTone, value = new Phoneme() {
                                            phoneme = GetRightOne(TransformPattern(conf.getSection("vc").GetKey("pattern").value, "m", v1: lastNote.vowel, c1: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Cons))), lastTone, "", ""),
                                            position = Math.Max(position - (LastDuration / 2), position - l)
                                        }
                                    });
                                } else {
                                    //v c cc cv
                                    int l2;

                                    if (AliaExist2(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "m", c2: GetReplaceC(GetSmartC(tempNote.Cons, tempNote.vowel.k)), c1: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Cons))), lastTone, "", ""), lastTone, "", "", out bool use)) {
                                        l2 = GetCLenght(GetPreOto(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "m", c2: GetReplaceC(GetSmartC(tempNote.Cons, tempNote.vowel.k)), c1: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Cons))), lastTone, "", ""), lastTone, "", ""), vel);

                                        FixCC = ((l + l2) > (LastDuration / 2)) ? (float)(LastDuration / 2) / (l + l2) : 1;

                                        l = (int)(l * FixCC);
                                        l2 = (int)(l2 * FixCC);
                                        phonemes.Add(new Key_ph() {
                                            key = lastTone, value = new Phoneme() {
                                                phoneme = (use ? "" : "") + GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "m", c2: GetReplaceC(GetSmartC(tempNote.Cons, tempNote.vowel.k)), c1: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Cons))), lastTone, "", "") + (use ? "" : ""),
                                                position = position - l
                                            }
                                        });
                                    } else {
                                        l2 = l;
                                    }


                                    string sec_vc = "v c";
                                    if (conf.getSection("cc").GetKey("vc_exception")?.value != null) {
                                        var ex = conf.getSection("cc").GetKey("vc_exception").value.Split(",", StringSplitOptions.RemoveEmptyEntries);
                                        foreach (var e in ex) {
                                            if (e.Split("=")[0].Split("/", StringSplitOptions.RemoveEmptyEntries).Contains(tempNote.Init[0])) {
                                                sec_vc = e.Split("=")[1];
                                            }
                                        }
                                    }

                                    if ((!IgnoreVC) && (conf.getSection(sec_vc)?.GetKey("use") == null || bool.Parse(conf.getSection(sec_vc).GetKey("use").value))) {
                                        if (AliaExist2(GetRightOne(TransformPattern(conf.getSection(sec_vc).GetKey("pattern").value, "m", c2: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Cons)), v1: lastNote.vowel), lastTone, "", ""), lastTone, "", "", out bool f)) {
                                            phonemes.Add(new Key_ph() {
                                                key = lastTone, value = new Phoneme() {
                                                    phoneme = GetRightOne(TransformPattern(conf.getSection(sec_vc).GetKey("pattern").value, "m", c2: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Cons)), v1: lastNote.vowel), lastTone, "", ""),
                                                    position = position - l - l2
                                                }
                                            });
                                        } else {
                                            phonemes.Add(new Key_ph() {
                                                key = lastTone, value = new Phoneme() {
                                                    phoneme = GetRightOne(TransformPattern(conf.getSection("v c").GetKey("pattern").value, "m", c2: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Cons)), v1: lastNote.vowel), lastTone, "", ""),
                                                    position = position - l - l2
                                                }
                                            });
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            //V C C...CV
                            List<string> cc = new List<string>();
                            float TotalL = l;

                            phonemes.Add(new Key_ph() {
                                key = tone, value = new Phoneme() {
                                    phoneme = cv,
                                    position = position
                                }
                            });
                            if (conf.getSection("v").GetKey("use_always") != null && bool.Parse(conf.getSection("v").GetKey("use_always").value)) {
                                phonemes.Add(new Key_ph() {
                                    key = tone, value = new Phoneme() {
                                        phoneme = GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""),
                                        position = position + Math.Min(duration / 4, GetCLenght(GetPreOto(GetRightOne(TransformPattern(conf.getSection("v").GetKey("pattern").value, "m", v2: tempNote.vowel), tone, "", ""), tone, "", ""), vel))
                                    }
                                });
                            }

                            for (int x = 0; x < tempNote.Init.Count; x++) {
                                if (x == 0) {
                                    if (!IgnoreVC) {
                                        if (bool.Parse(conf.getSection("vcc").GetKey("use").value) && AliaExist(GetRightOne(TransformPattern(conf.getSection("vcc").GetKey("pattern").value, "m", v1: lastNote.vowel, c1: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Init[1])), c2: GetReplaceC(GetSmartC(tempNote.Init[1], tempNote.Init.Count == 2 ? tempNote.Cons : tempNote.Init[2]))), tone, "", ""), tone, "", "")) {
                                            cc.Add(GetRightOne(TransformPattern(conf.getSection("vcc").GetKey("pattern").value, "m", v1: lastNote.vowel, c1: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Init[1])), c2: GetReplaceC(GetSmartC(tempNote.Init[1], tempNote.Init.Count == 2 ? tempNote.Cons : tempNote.Init[2]))), tone, "", ""));
                                            x++;
                                        } else if (tempNote.Init.Count > 2 &&
                                    !AliaExist(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "m", c1: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Init[1])), c2: GetReplaceC(GetSmartC(tempNote.Init[1], tempNote.Init.Count == 2 ? tempNote.Cons : tempNote.Init[2]))), tone, "", ""), tone, "", "")
                                    && bool.Parse(conf.getSection("vc").GetKey("use").value) && AliaExist(GetRightOne(TransformPattern(conf.getSection("vc").GetKey("pattern").value, "m", v1: lastNote.vowel, c1: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Init[1]))), tone, "", ""), tone, "", "")) {
                                            cc.Add(GetRightOne(TransformPattern(conf.getSection("vc").GetKey("pattern").value, "m", v1: lastNote.vowel, c1: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Init[1]))), tone, "", ""));
                                        } else {

                                            string sec_vc = "v c";
                                            if (conf.getSection("cc").GetKey("vc_exception")?.value != null) {
                                                var ex = conf.getSection("cc").GetKey("vc_exception").value.Split(",", StringSplitOptions.RemoveEmptyEntries);
                                                foreach (var e in ex) {
                                                    if (e.Split("=")[0].Split("/", StringSplitOptions.RemoveEmptyEntries).Contains(tempNote.Init[0])) {
                                                        sec_vc = e.Split("=")[1];
                                                    }
                                                }
                                            }
                                            if (conf.getSection(sec_vc)?.GetKey("use") == null || bool.Parse(conf.getSection(sec_vc).GetKey("use").value)) {
                                                if (AliaExist2(GetRightOne(TransformPattern(conf.getSection(sec_vc).GetKey("pattern").value, "m", c2: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Init[1])), v1: lastNote.vowel), tone, "", ""), lastTone, "", "", out bool f)) {
                                                    cc.Add(GetRightOne(TransformPattern(conf.getSection(sec_vc).GetKey("pattern").value, "m", c2: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Init[1])), v1: lastNote.vowel), tone, "", ""));
                                                } else {
                                                    cc.Add(GetRightOne(TransformPattern(conf.getSection("v c").GetKey("pattern").value, "m", c2: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Cons)), v1: lastNote.vowel), lastTone, "", ""));
                                                }
                                            }
                                        }
                                    }
                                } else {
                                    //to add CC F >WORKING<
                                    if (x < tempNote.Init.Count && conf.getSection("cc F") != null && !AliaExist(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "m", c1: GetReplaceC(GetSmartC(tempNote.Init[x], x + 1 < tempNote.Init.Count ? tempNote.Init[x + 1] : tempNote.Cons)), c2: GetReplaceC(GetSmartC(x + 1 < tempNote.Init.Count ? tempNote.Init[x + 1] : tempNote.Cons, x + 2 < tempNote.Init.Count ? tempNote.Init[x + 2] : x + 1 < tempNote.Init.Count ? tempNote.Cons : tempNote.vowel.k))), tone, "", ""), tone, "", "")) {
                                        if (AliaExist(GetRightOne(TransformPattern(conf.getSection("cc F").GetKey("pattern").value, "m", c1: GetReplaceC(GetSmartC(tempNote.Init[x - 1], tempNote.Init[x])), c2: GetReplaceC(GetSmartC(tempNote.Init[x], x + 1 < tempNote.Init.Count ? tempNote.Init[x + 1] : tempNote.Cons))), tone, "", ""), tone, "", "")) {
                                            cc.Add(GetRightOne(TransformPattern(conf.getSection("cc F").GetKey("pattern").value, "m", c1: GetReplaceC(GetSmartC(tempNote.Init[x - 1], tempNote.Init[x])), c2: GetReplaceC(GetSmartC(tempNote.Init[x], x + 1 < tempNote.Init.Count ? tempNote.Init[x + 1] : tempNote.Cons))), tone, "", ""));
                                        } else {
                                            cc.Add(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "m", c1: GetReplaceC(GetSmartC(tempNote.Init[x - 1], tempNote.Init[x])), c2: GetReplaceC(GetSmartC(tempNote.Init[x], x < tempNote.Init.Count - 1 ? tempNote.Init[x + 1] : tempNote.Cons))), tone, "", ""));
                                        }
                                    } else {
                                        cc.Add(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "m", c1: GetReplaceC(GetSmartC(tempNote.Init[x - 1], tempNote.Init[x])), c2: GetReplaceC(GetSmartC(tempNote.Init[x], x < tempNote.Init.Count - 1 ? tempNote.Init[x + 1] : tempNote.Cons))), tone, "", ""));
                                    }
                                }
                            }

                            cc.Add(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "m", c1: GetReplaceC(GetSmartC( tempNote.Init[^1], tempNote.Cons)), c2: GetReplaceC(GetSmartC( tempNote.Cons, tempNote.vowel.k))), tone, "", ""));
                            
                            for (int x = 0; x < cc.Count; x++) {
                                if(AliaExist2(cc[x], lastTone, "", "", out bool use)) {
                                    TotalL += GetCLenght(GetPreOto(cc[x], tone, "", ""),vel);
                                    if (use) {
                                        cc[x] = "" + cc[x] + "";
                                    }
                                } else {
                                    cc.RemoveAt(x--);
                                }
                            }


                            FixCC = (TotalL > (LastDuration / 2)) ? (LastDuration / TotalL) / 2 : 1;

                            for (int x = cc.Count - 1; x > -1; x--) {
                                phonemes.Add(new Key_ph() {
                                    key = lastTone, value = new Phoneme() {
                                        phoneme = cc[x],
                                        position = position - (int)(l * FixCC)
                                    }
                                });
                                if (x > 0) {
                                    l += GetCLenght(GetPreOto(cc[x], lastTone, "", ""), vel);
                                }
                            }
                            break;
                    }
                }
            }
        }

        void final(int position, int tone, int duration, bool DoFinal, double? vel, bool NoMove = false) {
            int l = 0;
            bool move = false;

            if (tempNote._final.Count != 0 && conf.getSection("ConsonantsTime")?.GetKey(tempNote._final[^1]) != null) {
                if (new Regex("F|A").IsMatch(conf.getSection("ConsonantsTime").GetKey(tempNote._final[^1]).value.Split(",")[^1].ToUpper())) {
                    if (float.TryParse(conf.getSection("ConsonantsTime").GetKey(tempNote._final[^1]).value.Split(",")[1], out float offset)) {
                        if (conf.getSection("ConsonantsTime").GetKey("IsMs") != null && bool.Parse(conf.getSection("ConsonantsTime").GetKey("IsMs").value)) {
                            offset = MsToTick(offset);
                        }
                        if (conf.getSection("ConsonantsTime").GetKey(tempNote._final[^1]).value.Split(",")[0] == "+") {
                            position += (int)offset;
                            duration += (int)offset;
                        } else {
                            duration -= (int)Math.Min(offset, duration / 2);
                            position -= (int)Math.Min(offset, duration / 2);
                        }
                    } else if (conf.getSection("ConsonantsTime").GetKey(tempNote._final[^1]).value.Split(",")[1] == "false") {
                        move = true;
                    }
                }
            } else {
                position -= Math.Min(duration / 2, 10);
                duration -= Math.Min(duration / 2, 10);
            }

            switch (tempNote._final.Count) {
                case 0:
                    if (DoFinal) {
                        if (AliaExist(GetRightOne(TransformPattern(conf.getSection("v-").GetKey("pattern").value, "", v2: tempNote.vowel), tone, "", ""), tone, "", "")) {
                            phonemes.Add(new Key_ph() {
                                key = tone, value = new Phoneme() {
                                    phoneme = GetRightOne(TransformPattern(conf.getSection("v-").GetKey("pattern").value, "", v2: tempNote.vowel), tone, "", ""),
                                    position = position
                                }
                            });
                        }
                    }
                    break;
                case 1:
                    //vc-
                    string vc = GetRightOne(TransformPattern(conf.getSection("vc-").GetKey("pattern").value, "", v2: tempNote.vowel, c2: GetReplaceC(GetSmartC(tempNote._final[^1], null))), tone, "", "");
                    if (!NoMove && move && singer.TryGetMappedOto(vc.Split("/")[^1], tone, out var oto))
                        { 
                        position -= (int)Math.Min(Math.Abs(oto.Consonant - oto.Preutter), duration / 2);
                        duration -= (int)Math.Min(Math.Abs(oto.Consonant - oto.Preutter), duration / 2);
                    }
                    if (DoFinal)
                        phonemes.Add(new Key_ph() {
                            key = tone, value = new Phoneme() {
                                phoneme = vc.Split("/")[^1],
                                position = position
                            }
                        });
                    if (vc.Contains("/")) {
                        l = GetCLenght(GetPreOto(vc.Split("/")[1], tone, "", ""), vel);
                        phonemes.Add(new Key_ph() {
                            key = tone, value = new Phoneme() {
                                phoneme = vc.Split("/")[0],
                                position = position - Math.Min(l, duration / 2)
                            }
                        });
                    }
                    break;
                default:
                    //v c cc cc-
                    List<string> cc = new List<string>();
                    float TotalL = 0, FixCC = 1f;
                    string pass = "";
                    bool final = false;

                    for (int x = 0; x < tempNote._final.Count - 1; x++) {
                        if (x == 0) {
                            if (bool.Parse(conf.getSection("vcc").GetKey("use").value) && AliaExist(GetRightOne(TransformPattern(conf.getSection("vcc").GetKey("pattern").value, "", v1: tempNote.vowel, c1: GetReplaceC(GetSmartC(tempNote._final[0], tempNote._final[1])), c2: GetReplaceC(GetSmartC(tempNote._final[1], tempNote._final.Count == 2 ? null : tempNote._final[2]))), tone, "", ""), tone, "", "")) {
                                cc.Add(GetRightOne(TransformPattern(conf.getSection("vcc").GetKey("pattern").value, "", v1: tempNote.vowel, c1: GetReplaceC(GetSmartC(tempNote._final[0], tempNote._final[1])), c2: GetReplaceC(GetSmartC(tempNote._final[1], tempNote._final.Count == 2 ? null : tempNote._final[2]))), tone, "", ""));
                                if (tempNote._final.Count == 2) {
                                    if (bool.Parse(conf.getSection("c-").GetKey("use").value) && AliaExist(GetRightOne(TransformPattern(conf.getSection("c-").GetKey("pattern").value, "", c2: GetReplaceC(GetSmartC(tempNote._final[1], null))), tone, "", ""), tone, "", "")) {
                                        pass = GetRightOne(TransformPattern(conf.getSection("c-").GetKey("pattern").value, "", c2: GetReplaceC(GetSmartC(tempNote._final[1], null))), tone, "", "");
                                    } else {
                                        final = true;
                                    }
                                }
                                x++;
                            } else if (tempNote._final.Count > 2 &&
                                !AliaExist(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "", c1: GetReplaceC(GetSmartC(tempNote._final[0], tempNote._final[1])), c2: GetReplaceC(GetSmartC(tempNote._final[1], tempNote._final[2]))), tone, "", ""), tone, "", "")
                                && bool.Parse(conf.getSection("vc").GetKey("use").value) && AliaExist(GetRightOne(TransformPattern(conf.getSection("vc").GetKey("pattern").value, "", v1: tempNote.vowel, c1: GetReplaceC(GetSmartC(tempNote._final[0], tempNote._final[1]))), tone, "", ""), tone, "", "")) {
                                cc.Add(GetRightOne(TransformPattern(conf.getSection("vc").GetKey("pattern").value, "", v1: tempNote.vowel, c1: GetReplaceC(GetSmartC(tempNote._final[0], tempNote._final[1]))), tone, "", ""));
                            } else {
                                string sec_vc = "v c";
                                if (conf.getSection("cc").GetKey("vc_exception")?.value != null) {
                                    var ex = conf.getSection("cc").GetKey("vc_exception").value.Split(",");
                                    foreach (var e in ex) {
                                        if (e.Split("=")[0].Split("/", StringSplitOptions.RemoveEmptyEntries).Contains(tempNote._final[0])) {
                                            sec_vc = e.Split("=")[1];
                                        }
                                    }
                                }
                                if (conf.getSection(sec_vc).GetKey("use") == null || bool.Parse(conf.getSection(sec_vc).GetKey("use").value)) {
                                    if (AliaExist(GetRightOne(TransformPattern(conf.getSection(sec_vc).GetKey("pattern").value, "", c2: GetReplaceC(GetSmartC(tempNote._final[0], tempNote._final[1])), v1: (lastNote != null) ? lastNote.vowel : null), tone, "", ""), tone, "", "")) {
                                        cc.Add(GetRightOne(TransformPattern(conf.getSection(sec_vc).GetKey("pattern").value, "", c2: GetReplaceC(GetSmartC(tempNote._final[0], tempNote._final[1])), v1: (lastNote != null) ? lastNote.vowel : null), tone, "", ""));
                                    } else {
                                        cc.Add(GetRightOne(TransformPattern(conf.getSection("v c").GetKey("pattern").value, "m", c2: GetReplaceC(GetSmartC(tempNote.Init[0], tempNote.Cons)), v1: lastNote.vowel), tone, "", ""));
                                    }
                                }
                            }
                        } else {
                            //maybe to add CC F
                            cc.Add(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "f", c1: GetReplaceC(GetSmartC(tempNote._final[x - 1], tempNote._final[x])), c2: GetReplaceC(GetSmartC(tempNote._final[x], x < tempNote._final.Count - 1 ? tempNote._final[x + 1] : null))), tone, "", ""));
                            /*if (x < tempNote.Init.Count && conf.getSection("cc F") != null && !AliaExist(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "m", c1: GetReplaceC(GetSmartC(tempNote._final[x], x + 1 < tempNote._final.Count ? tempNote._final[x + 1] : tempNote.Cons)), c2: GetReplaceC(GetSmartC(x + 1 < tempNote._final.Count ? tempNote._final[x + 1] : null, x + 2 < tempNote._final.Count ? tempNote._final[x + 2] : null))), tone, "", ""), tone, "", "")) {
                                if (AliaExist(GetRightOne(TransformPattern(conf.getSection("cc F").GetKey("pattern").value, "m", c1: GetReplaceC(GetSmartC(tempNote._final[x - 1], tempNote._final[x])), c2: GetReplaceC(GetSmartC(tempNote._final[x], x + 1 < tempNote._final.Count ? tempNote._final[x + 1] : tempNote.Cons))), tone, "", ""), tone, "", "")) {
                                    cc.Add(GetRightOne(TransformPattern(conf.getSection("cc F").GetKey("pattern").value, "m", c1: GetReplaceC(GetSmartC(tempNote._final[x - 1], tempNote._final[x])), c2: GetReplaceC(GetSmartC(tempNote._final[x], x + 1 < tempNote._final.Count ? tempNote._final[x + 1] : tempNote.Cons))), tone, "", ""));
                                } else {
                                    cc.Add(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "m", c1: GetReplaceC(GetSmartC(tempNote._final[x - 1], tempNote._final[x])), c2: GetReplaceC(GetSmartC(tempNote._final[x], x < tempNote._final.Count - 1 ? tempNote._final[x + 1] : tempNote.Cons))), tone, "", ""));
                                }
                            } else {
                                cc.Add(GetRightOne(TransformPattern(conf.getSection("cc").GetKey("pattern").value, "m", c1: GetReplaceC(GetSmartC(tempNote._final[x - 1], tempNote._final[x])), c2: GetReplaceC(GetSmartC(tempNote._final[x], x < tempNote._final.Count - 1 ? tempNote._final[x + 1] : tempNote.Cons))), tone, "", ""));
                            }*/
                        }
                    }
                    if (final) {
                        l = GetCLenght(GetPreOto(cc[^1], tone, "", ""), null);
                    } else {
                        string ccF;
                        if (pass == "")
                            ccF = GetRightOne(TransformPattern(conf.getSection("cc-").GetKey("pattern").value, "", c1: GetReplaceC(GetSmartC(tempNote._final[^2], tempNote._final[^1])), c2: GetReplaceC(GetSmartC(tempNote._final[^1], null))), tone, "", "");
                        else
                            ccF = pass;

                        string[] tempArray = ccF.Split("/");
                        if (AliaExist2(tempArray[^1], tone, "", "", out bool use)) {
                            if (!NoMove && move) {
                                if (singer.TryGetMappedOto(tempArray[^1], tone, out var oto2)) {
                                    position -= (int)Math.Min(Math.Abs(oto2.Consonant - oto2.Preutter), duration / 2);
                                    duration -= (int)Math.Min(Math.Abs(oto2.Consonant - oto2.Preutter), duration / 2);
                                }
                            }
                            if (DoFinal)
                                phonemes.Add(new Key_ph() {
                                    key = tone, value = new Phoneme() {
                                        phoneme = (use ? "" : "") + tempArray[^1] + (use ? "" : ""),
                                        position = position
                                    }
                                });
                        }
                        l = GetCLenght(GetPreOto(tempArray[^1], tone, "", ""), null);
                        if (tempArray.Length > 1) {
                            cc.Add(tempArray[^2]);
                        }
                    }

                    for (int x = 1; x < cc.Count; x++) {
                        if (AliaExist2(cc[x], tone, "", "", out bool use)) {
                            if (use) {
                                cc[x] = cc[x];
                            }
                            TotalL += GetCLenght(GetPreOto(cc[x], tone, "", ""), vel);
                        } else {
                            cc.RemoveAt(x--);
                        }
                    }
                    TotalL += l;

                    FixCC = (TotalL > (duration / 2)) ? duration / 2 / TotalL : 1;
                    for (int x = cc.Count - 1; x >= 0; x--) {
                        if (l < 5) {
                            l = 5;
                        }
                        phonemes.Add(new Key_ph() {
                            key = tone, value = new Phoneme() {
                                phoneme = cc[x],
                                position = position - (int)(l * FixCC)
                            }
                        });
                        if (x > 0) {
                            l += GetCLenght(GetPreOto(cc[x], tone, "", ""), vel);
                        }
                    }
                    break;
            }
        }

        bool IsInRange(string range, int tone) {
            return GetIntNote(range.Split("-")[0]) <= tone && tone <= GetIntNote(range.Split("-")[1]);
        }
        int GetIntNote(string num) {
            int tn = (int.Parse(num.Replace(getLetters(num), "")) + 1) * 12;
            switch (getLetters(num.ToLower())) {
                case "c": tn += 0; break;
                case "c#": tn += 1; break;
                case "d": tn += 2; break;
                case "d#": tn += 3; break;
                case "e": tn += 4; break;
                case "f": tn += 5; break;
                case "f#": tn += 6; break;
                case "g": tn += 7; break;
                case "g#": tn += 8; break;
                case "a": tn += 9; break;
                case "a#": tn += 10; break;
                case "b": tn += 11; break;
            }
            return tn;
        }
        static string getLetters(string str) {
            string t = "";
            foreach (char c in str) {
                if (c < '0' || c > '9' || c == '#')
                    t += c;
            }
            return t;
        }
        string GetReplacePrefix(string pre, int tone) {
            if (conf.getSection("Prefix").GetKey(pre) != null)
                return conf.getSection("Prefix").GetKey(pre).value;
            else
                if (pre == "") {
                foreach (var sb in singer.Subbanks)
                    if (IsInRange(sb.ToneRangesString, tone))
                        return sb.Prefix;
                return "";
            } else {
                return pre;
            }
        }
        string GetReplaceSufix(string suf, int tone) {
            if (conf.getSection("Sufix").GetKey(suf) != null)
                return conf.getSection("Sufix").GetKey(suf).value;
            else
                if (suf == "") {
                foreach (var sb in singer.Subbanks)
                    if (IsInRange(sb.ToneRangesString, tone))
                        return sb.Suffix;
                return "";
            } else {
                return suf;
            }
        }
        string DicReplace(string str, string hint) {
            if (conf == null || (conf.getSection("Replace") == null && hint == null)) return str;

            string pre = "", suf = "";

            if (hint != null) {
                hint = hint.Trim();

                if (hint.Contains(" ")) {
                    string tmp = "";
                    foreach (var s in hint.Split(" ")) {
                        if (infoNote.isVowel(s)) {
                            tmp += $"{s},";
                        } else if (infoNote.HaveVowel(s) != null) {
                            tmp += $"{s.Replace(infoNote.HaveVowel(s).k, "")}/{infoNote.HaveVowel(s).k},";
                        } else {
                            tmp += $"{s}/";
                        }
                    }
                    tmp = FixFinal(tmp);
                    if (tmp.EndsWith("/") || tmp.EndsWith(",")) tmp = tmp[0..^1];
                    hint = tmp;
                }
                if (infoNote.HaveVowel(hint.Split(",")[^1]) == null) {
                    string temp = hint.Split(",")[0];
                    for (int i = 1; i < hint.Split(",").Length - 1; i++) {
                        temp += "," + hint.Split(",")[i];
                    }
                    hint = temp + "/" + hint.Split(",")[^1];
                }
                return pre + hint + suf;
            }
            if (reading) return "%Loading...%";
            if (!string.IsNullOrEmpty(str)) {

                var tmp_str = "";
                foreach (var s in str.Split(" ")) {
                    tmp_str += " " + (conf.getSection("Replace").GetKey(s) != null ? conf.getSection("Replace").GetKey(s).value : s);
                }
                str = tmp_str.Trim();

                if (str.Contains(" ")) {
                    string tmp = "";
                    foreach (var s in str.Split(" ", StringSplitOptions.RemoveEmptyEntries)) {
                        if (infoNote.isVowel(s)) {
                            tmp += $"{s},";
                        } else if (infoNote.HaveVowel(s) != null) {
                            tmp += $"{s.Replace(infoNote.HaveVowel(s).k, "")}/{infoNote.HaveVowel(s).k},";
                        } else {
                            tmp += $"{s}/";
                        }
                    }
                    tmp = FixFinal(tmp);
                    if (tmp.EndsWith("/") || tmp.EndsWith(",")) tmp = tmp[0..^1];
                    str = tmp;
                }
                if (str.Contains(",")) {
                    if (infoNote.HaveVowel(str.Split(",")[^1]) == null) {
                        string temp = str.Split(",")[0];
                        for (int i = 1; i < str.Split(",").Length - 1; i++) {
                            temp += "," + str.Split(",")[i];
                        }
                        str = temp + "/" + str.Split(",")[^1];
                    }
                }

                str = str.Replace(",", suf + "," + pre);
                return pre + str + suf;
            }
            return "%Empty note%";
        }
        string GetSmartC(string c, string next) {
            if (conf.getSection("ConsonantsAuto")?.GetKey(c) != null) {
                string[] op = conf.getSection("ConsonantsAuto").GetKey(c).value.Split(",");
                if (next != null) {
                    for (int i = 1; i < op.Length; i++) {
                        if (op[i].Split("=")[1].Split("/").Contains(next)) {
                            return op[i].Split("=")[0];
                        }
                    }
                }
                return op[0];
            }
            return c;
        }
        ConsData.Cons GetReplaceC(string c) {
            ConsData.Cons con = new ConsData.Cons();
            if (cons == null) {
                con.SetAll(c);
                return con;
            } else if (cons._Cons.ContainsKey(c)) {
                return cons._Cons[c];
            } else {
                con.SetAll(c);
                return con;
            }
        }
        Note[] FixNotes(Note[] n) {
            List<Note> result = new List<Note>();
            foreach (Note note in n) {
                if (note.lyric != "+*" && note.lyric != "+~" && note.lyric != "+-") {
                    result.Add(note);
                } else {
                    var temp = result[^1];
                    temp.duration += note.duration;
                    result[^1] = temp;
                }
            }
            return result.ToArray();
        }
        int GetPreOto(string alia, int tone, string prefix, string sufix) {
            if (singer.TryGetMappedOto(prefix + alia + sufix, tone, out var oto)) {
                return MsToTick(oto.Preutter + ((oto.Overlap < 0) ? Math.Abs(oto.Overlap) : 0));
            }
            if (singer.TryGetMappedOto(alia, tone, out var oto2)) {
                return MsToTick(oto2.Preutter + ((oto2.Overlap < 0) ? Math.Abs(oto2.Overlap) : 0));
            }
            return 30;
        }
        bool AliaExist(string alia, int tone, string prefix, string sufix) {
            if (sufix == "" && prefix == "") {
                if (singer.TryGetMappedOto(alia, tone, out var oto)) {
                    return true;
                }
            } else {
                if (singer.TryGetMappedOto(prefix + alia + sufix, tone, out var oto)) {
                    return true;
                } else if (singer.TryGetMappedOto(alia, tone, out var oto2)) {
                    return true;
                }
            }
            return false;
        }
        bool AliaExist2(string alia, int tone, string prefix, string sufix, out bool find) {
            if (sufix == "" && prefix == "") {
                if (singer.TryGetMappedOto(alia, tone, out var oto)) {
                    find = false;
                    return true;
                }
            } else {
                if (singer.TryGetMappedOto(prefix + alia + sufix, tone, out var oto)) {
                    find = true;
                    return true;
                } else if (singer.TryGetMappedOto(alia, tone, out var oto2)) {
                    find = false;
                    return true;
                }
            }
            find = false;
            return false;
        }
        //change to AliaExist3
        string AliaExist3(string alia, int tone, string color, string sufix, int? alternator) {
            if (singer.TryGetMappedOto(alia, tone, color, out var oto)) {
                return oto.Alias;
            }
            return null;
        }
        void AddDettais(Note note) {
            foreach(var attr in note.phonemeAttributes) {
                int i = attr.index;
                if (i < phonemes.Count) {
                    var ph = phonemes[i];
                    if (singer.TryGetMappedOto(phonemes[i].value.phoneme + attr.alternate, phonemes[i].key + attr.toneShift, attr.voiceColor, out var oto)) {
                        ph.value.phoneme = oto.Alias;
                    } else if (singer.TryGetMappedOto(phonemes[i].value.phoneme, phonemes[i].key + attr.toneShift, attr.voiceColor, out var oto2)) {
                        ph.value.phoneme = oto2.Alias;
                    }
                    phonemes[i] = ph;
                }
            }
        }
        string GetRightOne(string alia, int tone, string prefix, string sufix) {
            if (conf.getSection("ReplacePieces") != null) {
                foreach (string piece in alia.Split(",")) {
                    foreach(var pair in conf.getSection("ReplacePieces").keys) {
                        if(Regex.IsMatch(alia, pair.Key)) {
                            alia = Regex.Replace(alia, pair.Key, pair.Value);
                        }
                    }
                }
            }
            List<string> temp = new List<string>(alia.Split(","));
            List<Piece> s = new List<Piece>();
            foreach (string x in temp) {
                int f = 0;
                foreach (string y in x.Split("/")) {
                    if (singer.TryGetMappedOto(y, tone, out var oto) || singer.TryGetMappedOto(prefix + y + sufix, tone, out var oto2)) {
                        f++;
                    }
                }
                s.Add(new Piece() { ratio = (double)f / x.Split("/").Length, value = x, Npiece = x.Split("/").Length });
            }

            Piece best = new Piece();
            foreach (var x in s) {
                if (best.ratio < x.ratio) {
                    best = x;
                } else if (best.ratio == x.ratio && best.Npiece > x.Npiece) {
                    best = x;
                }
            }
#if DEBUG
            if(best.ratio == 0) {
                Log.Information($"Don't find any piece at {alia}");
            }
#endif
            return best.value;
        }

        class Piece { public double ratio = 0; public int Npiece = 0; public string value = "Not Find"; }

        int GetCLenght(int tick, double? Vel) {
            if (Vel == null) {
                    return tick;
            } else {
                return (int)(tick * Vel);
            }
        }
        string FixFinal(string l) {
            while (l.Contains("//"))
                l = l.Replace("//", "/");

            return l;
        }

        //port function

        public class IniManager {
            public Dictionary<string, Section> sections = new Dictionary<string, Section>(StringComparer.OrdinalIgnoreCase);

            public IniManager() { }
            public IniManager(string path) {
                if (File.Exists(path)) {
                    string currentSec = "";
                    foreach (string temp in File.ReadAllLines(path)) {
                        if (temp.StartsWith(";") || temp.StartsWith("#") || string.IsNullOrEmpty(temp.Trim())) {
                            continue;
                        } else if (temp.StartsWith("[")) {
                            currentSec = temp[1..^1];
                            if (getSection(currentSec) == null)
                                addSection(currentSec);
                        } else if (temp.Contains("=")) {
                            getSection(currentSec).ModKey(temp.Split("=")[0], string.Join("=", temp.Split("=")[1..]));
                        }
                    }
                }
            }

            public void Add(string path, string sec = "", string[] rex = null) {
                if (File.Exists(path)) {
                    string currentSec = sec, separator;
                    foreach (var temp in File.ReadAllLines(path)) {
                        if (temp.StartsWith(";") || temp.StartsWith("#") || string.IsNullOrEmpty(temp.Trim())) {
                            continue;
                        } else if (temp.StartsWith("[")) {
                            currentSec = temp[1..^1];
                            if (getSection(currentSec) == null)
                                addSection(currentSec);
                        } else if (Regex.IsMatch(temp, "(  |=)")) {
                            separator = Regex.Match(temp, "(  |=)").Value;
                            if (getSection(currentSec) == null && currentSec != "")
                                addSection(currentSec);
                            string value = string.Join(separator, temp.Split(separator)[1..]);
                            if (rex != null && rex.Length > 0) {
                                foreach (var pair in rex) {
                                    if (Regex.IsMatch(value, pair.Split("=")[0]))
                                        value = Regex.Replace(value, pair.Split("=")[0], pair.Split("=")[1]);
                                }
                            }
                            getSection(currentSec).ModKey(temp.Split(separator)[0], value);
                        }
                    }
                    Log.Information("End reading " + path);
                } else {
                    Log.Error(path + " don't exist!");
                }
            }

            public void AddBuildInFile(string ZipName, string sec = "", string[] rex = null) {
                if (Data.Resources.ResourceManager.GetObject(ZipName) != null) {
                    string currentSec = sec, separator;
                    foreach (var temp in Zip.ExtractText((byte[])Data.Resources.ResourceManager.GetObject(ZipName), "dict.txt")) {
                        if (temp.StartsWith(";") || temp.StartsWith("#")) {
                            continue;
                        } else if (temp.StartsWith("[")) {
                            currentSec = temp[1..^1];
                            if (getSection(currentSec) == null)
                                addSection(currentSec);
                        } else if (Regex.IsMatch(temp, "(  |=)")) {
                            separator = Regex.Match(temp, "(  |=)").Value;
                            if (getSection(currentSec) == null && currentSec != "")
                                addSection(currentSec);
                            string value = string.Join(separator, temp.Split(separator)[1..]);
                            if (ZipName == "g2p_arpabet") value = value.ToLower();
                            if (rex != null && rex.Length > 0) {
                                foreach (var pair in rex) {
                                    if (Regex.IsMatch(value, pair.Split("=")[0]))
                                        value = Regex.Replace(value, pair.Split("=")[0], pair.Split("=")[1]);
                                }
                            }
                            getSection(currentSec).ModKey(temp.Split(separator)[0], value);
                        }
                    }
                    Log.Information("End reading " + ZipName);
                } else {
                    Log.Error(ZipName + " don't exist!");
                }
            }
            public void addSection(string sec) {
                sections.Add(sec, new Section());
            }
            public Section getSection(string s) {
                if (sections.ContainsKey(s)) {
                    return sections[s];
                } else {
                    return null;
                }
            }
        }
        public class Section {
            public Dictionary<string, string> keys = new Dictionary<string, string>();

            public void ModKey(string key, string value) {
                if (keys.ContainsKey(key)) {
                    keys[key] = value;
                } else {
                    keys.Add(key, value);
                }
            }

            public Key GetKey(string key) {
                if(keys.TryGetValue(key, out string value)) {
                    return new Key() { key = key, value = value };
                } else {
                    return null;
                }
            }

            public Key GetKeyLow(string lkey) {
                string temp = keys.FirstOrDefault(x => x.Key.ToLower() == lkey.ToLower()).Key;
                if (temp == null) {
                    return null;
                } else {
                    return new Key() { key = temp, value = keys[temp] };
                }
            }
        }
        public class Key { public string key = "", value = ""; }
        public class Key_ph { public int key = 0; public Phoneme value; }
        public class NoteInfo {
            public string Sufix = "", Prefix = "", Lyric = "", Cons, Final = "";
            public List<string> Init = new List<string>(), _final = new List<string>();
            public VowelsData.Vowel vowel = new VowelsData.Vowel();
        }
        public class GetInfoNote {
            public VowelsData v;
            public GetInfoNote(VowelsData vd) {
                v = vd;
            }

            public NoteInfo GetInfo(string Lyric) {
                NoteInfo note = new NoteInfo();
                note.Lyric = Lyric;
                if (Lyric.Contains("/")) {
                    string[] temp = Lyric.Split("/", StringSplitOptions.RemoveEmptyEntries);
                    List<string> c = new List<string>(), c2 = new List<string>();
                    foreach (var x in temp) {
                        if (HaveVowel(x) != null && note.vowel.k == "") {
                            note.vowel = HaveVowel(x);
                            note.Cons = x.Replace(note.vowel.k, "");
                        } else {
                            if (note.vowel.k == "") {
                                c.Add(x);
                            } else {
                                c2.Add(x);
                                note.Final += x + "/";
                            }
                        }
                    }
                    if (note.Cons == null) {
                        note.Cons = c[^1];
                        c.RemoveAt(c.Count - 1);
                        note.Init = c;
                        note._final = c2;
                    } else {
                        note.Init = c;
                        note._final = c2;
                    }
                } else {
                    if (HaveVowel(Lyric) != null) {
                        note.vowel = HaveVowel(Lyric);
                        note.Cons = Lyric.Replace(note.vowel.k, "");
                    } else {
                        note.Cons = Lyric;
                    }
                }
                if (note.Cons == "" && note.Init.Count > 0) {
                    note.Cons = note.Init[^1];
                    note.Init.RemoveAt(note.Init.Count - 1);
                }
                note.Final = note.Final.Replace("//", "/");
                return note;
            }

            public VowelsData.Vowel HaveVowel(string s) {
                return DictionarySort(v._Vowel).FirstOrDefault(v => s.Contains(v.Key)).Value;
            }

            public bool isVowel(string s) {
                return DictionarySort(v._Vowel).FirstOrDefault(v => s.Contains(v.Key)).Key == s;
            }
        }
        public class ConsData {
            public Dictionary<string, Cons> _Cons = new Dictionary<string, Cons>();
            public ConsData(Section s) {
                if(s != null)
                foreach (var x in s.keys) {
                    _Cons.Add(x.Key, new Cons());
                    if (x.Value.Split(",").Length < 2) {
                        _Cons[x.Key].SetAll(x.Value);
                    } else if (x.Value.Split(",").Length < 4) {
                        _Cons[x.Key].SetAll(x.Value.Split(",")[0], x.Value.Split(",")[1]);
                    } else if (x.Value.Split(",").Length < 6) {
                        _Cons[x.Key].SetSta(x.Value.Split(",")[0], x.Value.Split(",")[1]);
                        _Cons[x.Key].SetMid(x.Value.Split(",")[2], x.Value.Split(",")[3]);
                        _Cons[x.Key].SetFin(x.Value.Split(",")[2], x.Value.Split(",")[3]);
                    } else {
                        _Cons[x.Key].SetSta(x.Value.Split(",")[0], x.Value.Split(",")[1]);
                        _Cons[x.Key].SetMid(x.Value.Split(",")[2], x.Value.Split(",")[3]);
                        _Cons[x.Key].SetFin(x.Value.Split(",")[4], x.Value.Split(",")[5]);
                    }
                }
            }
            public class Cons {
                public string ConsL = "", ConsR = "", SConsL = "", SConsR = "", FConsL = "", FConsR = "";
                public void SetAll(string c) { ConsL = c; ConsR = c; SConsL = c; SConsR = c; FConsL = c; FConsR = c; }
                public void SetAll(string s, string f) { ConsL = s; ConsR = f; SConsL = s; SConsR = f; FConsL = s; FConsR = f; }
                public void SetSta(string s, string f) { SConsL = s; SConsR = f; }
                public void SetMid(string s, string f) { ConsL = s; ConsR = f; }
                public void SetFin(string s, string f) { FConsL = s; FConsR = f; }
            }
        }
        public class VowelsData {
            public Dictionary<string, Vowel> _Vowel = new Dictionary<string, Vowel>();
            public VowelsData(Section s) {
                foreach (var x in s.keys) {
                    _Vowel.Add(x.Key, new Vowel() { k = x.Key, VowelL = x.Value.Split(",")[0], VowelR = x.Value.Split(",")[1] });
                }
                _Vowel.OrderBy(x => x.Key.Length);
            }
            public class Vowel {
                public string k = "", VowelL = "", VowelR = "";
            }
        }
        public static IOrderedEnumerable<KeyValuePair<string, VowelsData.Vowel>> DictionarySort(Dictionary<string, VowelsData.Vowel> dict) {
            return dict.OrderByDescending(i => i.Key.Length);
        }
        string TransformPattern(string pattern,string sec, ConsData.Cons c2 = null, VowelsData.Vowel v2 = null, VowelsData.Vowel v1 = null, ConsData.Cons c1 = null ) {
            if (c1 != null) {
                if (sec == "s") {
                    pattern = pattern.Replace("%C1L%", c1.SConsL);
                    pattern = pattern.Replace("%C1R%", c1.SConsR);
                } else if (sec == "m") {
                    pattern = pattern.Replace("%C1L%", c1.ConsL);
                    pattern = pattern.Replace("%C1R%", c1.ConsR);
                } else {
                    pattern = pattern.Replace("%C1L%", c1.FConsL);
                    pattern = pattern.Replace("%C1R%", c1.FConsR);
                }
            }
            if (c2 != null) {
                if (sec == "s") {
                    pattern = pattern.Replace("%C2L%", c2.SConsL);
                    pattern = pattern.Replace("%C2R%", c2.SConsR);
                } else if (sec == "m") {
                    pattern = pattern.Replace("%C2L%", c2.ConsL);
                    pattern = pattern.Replace("%C2R%", c2.ConsR);
                } else {
                    pattern = pattern.Replace("%C2L%", c2.FConsL);
                    pattern = pattern.Replace("%C2R%", c2.FConsR);
                }
            }
            if (v1 != null) {
                pattern = pattern.Replace("%V1L%", v1.VowelL);
                pattern = pattern.Replace("%V1R%", v1.VowelR);
            }
            if (v2 != null) {

                pattern = pattern.Replace("%V2L%", v2.VowelL);
                pattern = pattern.Replace("%V2R%", v2.VowelR);
            }

            return pattern;
        }
    }
}
