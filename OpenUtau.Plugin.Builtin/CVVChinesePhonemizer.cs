using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("CVVChinese Phonemizer", "ZH CVVC", "Caprice")]
    public class CVVChinesePhonemizer : Phonemizer {
        static readonly string[] plainVowels = new string[] { "a", "o", "e", "ai", "ei", "ao", "ou", "an", "en", "ang", "eng", "er", "yu", "ir" };

        static readonly string[] vowels = new string[] {
            "a=a,ba,pa,ma,fa,da,ta,na,la,ga,ka,ha,sha,cha,zha,za,ca,sa,ya,dia,nia,lia,jia,qia,xia,wa,gua,kua,hua,zhua,chua,shua,rua",
            "ai=bai,pai,mai,dai,tai,nai,lai,gai,kai,hai,zhai,chai,shai,zai,cai,sai,yai,wai,guai,kuai,huai,zhuai,chuai,shuai",
			"ao=bao,pao,mao,dao,tao,nao,lao,gao,kao,hao,zhao,chao,shao,rao,zao,cao,sao,yao,biao,piao,miao,fiao,diao,tiao,niao,liao,jiao,qiao,xiao",
			"an=ban,pan,man,fan,dan,tan,nan,lan,gan,kan,han,zhan,chan,shan,ran,zan,can,san,wan,duan,tuan,nuan,luan,guan,kuan,huan,zhuan,chuan,shuan,ruan,zuan,cuan,suan",
			"ang=bang,pang,mang,fang,dang,tang,nang,lang,gang,kang,hang,zhang,chang,shang,rang,zang,cang,sang,yang,biang,diang,niang,liang,jiang,qiang,xiang,wang,guang,kuang,huang,zhuang,chuang,shuang",
			"e=me,de,te,ne,le,ge,ke,he,zhe,che,she,re,ze,ce,se",
			"e0=ye,bie,pie,mie,die,tie,nie,lie,jie,qie,xie,yue,nve,lve,jue,que,xue",
			"ei=bei,pei,mei,fei,dei,nei,lei,gei,kei,hei,zhei,shei,zei,sei,wei,dui,tui,gui,kui,hui,zhui,chui,shui,rui,zui,cui,sui",
			"en=ben,pen,men,fen,den,nen,gen,ken,hen,zhen,chen,shen,ren,zen,cen,sen,wen,dun,tun,nun,lun,gun,kun,hun,zhun,chun,shun,run,zun,cun,sun",
			"en0=yan,bian,pian,mian,dian,tian,nian,lian,jian,qian,xian,yuan,lvan,juan,quan,xuan",
			"eng=beng,peng,meng,feng,deng,teng,neng,leng,geng,keng,heng,zheng,cheng,sheng,reng,zeng,ceng,seng,weng",
			"i=yi,bi,pi,mi,di,ti,ni,li,ji,qi,xi",
            "i0=zi,ci,si",
			"ir=zhi,chi,shi,ri",
			"in=yin,bin,pin,min,nin,lin,jin,qin,xin",
			"ing=ying,bing,ping,ming,ding,ting,ning,ling,jing,qing,xing",
			"o=bo,po,mo,fo,lo,wo,duo,tuo,nuo,luo,guo,kuo,huo,zhuo,chuo,shuo,ruo,zuo,cuo,suo",
			"ou=pou,mou,fou,dou,tou,nou,lou,gou,kou,hou,zhou,chou,shou,rou,zou,cou,sou,you,miu,diu,niu,liu,jiu,qiu,xiu",
			"ong=dong,tong,nong,long,gong,kong,hong,zhong,chong,shong,rong,zong,cong,song,yong,jiong,qiong,xiong",
			"u=wu,bu,pu,mu,fu,du,tu,nu,lu,gu,ku,hu,zhu,chu,shu,ru,zu,cu,su",
			"yu=yu,v,nv,lv,ju,qu,xu",
			"yun=yun,lvn,jun,qun,xun",
        };

        static readonly string[] consonants = new string[] {
			"y=yi,ya,ye,yai,yao,you,yan,yin,yang,ying,yong",
			"w=wu,wa,wo,wai,wei,wan,wen,wang,weng,",
			"v=yu,yue,yuan,yun",
            "b=ba,bo,bai,bei,bao,ban,ben,bang,beng,bi,bie,biao,bian,bin,biang,bing,bu",
			"p=pa,po,pai,pei,pao,pou,pan,pen,pang,peng,pi,pie,piao,pian,pin,ping,pu",
			"m=ma,mo,me,mai,mei,mao,mou,man,men,mang,meng,mi,mie,miao,miu,mian,min,ming,mu",
			"f=fa,fo,fei,fou,fan,fen,fang,feng,fiao,fu",
			"d=da,de,dai,dei,dao,dou,dan,den,dang,deng,dong,di,dia,die,diao,diu,dian,diang,ding,du,duo,dui,duan,dun",
			"t=ta,te,tai,tei,tao,tou,tan,tang,teng,tong,ti,tia,tie,tiao,tiu,tian,ting,tu,tuo,tui,tuan,tun",
			"n=na,ne,nai,nei,nao,nou,nan,nen,nang,neng,nong,nu,nuo,nuan,nun,nv,nve",
			"ny=ni,nia,nie,niao,niu,nian,nin,niang,ning",
			"l=la,lo,le,lai,lei,lao,lou,lan,lang,leng,long,lu,luo,luan,lun,lv,lve,lvan,lvn",
			"ly=li,lia,lie,liao,liu,lian,lin,liang,ling",
			"g=ga,ge,gai,gei,gao,gou,gan,gen,gang,geng,gong,gu,gua,guo,guai,gui,guan,gun,guang",
			"k=ka,ke,kai,kei,kao,kou,kan,ken,kang,keng,kong,ku,kua,kuo,kuai,kui,kuan,kun,kuang",
			"h=ha,he,hai,hei,hao,hou,han,hen,hang,heng,hong",
			"hw=hu,hua,huo,huai,hui,huan,hun,huang",
			"jy=ji,jia,jie,jiao,jiu,jian,jin,jiang,jing,jiong",
			"jw=ju,jue,juan,jun",
			"qy=qi,qia,qie,qiao,qiu,qian,qin,qiang,qing,qiong",
			"qw=qu,que,quan,qun",
			"xy=xi,xia,xie,xiao,xiu,xian,xin,xiang,xing,xiong",
			"xw=xu,xue,xuan,xun",
			"zh=zhi,zha,zhe,zhai,zhei,zhao,zhou,zhan,zhen,zhang,zheng,zhong",
			"zhw=zhu,zhua,zhuo,zhuai,zhui,zhuan,zhun,zhuang",
			"ch=chi,cha,che,chai,chao,chou,chan,chen,chang,cheng,chong",
			"chw=chu,chua,chuo,chuai,chui,chuan,chun,chuang",
			"sh=shi,sha,she,shai,shei,shao,shou,shan,shen,shang,sheng,shong",
			"shw=shu,shua,shuo,shuai,shui,shuan,shun,shuang",
			"r=ri,re,rao,rou,ran,ren,rang,reng,rong,ru,rua,ruo,rui,ruan,run",
			"z=zi,za,ze,zai,zei,zao,zou,zan,zen,zang,zeng,zong",
			"zw=zu,zuo,zui,zuan,zun",
			"c=ci,ca,ce,cai,cao,cou,can,cen,cang,ceng,cong",
			"cw=cu,cuo,cui,cuan,cun",
			"s=si,sa,se,sai,sei,sao,sou,san,sen,sang,seng,song",
			"sw=su,suo,sui,suan,sun",
            "R=R",
            "息=息",
            "吸=吸",
            "-=-"
        };

        static readonly Dictionary<string, string> vowelLookup;
        static readonly Dictionary<string, string> consonantLookup;

        static CVVChinesePhonemizer() {
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
                if (plainVowels.Contains(nextUnicode.FirstOrDefault() ?? string.Empty)) {
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
                if (consonantLookup.TryGetValue(nextUnicode.FirstOrDefault() ?? string.Empty, out var con)) {
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
