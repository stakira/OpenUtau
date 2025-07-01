using System;
using System.Collections.Generic;
using System.Linq;

//This file implement utaupy.hts python library's function
//https://github.com/oatsu-gh/utaupy/blob/master/utaupy/hts.py

//HTS labels use b instead of #
//In HTS labels, "xx" is a preserved keyword that means null
namespace OpenUtau.Plugin.Builtin.EnunuOnnx {
    public static class HTS {
        public static readonly string[] KeysInOctave = {
            "C",
            "Db",
            "D",
            "Eb",
            "E",
            "F",
            "Gb",
            "G",
            "Ab",
            "A",
            "Bb",
            "B" ,
        };

        public static readonly Dictionary<string, int> NameInOctave = new Dictionary<string, int> {
            { "C", 0 }, { "C#", 1 }, { "Db", 1 },
            { "D", 2 }, { "D#", 3 }, { "Eb", 3 },
            { "E", 4 },
            { "F", 5 }, { "F#", 6 }, { "Gb", 6 },
            { "G", 7 }, { "G#", 8 }, { "Ab", 8 },
            { "A", 9 }, { "A#", 10 }, { "Bb", 10 },
            { "B", 11 },
        };

        public static string GetToneName(int noteNum) {
            return noteNum < 0 ? string.Empty : KeysInOctave[noteNum % 12] + (noteNum / 12 - 1).ToString();
        }

        //return -1 if error
        public static int NameToTone(string name) {
            if (name.Length < 2) {
                return -1;
            }
            var str = name.Substring(0, (name[1] == '#' || name[1] == 'b') ? 2 : 1);
            var num = name.Substring(str.Length);
            if (!int.TryParse(num, out int octave)) {
                return -1;
            }
            if (!NameInOctave.TryGetValue(str, out int inOctave)) {
                return -1;
            }
            return 12 * (octave + 1) + inOctave;
        }

        //write integer with "p" as positive and "n" as negative. 0 is "p0"
        public static string WriteInt(int integer) {
            return (integer >= 0 ? "p":"m" )+Math.Abs(integer).ToString();
        }
    }
    
    public class HTSPhoneme{
        public string symbol;

        //Links to this phoneme's neighbors and parent
        public HTSPhoneme? prev;
        public HTSPhoneme? next;
        public HTSNote parent;

        //informations about this phoneme
        //v:vowel, c:consonant, p:pause, s:silence, b:break
        public string type = "xx";
        //(number of phonemes before this phoneme in this note) + 1
        public int position = 1;
        //(number of phonemes after this phoneme in this note) + 1
        public int position_backward = 1;
        //Here -1 means null
        //distances to vowels in this note, -1 for vowels themselves
        public int distance_from_previous_vowel = -1;
        public int distance_to_next_vowel = -1;

        public HTSPhoneme(string phoneme, HTSNote note) {
            this.symbol = phoneme;
            this.parent = note;
        }

        public HTSPhoneme? beforePrev {
            get {
                if (prev == null) { return null; } else { return prev.prev;}
            }
        }

        public HTSPhoneme? afterNext {
            get {
                if (next == null) { return null; } else { return next.next; }
            }
        }

        public string dump() {
            //Write phoneme as an HTS line

            string result =
                $"{parent.startMs * 100000} {parent.endMs * 100000} "
                //Phoneme informations
                + string.Format("{0}@{1}^{2}-{3}+{4}={5}_{6}%{7}^{8}_{9}~{10}-{11}!{12}[{13}${14}]{15}", p())
                //Syllable informations
                + string.Format("/A:{0}-{1}-{2}@{3}~{4}", a())
                + string.Format("/B:{0}_{1}_{2}@{3}|{4}", b())
                + string.Format("/C:{0}+{1}+{2}@{3}&{4}", c())
                //Note informations
                + string.Format("/D:{0}!{1}#{2}${3}%{4}|{5}&{6};{7}-{8}", d())
                + string.Format(
                    "/E:{0}]{1}^{2}={3}~{4}!{5}@{6}#{7}+{8}]{9}${10}|{11}[{12}&{13}]{14}={15}^{16}~{17}#{18}_{19};{20}${21}&{22}%{23}[{24}|{25}]{26}-{27}^{28}+{29}~{30}={31}@{32}${33}!{34}%{35}#{36}|{37}|{38}-{39}&{40}&{41}+{42}[{43};{44}]{45};{46}~{47}~{48}^{49}^{50}@{51}[{52}#{53}={54}!{55}~{56}+{57}!{58}^{59}",
                    e())
                +string.Format("/F:{0}#{1}#{2}-{3}${4}${5}+{6}%{7};{8}",f())
                + "/G:xx_xx/H:xx_xx/I:xx_xx/J:xx~xx@1"
                ;
            return result;
        }

