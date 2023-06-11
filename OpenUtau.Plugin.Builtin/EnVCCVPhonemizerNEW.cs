using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Core.G2p;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("English VCCV Phonemizer (New Version)", "EN VCCV (New Version)", "Lotte V", language: "EN")]
    public class EnVCCVPhonemizerNEW : SyllableBasedPhonemizer {
        /// <summary>
        /// Alternative English VCCV phonemizer.
        /// I'm not sure it's good enough, but I tried.
        ///</summary>

        private readonly string[] vowels = "a,e,i,o,u,A,E,I,O,9,@,6,3,Q,8,&,1,0,Ang,9l,8n,x,ll,mm,nn,nng".Split(',');
        private readonly string[] consonants = "b,ch,d,dh,dd,f,g,h,hh,j,k,l,m,n,ng,p,r,s,sh,t,th,v,w,y,z,zh,rr,tt,sk,sp,st,sm,sn,'".Split(',');
        private readonly string[] shortConsonants = "dd".Split(',');
        private readonly string[] longConsonants = "ch,f,j,k,p,s,sh,t,th,tt".Split(',');
        private readonly string[] normalConsonants = "b,d,dh,g,h,hh,l,m,n,ng,r,v,w,y,z,zh,'".Split(',');
        private readonly string[] dentalStops = "t,d".Split(',');
        private readonly string[] notClusters = "dh,dd,hh,ng,sh,th,zh,rr,tt".Split(',');
        private readonly Dictionary<string, string> dictionaryReplacements = ("aa=a;ae=@;ah=u;ao=0;aw=8;ax=x;ay=I;" +
            "b=b;ch=ch;d=d;dh=dh;dx=dd;eh=e;el=ll;em=mm;en=nn;eng=nng;er=3;ey=A;f=f;g=g;" + "hh=h;ih=i;iy=E;jh=j;k=k;l=l;m=m;n=n;ng=ng;ow=O;oy=Q;" +
            "p=p;r=r;s=s;sh=sh;t=t;th=th;uh=6;uw=o;v=v;w=w;y=y;z=z;zh=zh").Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);
        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict-0_7b.txt";
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;
        protected override IG2p LoadBaseDictionary() => new ArpabetG2p();

        //protected override IG2p LoadBaseDictionary() {
        //    var g2ps = new List<IG2p>();

        //    // Load dictionary from plugin folder.
        //    string path = Path.Combine(PluginDir, "envccv.yaml");
        //    if (!File.Exists(path)) {
        //        Directory.CreateDirectory(PluginDir);
        //        File.WriteAllBytes(path, Data.Resources.envccv_template);
        //    }
        //    g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(path)).Build());

        //    // Load dictionary from singer folder.
        //    if (singer != null && singer.Found && singer.Loaded) {
        //        string file = Path.Combine(singer.Location, "envccv.yaml");
        //        if (File.Exists(file)) {
        //            try {
        //                g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(file)).Build());
        //            } catch (Exception e) {
        //                Log.Error(e, $"Failed to load {file}");
        //            }
        //        }
        //    }
        //    g2ps.Add(new ArpabetG2p());
        //    return new G2pFallbacks(g2ps.ToArray());
        //}

        protected override List<string> ProcessSyllable(Syllable syllable) {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;
            //var lastCPrevWord = syllable.prevWordConsonantsCount;

            string basePhoneme;
            var phonemes = new List<string>();
            var lastC = cc.Length - 1;
            var firstC = 0;
            var rv = $"-{v}";
            if (syllable.IsStartingV) {
                if (HasOto(rv, syllable.vowelTone)) {
                    basePhoneme = rv;
                } else if (!HasOto(rv, syllable.vowelTone)) {
                    rv = ValidateAlias(rv);
                    basePhoneme = rv;
                } else {
                    basePhoneme = v;
                }
            } else if (syllable.IsVV) {
                var vv = $"{prevV}{v}";
                var vv2 = $"{prevV} {v}";
                if (!CanMakeAliasExtension(syllable)) {
                    if (HasOto(vv, syllable.vowelTone)) {
                        basePhoneme = vv;
                    }
                    else if (!HasOto(vv, syllable.vowelTone) && HasOto(vv2, syllable.vowelTone)) {
                        basePhoneme = vv2;
                    } else if (!HasOto(vv2, syllable.vowelTone)) {
                        basePhoneme = $"_{v}";
                        //} else {
                        //basePhoneme = $"_{v}";
                        var vr = $"{prevV}-";
                        if (prevV == "Ang") {
                            TryAddPhoneme(phonemes, syllable.vowelTone, $"{prevV} ng", $"A ng", vr);
                            //if (HasOto(vv, syllable.vowelTone)) {
                                if (HasOto($"ng{v}", syllable.vowelTone)) {
                                    basePhoneme = $"ng{v}";
                                //}
                            //} else if (HasOto(vv, syllable.vowelTone)) {
                            //    basePhoneme = vv;
                            }
                        }
                        else if (prevV == "8n" && !HasOto(vv2, syllable.vowelTone)) {
                            TryAddPhoneme(phonemes, syllable.vowelTone, $"{prevV} n", $"8 n", vr);
                            if (HasOto($"n{v}", syllable.vowelTone)) {
                                basePhoneme = $"n{v}";
                            } else {
                                basePhoneme = $"_{v}";
                            }
                        //} else if (prevV == "Ang" && HasOto(vv, syllable.vowelTone) || prevV == "8n" && HasOto(vv, syllable.vowelTone)) {
                        //    basePhoneme = vv;
                        }
                    } else {
                    //    //i give up
                        basePhoneme = vv;
                    }
                    //if (!HasOto(ValidateAlias(vv), syllable.vowelTone) && !HasOto(vv, syllable.vowelTone)) {
                    //    basePhoneme = $"_{v}";
                        
                    //}
                } else {
                    // the previous alias will be extended
                    basePhoneme = null;
                }
            } else if (syllable.IsStartingCVWithOneConsonant) {
                // TODO: move to config -CV or -C CV
                var rcv = $"-{cc[0]}{v}";
                var cv = $"{cc[0]}{v}";
                if (HasOto(rcv, syllable.vowelTone)) {
                    basePhoneme = rcv;
                } else if (!HasOto(rcv, syllable.vowelTone)) {
                    rcv = ValidateAlias(rcv);
                    basePhoneme = rcv;
                } else if (!HasOto(ValidateAlias(rcv), syllable.vowelTone)) {
                    basePhoneme = cv;
                    //if (consonants.Contains(cc[0])) {
                    //    TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}");
                    //}
                } else {
                    basePhoneme = cv;
                    //if (consonants.Contains(cc[0])) {
                    //    TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}");
                    //}
                }
            } else if (syllable.IsStartingCVWithMoreThanOneConsonant) {
                // try RCCV
                var rccv = $"-{string.Join("", cc)}{v}";
                var ucv = $"_{cc.Last()}{v}";
                if (HasOto(rccv, syllable.vowelTone)) {
                    basePhoneme = rccv;
                } else if (!HasOto(rccv, syllable.vowelTone) && HasOto(ValidateAlias(rccv), syllable.vowelTone)) {
                    rccv = ValidateAlias(rccv);
                    basePhoneme = rccv;
                } else {
                    basePhoneme = $"{cc.Last()}{v}";
                    if (HasOto(ucv, syllable.vowelTone)) {
                        basePhoneme = ucv;
                        if (!HasOto(ucv, syllable.vowelTone)) {
                            ucv = ValidateAlias(ucv);
                            basePhoneme = ucv;
                        }
                    }
                    // try RCC
                    for (var i = cc.Length; i > 1; i--) {
                        if (TryAddPhoneme(phonemes, syllable.tone, $"-{string.Join("", cc.Take(i))}")) {
                            firstC = i;
                            break;
                        }
                    }
                    //if (phonemes.Count == 0) {
                    //    TryAddPhoneme(phonemes, syllable.tone, $"- {cc[0]}");
                    //}
                    // try CCV
                    for (var i = firstC; i < cc.Length - 1; i++) {
                        var ccv = string.Join("", cc.Skip(i)) + v;
                        ccv = ValidateAlias(ccv);
                        if (HasOto(ccv, syllable.tone)) {
                            basePhoneme = ccv;
                            lastC = i;
                            if (!HasOto(ccv, syllable.tone)) {
                                ccv = ValidateAlias(ccv);
                                basePhoneme = ccv;
                                lastC = i;
                                break;
                            }
                            break;
                        } else {
                            basePhoneme = $"{cc.Last()}{v}";
                            if (HasOto(ucv, syllable.vowelTone)) {
                                basePhoneme = ucv;
                                if (!HasOto(ucv, syllable.vowelTone)) {
                                    ucv = ValidateAlias(ucv);
                                    basePhoneme = ucv;
                                }
                                break;
                            }
                        }
                    }
                }
            } else { // VCV
                var vcv = $"{prevV}{cc[0]} {v}";
                //var vccv = $"{prevV} {string.Join("", cc)}{v}";
                if (syllable.IsVCVWithOneConsonant && HasOto(vcv, syllable.vowelTone)) {
                    basePhoneme = vcv;
                } else if (syllable.IsVCVWithOneConsonant && !HasOto(vcv, syllable.vowelTone) && HasOto(ValidateAlias(vcv), syllable.vowelTone)) {
                    vcv = ValidateAlias(vcv);
                    basePhoneme = vcv;
                   //}
                //} else if (syllable.IsVCVWithMoreThanOneConsonant && HasOto(vccv, syllable.vowelTone)) {
                //    basePhoneme = vccv;
                //    if (!HasOto(vccv, syllable.vowelTone)) {
                //        vccv = ValidateAlias(vccv);
                //        basePhoneme = vccv;
                //    }
                } else {
                    var cv = cc.Last() + v;
                    if (HasOto(cv, syllable.vowelTone)) {
                        basePhoneme = cv;
                    } else {
                        basePhoneme = $"_{v}";
                    }
                    // try CCV
                    if (cc.Length - firstC > 1) {
                        for (var i = firstC; i < cc.Length; i++) {
                            var ccv = $"{string.Join("", cc.Skip(i))}{v}";
                            var rccv = $"-{string.Join("", cc.Skip(i))}{v}";
                            if (HasOto(ccv, syllable.vowelTone) && !notClusters.Contains(string.Join("", cc.Skip(i)))) {
                                lastC = i;
                                basePhoneme = ccv;
                                break;
                            } else if (!HasOto(ccv, syllable.vowelTone) && HasOto(ValidateAlias(ccv), syllable.vowelTone) && !notClusters.Contains(string.Join("", cc.Skip(i)))) {
                                lastC = i;
                                ccv = ValidateAlias(ccv);
                                basePhoneme = ccv;
                                break;
                            } else if (HasOto(rccv, syllable.vowelTone) && (!HasOto(ccv, syllable.vowelTone)) && !notClusters.Contains(string.Join("", cc.Skip(i)))) {
                                lastC = i;
                                basePhoneme = rccv;
                                if (!HasOto(rccv, syllable.vowelTone)) {
                                    lastC = i;
                                    rccv = ValidateAlias(rccv);
                                    basePhoneme = rccv;
                                    break;
                                }
                                break;
                            }
                        }
                    }
                    // try vcc
                    for (var i = lastC + 1; i >= 0; i--) {
                        //var vr = $"{prevV}-";
                        var vcc = $"{prevV} {string.Join("", cc.Take(i))}";
                        var vcc2 = $"{prevV}{string.Join(" ", cc.Take(2))}";
                        var vcc3 = $"{prevV}{string.Join(" ", cc.Take(i))}";
                        var vcc4 = $"{prevV}{string.Join("", cc.Take(i - 1))}";
                        var vcc5 = $"{prevV}{string.Join(" ", cc.Take(i + 1))}";
                        //var vcc6 = $"{prevV}{cc[0]}{cc[1]} {string.Join("", cc.Take(i + 2))}";
                        //var vcc7 = $"{prevV}{string.Join(" ", cc.Take(i))}";
                        var cc1 = $"{string.Join(" ", cc.Take(2))}";
                        var cc2 = $"{string.Join("", cc.Take(2))}";
                        var vc = $"{prevV} {cc[0]}";
                        var vcr = $"{prevV}{cc[0]}-";
                        //if (i == 0) {
                        //phonemes.Add(vr);
                        //TryAddPhoneme(phonemes, syllable.tone, ValidateAlias(vcc), ValidateAlias(vc), ValidateAlias(vcr));
                        //if (!HasOto(vr, syllable.tone)) {
                        //    vr = ValidateAlias(vr);
                        //    phonemes.Add(vr);
                        //}
                        //}
                        //if (firstC == i - 2 && !HasOto(cc1, syllable.tone) && !HasOto(cc2, syllable.tone)) {
                        //    TryAddPhoneme(phonemes, syllable.tone, ValidateAlias(vcc2));
                        //    //break;
                        //} else if (firstC == i - 1) {

                        //TryAddPhoneme(phonemes, syllable.tone, ValidateAlias(vcc), ValidateAlias(vc), ValidateAlias(vcr));
                        //{
                        //    firstC = i - 1;
                        //} else if (TryAddPhoneme(phonemes, syllable.tone, ValidateAlias(vcc2))) {
                        //    firstC = i - 2;
                        //} else if (TryAddPhoneme(phonemes, syllable.tone, ValidateAlias(vc), ValidateAlias(vcr))) {
                        //    //continue as usual
                        //}
                        //}
                        //i++;
                        //break;
                        //} else {
                        //phonemes.Remove(cc1);
                        //phonemes.Remove(cc2);
                        //TryAddPhoneme(phonemes, syllable.tone, ValidateAlias(vcc4));
                        //if (TryAddPhoneme(phonemes, syllable.tone, ValidateAlias(vcc2))) {

                        //}
                        //    //break;
                        //}
                        //if (!HasOto(cc1, syllable.tone) && !HasOto(cc2, syllable.tone)) {
                        //    vcc2 = ValidateAlias(vcc2);
                        //    phonemes.Add(vcc2);
                        //firstC = i - 2;
                        //} else {

                        //}

                        //} else
                        if (HasOto(vcc, syllable.tone) && !notClusters.Contains(string.Join("", cc.Take(i)))) {
                            phonemes.Add(vcc);
                            firstC = i - 1;
                            break;
                        } else if (!HasOto(vcc, syllable.tone) && HasOto(ValidateAlias(vcc), syllable.tone) && !notClusters.Contains(string.Join("", cc.Take(i)))) {
                            vcc = ValidateAlias(vcc);
                            phonemes.Add(vcc);
                            firstC = i - 1;
                            break;
                        } else if (HasOto(vcc2, syllable.tone) && !HasOto(cc1, syllable.tone) && !HasOto(cc2, syllable.tone) && !notClusters.Contains(string.Join(" ", cc.Take(2)))) {
                            phonemes.Add(vcc2);
                            firstC = i;
                            break;
                        } else if (!HasOto(vcc2, syllable.tone) && HasOto(ValidateAlias(vcc2), syllable.tone) && !HasOto(cc1, syllable.tone) && !HasOto(cc2, syllable.tone) && !notClusters.Contains(string.Join(" ", cc.Take(2)))) {
                            vcc2 = ValidateAlias(vcc2);
                            phonemes.Add(vcc2);
                            firstC = i;
                            break;
                        } else if (HasOto(vcc3, syllable.tone) && !notClusters.Contains(string.Join(" ", cc.Take(i)))) {
                            phonemes.Add(vcc3);
                            firstC = i - 1;
                            break;
                        } else if (!HasOto(vcc3, syllable.tone) && HasOto(ValidateAlias(vcc3), syllable.tone) && !notClusters.Contains(string.Join(" ", cc.Take(i)))) {
                            vcc3 = ValidateAlias(vcc3);
                            phonemes.Add(vcc3);
                            firstC = i - 1;
                            break;
                        } else if (HasOto(vcc4, syllable.tone) && !vcc4.EndsWith("t") && !vcc4.EndsWith("d")) {
                            phonemes.Add(vcc4);
                            firstC = i - 1;
                            break;
                        } else if (!HasOto(vcc4, syllable.tone) && HasOto(ValidateAlias(vcc4), syllable.tone) && !ValidateAlias(vcc4).EndsWith("t") && !ValidateAlias(vcc4).EndsWith("d")) {
                            vcc4 = ValidateAlias(vcc4);
                            phonemes.Add(vcc4);
                            firstC = i - 1;
                            break;
                        } else if (HasOto(vcc5, syllable.tone) && !notClusters.Contains(string.Join("", cc.Take(i + 1)))) {
                            phonemes.Add(vcc5);
                            firstC = i - 1;
                            break;
                        } else if (!HasOto(vcc5, syllable.tone) && HasOto(ValidateAlias(vcc5), syllable.tone) && !notClusters.Contains(string.Join("", cc.Take(i + 1)))) {
                            vcc5 = ValidateAlias(vcc5);
                            phonemes.Add(vcc5);
                            firstC = i - 1;
                            break;
                        //} else if (HasOto(vcc6, syllable.tone)) {
                        //    phonemes.Add(vcc6);
                        //    firstC = i + 2;
                        //    break;
                        //} else if (!HasOto(vcc6, syllable.tone) && HasOto(ValidateAlias(vcc6), syllable.tone)) {
                        //    vcc6 = ValidateAlias(vcc6);
                        //    phonemes.Add(vcc6);
                        //    firstC = i + 2;
                        //    break;
                        //} else if (HasOto(vcc7, syllable.tone)) {
                        //    phonemes.Add(vcc7);
                        //    firstC = i - 1;
                        //    break;
                        //} else if (!HasOto(vcc7, syllable.tone) && HasOto(ValidateAlias(vcc7), syllable.tone)) {
                        //    vcc7= ValidateAlias(vcc7);
                        //    phonemes.Add(vcc7);
                        //    firstC = i - 1;
                        //    break;
                        } else if (HasOto(vc, syllable.tone)) {
                            phonemes.Add(vc);
                            break;
                        } else if (!HasOto(vc, syllable.tone) && HasOto(ValidateAlias(vc), syllable.tone)) {
                            vc = ValidateAlias(vc);
                            phonemes.Add(vc);
                            break;
                        } else if (HasOto(vcr, syllable.tone)) {
                            phonemes.Add(vcr);
                            break;
                        } else if (!HasOto(vcr, syllable.tone) && HasOto(ValidateAlias(vcr), syllable.tone)) {
                            vcr = ValidateAlias(vcr);
                            phonemes.Add(vcr);
                            break;
                            //} else if (HasOto(vcc5, syllable.tone)) {
                            //    phonemes.Add(vcc5);
                            //    firstC = i - 2;
                            //    if (!HasOto(vcc5, syllable.tone)) {
                            //        vcc5 = ValidateAlias(vcc5);
                            //        phonemes.Add(vcc5);
                            //        firstC = i - 2;
                            //        break;
                            //    }
                            //    break;
                            //} else if (HasOto(vcr, syllable.tone)) {
                            //    phonemes.Add(vcr);
                            //    //firstC = i;
                            //    if (!HasOto(vcr, syllable.tone)) {
                            //        vcr = ValidateAlias(vcr);
                            //        phonemes.Add(vcr);
                            //        //firstC = i;
                            //        break;
                            //    }
                            //    break;
                            //} else if (HasOto(vc, syllable.tone)) {
                            //    phonemes.Add(vc);
                            //    //firstC = i;
                            //    if (!HasOto(vc, syllable.tone)) {
                            //        vc = ValidateAlias(vc);
                            //        phonemes.Add(vc);
                            //        //firstC = i;
                            //        break;
                            //    }
                            //    break;
                        } else {
                            continue;
                        }
                    }
                }
            }
            for (var i = firstC; i < lastC; i++) {
                // we could use some CCV, so lastC is used
                // we could use -CC so firstC is used
                var rccv = $"-{string.Join("", cc)}{v}";
                var cc1 = $"{string.Join("", cc.Skip(i))}";
                var ccv = string.Join("", cc.Skip(i)) + v;
                var ucv = $"_{cc.Last()}{v}";
                //var vcc2 = $"{prevV}{cc[i]} {cc[i + 1]}";
                if (!HasOto(rccv, syllable.vowelTone) && !notClusters.Contains(cc1)) {
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = ValidateAlias(cc1);
                    }
                    if (!HasOto(cc1, syllable.tone) || vowels.Contains(cc1)) {
                        cc1 = $"{cc[i]} {cc[i + 1]}";
                    }
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = ValidateAlias(cc1);
                    }
                    if (!HasOto(cc1, syllable.tone) && !notClusters.Contains(cc1)) {
                        cc1 = $"{cc[i]}{cc[i + 1]}";
                    }
                    if (!HasOto(cc1, syllable.tone)) {
                        cc1 = ValidateAlias(cc1);
                    }
                    if (HasOto(ccv, syllable.vowelTone) && !notClusters.Contains(ccv)) {
                        basePhoneme = ccv;
                        if (!HasOto(ccv, syllable.vowelTone) && !notClusters.Contains(string.Join("", cc.Take(i)))) {
                            ccv = ValidateAlias(ccv);
                            basePhoneme = ccv;
                        }
                    } else if (HasOto(ucv, syllable.vowelTone) && HasOto(cc1, syllable.vowelTone) && !cc1.Contains($"{cc[i]} {cc[i + 1]}")) {
                        basePhoneme = ucv;
                        if (!HasOto(ucv, syllable.vowelTone)) {
                            ucv = ValidateAlias(ucv);
                            basePhoneme = ucv;
                        }
                    }
                    var cc2 = $"{string.Join("", cc.Skip(i))}";
                    if (i + 1 < lastC && !notClusters.Contains(cc2)) {
                        if (!HasOto(cc2, syllable.tone)) {
                            cc2 = ValidateAlias(cc2);
                        }
                        if (!HasOto(cc2, syllable.tone) && !notClusters.Contains(cc2)) {
                            cc2 = $"{cc[i + 1]}{cc[i + 2]}";
                        }
                        if (!HasOto(cc2, syllable.tone)) {
                            cc2 = ValidateAlias(cc2);
                        }
                        if (!HasOto(cc2, syllable.tone) || vowels.Contains(cc2)) {
                            cc2 = $"{cc[i + 1]} {cc[i + 2]}";
                        }
                        if (!HasOto(cc2, syllable.tone)) {
                            cc2 = ValidateAlias(cc2);
                        }
                        if (HasOto(ccv, syllable.vowelTone) && !notClusters.Contains(ccv)) {
                            basePhoneme = ccv;
                            if (!HasOto(ccv, syllable.vowelTone) && !notClusters.Contains(ccv)) {
                                ccv = ValidateAlias(ccv);
                                basePhoneme = ccv;
                            }
                        } else if (HasOto(ucv, syllable.vowelTone) && HasOto(cc2, syllable.vowelTone) && !cc2.Contains($"{cc[i + 1]} {cc[i + 2]}")) {
                            basePhoneme = ucv;
                            if (!HasOto(ucv, syllable.vowelTone)) {
                                ucv = ValidateAlias(ucv);
                                basePhoneme = ucv;
                            }
                        }
                        if (HasOto(cc1, syllable.tone) && HasOto(cc2, syllable.tone) && !cc1.Contains($"{string.Join("", cc.Skip(i))}")) {
                            // like [V C1] [C1 C2] [C2 C3] [C3 ..]
                            phonemes.Add(cc1);
                        } else if (TryAddPhoneme(phonemes, syllable.tone, cc1)) {
                            // like [V C1] [C1 C2] [C2 ..]
                            if (cc1.Contains($"{string.Join("", cc.Skip(i))}")) {
                                i++;
                            }
                        } else if (TryAddPhoneme(phonemes, syllable.tone, $"{cc[i]}{cc[i + 1]}-")) {
                            // like [V C1] [C1 C2-] [C3 ..]
                            //if (affricates.Contains(cc[i + 1])) {
                                i++;
                            //} else {
                            //    // continue as usual
                            //}
                        //} else if (affricates.Contains(cc[i])) {
                        //    // like [V C1] [C1] [C2 ..]
                        //    TryAddPhoneme(phonemes, syllable.tone, cc[i], $"{cc[i]}-");
                        }
                    } else {
                        // like [V C1] [C1 C2]  [C2 ..] or like [V C1] [C1 -] [C3 ..]
                        TryAddPhoneme(phonemes, syllable.tone, cc1);
                        //if (affricates.Contains(cc[i]) && !HasOto(cc1, syllable.tone)) {
                        //    TryAddPhoneme(phonemes, syllable.tone, cc[i], $"{cc[i]}-");
                        //}
                    }
                }
            }

            phonemes.Add(basePhoneme);
            return phonemes;
        }

        protected override List<string> ProcessEnding(Ending ending) {
            string[] cc = ending.cc;
            string v = ending.prevV;

            var phonemes = new List<string>();
            var vr = $"{v}-";
            if (ending.IsEndingV) {
                TryAddPhoneme(phonemes, ending.tone, vr, ValidateAlias(vr));
            } else if (ending.IsEndingVCWithOneConsonant) {
                //var vc = $"{v} {cc[0]}";
                var vcr = $"{v}{cc[0]}-";
                //if (HasOto(vcr, ending.tone)) {
                    phonemes.Add(vcr);
                    //if (!HasOto(vcr, ending.tone)) {
                    //    vcr = ValidateAlias(vcr);
                    //    phonemes.Add(vcr);
                    //}
                //} else {
                //    phonemes.Add(vc);
                //    if (!HasOto(vc, ending.tone)) {
                //        vc = ValidateAlias(vc);
                //        phonemes.Add(vc);
                //    }
                    //if (affricates.Contains(cc[0])) {
                    //    TryAddPhoneme(phonemes, ending.tone, $"{cc[0]}-", cc[0]);
                    //} else {
                    //    TryAddPhoneme(phonemes, ending.tone, $"{cc[0]} -", $"{cc[0]}-");
                    //}
                //}
            } else {
                var vccr = $"{v}{string.Join("", cc)}-";
                //var vcc2 = $"{v}{string.Join(" ", cc)}-";
                //var vcc3 = $"{v}{cc[0]} {cc[0 + 1]}-";
                //var vcc4 = $"{v}{cc[0]} {cc[0 + 1]}";
                var vcr = $"{v}{cc[0]}-";
                if (HasOto(vccr, ending.tone)) {
                    //vccr = ValidateAlias(vccr);
                    phonemes.Add(vccr);
                    //if (!HasOto(vccr, ending.tone)) {
                    //    vccr = ValidateAlias(vccr);
                    //    phonemes.Add(vccr);
                    //}
                    //} else if (HasOto(ValidateAlias(vccr), ending.tone)) {
                    //vccr = ValidateAlias(vccr);
                    //phonemes.Add(vccr);
                    //}
                } else if (!HasOto(vccr, ending.tone) && HasOto(ValidateAlias(vccr), ending.tone)) {
                    phonemes.Add(ValidateAlias(vccr));
                    //    phonemes.Add(vcc2);
                    //    if (!HasOto(vcc2, ending.tone)) {
                    //        vcc2 = ValidateAlias(vcc2);
                    //        phonemes.Add(vcc2);
                    //    }
                    //} else if (HasOto(vcc3, ending.tone)) {
                    //    phonemes.Add(vcc3);
                    //    if (!HasOto(vcc3, ending.tone)) {
                    //        vcc3 = ValidateAlias(vcc3);
                    //        phonemes.Add(vcc3);
                    //    }
                } else {
                    //if (HasOto(vcc4, ending.tone)) {
                    //    phonemes.Add(vcc4);
                    //    if (!HasOto(vcc4, ending.tone)) {
                    //        vcc4 = ValidateAlias(vcc4);
                    //        phonemes.Add(vcc4);
                    //    }
                    //} else
                    if (HasOto(vcr, ending.tone)) {
                        phonemes.Add(vcr);
                    } else {
                        vcr = ValidateAlias(vcr);
                        phonemes.Add(vcr);
                    }
                    // all CCs except the first one are /C1C2/, the last one is /C1 C2-/
                    // but if there is no /C1C2/, we try /C1 C2-/, vise versa for the last one
                    for (var i = 0; i < cc.Length - 1; i++) {
                        var cc1 = $"{cc[i]}{cc[i + 1]}-";
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = ValidateAlias(cc1);
                        }
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = $"{cc[i]} {cc[i + 1]}";
                        }
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = ValidateAlias(cc1);
                        }
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = $"{cc[i]}{cc[i + 1]}";
                        }
                        if (!HasOto(cc1, ending.tone)) {
                            cc1 = ValidateAlias(cc1);
                        }
                        if (i < cc.Length - 2) {
                            var cc2 = $"{cc[i]}{string.Join("", cc.Skip(i))}-";
                            var cc3 = $"{cc[i]}{cc[i + 1]}{cc[i + 2]}-";
                            if (HasOto(cc2, ending.tone)) {
                                phonemes.Add(cc2);
                                i++;
                            } else if (!HasOto(cc2, ending.tone)) {
                                cc2 = ValidateAlias(cc2);
                                phonemes.Add(cc2);
                                i++;
                            } else if (HasOto(cc3, ending.tone)) {
                                // like [C1 C2][C2 ...]
                                phonemes.Add(cc3);
                                i++;
                            } else if (!HasOto(cc3, ending.tone)) {
                                cc3 = ValidateAlias(cc3);
                                phonemes.Add(cc3);
                                i++;
                            } else {
                                if (HasOto(cc1, ending.tone)) { //&& (!HasOto(vcc4, ending.tone))) {
                                    // like [C1 C2][C2 ...]
                                    phonemes.Add(cc1);
                                } else if (!HasOto(cc1, ending.tone) && HasOto(ValidateAlias(cc1), ending.tone)) { //&& (!HasOto(vcc4, ending.tone))) {
                                    // like [C1 C2][C2 ...]
                                    phonemes.Add(ValidateAlias(cc1));
                                } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]}{cc[i + 2]}-", ValidateAlias($"{cc[i + 1]}{cc[i + 2]}-"))) {
                                    // like [C1 C2-][C2 ...]
                                    i++;
                                } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]}{cc[i + 2]}", ValidateAlias($"{cc[i + 1]}{cc[i + 2]}"), $"{cc[i + 1]} {cc[i + 2]}", ValidateAlias($"{cc[i + 1]} {cc[i + 2]}"))) {
                                    // like [C1 C2][C3 ...]
                                    //if (cc[i + 2] == cc.Last()) {
                                    //    TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 2]} -", $"{cc[i + 2]}-");
                                    //    i++;
                                    //} else {
                                    //    continue;
                                    //}
                                } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]}{cc[i + 2]}", ValidateAlias($"{cc[i + 1]}{cc[i + 2]}"))) {
                                    // like [C1C2][C3 ...]
                                //} else if (!cc.First().Contains(cc[i + 1]) || !cc.First().Contains(cc[i + 2])) {
                                //    // like [C1][C2 ...]
                                //    //if (affricates.Contains(cc[i]) && (!HasOto(vcc4, ending.tone))) {
                                //    //    TryAddPhoneme(phonemes, ending.tone, cc[i], $"{cc[i]} -", $"{cc[i]}-");
                                //    //}
                                //    TryAddPhoneme(phonemes, ending.tone, cc[i + 1], $"{cc[i + 1]} -", $"{cc[i + 1]}-");
                                //    TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 2]} -", $"{cc[i + 2]}-", cc[i + 2]);
                                //    i++;
                                //} else if (!cc.First().Contains(cc[i])) {
                                //    // like [C1][C2 ...]
                                //    TryAddPhoneme(phonemes, ending.tone, cc[i], $"{cc[i]} -", $"{cc[i]}-");
                                //    i++;
                                }
                            }
                        } else {
                            //if (!HasOto(vcc4, ending.tone)) {
                                if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i]}{cc[i + 1]}-", ValidateAlias($"{cc[i]}{cc[i + 1]}-"))) {
                                    // like [C1 C2-]
                                    i++;
                                } else if (TryAddPhoneme(phonemes, ending.tone, $"{cc[i]}{cc[i + 1]}", ValidateAlias($"{cc[i]}{cc[i + 1]}"), $"{cc[i]} {cc[i + 1]}", ValidateAlias($"{cc[i]} {cc[i + 1]}"))) {
                                    // like [C1C2][C2 -]
                                    //if (affricates.Contains(cc[i + 1])) {
                                    //    TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", $"{cc[i + 1]}-", cc[i + 1]);
                                    //} else {
                                    //    TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", $"{cc[i + 1]}-");
                                    //}
                                    i++;
                                } else if (TryAddPhoneme(phonemes, ending.tone, cc1, ValidateAlias(cc1))) {
                                    // like [C1 C2][C2 -]
                                    //if (affricates.Contains(cc[i + 1])) {
                                    //    TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", $"{cc[i + 1]}-", cc[i + 1]);
                                    //} else {
                                    //    TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", $"{cc[i + 1]}-");
                                    //}
                                    i++;
                                } else {
                                    // like [C1][C2 -]
                                    //if (!HasOto(vcc4, ending.tone)) {
                                        //TryAddPhoneme(phonemes, ending.tone, cc[i], $"{cc[i]} -", $"{cc[i]}-");
                                        //if (!affricates.Contains(cc[0])) {
                                        //    phonemes.Remove(cc[0]);
                                        //}
                                        //TryAddPhoneme(phonemes, ending.tone, $"{cc[i + 1]} -", $"{cc[i + 1]}-", cc[i + 1]);
                                        i++;
                                    //}
                                //}
                            }
                        }
                    }
                }
            }
            return phonemes;
        }

        protected override string ValidateAlias(string alias) {
            //foreach (var consonant in new[] { "b" }) {
            //    foreach (var vowel in new[] { "V" }) {
            //        alias = alias.Replace(consonant + vowel, consonant + "A");
            //    }
            //}
            //foreach (var vowel in new[] { "V " }) {
            //    foreach (var consonant in new[] { "b" }) {
            //        alias = alias.Replace("V " + consonant, "A " + consonant);
            //    }
            //}
            //foreach (var vowel in new[] { "3" }) {
            //    alias = alias.Replace(vowel, "@r");
            //}
            //return alias;

            foreach (var vowel in new[] { "0" }) {
                alias = alias.Replace(vowel, "9");
            }
            foreach (var vowel in new[] { "O" }) {
                alias = alias.Replace(vowel, "0");
            }
            if (alias.Contains("@ngk")) {
                return alias.Replace("@ngk", "Ank");
            } else if (alias.Contains("@ng")) {
                return alias.Replace('@', 'A');
            } else if (alias.Contains("Angk")) {
                return alias.Replace("Angk", "Ank");
            } else if (alias.Contains("@ ng")) {
                return alias.Replace("@", "Ang");
            } else if (alias.Contains("Ang ng")) {
                return alias.Replace("Ang", "A");
            } else if (alias.Contains("@m") || alias.Contains("@n")) {
                return alias.Replace('@', '&');
            } else if (alias.Contains("ingk")) {
                return alias.Replace("ingk", "1nk");;
            } else if (alias.Contains("ing") || alias.Contains("i ng")) {
                return alias.Replace('i', '1');
            } else if (alias.Contains("ngk")) {
                return alias.Replace("ng", "n");
            } else if (alias.Contains("6r")) {
                return alias.Replace('6', 'o');
            } else if (alias.Contains("ir")) {
                return alias.Replace('i', 'E');
            } else if (alias.Contains("er")) {
                return alias.Replace('e', 'A');
            } else if (alias.Contains("al")) {
                return alias.Replace('a', '9');
            } else if (alias.Contains("hy") || alias.Contains("hE")) {
                return alias.Replace("h", "hh");
            } else {
                return alias;
            }
        }

        protected override double GetTransitionBasicLengthMs(string alias = "") {
            foreach (var c in longConsonants) {
                if (alias.Contains(c)) {
                    if (!alias.StartsWith(c)) {
                        return base.GetTransitionBasicLengthMs() * 2.0;
                    }
                }
            }
            foreach (var c in normalConsonants) {
                if (!alias.Contains("_D")) {
                    if (alias.Contains(c)) {
                        if (!alias.StartsWith(c)) {
                            return base.GetTransitionBasicLengthMs();
                        }
                    }
                }
            }

            foreach (var c in shortConsonants) {
                if (alias.Contains(c)) {
                    if (!alias.Contains(" _")) {
                        return base.GetTransitionBasicLengthMs() * 0.50;
                    }
                }
            }
            return base.GetTransitionBasicLengthMs();
        }
    }
}
