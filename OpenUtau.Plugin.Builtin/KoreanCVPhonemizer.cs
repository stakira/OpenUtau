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

            koreanCVIniSetting = new KoreanCVIniSetting();
            koreanCVIniSetting.Initialize(singer, "ko-CV.ini");

            isUsingShi = koreanCVIniSetting.IsUsingShi();
            isUsing_aX = koreanCVIniSetting.IsUsing_aX();
            isUsing_i = koreanCVIniSetting.IsUsing_i();
            isRentan = koreanCVIniSetting.IsRentan();
        }




        private class KoreanCVIniSetting : BaseIniManager{
            protected override void IniSetUp(IniFile iniFile) {
                // ko-CV.ini
                SetOrReadThisValue("CV", "Use rentan", false); // 연단음 사용 유무 - 기본값 false
                SetOrReadThisValue("CV", "Use 'shi' for '시'(otherwise 'si')", false); // 시를 [shi]로 표기할 지 유무 - 기본값 false
                SetOrReadThisValue("CV", "Use 'i' for '의'(otherwise 'eui')", false); // 의를 [i]로 표기할 지 유무 - 기본값 false
                SetOrReadThisValue("BATCHIM", "Use 'aX' instead of 'a X'", false); // 받침 표기를 a n 처럼 할 지 an 처럼 할지 유무 - 기본값 false(=a n 사용)
            }

            public bool IsRentan() {
                bool isRentan = iniFile["CV"]["Use rentan"].ToBool();
                return isRentan;
            }

            public bool IsUsingShi() {
                bool isUsingShi = iniFile["CV"]["Use 'shi' for '시'(otherwise 'si')"].ToBool();
                return isUsingShi;
            }

            public bool IsUsing_aX() {
                bool isUsing_aX = iniFile["BATCHIM"]["Use 'aX' instead of 'a X'"].ToBool();
                return isUsing_aX;
            }

            public bool IsUsing_i() {
                bool isUsing_i = iniFile["CV"]["Use 'i' for '의'(otherwise 'eui')"].ToBool();
                return isUsing_i;
            }

        }
        private class KOCV {
            /// <summary>
            /// First Consonant's type.
            /// </summary>
            public enum ConsonantType{ 
                /// <summary>예사소리</summary>
                NORMAL, 
                /// <summary>거센소리</summary>
                ASPIRATE, 
                /// <summary>된소리</summary>
                FORTIS, 
                /// <summary>마찰음</summary>
                FRICATIVE, 
                /// <summary>비음</summary>
                NASAL,
                /// <summary>유음</summary>
                LIQUID, 
                /// <summary>ㅎ</summary>
                H,
                /// <summary>자음의 음소값 없음(ㅇ)</summary>
                NOCONSONANT, 
                /// <summary>음소 자체가 없음</summary>
                PHONEME_IS_NULL
            }

            /// <summary>
            /// Last Consonant's type.
            /// </summary>
            public enum BatchimType{ 
                /// <summary>예사소리 받침</summary>
                NORMAL_END, 
                /// <summary>비음 받침</summary>
                NASAL_END,
                /// <summary>유음 받침</summary>
                LIQUID_END, 
                /// <summary>ㅇ받침</summary>
                NG_END, 
                /// <summary>ㅎ받침</summary>
                H_END,
                /// <summary>받침이 없음</summary>
                NO_END,
                /// <summary>음소 자체가 없음</summary>
                PHONEME_IS_NULL
            }
            

            /// <summary>
            /// CBNN phoneme table of first consonants. (key "null" is for Handling empty string)
            /// </summary>
            static readonly Dictionary<string, string[]> FIRST_CONSONANTS = new Dictionary<string, string[]>(){
                {"ㄱ", new string[2]{"g", ConsonantType.NORMAL.ToString()}},
                {"ㄲ", new string[2]{"gg", ConsonantType.FORTIS.ToString()}},
                {"ㄴ", new string[2]{"n", ConsonantType.NASAL.ToString()}},
                {"ㄷ", new string[2]{"d", ConsonantType.NORMAL.ToString()}},
                {"ㄸ", new string[2]{"dd", ConsonantType.FORTIS.ToString()}},
                {"ㄹ", new string[2]{"r", ConsonantType.LIQUID.ToString()}},
                {"ㅁ", new string[2]{"m", ConsonantType.NASAL.ToString()}},
                {"ㅂ", new string[2]{"b", ConsonantType.NORMAL.ToString()}},
                {"ㅃ", new string[2]{"bb", ConsonantType.FORTIS.ToString()}},
                {"ㅅ", new string[2]{"s", ConsonantType.NORMAL.ToString()}},
                {"ㅆ", new string[2]{"ss", ConsonantType.FRICATIVE.ToString()}},
                {"ㅇ", new string[2]{"", ConsonantType.NOCONSONANT.ToString()}},
                {"ㅈ", new string[2]{"j", ConsonantType.NORMAL.ToString()}},
                {"ㅉ", new string[2]{"jj", ConsonantType.FORTIS.ToString()}},
                {"ㅊ", new string[2]{"ch", ConsonantType.ASPIRATE.ToString()}},
                {"ㅋ", new string[2]{"k", ConsonantType.ASPIRATE.ToString()}},
                {"ㅌ", new string[2]{"t", ConsonantType.ASPIRATE.ToString()}},
                {"ㅍ", new string[2]{"p", ConsonantType.ASPIRATE.ToString()}},
                {"ㅎ", new string[2]{"h", ConsonantType.H.ToString()}},
                {" ", new string[2]{"", ConsonantType.NOCONSONANT.ToString()}},
                {"null", new string[2]{"", ConsonantType.PHONEME_IS_NULL.ToString()}} // 뒤 글자가 없을 때를 대비
                };

            /// <summary>
            /// CBNN phoneme table of middle vowels (key "null" is for Handling empty string)
            /// </summary>
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
                {"ㅢ", new string[3]{"i", "", "i"}}, // ㅢ는 ㅣ로 발음
                {"ㅣ", new string[3]{"i", "", "i"}},
                {" ", new string[3]{"", "", ""}},
                {"null", new string[3]{"", "", ""}} // 뒤 글자가 없을 때를 대비
                };

            /// <summary>
            /// CBNN phoneme table of last consonants. (key "null" is for Handling empty string)
            /// </summary>
            static readonly Dictionary<string, string[]> LAST_CONSONANTS = new Dictionary<string, string[]>(){
                 //ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ
                {"ㄱ", new string[3]{"k", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄲ", new string[3]{"k", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄳ", new string[3]{"k", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄴ", new string[3]{"n", "2", BatchimType.NASAL_END.ToString()}},
                {"ㄵ", new string[3]{"n", "2", BatchimType.NASAL_END.ToString()}},
                {"ㄶ", new string[3]{"n", "2", BatchimType.NASAL_END.ToString()}},
                {"ㄷ", new string[3]{"t", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㄹ", new string[3]{"l", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄺ", new string[3]{"k", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄻ", new string[3]{"m", "1", BatchimType.NASAL_END.ToString()}},
                {"ㄼ", new string[3]{"l", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄽ", new string[3]{"l", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄾ", new string[3]{"l", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄿ", new string[3]{"p", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅀ", new string[3]{"l", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㅁ", new string[3]{"m", "1", BatchimType.NASAL_END.ToString()}},
                {"ㅂ", new string[3]{"p", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅄ", new string[3]{"p", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅅ", new string[3]{"t", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅆ", new string[3]{"t", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅇ", new string[3]{"ng", "3", BatchimType.NG_END.ToString()}},
                {"ㅈ", new string[3]{"t", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅊ", new string[3]{"t", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅋ", new string[3]{"k", "", BatchimType.NORMAL_END.ToString()}},
                {"ㅌ", new string[3]{"t", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅍ", new string[3]{"p", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅎ", new string[3]{"t", "1", BatchimType.H_END.ToString()}},
                {" ", new string[3]{"", "", BatchimType.NO_END.ToString()}},
                {"null", new string[3]{"", "", BatchimType.PHONEME_IS_NULL.ToString()}} // 뒤 글자가 없을 때를 대비
                };


            private string thisFirstConsonant, thisVowelHead, thisVowelTail, thisLastConsonant;
            private string nextFirstConsonant, nextVowelHead, nextLastConsonant;
            private string prevLastConsonant, prevVowelHead; 

            public string VV, CV, cVC, VC, CV_noSuffix; 
            public string frontCV, frontCV_noSuffix; // - {CV}
            public string? endSoundVowel, endSoundLastConsonant; // ng -
            public int cVCLength, vcLength, vcLengthShort; // 받침 종류에 따라 길이가 달라짐 / 이웃이 있을 때에만 사용
            private int totalDuration;

            private ConsonantType thisFirstConsonantType, prevFirstConsonantType, nextFirstConsonantType;
            private BatchimType thisLastConsonantType, prevLastConsonantType, nextLastConsonantType;
            private Note note;
            private USinger singer;

            public KOCV(USinger singer, Note note, int totalDuration, int vcLength = 120, int vcLengthShort = 90) {
                this.totalDuration = totalDuration;
                this.vcLength = vcLength;
                this.vcLengthShort = vcLengthShort;
                this.singer = singer;
                this.note = note;
            }

            private string? FindInOto(String phoneme, Note note, bool nullIfNotFound=false){
                return BaseKoreanPhonemizer.FindInOto(singer, phoneme, note, nullIfNotFound);
            }
            
            private Hashtable ConvertForCV(Hashtable separated, bool[] setting) {
                // Hangeul.Separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CV식으로 변경
                Hashtable cvPhonemes; 
                bool isUsing_aX, isUsing_i, isRentan;

                isUsing_aX = setting[1];
                isUsing_i = setting[2];
                isRentan = setting[3];

                cvPhonemes = new Hashtable() {
                    [0] = FIRST_CONSONANTS[(string)separated[0]][0],
                    [1] = MIDDLE_VOWELS[(string)separated[1]][1],
                    [2] = MIDDLE_VOWELS[(string)separated[1]][2],
                    [3] = LAST_CONSONANTS[(string)separated[2]][0],

                    [4] = FIRST_CONSONANTS[(string)separated[3]][0],
                    [5] = MIDDLE_VOWELS[(string)separated[4]][1],
                    [6] = MIDDLE_VOWELS[(string)separated[4]][2],
                    [7] = LAST_CONSONANTS[(string)separated[5]][0],

                    [8] = FIRST_CONSONANTS[(string)separated[6]][0],
                    [9] = MIDDLE_VOWELS[(string)separated[7]][1],
                    [10] = MIDDLE_VOWELS[(string)separated[7]][2],
                    [11] = LAST_CONSONANTS[(string)separated[8]][0]
                };

                if (setting[0] && cvPhonemes[4].Equals("s") && cvPhonemes[6].Equals("i")) {
                    // [isUsingShi], isUsing_aX, isUsing_i, isRentan
                    cvPhonemes[4] = "sh"; // si to shi
                } 
                else if ((!setting[2]) && separated[4].Equals("ㅢ")) {
                    // isUsingShi, isUsing_aX, [isUsing_i], isRentan
                    cvPhonemes[5] = "eu"; // to eui
                }

                // ex 냥냐 (nya3 ang nya)
                thisFirstConsonant = (string)cvPhonemes[4]; // n
                thisVowelHead = (string)cvPhonemes[5]; // y
                thisVowelTail = (string)cvPhonemes[6]; // a
                thisLastConsonant = (string)cvPhonemes[7]; // ng

                nextVowelHead = (string)cvPhonemes[9]; // 다음 노트 모음의 머리 음소 / y

                prevLastConsonant = (string)cvPhonemes[3]; // VV음소 만들 때 쓰는 이전 노트의 받침 음소

                
                CV = $"{thisFirstConsonant}{thisVowelHead}{thisVowelTail}"; // nya

                endSoundVowel = FindInOto($"{thisVowelTail} -", note, true);
                endSoundLastConsonant = FindInOto($"{thisLastConsonant} -", note, true);

            
                if (thisLastConsonant.Equals("l")) {
                // ㄹ받침
                    cVCLength = totalDuration / 2;
                } 
                else if (thisLastConsonant.Equals("n")) {
                    // ㄴ받침
                    cVCLength = 170;
                } 
                else if (thisLastConsonant.Equals("ng")) {
                    // ㅇ받침
                    cVCLength = 230;
                } 
                else if (thisLastConsonant.Equals("m")) {
                    // ㅁ받침
                    cVCLength = 280;
                } 
                else if (thisLastConsonant.Equals("k")) {
                    // ㄱ받침
                    cVCLength = totalDuration / 2;
                } 
                else if (thisLastConsonant.Equals("t")) {
                    // ㄷ받침
                    cVCLength = totalDuration / 2;
                } 
                else if (thisLastConsonant.Equals("p")) {
                    cVCLength = totalDuration / 2;
                } 
                else {
                    // 나머지
                    cVCLength = totalDuration / 3;
                }

                if (thisVowelTail.Equals("u")) {
                    cVCLength += 50; // 모음이 u일때엔 cVC의 발음 길이가 더 길어짐
                    vcLength += 50;
                }

                cVC = isUsing_aX ? $"{thisVowelTail}{thisLastConsonant}" : $"{thisVowelTail} {thisLastConsonant}";

                // ㅢ를 ㅣ로 대체해서 발음하지 않을 때
                CV = (!isUsing_i && singer.TryGetMappedOto($"{CV}", note.tone, out UOto oto)) ? $"{CV}" : $"{thisFirstConsonant}{thisVowelTail}";

                if (isRentan) {
                    frontCV = FindInOto($"- {CV}", note, true);
                    CV = FindInOto($"{CV}", note);
                    // 연단음 / 어두 음소(-) 사용 
                    if (frontCV == null) {
                        frontCV = FindInOto($"-{CV}", note, true);
                        CV = FindInOto($"{CV}", note);

                        if (frontCV == null) {frontCV = CV;} 
                    }
                }

                cVC = FindInOto(cVC, note);

                if (endSoundVowel == null) {endSoundVowel = "";}
                if (endSoundLastConsonant == null) {endSoundLastConsonant = "";}
                if (frontCV == null) {frontCV = CV;}

                return cvPhonemes;
            }

            private Hashtable ConvertForCVSingle(Hashtable separated, bool[] setting) {
                // Hangeul.Separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CV식으로 변경
                // 한 글자짜리 노트 받아서 반환함 (숨소리 생성용)
                Hashtable separatedConvertedForCV;

                separatedConvertedForCV = new Hashtable() {
                    [0] = FIRST_CONSONANTS[(string)separated[0]][0], // n
                    [1] = MIDDLE_VOWELS[(string)separated[1]][1], // y
                    [2] = MIDDLE_VOWELS[(string)separated[1]][2], // a
                    [3] = LAST_CONSONANTS[(string)separated[2]][0], // ng

                };

                if ((setting[0]) && (separatedConvertedForCV[0].Equals("s")) && (separatedConvertedForCV[2].Equals("i"))) {
                    // [isUsingShi], isUsing_aX, isUsing_i, isRentan
                    separatedConvertedForCV[0] = "sh"; // si to shi
                } else if ((!setting[2]) && (separated[1].Equals("ㅢ"))) {
                    // isUsingShi, isUsing_aX, [isUsing_i], isRentan
                    separatedConvertedForCV[2] = "eu"; // to eui
                }

                return separatedConvertedForCV;
            }

            public Hashtable ConvertForCV(Note? prevNeighbour, Note note, Note? nextNeighbour, bool[] setting) {
                // Hangeul.Separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CV식으로 변경
                Hashtable variated = KoreanPhonemizerUtil.Variate(prevNeighbour, note, nextNeighbour);

                thisFirstConsonantType = Enum.Parse<ConsonantType>(FIRST_CONSONANTS[(string)variated[3]][1]);
                thisLastConsonantType = Enum.Parse<BatchimType>(LAST_CONSONANTS[(string)variated[5]][2]);
                prevFirstConsonantType = Enum.Parse<ConsonantType>(FIRST_CONSONANTS[(string)variated[0]][1]);
                prevLastConsonantType = Enum.Parse<BatchimType>(LAST_CONSONANTS[(string)variated[2]][2]);
                nextFirstConsonantType = Enum.Parse<ConsonantType>(FIRST_CONSONANTS[(string)variated[6]][1]);
                nextLastConsonantType = Enum.Parse<BatchimType>(LAST_CONSONANTS[(string)variated[8]][2]);
                
                return ConvertForCV(variated, setting);
            }

            public Hashtable ConvertForCV(Note? prevNeighbour, bool[] setting) {
                return ConvertForCVSingle(KoreanPhonemizerUtil.Variate(prevNeighbour?.lyric), setting);
            }

            /// <summary>
            /// true when current Target has Batchim, otherwise false.
            /// </summary>
            public bool ThisHasBatchim(){
                return (!thisLastConsonant.Equals("")) ? true : false;
            }

            /// <summary>
            /// true when previous Target has Batchim, otherwise false.
            /// </summary>
            public bool PrevHasBatchim(){
                return (!prevLastConsonant.Equals("")) ? true : false;
            }

            /// <summary>
            /// true when next Target has Batchim, otherwise false.
            /// </summary>
            public bool NextHasBatchim(){
                return (!nextLastConsonant.Equals("")) ? true : false;
            }
            /// <summary>
            /// true when current FirstConsonant is Normal(ㄱ, ㄷ, ㅂ, ㅅ, ㅈ), otherwise false.
            /// </summary>
            public bool ThisFirstConsonantIsNormal(){
                return (thisFirstConsonantType == ConsonantType.NORMAL) ? true : false;
            }

            /// <summary>
            /// true when next FirstConsonant is Normal(ㄱ, ㄷ, ㅂ, ㅅ, ㅈ), otherwise false.
            /// </summary>
            public bool NextFirstConsonantIsNormal(){
                return (nextFirstConsonantType == ConsonantType.NORMAL) ? true : false;
            }

            /// <summary>
            /// true when previous FirstConsonant is Normal(ㄱ, ㄷ, ㅂ, ㅅ, ㅈ), otherwise false.
            /// </summary>
            public bool PrevFirstConsonantIsNormal(){
                return (prevFirstConsonantType == ConsonantType.NORMAL) ? true : false;
            }

            /// <summary>
            /// true when current FirstConsonant is Fortis(ㄲ, ㄸ, ㅃ, ㅉ), otherwise false.
            /// </summary>
            public bool ThisFirstConsonantIsFortis(){
                return (thisFirstConsonantType == ConsonantType.FORTIS) ? true : false;
            }

            /// <summary>
            /// true when next FirstConsonant is Fortis(ㄲ, ㄸ, ㅃ, ㅉ), otherwise false.
            /// </summary>
            public bool NextFirstConsonantIsFortis(){
                return (nextFirstConsonantType == ConsonantType.FORTIS) ? true : false;
            }

            /// <summary>
            /// true when previous FirstConsonant is Fortis(ㄲ, ㄸ, ㅃ, ㅉ), otherwise false.
            /// </summary>
            public bool PrevFirstConsonantIsFortis(){
                return (prevFirstConsonantType == ConsonantType.FORTIS) ? true : false;
            }

            /// <summary>
            /// true when current FirstConsonant is Aspirate(ㅋ, ㅌ, ㅍ, ㅊ), otherwise false.
            /// </summary>
            public bool ThisFirstConsonantIsAspirate(){
                return (thisFirstConsonantType == ConsonantType.ASPIRATE) ? true : false;
            }
            
            /// <summary>
            /// true when next FirstConsonant is Aspirate(ㅋ, ㅌ, ㅍ, ㅊ), otherwise false.
            /// </summary>
            public bool NextFirstConsonantIsAspirate(){
                return (nextFirstConsonantType == ConsonantType.ASPIRATE) ? true : false;
            }

            /// <summary>
            /// true when previous FirstConsonant is Aspirate(ㅋ, ㅌ, ㅍ, ㅊ), otherwise false.
            /// </summary>
            public bool PrevFirstConsonantIsAspirate(){
                return (prevFirstConsonantType == ConsonantType.ASPIRATE) ? true : false;
            }
            /// <summary>
            /// true when current FirstConsonant is Fricative(ㅆ), otherwise false.
            /// </summary>
            public bool ThisFirstConsonantIsFricative(){
                return (thisFirstConsonantType == ConsonantType.FRICATIVE) ? true : false;
            }

            /// <summary>
            /// true when next FirstConsonant is Fricative(ㅆ), otherwise false.
            /// </summary>
            public bool NextFirstConsonantIsFricative(){
                return (nextFirstConsonantType == ConsonantType.FRICATIVE) ? true : false;
            }

            /// <summary>
            /// true when previous FirstConsonant is Fricative(ㅆ), otherwise false.
            /// </summary>
            public bool PrevFirstConsonantIsFricative(){
                return (prevFirstConsonantType == ConsonantType.FRICATIVE) ? true : false;
            }

            /// <summary>
            /// true when current FirstConsonant is ㅇ, otherwise false.
            /// </summary>
            public bool ThisFirstConsonantIsNone(){
                return (thisFirstConsonantType == ConsonantType.NOCONSONANT) ? true : false;
            }

            /// <summary>
            /// true when next FirstConsonant is ㅇ, otherwise false.
            /// </summary>
            public bool NextFirstConsonantIsNone(){
                return (nextFirstConsonantType == ConsonantType.NOCONSONANT) ? true : false;
            }

            /// <summary>
            /// true when previous FirstConsonant is ㅇ, otherwise false.
            /// </summary>
            public bool PrevFirstConsonantIsNone(){
                return (prevFirstConsonantType == ConsonantType.NOCONSONANT) ? true : false;
            }

            /// <summary>
            /// true when current FirstConsonant is Nasal(ㄴ, ㅇ, ㅁ), otherwise false.
            /// </summary>
            public bool ThisFirstConsonantIsNasal(){
                return (thisFirstConsonantType == ConsonantType.NASAL) ? true : false;
            }

            /// <summary>
            /// true when next FirstConsonant is Nasal(ㄴ, ㅇ, ㅁ), otherwise false.
            /// </summary>
            public bool NextFirstConsonantIsNasal(){
                return (nextFirstConsonantType == ConsonantType.NASAL) ? true : false;
            }

            /// <summary>
            /// true when previous FirstConsonant is Nasal(ㄴ, ㅇ, ㅁ), otherwise false.
            /// </summary>
            public bool PrevFirstConsonantIsNasal(){
                return (prevFirstConsonantType == ConsonantType.NASAL) ? true : false;
            }

            /// <summary>
            /// true when current FirstConsonant is Liquid(ㄹ), otherwise false.
            /// </summary>
            public bool ThisFirstConsonantIsLiquid(){
                return (thisFirstConsonantType == ConsonantType.LIQUID) ? true : false;
            }

            /// <summary>
            /// true when next FirstConsonant is Liquid(ㄹ), otherwise false.
            /// </summary>
            public bool NextFirstConsonantIsLiquid(){
                return (nextFirstConsonantType == ConsonantType.LIQUID) ? true : false;
            }

            /// <summary>
            /// true when previous FirstConsonant is Liquid(ㄹ), otherwise false.
            /// </summary>
            public bool PrevFirstConsonantIsLiquid(){
                return (prevFirstConsonantType == ConsonantType.LIQUID) ? true : false;
            }

            /// <summary>
            /// true when current FirstConsonant is ㅎ, otherwise false.
            /// </summary>
            public bool ThisFirstConsonantIsH(){
                return (thisFirstConsonantType == ConsonantType.H) ? true : false;
            }

            /// <summary>
            /// true when next FirstConsonant is ㅎ, otherwise false.
            /// </summary>
            public bool NextFirstConsonantIsH(){
                return (thisFirstConsonantType == ConsonantType.H) ? true : false;
            }

            /// <summary>
            /// true when previous FirstConsonant is ㅎ, otherwise false.
            /// </summary>
            public bool PrevFirstConsonantIsH(){
                return (prevFirstConsonantType == ConsonantType.H) ? true : false;
            }

            /// <summary>
            /// true when current Target is Plain vowel(ㅏ, ㅣ, ㅜ, ㅔ, ㅗ, ㅡ, ㅓ), otherwise false.
            /// </summary>
            public bool ThisIsPlainVowel(){
                return (ThisFirstConsonantIsNone() && thisVowelHead.Equals("")) ? true : false;
            }

            /// <summary>
            /// true when next Target is Plain vowel(ㅏ, ㅣ, ㅜ, ㅔ, ㅗ, ㅡ, ㅓ), otherwise false.
            /// </summary>
            public bool NextIsPlainVowel(){
                return (NextFirstConsonantIsNone() && nextVowelHead.Equals("")) ? true : false;
            }

            /// <summary>
            /// true when previous Target is Plain vowel(ㅏ, ㅣ, ㅜ, ㅔ, ㅗ, ㅡ, ㅓ), otherwise false.
            /// </summary>
            public bool PrevIsPlainVowel(){
                return (PrevFirstConsonantIsNone() && prevVowelHead.Equals("")) ? true : false;
            }

            /// <summary>
            /// true when current LastConsonant is Nasal(ㄴ, ㅇ, ㅁ), otherwise false.
            /// </summary>
            public bool ThisLastConsonantIsNasal(){
                return (thisLastConsonantType == BatchimType.NASAL_END || thisLastConsonantType == BatchimType.NG_END) ? true : false;
            }

            /// <summary>
            /// true when next LastConsonant is Nasal(ㄴ, ㅇ, ㅁ), otherwise false.
            /// </summary>
            public bool NextLastConsonantIsNasal(){
                return (nextLastConsonantType == BatchimType.NASAL_END || nextLastConsonantType == BatchimType.NG_END) ? true : false;
            }

            /// <summary>
            /// true when previous LastConsonant is Nasal(ㄴ, ㅇ, ㅁ), otherwise false.
            /// </summary>
            public bool PrevLastConsonantIsNasal(){
                return (prevLastConsonantType == BatchimType.NASAL_END || prevLastConsonantType == BatchimType.NG_END) ? true : false;
            }

            /// <summary>
            /// true when current LastConsonant is Liquid(ㄹ), otherwise false.
            /// </summary>
            public bool ThisLastConsonantIsLiquid(){
                return (thisLastConsonantType == BatchimType.LIQUID_END) ? true : false;
            }

            /// <summary>
            /// true when next LastConsonant is Liquid(ㄹ), otherwise false.
            /// </summary>
            public bool NextLastConsonantIsLiquid(){
                return (nextLastConsonantType == BatchimType.LIQUID_END) ? true : false;
            }

            /// <summary>
            /// true when previous LastConsonant is Liquid.(ㄹ), otherwise false.
            /// </summary>
            public bool PrevLastConsonantIsLiquid(){
                return (prevLastConsonantType == BatchimType.LIQUID_END) ? true : false;
            }

            /// <summary>
            /// true when previous FirstConsonant is Aspirate or Fortis or Fricative (ㅋ, ㅌ, ㅍ, ㅊ, ㄲ, ㄸ, ㅃ, ㅆ, ㅉ), otherwise false.
            /// </summary>
            public bool PrevFirstConsonantNeedsPause(){
                return (PrevFirstConsonantIsAspirate() || PrevFirstConsonantIsFortis() || PrevFirstConsonantIsFricative());
            }

            /// <summary>
            /// true when current FirstConsonant is Aspirate or Fortis or Fricative (ㅋ, ㅌ, ㅍ, ㅊ, ㄲ, ㄸ, ㅃ, ㅆ, ㅉ), otherwise false.
            /// </summary>
            public bool ThisFirstConsonantNeedsPause(){
                return (ThisFirstConsonantIsAspirate() || ThisFirstConsonantIsFortis() || ThisFirstConsonantIsFricative());
            }

            /// <summary>
            /// true when next FirstConsonant is Aspirate or Fortis or Fricative (ㅋ, ㅌ, ㅍ, ㅊ, ㄲ, ㄸ, ㅃ, ㅆ, ㅉ), otherwise false.
            /// </summary>
            public bool NextFirstConsonantNeedsPause(){
                return (NextFirstConsonantIsAspirate() || NextFirstConsonantIsFortis() || NextFirstConsonantIsFricative());
            }

            /// <summary>
            /// true when current LastConsonant is Nasal or Liquid (ㄴ, ㅇ, ㅁ, ㄹ), otherwise false.
            /// </summary>
            public bool ThisLastConsonantIsNasalOrLiquid(){
                return (ThisLastConsonantIsNasal() || ThisLastConsonantIsLiquid());
            }

            /// <summary>
            /// true when next LastConsonant is Nasal or Liquid (ㄴ, ㅇ, ㅁ, ㄹ), otherwise false.
            /// </summary>
            public bool NextLastConsonantIsNasalOrLiquid(){
                return (NextLastConsonantIsNasal() || NextLastConsonantIsLiquid());
            }

            /// <summary>
            /// true when previous LastConsonant is Nasal or Liquid (ㄴ, ㅇ, ㅁ, ㄹ), otherwise false.
            /// </summary>
            public bool PrevLastConsonantIsNasalOrLiquid(){
                return (PrevLastConsonantIsNasal() || PrevLastConsonantIsLiquid());
            }

            /// <summary>
            /// true when current Target needs VV for Vowel Phoneme(Example: a i, u eo...), otherwise false.
            /// </summary>
            public bool ThisVowelNeedsVV(){
                return ((! PrevHasBatchim()) && ThisIsPlainVowel());
            }

            /// <summary>
            /// true when current Target needs CV for Vowel Phoneme(Example: a, ya...), otherwise false.
            /// </summary>
            public bool ThisVowelNeedsCV(){
                return ((ThisFirstConsonantIsNone() && PrevHasBatchim()) || (PrevHasBatchim() && ThisIsPlainVowel()));
            }

            /// <summary>
            /// true when current Target needs frontCV for CV Phoneme(Example: - ka), otherwise false.
            /// </summary>
            public bool ThisNeedsFrontCV(){
                return (PrevHasBatchim() && ThisFirstConsonantNeedsPause());
            }
        }

        public override Result ConvertPhonemes(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            Hashtable cvPhonemes;

            Note note = notes[0];
            string lyric = note.lyric;
            string phoneticHint = note.phoneticHint;

            Note? prevNote = prevNeighbour; // null or Note
            Note thisNote = note;
            Note? nextNote = nextNeighbour; // null or Note

            int totalDuration = notes.Sum(n => n.duration);
            int vcLength = 120;
            int vcLengthShort = 30;

            KOCV cv = new KOCV(singer, thisNote, totalDuration, vcLength, vcLengthShort);

            try{
                // change lyric to CV phonemes, with phoneme variation.
                cvPhonemes = cv.ConvertForCV(prevNote, thisNote, nextNote, 
                        new bool[] {isUsingShi, isUsing_aX, isUsing_i, isRentan});
            
            }
            catch {
                return GenerateResult(lyric);
            }
            

            // return phonemes
            if ((prevNeighbour == null) && (nextNeighbour == null)) { // 이웃이 없음 / 냥
                return (! cv.ThisHasBatchim()) ? GenerateResult(cv.frontCV, cv.endSoundVowel, totalDuration, vcLengthShort, 8) : GenerateResult(cv.frontCV, cv.cVC, totalDuration, cv.cVCLength, 8);
            } 
            
            else if ((prevNeighbour != null) && (nextNeighbour == null)) {
                // 앞에 이웃 있고 뒤에 이웃 없음 / 냥[냥]
                if (! cv.ThisHasBatchim()) { // 둘다 이웃 있고 받침 없음 / 냥[냐]냥
                    return cv.ThisNeedsFrontCV() ? GenerateResult(cv.frontCV) : GenerateResult(cv.CV);
                }
                else{
                    return cv.ThisNeedsFrontCV() ? GenerateResult(cv.frontCV, cv.cVC, totalDuration, cv.vcLength, 8) : GenerateResult(cv.CV, cv.cVC, totalDuration, cv.vcLength, 8);
                }
            }   

            else if ((prevNeighbour == null) && (nextNeighbour != null)) {
                if (KoreanPhonemizerUtil.IsHangeul(nextNeighbour?.lyric)) {// 뒤 글자가 한글임
                    if (! cv.ThisHasBatchim()) { // 앞이웃만 없고 받침 없음 / [냐]냥
                        return cv.NextFirstConsonantNeedsPause() ? GenerateResult(cv.frontCV, "", totalDuration, vcLength) : GenerateResult(cv.frontCV);
                    } 
                    else {
                        return cv.NextFirstConsonantNeedsPause() ? GenerateResult(cv.frontCV, cv.cVC, "", totalDuration, cv.cVCLength, 2, 8) : GenerateResult(cv.frontCV, cv.cVC, totalDuration, cv.vcLength);
                    }
                } 
                else {
                    return (! cv.ThisHasBatchim()) ? GenerateResult(cv.frontCV) : GenerateResult(cv.frontCV, cv.cVC, totalDuration, cv.vcLength);
                }
            } 

            else if ((prevNeighbour != null) && (nextNeighbour != null)) {// 둘다 이웃 있음
                if (KoreanPhonemizerUtil.IsHangeul(nextNeighbour?.lyric)) {// 뒤의 이웃이 한국어임
                    if (! cv.ThisHasBatchim()) { // 둘다 이웃 있고 받침 없음 / 냥[냐]냥
                        if (cv.ThisNeedsFrontCV()) {
                            return cv.NextFirstConsonantNeedsPause() ? GenerateResult(cv.frontCV, "", totalDuration, cv.vcLength, 2) : GenerateResult(cv.frontCV);
                        }
                        else{// 뒤 음소가 파열음 혹은 된소리일 때엔 VC로 공백을 준다 ))
                            return cv.NextFirstConsonantNeedsPause() ? GenerateResult(cv.CV, "", totalDuration, cv.vcLength, 2) : GenerateResult(cv.CV);
                        }   
                    }
                    else{
                        if (cv.ThisNeedsFrontCV()) {
                            return cv.NextFirstConsonantNeedsPause() ? GenerateResult(cv.frontCV, cv.cVC, "", totalDuration, cv.cVCLength, 2, 8) : GenerateResult(cv.frontCV, cv.cVC, totalDuration, cv.cVCLength, 2);
                        }
                        else{// 뒤 음소가 파열음 혹은 된소리일 때엔 VC로 공백을 준다 ))
                            return cv.NextFirstConsonantNeedsPause() ? GenerateResult(cv.CV, cv.cVC, "", totalDuration, cv.cVCLength, 2, 8) : GenerateResult(cv.CV, cv.cVC, totalDuration, cv.cVCLength, 2);
                        }   
                    }
                } 
                else if ((bool)(nextNeighbour?.lyric.Equals("-")) || (bool)(nextNeighbour?.lyric.Equals("R"))) {
                    // 둘다 이웃 있고 뒤에 -가 옴
                    return (! cv.ThisHasBatchim())  ? GenerateResult(cv.CV) : GenerateResult(cv.CV, cv.cVC, totalDuration, cv.cVCLength, 3);
                } 
                else {
                    return (! cv.ThisHasBatchim()) ? GenerateResult(cv.CV) : GenerateResult(cv.CV, cv.cVC, totalDuration, cv.cVCLength, 3);
                }
            } 
            else {
                return GenerateResult(cv.CV);
            }
        }
        

        public override Result GenerateEndSound(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            Hashtable cvPhonemes;

            Note note = notes[0];
            string lyric = note.lyric;
            string phoneticHint = note.phoneticHint;

            Note? prevNote = prevNeighbour; // null or Note
            Note thisNote = note;
            Note? nextNote = nextNeighbour; // null or Note

            int totalDuration = notes.Sum(n => n.duration);

            KOCV cv = new KOCV(singer, thisNote, totalDuration, vcLength, vcLengthShort);
            string phonemeToReturn = lyric; // 아래에서 아무것도 안 걸리면 그냥 가사 반환
            string prevLyric = prevNote?.lyric;

            if (phonemeToReturn.Equals("-")) {
                if (KoreanPhonemizerUtil.IsHangeul(prevLyric)) {
                    cvPhonemes = cv.ConvertForCV(prevNote, 
                                new bool[] { isUsingShi, isUsing_aX, isUsing_i, isRentan }); 

                    string prevVowelTail = (string)cvPhonemes[2]; // V이전 노트의 모음 음소 
                    string prevLastConsonant = (string)cvPhonemes[3]; // 이전 노트의 받침 음소

                    // 앞 노트가 한글
                    if (!prevLastConsonant.Equals("")) {
                        phonemeToReturn = $"{prevLastConsonant} -";
                    } 
                    else if (!prevVowelTail.Equals("")) {
                        phonemeToReturn = $"{prevVowelTail} -";
                    }
                }
                return GenerateResult(phonemeToReturn);
            } 
            else if (phonemeToReturn.Equals("R")) {
                if (KoreanPhonemizerUtil.IsHangeul(prevLyric)) {
                    cvPhonemes = cv.ConvertForCV(prevNote, 
                                new bool[] { isUsingShi, isUsing_aX, isUsing_i, isRentan }); // [isUsingShi], isUsing_aX, isUsing_i, isRentan

                    string prevVowelTail = (string)cvPhonemes[2]; // V이전 노트의 모음 음소 
                    string prevLastConsonant = (string)cvPhonemes[3]; // 이전 노트의 받침 음소

                    // 앞 노트가 한글
                    if (!prevLastConsonant.Equals("")) {
                        phonemeToReturn = $"{prevLastConsonant} R";
                    } else if (!prevVowelTail.Equals("")) {
                        phonemeToReturn = $"{prevVowelTail} R";
                    }

                }
                return GenerateResult(phonemeToReturn);
            } else {
                return GenerateResult(phonemeToReturn);
            }
        }
    }
}