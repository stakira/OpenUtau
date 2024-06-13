using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using OpenUtau.Core;

namespace OpenUtau.Plugin.Builtin {
    /// Phonemizer for 'KOR CV' ///
    [Phonemizer("Korean CV Phonemizer", "KO CV", "EX3", language: "KO")]

    public class KoreanCVPhonemizer : BaseKoreanPhonemizer {

        // 1. Load Singer and Settings
        private KoreanCVIniSetting koreanCVIniSetting; // Manages Setting

        public bool isUsingShi, isUsing_aX, isUsing_i, isRentan;

        public override void SetSinger(USinger singer) {
            if (this.singer == singer) {return;}
            this.singer = singer;
            if (this.singer == null) {return;}

            if (this.singer.SingerType != USingerType.Classic){return;}

            koreanCVIniSetting = new KoreanCVIniSetting();
            koreanCVIniSetting.Initialize(singer, "ko-CV.ini", new Hashtable(){
                {"CV", new Hashtable(){
                    {"Use rentan", false},
                    {"Use 'shi' for '시'(otherwise 'si')", false},
                    {"Use 'i' for '의'(otherwise 'eui')", false},
                }},
                {"BATCHIM", new Hashtable(){
                    {"Use 'aX' instead of 'a X'", false}
                }}
            });

            isUsingShi = koreanCVIniSetting.isUsingShi;
            isUsing_aX = koreanCVIniSetting.isUsing_aX;
            isUsing_i = koreanCVIniSetting.isUsing_i;
            isRentan = koreanCVIniSetting.isRentan;
        }

        private class KoreanCVIniSetting : BaseIniManager{
            public bool isRentan;
            public bool isUsingShi;
            public bool isUsing_aX;
            public bool isUsing_i;

            protected override void IniSetUp(Hashtable iniSetting) {
                // ko-CV.ini
                SetOrReadThisValue("CV", "Use rentan", false, out var resultValue); // 연단음 사용 유무 - 기본값 false
                isRentan = resultValue;
                
                SetOrReadThisValue("CV", "Use 'shi' for '시'(otherwise 'si')", false, out resultValue); // 시를 [shi]로 표기할 지 유무 - 기본값 false
                isUsingShi = resultValue;

                SetOrReadThisValue("CV", "Use 'i' for '의'(otherwise 'eui')", false, out resultValue); // 의를 [i]로 표기할 지 유무 - 기본값 false
                isUsing_i = resultValue;

                SetOrReadThisValue("BATCHIM", "Use 'aX' instead of 'a X'", false, out resultValue); // 받침 표기를 a n 처럼 할 지 an 처럼 할지 유무 - 기본값 false(=a n 사용)
                isUsing_aX = resultValue;
            }
        }
        
        static readonly Dictionary<string, string> FIRST_CONSONANTS = new Dictionary<string, string>(){
            {"ㄱ", "g"},
            {"ㄲ", "gg"},
            {"ㄴ", "n"},
            {"ㄷ", "d"},
            {"ㄸ", "dd"},
            {"ㄹ", "r"},
            {"ㅁ", "m"},
            {"ㅂ", "b"},
            {"ㅃ", "bb"},
            {"ㅅ", "s"},
            {"ㅆ", "ss"},
            {"ㅇ", ""},
            {"ㅈ", "j"},
            {"ㅉ", "jj"},
            {"ㅊ", "ch"},
            {"ㅋ", "k"},
            {"ㅌ", "t"},
            {"ㅍ", "p"},
            {"ㅎ", "h"},
            {"null", ""} // 뒤 글자가 없을 때를 대비
            };
        
