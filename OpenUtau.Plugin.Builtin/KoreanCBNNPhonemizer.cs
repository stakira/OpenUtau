using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    /// Phonemizer for 'KOR CBNN(Combination)' ///
    [Phonemizer("Korean CBNN Phonemizer", "KO CBNN", "EX3", language: "KO")]

    public class KoreanCBNNPhonemizer : BaseKoreanPhonemizer {
        private class CBNN {
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

            private string thisFirstConsonant, thisVowelHead, thisVowelTail, thisSuffix, thisLastConsonant;
            private string nextFirstConsonant, nextVowelHead, nextLastConsonant;
            private string prevVowelTail, prevLastConsonant, prevSuffix, prevVowelHead; 

            public string VV, CV, cVC, VC, CV_noSuffix; 
            public string frontCV, frontCV_noSuffix; // - {CV}
            public string endSoundVowel, endSoundLastConsonant; // ng -
            public int cVCLength, vcLength, vcLengthShort; // 받침 종류에 따라 길이가 달라짐 / 이웃이 있을 때에만 사용
            private int totalDuration;

            private ConsonantType thisFirstConsonantType, prevFirstConsonantType, nextFirstConsonantType;
            private BatchimType thisLastConsonantType, prevLastConsonantType, nextLastConsonantType;
            private Note note;
            private USinger singer;
            public CBNN(USinger singer, Note note, int totalDuration, int vcLength = 120, int vcLengthShort = 90) {
                this.totalDuration = totalDuration;
                this.vcLength = vcLength;
                this.vcLengthShort = vcLengthShort;
                this.singer = singer;
                this.note = note;
            }

            private string? FindInOto(String phoneme, Note note, bool nullIfNotFound=false){
                return BaseKoreanPhonemizer.FindInOto(singer, phoneme, note, nullIfNotFound);
            }

            /// <summary>
            /// Converts result of Hangeul.Variate(Note? prevNeighbour, Note note, Note? nextNeighbour) into CBNN format.
            /// <br/>Hangeul.Variate(Note? prevNeighbour, Note note, Note? nextNeighbour)를 사용한 결과물을 받아 CBNN식으로 변경합니다.
            /// </summary>
            /// <param name="separated">
            /// result of Hangeul.Variate(Note? prevNeighbour, Note note, Note? nextNeighbour).
            /// </param>
            /// <returns>
            /// Returns CBNN formated result. 
            /// </returns>
            private Hashtable ConvertForCBNN(Hashtable separated) {
                // VV 음소를 위해 앞의 노트의 변동된 결과까지 반환한다
                // vc 음소를 위해 뒤의 노트의 변동된 결과까지 반환한다
                Hashtable cbnnPhonemes;

                cbnnPhonemes = new Hashtable() {
                    // first character
                    [0] = FIRST_CONSONANTS[(string)separated[0]][0], //n
                    [1] = MIDDLE_VOWELS[(string)separated[1]][1], // y
                    [2] = MIDDLE_VOWELS[(string)separated[1]][2], // a
                    [3] = LAST_CONSONANTS[(string)separated[2]][1], // 3
                    [4] = LAST_CONSONANTS[(string)separated[2]][0], // ng

                    // second character
                    [5] = FIRST_CONSONANTS[(string)separated[3]][0],
                    [6] = MIDDLE_VOWELS[(string)separated[4]][1],
                    [7] = MIDDLE_VOWELS[(string)separated[4]][2],
                    [8] = LAST_CONSONANTS[(string)separated[5]][1],
                    [9] = LAST_CONSONANTS[(string)separated[5]][0],

                    // last character
                    [10] = FIRST_CONSONANTS[(string)separated[6]][0],
                    [11] = MIDDLE_VOWELS[(string)separated[7]][1],
                    [12] = MIDDLE_VOWELS[(string)separated[7]][2],
                    [13] = LAST_CONSONANTS[(string)separated[8]][1],
                    [14] = LAST_CONSONANTS[(string)separated[8]][0]
                };

                // ex 냥냐 (nya3 ang nya)
                thisFirstConsonant = (string)cbnnPhonemes[5]; // n
                thisVowelHead = (string)cbnnPhonemes[6]; // y
                thisVowelTail = (string)cbnnPhonemes[7]; // a
                thisSuffix = (string)cbnnPhonemes[8]; // 3
                thisLastConsonant = (string)cbnnPhonemes[9]; // ng

                nextVowelHead = (string)cbnnPhonemes[11]; // 다음 노트 모음의 머리 음소 / y
                nextLastConsonant = (string)cbnnPhonemes[14];

                prevVowelHead = (string)cbnnPhonemes[1];
                prevVowelTail = (string)cbnnPhonemes[2]; // VV음소 만들 때 쓰는 이전 노트의 모음 음소 / CV, CVC 음소와는 관계 없음 // a
                prevLastConsonant = (string)cbnnPhonemes[4]; // VV음소 만들 때 쓰는 이전 노트의 받침 음소
                prevSuffix = (string)cbnnPhonemes[3]; // VV음소 만들 때 쓰는 이전 노트의 접미사 / 3

                VV = $"{prevVowelTail} {thisVowelTail}"; // i a
                CV = $"{thisFirstConsonant}{thisVowelHead}{thisVowelTail}{thisSuffix}"; // nya4
                frontCV = $"- {CV}"; // - nya4
                CV_noSuffix = $"{thisFirstConsonant}{thisVowelHead}{thisVowelTail}"; // nya
                frontCV_noSuffix = $"- {CV_noSuffix}"; // - nya
                cVC = $"{thisVowelTail}{thisLastConsonant}"; // ang 

                endSoundVowel = $"{thisVowelTail} -"; // a -
                endSoundLastConsonant = $"{thisLastConsonant} -"; // ng -

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


                if (((nextVowelHead.Equals("w")) && (thisVowelTail.Equals("eu"))) || ((nextVowelHead.Equals("w")) && (thisVowelTail.Equals("o"))) || ((nextVowelHead.Equals("w")) && (thisVowelTail.Equals("u")))) {
                    nextFirstConsonant = $"{(string)cbnnPhonemes[10]}"; // VC에 썼을 때 eu bw 대신 eu b를 만들기 위함
                } 
                else if (((nextVowelHead.Equals("y") && (thisVowelTail.Equals("i")))) || ((nextVowelHead.Equals("y")) && (thisVowelTail.Equals("eu")))) {
                    nextFirstConsonant = $"{(string)cbnnPhonemes[10]}"; // VC에 썼을 때 i by 대신 i b를 만들기 위함
                } 
                else {
                    nextFirstConsonant = $"{(string)cbnnPhonemes[10]}{(string)cbnnPhonemes[11]}"; // 나머지... ex) ny
                }

                VC = $"{thisVowelTail} {nextFirstConsonant}"; // 다음에 이어질 VV, CVC에게는 해당 없음



                // set Voice color & Tone

                frontCV = FindInOto(frontCV, note, true);

                if (!thisSuffix.Equals("")) {
                    // 접미사가 있는 발음일 때 / nya2
                    if (!singer.TryGetMappedOto($"{CV}", note.tone, out UOto oto)) {CV = $"{thisFirstConsonant}{thisVowelHead}{thisVowelTail}";}
                }
                
                CV = thisSuffix.Equals("") ? FindInOto(CV, note) : FindInOto(CV, note, true);

                VC = FindInOto(VC, note, true);
                VV = FindInOto(VV, note, true);
                cVC = FindInOto(cVC, note);
                endSoundVowel = FindInOto(endSoundVowel, note);
                endSoundLastConsonant = FindInOto(endSoundLastConsonant, note);

                if (CV == null) {CV = FindInOto(CV_noSuffix, note);}
                if (frontCV == null) {frontCV = CV;}
                if (VV == null) {VV = CV;} // VV음소 없으면 (ex : a i) 대응하는 CV음소 사용 (ex:  i)
            
                return cbnnPhonemes;
            }

            /// <summary>
            /// Converts result of Hangeul.Variate(charcter) into CBNN format.
            /// <br/>Hangeul.Variate(character)를 사용한 결과물을 받아 CBNN식으로 변경합니다.
            /// </summary>
            /// <param name="separated">
            /// result of Hangeul.Variate(Note? prevNeighbour, Note note, Note? nextNeighbour).
            /// </param>
            /// <returns>
            /// Returns CBNN formated result. 
            /// </returns>
            private Hashtable ConvertForCBNNSingle(Hashtable separated) {
                // inputs and returns only one character. (한 글자짜리 인풋만 받음)
                Hashtable separatedConvertedForCBNN;

                separatedConvertedForCBNN = new Hashtable() {
                    // first character
                    [0] = FIRST_CONSONANTS[(string)separated[0]][0], //n
                    [1] = MIDDLE_VOWELS[(string)separated[1]][1], // y
                    [2] = MIDDLE_VOWELS[(string)separated[1]][2], // a
                    [3] = LAST_CONSONANTS[(string)separated[2]][1], // 3
                    [4] = LAST_CONSONANTS[(string)separated[2]][0], // ng

                };

                return separatedConvertedForCBNN;
            }


            /// <summary>
            /// Conducts phoneme variation automatically with prevNeighbour, note, nextNeighbour, in CBNN format.  
            /// <br/><br/> prevNeighbour, note, nextNeighbour를 입력받아 자동으로 음운 변동을 진행하고, 결과물을 CBNN 식으로 변경합니다.
            /// </summary>
            /// <param name="prevNeighbour"> Note of prev note, if exists(otherwise null).
            /// <br/> 이전 노트 혹은 null.
            /// <br/><br/>(Example: Note with lyric '춘')
            /// </param>
            /// <param name="note"> Note of current note. 
            /// <br/> 현재 노트.
            /// <br/><br/>(Example: Note with lyric '향')
            /// </param>
            /// <param name="nextNeighbour"> Note of next note, if exists(otherwise null).
            /// <br/> 다음 노트 혹은 null.
            /// <br/><br/>(Example: null)
            /// </param>
            /// <returns> Returns phoneme variation result of prevNote, currentNote, nextNote.
            /// <br/>이전 노트, 현재 노트, 다음 노트의 음운변동 결과를 CBNN 식으로 변환해 반환합니다.
            /// <br/>Example: 춘 [향] null: {[0]="ch", [1]="", [1]="u", [3]="", [4]="", 
            /// <br/>[5]="n", [6]="y", [7]="a", [8]="2", [9]="ng", 
            /// <br/>[10]="", [11]="", [12]="", [13]="", [14]=""} [추 냥 null]
            /// </returns>
            public Hashtable ConvertForCBNN(Note? prevNeighbour, Note note, Note? nextNeighbour) {
                // Hangeul.separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CBNN식으로 변경
                // 이 함수만 불러서 모든 것을 함 (1) [냥]냥
                Hashtable variated = KoreanPhonemizerUtil.Variate(prevNeighbour, note, nextNeighbour);
                thisFirstConsonantType = Enum.Parse<ConsonantType>(FIRST_CONSONANTS[(string)variated[3]][1]);
                thisLastConsonantType = Enum.Parse<BatchimType>(LAST_CONSONANTS[(string)variated[5]][2]);
                prevFirstConsonantType = Enum.Parse<ConsonantType>(FIRST_CONSONANTS[(string)variated[0]][1]);
                prevLastConsonantType = Enum.Parse<BatchimType>(LAST_CONSONANTS[(string)variated[2]][2]);
                nextFirstConsonantType = Enum.Parse<ConsonantType>(FIRST_CONSONANTS[(string)variated[6]][1]);
                nextLastConsonantType = Enum.Parse<BatchimType>(LAST_CONSONANTS[(string)variated[8]][2]);
                return ConvertForCBNN(variated);
            }

            public Hashtable ConvertForCBNN(Note? prevNeighbour) {
                // Hangeul.separate() 함수 등을 사용해 [초성 중성 종성]으로 분리된 결과물을 CBNN식으로 변경
                return ConvertForCBNNSingle(KoreanPhonemizerUtil.Variate(prevNeighbour?.lyric));
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
            Hashtable cbnnPhonemes;

            Note note = notes[0];
            string lyric = note.lyric;
            string phoneticHint = note.phoneticHint;

            Note? prevNote = prevNeighbour; // null or Note
            Note thisNote = note;
            Note? nextNote = nextNeighbour; // null or Note

            int totalDuration = notes.Sum(n => n.duration);
            int vcLength = 120; 
            int vcLengthShort = 90;

            CBNN CBNN = new CBNN(singer, thisNote, totalDuration, vcLength, vcLengthShort);

            try{
                // change lyric to CBNN phonemes, with phoneme variation.
                cbnnPhonemes = CBNN.ConvertForCBNN(prevNote, thisNote, nextNote);
            }
            catch {
                return GenerateResult(lyric);
            }
                

            // Return phonemes
            if ((prevNeighbour == null) && (nextNeighbour == null)) { // No neighbours / 냥
                return (! CBNN.ThisHasBatchim()) ? GenerateResult(CBNN.frontCV, CBNN.endSoundVowel, totalDuration, vcLengthShort) : GenerateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.cVCLength, 6);
            } 

            else if ((prevNeighbour != null) && (nextNeighbour == null)) { // Prev neighbour only / 냥[냥]
                if (! CBNN.ThisHasBatchim()) { // No Batchim / 냐[냐]
                    if (CBNN.ThisVowelNeedsVV()) {// when comes Vowel and there's no previous batchim / 냐[아]
                        return GenerateResult(CBNN.VV, CBNN.endSoundVowel, totalDuration, CBNN.vcLengthShort, 8); 
                    } 
                    else if (CBNN.ThisVowelNeedsCV()) {// when came Vowel behind Batchim / 냥[아]
                        return GenerateResult(CBNN.CV, CBNN.endSoundVowel, totalDuration, CBNN.vcLengthShort, 8);
                    } 
                    else {// Not vowel / 냐[냐]
                        return CBNN.ThisNeedsFrontCV() ? GenerateResult(CBNN.frontCV, CBNN.endSoundVowel, totalDuration, CBNN.vcLengthShort, 8) : GenerateResult(CBNN.CV, CBNN.endSoundVowel, totalDuration, CBNN.vcLengthShort, 8);
                    }
                } 
                else if (CBNN.ThisLastConsonantIsNasalOrLiquid()) {// Batchim - ㄴㄹㅇㅁ  / 냐[냥]
                    if (CBNN.ThisVowelNeedsVV()) {// when comes Vowel and there's no previous batchim / 냐[앙]
                        return GenerateResult(CBNN.VV, CBNN.cVC, totalDuration, CBNN.vcLength, 6);
                    } 
                    else if (CBNN.ThisVowelNeedsCV()) {// when came Vowel behind Batchim / 냥[앙]
                        return GenerateResult(CBNN.CV, CBNN.cVC, totalDuration, CBNN.vcLength, 6);
                    } 
                    else {// batchim / 냐[냑]
                        return CBNN.ThisNeedsFrontCV() ? GenerateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.vcLength, 3) : GenerateResult(CBNN.CV, CBNN.cVC, totalDuration, CBNN.vcLength, 3);
                    }
                } 
                else {// 유음받침 아니고 비음받침도 아님
                    return GenerateResult(CBNN.CV, CBNN.cVC, totalDuration, CBNN.cVCLength, 6);
                }
            } 

            else if ((prevNeighbour == null) && (nextNeighbour != null)) {// next lyric is Hangeul
                if (KoreanPhonemizerUtil.IsHangeul(nextNeighbour?.lyric)) {// Next neighbour only  / null [아] 아
                    if (!CBNN.ThisHasBatchim()) { // No batchim / null [냐] 냥
                        return CBNN.VC != null ? GenerateResult(CBNN.frontCV, CBNN.VC, totalDuration, CBNN.vcLength, 3) : GenerateResult(CBNN.frontCV);
                    } 
                    else if (CBNN.ThisLastConsonantIsNasalOrLiquid()) {// Batchim - ㄴㄹㅇㅁ / null [냥]냐
                        return CBNN.NextFirstConsonantNeedsPause() ? 
                        GenerateResult(CBNN.frontCV, CBNN.cVC, CBNN.endSoundLastConsonant, totalDuration, CBNN.cVCLength, 2, 2)
                        : (CBNN.NextFirstConsonantIsNone() ? GenerateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.cVCLength, 6) : GenerateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2));
                    } 
                    else {// 앞이웃만 없고 받침 있음 - 나머지 / [꺅]꺄
                        return GenerateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2);
                    }
                } 
                else { // 뒤에 한글 안옴
                    return (! CBNN.ThisHasBatchim()) ? GenerateResult(CBNN.frontCV) : GenerateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.cVCLength, 3);
                } 
            } 

            else if ((prevNeighbour != null) && (nextNeighbour != null)) {// 둘다 이웃 있음
                if (KoreanPhonemizerUtil.IsHangeul(nextNeighbour?.lyric)) {// 뒤의 이웃이 한국어임
                    if (! CBNN.ThisHasBatchim()) { // 둘다 이웃 있고 받침 없음 / 냥[냐]냥
                        if (CBNN.ThisVowelNeedsVV()) {
                            return CBNN.VC != null ? GenerateResult(CBNN.VV, CBNN.VC, totalDuration, CBNN.vcLength) : GenerateResult(CBNN.VV);
                        } 
                        else if (CBNN.ThisVowelNeedsCV()) {
                            return CBNN.VC != null ? GenerateResult(CBNN.CV, CBNN.VC, totalDuration, CBNN.vcLength) : GenerateResult(CBNN.CV);
                        }
                        else {
                            if (CBNN.NextIsPlainVowel()) {
                                return CBNN.ThisNeedsFrontCV() ? GenerateResult(CBNN.frontCV) : GenerateResult(CBNN.CV);
                            } 
                            else {
                                return CBNN.ThisNeedsFrontCV() ? 
                                (CBNN.VC != null ? GenerateResult(CBNN.frontCV, CBNN.VC, totalDuration, CBNN.vcLengthShort) : GenerateResult(CBNN.frontCV)) 
                                : (CBNN.VC != null ? GenerateResult(CBNN.CV, CBNN.VC, totalDuration, CBNN.vcLengthShort) : GenerateResult(CBNN.CV));
                            }
                        }
                    } 
                    else if (CBNN.ThisHasBatchim() && (CBNN.NextFirstConsonantIsFricative() || CBNN.ThisLastConsonantIsNasalOrLiquid() || CBNN.NextFirstConsonantIsNone())) {// 둘다 이웃 있고 받침 있음 - ㄴㄹㅇㅁ + 뒤에 오는 음소가 ㅆ인 아무런 받침 / 냐[냥]냐
                        if (CBNN.NextFirstConsonantIsNormal() || CBNN.NextFirstConsonantIsNasal() || CBNN.NextFirstConsonantIsNone() || CBNN.NextFirstConsonantIsLiquid()) {
                            // 다음 음소가 ㄱㄷㅂㅅㅈㄴㅇㄹㅇ 임
                            if (CBNN.ThisVowelNeedsVV()) {
                                return GenerateResult(CBNN.VV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2);
                            } 
                            else {// 앞에 받침 있고 받침 오는 CV / 냥[냥]냐 
                                return CBNN.ThisNeedsFrontCV() ? 
                                GenerateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2) 
                                : (CBNN.NextFirstConsonantIsNone() ? GenerateResult(CBNN.CV, CBNN.cVC, totalDuration, CBNN.cVCLength, 6) : GenerateResult(CBNN.CV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2));
                            }
                        } 
                        else {// 다음 음소가 ㄴㅇㄹㅁ 제외 나머지임
                            return CBNN.ThisVowelNeedsVV() ?
                            GenerateResult(CBNN.VV, CBNN.cVC, CBNN.endSoundLastConsonant, totalDuration, CBNN.cVCLength, 2, 2) 
                            : (CBNN.ThisNeedsFrontCV() ? GenerateResult(CBNN.frontCV, CBNN.cVC, CBNN.endSoundLastConsonant, totalDuration, CBNN.cVCLength, 2, 2) : GenerateResult(CBNN.CV, CBNN.cVC, CBNN.endSoundLastConsonant, totalDuration, CBNN.cVCLength, 2, 2));
                        }
                    } 
                    else {// 둘다 이웃 있고 받침 있음 - 나머지 / 꺅[꺅]꺄
                        return CBNN.ThisVowelNeedsVV() ? 
                        GenerateResult(CBNN.VV, CBNN.cVC, CBNN.endSoundLastConsonant, totalDuration, CBNN.cVCLength, 2, 2)
                        : (CBNN.ThisNeedsFrontCV() ? GenerateResult(CBNN.frontCV, CBNN.cVC, CBNN.endSoundLastConsonant, totalDuration, CBNN.cVCLength, 2, 2) : GenerateResult(CBNN.CV, CBNN.cVC, CBNN.endSoundLastConsonant, totalDuration, CBNN.cVCLength, 2, 2));
                    }
                } 
                else if ((bool)(nextNeighbour?.lyric.Equals("-")) || (bool)(nextNeighbour?.lyric.Equals("R"))) {// 둘다 이웃 있고 뒤에 -가 옴
                    if (! CBNN.ThisHasBatchim()) { // 둘다 이웃 있고 받침 없음 / 냥[냐]냥
                        if (CBNN.ThisVowelNeedsVV()) {return GenerateResult(CBNN.VV);} 
                        else if (CBNN.ThisNeedsFrontCV()) {return GenerateResult(CBNN.frontCV);}
                        else {return GenerateResult(CBNN.CV);}
                    } 
                    else {
                        if (CBNN.NextFirstConsonantIsLiquid() || CBNN.NextFirstConsonantIsNasal() || CBNN.NextFirstConsonantIsNone()) {// 다음 음소가 ㄴㅇㄹㅇ 임
                            if (CBNN.ThisVowelNeedsVV()) {return GenerateResult(CBNN.VV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2);} 
                        else {// 앞에 받침이 온 CVC 음소(받침 있음) / 냥[악]꺅  냥[먁]꺅
                            return CBNN.ThisNeedsFrontCV() ? GenerateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2) : GenerateResult(CBNN.CV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2);
                        }
                    } 
                        else {// 다음 음소가 ㄴㅇㄹㅁ 제외 나머지임
                            return CBNN.ThisVowelNeedsVV() ?
                            GenerateResult(CBNN.VV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2)
                            : (CBNN.ThisNeedsFrontCV() ? GenerateResult(CBNN.frontCV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2) : GenerateResult(CBNN.CV, CBNN.cVC, totalDuration, CBNN.cVCLength, 2));
                        }
                    } 
                } 
                else {
                    return (! CBNN.ThisHasBatchim()) ? GenerateResult(CBNN.CV) : GenerateResult(CBNN.CV, CBNN.cVC, totalDuration, CBNN.cVCLength, 3);
                }
            } 
            else {
                return GenerateResult(CBNN.CV);
            }
        }

        public override Result GenerateEndSound(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            Hashtable cbnnPhonemes;

            Note note = notes[0];
            string lyric = note.lyric;
            string phoneticHint = note.phoneticHint;

            Note? prevNote = prevNeighbour; // null or Note
            Note thisNote = note;
            Note? nextNote = nextNeighbour; // null or Note

            int totalDuration = notes.Sum(n => n.duration);
            int vcLength = 120; // TODO
            int vcLengthShort = 90;

            CBNN CBNN = new CBNN(singer, thisNote, totalDuration, vcLength, vcLengthShort);
            string phonemeToReturn = lyric; // 아래에서 아무것도 안 걸리면 그냥 가사 반환
            string prevLyric = prevNote?.lyric;

            if (thisNote.lyric.Equals("-")) {
                if (KoreanPhonemizerUtil.IsHangeul(prevLyric)) {
                    cbnnPhonemes = CBNN.ConvertForCBNN(prevNote);

                    string prevVowelTail = (string)cbnnPhonemes[2]; // V이전 노트의 모음 음소 
                    string prevLastConsonant = (string)cbnnPhonemes[4]; // 이전 노트의 받침 음소

                    // 앞 노트가 한글
                    if (!prevLastConsonant.Equals("")) {
                        phonemeToReturn = $"{prevLastConsonant} -";
                    } else if (!prevVowelTail.Equals("")) {
                        phonemeToReturn = $"{prevVowelTail} -";
                    }

                }
                return GenerateResult(phonemeToReturn);
            } else if (thisNote.lyric.Equals("R")) {
                if (KoreanPhonemizerUtil.IsHangeul(prevLyric)) {
                    cbnnPhonemes = CBNN.ConvertForCBNN(prevNote);

                    string prevVowelTail = (string)cbnnPhonemes[2]; // V이전 노트의 모음 음소 
                    string prevLastConsonant = (string)cbnnPhonemes[4]; // 이전 노트의 받침 음소

                    // 앞 노트가 한글
                    if (!prevLastConsonant.Equals("")) {
                        phonemeToReturn = $"{prevLastConsonant} R";
                    } 
                    else if (!prevVowelTail.Equals("")) {
                        phonemeToReturn = $"{prevVowelTail} R";
                    }

                }
                return GenerateResult(phonemeToReturn);
            } 
            else {
                return GenerateResult(phonemeToReturn);
            }
        }
    }
}