        public string[] p() {
            var result = Enumerable.Repeat("xx",16).ToArray();
            result[0] = type;
            result[1] = (beforePrev == null) ? "xx" : beforePrev.symbol;
            result[2] = (prev == null) ? "xx" : prev.symbol;
            result[3] = symbol;
            result[4] = (next == null) ? "xx" : next.symbol;
            result[5] = (afterNext == null) ? "xx" : afterNext.symbol;
            result[11] = position.ToString();
            result[12] = position_backward.ToString();
            result[13] = distance_from_previous_vowel < 0 ? "xx" : distance_from_previous_vowel.ToString();
            result[14] = distance_to_next_vowel < 0 ? "xx" : distance_to_next_vowel.ToString();
            return result;
        }

        public string[] a() {
            return parent.a();
        }

        public string[] b() {
            return parent.b();
        }

        public string[] c() {
            return parent.c();
        }

        public string[] d() {
            return parent.d();
        }
        
        public string[] e() {
            return parent.e();
        }

        public string[] f() {
            return parent.f();
        }
    }

    //TODO
    public class HTSNote {
        public int startMs = 0;
        public int endMs = 0;
        public int positionTicks;
        public int durationTicks = 0;
        public int index = 0;//index of this note in sentence
        public int indexBackwards = 0;
        public int sentenceDurMs = 0;

        public int tone = 0;
        public string[] symbols;

        public HTSNote? prev;
        public HTSNote? next;

        public HTSNote(string[] symbols, int tone, int startms,int endms,int positionTicks, int durationTicks) {
            this.startMs = startms;
            this.endMs = endms;
            this.tone = tone;
            this.symbols = symbols;
            this.positionTicks = positionTicks;
            this.durationTicks = durationTicks;
        }

        public int durationMs {
            get { return endMs - startMs; }
        }

        public int startMsBackwards {
            get { return sentenceDurMs - startMs; }
        }

        public string[] b() {
            return new string[] {
                symbols.Length.ToString(),
                "1",
                "1",
                "xx",
                "xx"
            };
        }

        public string[] a() {
            if (prev == null) {
                return Enumerable.Repeat("xx", 5).ToArray();
            } else {
                return prev.b();
            }
        }

        public string[] c() {
            if (next == null) {
                return Enumerable.Repeat("xx", 5).ToArray();
            } else {
                return next.b();
            }
        }

        public string[] e() {
            var result = Enumerable.Repeat("xx", 60).ToArray();
            result[0] = HTS.GetToneName(tone);
            result[5] = "1";//number_of_syllables
            result[6] = ((durationMs + 5) / 10).ToString();//duration in 10ms
            result[7] = ((durationTicks + 10) / 20).ToString(); //length in 96th note, or 20 ticks
            result[17] = index <= 0 ? "xx" : index.ToString();//index of note in sentence
            result[18] = indexBackwards <= 0 ? "xx" : indexBackwards.ToString();
            result[19] = ((startMs + 50) / 100).ToString();//position in 100ms
            result[20] = ((startMsBackwards + 50) / 100).ToString();
            if (this.tone > 0) {
                result[56] = (prev == null || prev.tone <= 0) ? "p0" : HTS.WriteInt(prev.tone - tone);
                result[57] = (next == null || next.tone <= 0) ? "p0" : HTS.WriteInt(next.tone - tone);
            } else {
                result[56] = "p0";
                result[57] = "p0";
            }
            return result;
        }

        public string[] d() {
            if(prev == null) {
                return Enumerable.Repeat("xx", 60).ToArray();
            } else {
                return prev.e();
            }
        }
        public string[] f() {
            if (next == null) {
                return Enumerable.Repeat("xx", 60).ToArray();
            } else {
                return next.e();
            }
        }
    }
}