        static readonly Dictionary<string, string[]> MIDDLE_VOWELS = new Dictionary<string, string[]>(){
            {"ㅏ", new string[3]{"a", "", "a"}},
            {"ㅐ", new string[3]{"e", "", "e"}},
            {"ㅑ", new string[3]{"ya", "y", "a"}},
            {"ㅒ", new string[3]{"ye", "y", "e"}},
            {"ㅓ", new string[3]{"eo", "", "eo"}},
            {"ㅔ", new string[3]{"e", "", "e"}},
            {"ㅕ", new string[3]{"yeo", "y", "eo"}},
            {"ㅖ", new string[3]{"ye", "y", "e"}},
            {"ㅗ", new string[3]{"o", "", "o"}},
            {"ㅘ", new string[3]{"wa", "w", "a"}},
            {"ㅙ", new string[3]{"we", "w", "e"}},
            {"ㅚ", new string[3]{"we", "w", "e"}},
            {"ㅛ", new string[3]{"yo", "y", "o"}},
            {"ㅜ", new string[3]{"u", "", "u"}},
            {"ㅝ", new string[3]{"weo", "w", "eo"}},
            {"ㅞ", new string[3]{"we", "w", "e"}},
            {"ㅟ", new string[3]{"wi", "w", "i"}},
            {"ㅠ", new string[3]{"yu", "y", "u"}},
            {"ㅡ", new string[3]{"eu", "", "eu"}},
            {"ㅢ", new string[3]{"eui", "eu", "i"}}, // ㅢ는 ㅣ로 발음
            {"ㅣ", new string[3]{"i", "", "i"}},
            {"null", new string[3]{"", "", ""}} // 뒤 글자가 없을 때를 대비
            };
        static readonly Dictionary<string, string[]> LAST_CONSONANTS = new Dictionary<string, string[]>(){
             //ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ
            {"ㄱ", new string[]{"k", ""}},
            {"ㄲ", new string[]{"k", ""}},
            {"ㄳ", new string[]{"k", ""}},
            {"ㄴ", new string[]{"n", "2"}},
            {"ㄵ", new string[]{"n", "2"}},
            {"ㄶ", new string[]{"n", "2"}},
            {"ㄷ", new string[]{"t", "1"}},
            {"ㄹ", new string[]{"l", "4"}},
            {"ㄺ", new string[]{"k", ""}},
            {"ㄻ", new string[]{"m", "1"}},
            {"ㄼ", new string[]{"l", "4"}},
            {"ㄽ", new string[]{"l", "4"}},
            {"ㄾ", new string[]{"l", "4"}},
            {"ㄿ", new string[]{"p", "1"}},
            {"ㅀ", new string[]{"l", "4"}},
            {"ㅁ", new string[]{"m", "1"}},
            {"ㅂ", new string[]{"p", "1"}},
            {"ㅄ", new string[]{"p", "1"}},
            {"ㅅ", new string[]{"t", "1"}},
            {"ㅆ", new string[]{"t", "1"}},
            {"ㅇ", new string[]{"ng", "3"}},
            {"ㅈ", new string[]{"t", "1"}},
            {"ㅊ", new string[]{"t", "1"}},
            {"ㅋ", new string[]{"k", ""}},
            {"ㅌ", new string[]{"t", "1"}},
            {"ㅍ", new string[]{"p", "1"}},
            {"ㅎ", new string[]{"t", "1"}},
            {" ", new string[]{""}}, // no batchim
            {"null", new string[]{"", ""}} // 뒤 글자가 없을 때를 대비
            };
        
        private Result ConvertForCV(Note[] notes, string[] prevLyric, string[] thisLyric, string[] nextLyric) {
            string thisMidVowelHead;
            string thisMidVowelTail;

            int totalDuration = notes.Sum(n => n.duration);
            Note note = notes[0];
            bool isItNeedsFrontCV;
            bool isRelaxedVC;
            isItNeedsFrontCV = prevLyric[0] == "null" || prevLyric[1] == "null" || (prevLyric[2] != "null" && HARD_BATCHIMS.Contains(prevLyric[2]) && prevLyric[2] != "ㅁ");
            isRelaxedVC = nextLyric[0] == "null" || nextLyric[1] == "null" || ((thisLyric[2] == nextLyric[0]) && (KoreanPhonemizerUtil.nasalSounds.ContainsKey(thisLyric[2]) || thisLyric[2] == "ㄹ"));

            if (thisLyric.All(part => part == null)) {
                return GenerateResult(FindInOto(note.lyric, note));
            }
            else if (thisLyric[1] == "ㅢ") {
                if (isUsing_i) {
                    thisMidVowelHead = $"{MIDDLE_VOWELS["ㅣ"][1]}";
                    thisMidVowelTail = $"{MIDDLE_VOWELS["ㅣ"][2]}";
                }
                else {
                    thisMidVowelHead = $"{MIDDLE_VOWELS["ㅢ"][1]}";
                    thisMidVowelTail = $"{MIDDLE_VOWELS["ㅢ"][2]}";
                }
            }
            else {
                thisMidVowelHead = $"{MIDDLE_VOWELS[thisLyric[1]][1]}";
                thisMidVowelTail = $"{MIDDLE_VOWELS[thisLyric[1]][2]}";
            }
            
            string CV = $"{FIRST_CONSONANTS[thisLyric[0]]}{thisMidVowelHead}{thisMidVowelTail}"; 
            string frontCV;
            string batchim;
            
            if (isRentan) {
                frontCV = $"- {CV}";
                if (FindInOto(frontCV, note, true) == null) {
                    frontCV = $"-{CV}";
                    if (FindInOto(frontCV, note, true) == null) {
                        frontCV = CV;
                    }
                }
            }
            else {
                frontCV = CV;
            }
        
            if (thisLyric[2] == " ") { // no batchim
                if (isItNeedsFrontCV){
                    return GenerateResult(FindInOto(frontCV, note));
                }
                return GenerateResult(FindInOto(CV, note));
            }
            
            if (isUsing_aX) {
                batchim = $"{thisMidVowelTail}{LAST_CONSONANTS[thisLyric[2]][0]}";
            }
            else {
                batchim = $"{thisMidVowelTail} {LAST_CONSONANTS[thisLyric[2]][0]}";
            }
            
            if (thisLyric[2] == "ㅁ" || ! HARD_BATCHIMS.Contains(thisLyric[2])) { // batchim ㅁ + ㄴ ㄹ ㅇ
                if (isItNeedsFrontCV){
                    return isRelaxedVC ? 
                    GenerateResult(FindInOto(frontCV, note), FindInOto(batchim, note), totalDuration, 120, 8)
                    : GenerateResult(FindInOto(frontCV, note), FindInOto(batchim, note), "", totalDuration, 120, 3, 5);
                }
                return isRelaxedVC ? 
                GenerateResult(FindInOto(CV, note), FindInOto(batchim, note), totalDuration, 120, 8)
                : GenerateResult(FindInOto(CV, note), FindInOto(batchim, note), "", totalDuration, 120, 3, 5);
            }
            else {
                if (isItNeedsFrontCV){
                    return isRelaxedVC ? 
                    GenerateResult(FindInOto(frontCV, note), FindInOto(batchim, note), totalDuration, 120, 8)
                    : GenerateResult(FindInOto(frontCV, note), FindInOto(batchim, note), totalDuration, 120, 5);
                }
                return isRelaxedVC ? 
                GenerateResult(FindInOto(CV, note), FindInOto(batchim, note), totalDuration, 120, 8)
                : GenerateResult(FindInOto(CV, note), FindInOto(batchim, note), totalDuration, 120, 5);
            }
            
        }

        private string? FindInOto(String phoneme, Note note, bool nullIfNotFound=false){
            return BaseKoreanPhonemizer.FindInOto(singer, phoneme, note, nullIfNotFound);
        }


        public override Result ConvertPhonemes(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            Note note = notes[0];

            Hashtable lyrics = KoreanPhonemizerUtil.Variate(prevNeighbour, note, nextNeighbour);
            string[] prevLyric = new string[]{ // "ㄴ", "ㅑ", "ㅇ"
                (string)lyrics[0], 
                (string)lyrics[1], 
                (string)lyrics[2]
                };
            string[] thisLyric = new string[]{ // "ㄴ", "ㅑ", "ㅇ"
                (string)lyrics[3], 
                (string)lyrics[4], 
                (string)lyrics[5]
                };
            string[] nextLyric = new string[]{ // "ㄴ", "ㅑ", "ㅇ"
                (string)lyrics[6], 
                (string)lyrics[7], 
                (string)lyrics[8]
                };

            if (thisLyric[0] == "null") { 
                return GenerateResult(FindInOto(notes[0].lyric, notes[0]));
            }
            
            return ConvertForCV(notes, prevLyric, thisLyric, nextLyric);

        }
        

        public override Result GenerateEndSound(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            Note note = notes[0];
            if (prevNeighbour == null) {
                return GenerateResult(FindInOto(note.lyric, note));
            }

            Note prevNeighbour_ = (Note)prevNeighbour;
            Hashtable lyrics = KoreanPhonemizerUtil.Separate(prevNeighbour_.lyric);

            string[] prevLyric = new string[]{ // "ㄴ", "ㅑ", "ㅇ"
                (string)lyrics[0], 
                (string)lyrics[1], 
                (string)lyrics[2]
                };

            string soundBeforeEndSound = prevLyric[2] == " " ? prevLyric[1] : prevLyric[2];
            string endSound = note.lyric;
            string prevMidVowel;

            

            if (prevLyric[1] == "ㅢ") {
                if (isUsing_i) {
                    prevMidVowel = $"{MIDDLE_VOWELS["ㅣ"][0]}";
                }
                else {
                    prevMidVowel = $"{MIDDLE_VOWELS["ㅢ"][0]}";
                }
            }
            else{
                prevMidVowel = MIDDLE_VOWELS.ContainsKey(soundBeforeEndSound) ? MIDDLE_VOWELS[soundBeforeEndSound][2] : LAST_CONSONANTS[soundBeforeEndSound][0];
            }
            
            if (FindInOto($"{prevMidVowel} {endSound}", note, true) == null) {
                if (FindInOto($"{prevMidVowel}{endSound}", note, true) == null) {
                    return GenerateResult(FindInOto($"{endSound}", note));
                }
                return GenerateResult(FindInOto($"{prevMidVowel}{endSound}", note, true));
            }
            return GenerateResult(FindInOto($"{prevMidVowel} {endSound}", note));            
        }
    }
}