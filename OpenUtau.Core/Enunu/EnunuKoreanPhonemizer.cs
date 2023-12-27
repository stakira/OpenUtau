using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using K4os.Hash.xxHash;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Enunu {
    [Phonemizer("Enunu Korean Phonemizer", "ENUNU KO", "EX3", language:"KO")]
    public class EnunuKoreanPhonemizer : EnunuPhonemizer {
        readonly string PhonemizerType = "ENUNU KO";
        public string semivowelSep;
        private KoreanENUNUIniSetting koreanENUNUIniSetting; // Manages Settings
        private bool isSeparateSemiVowels; // Nanages n y a or ny a

            /// <summary>
            /// KO ENUNU phoneme table of first consonants. (key "null" is for Handling empty string)
            /// </summary>
            private Dictionary<string, string[]> FirstConsonants = new Dictionary<string, string[]>(){
                {"ㄱ", new string[2]{"g", ConsonantType.NORMAL.ToString()}},
                {"ㄲ", new string[2]{"kk", ConsonantType.FORTIS.ToString()}},
                {"ㄴ", new string[2]{"n", ConsonantType.NASAL.ToString()}},
                {"ㄷ", new string[2]{"d", ConsonantType.NORMAL.ToString()}},
                {"ㄸ", new string[2]{"tt", ConsonantType.FORTIS.ToString()}},
                {"ㄹ", new string[2]{"r", ConsonantType.LIQUID.ToString()}},
                {"ㅁ", new string[2]{"m", ConsonantType.NASAL.ToString()}},
                {"ㅂ", new string[2]{"b", ConsonantType.NORMAL.ToString()}},
                {"ㅃ", new string[2]{"pp", ConsonantType.FORTIS.ToString()}},
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
            /// KO ENUNU phoneme table of middle vowels (key "null" is for Handling empty string)
            /// </summary>
            private Dictionary<string, string[]> MiddleVowels = new Dictionary<string, string[]>(){
                {"ㅏ", new string[3]{"a", "", "a"}},
                {"ㅐ", new string[3]{"e", "", "e"}},
                {"ㅑ", new string[3]{"ya", "y", " a"}},
                {"ㅒ", new string[3]{"ye", "y", " e"}},
                {"ㅓ", new string[3]{"eo", "", "eo"}},
                {"ㅔ", new string[3]{"e", "", "e"}},
                {"ㅕ", new string[3]{"yeo", "y", " eo"}},
                {"ㅖ", new string[3]{"ye", "y", " e"}},
                {"ㅗ", new string[3]{"o", "", "o"}},
                {"ㅘ", new string[3]{"wa", "w", " a"}},
                {"ㅙ", new string[3]{"we", "w", " e"}},
                {"ㅚ", new string[3]{"we", "w", " e"}},
                {"ㅛ", new string[3]{"yo", "y", " o"}},
                {"ㅜ", new string[3]{"u", "", "u"}},
                {"ㅝ", new string[3]{"weo", "w", " eo"}},
                {"ㅞ", new string[3]{"we", "w", " e"}},
                {"ㅟ", new string[3]{"wi", "w", " i"}},
                {"ㅠ", new string[3]{"yu", "y", " u"}},
                {"ㅡ", new string[3]{"eu", "", "eu"}},
                {"ㅢ", new string[3]{"i", "", "i"}}, // ㅢ는 ㅣ로 발음
                {"ㅣ", new string[3]{"i", "", "i"}},
                {" ", new string[3]{"", "", ""}},
                {"null", new string[3]{"", "", ""}} // 뒤 글자가 없을 때를 대비
                };

            /// <summary>
            /// KO ENUNU phoneme table of last consonants. (key "null" is for Handling empty string)
            /// </summary>
            private Dictionary<string, string[]> LastConsonants = new Dictionary<string, string[]>(){
                 //ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ
                {"ㄱ", new string[3]{" K", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄲ", new string[3]{" K", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄳ", new string[3]{" K", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄴ", new string[3]{" N", "2", BatchimType.NASAL_END.ToString()}},
                {"ㄵ", new string[3]{" N", "2", BatchimType.NASAL_END.ToString()}},
                {"ㄶ", new string[3]{" N", "2", BatchimType.NASAL_END.ToString()}},
                {"ㄷ", new string[3]{" T", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㄹ", new string[3]{" L", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄺ", new string[3]{" K", "", BatchimType.NORMAL_END.ToString()}},
                {"ㄻ", new string[3]{" M", "1", BatchimType.NASAL_END.ToString()}},
                {"ㄼ", new string[3]{" L", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄽ", new string[3]{" L", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄾ", new string[3]{" L", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㄿ", new string[3]{" P", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅀ", new string[3]{" L", "4", BatchimType.LIQUID_END.ToString()}},
                {"ㅁ", new string[3]{" M", "1", BatchimType.NASAL_END.ToString()}},
                {"ㅂ", new string[3]{" P", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅄ", new string[3]{" P", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅅ", new string[3]{" T", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅆ", new string[3]{" T", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅇ", new string[3]{" NG", "3", BatchimType.NG_END.ToString()}},
                {"ㅈ", new string[3]{" T", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅊ", new string[3]{" T", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅋ", new string[3]{" K", "", BatchimType.NORMAL_END.ToString()}},
                {"ㅌ", new string[3]{" T", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅍ", new string[3]{" P", "1", BatchimType.NORMAL_END.ToString()}},
                {"ㅎ", new string[3]{" T", "1", BatchimType.H_END.ToString()}},
                {" ", new string[3]{"", "", BatchimType.NO_END.ToString()}},
                {"null", new string[3]{"", "", BatchimType.PHONEME_IS_NULL.ToString()}} // 뒤 글자가 없을 때를 대비
                };
        
        struct TimingResult {
            public string path_full_timing;
            public string path_mono_timing;
        }

        struct TimingResponse {
            public string error;
            public TimingResult result;
        }
        public override void SetSinger(USinger singer) {
            this.singer = singer as EnunuSinger;

            koreanENUNUIniSetting = new KoreanENUNUIniSetting();
            koreanENUNUIniSetting.Initialize(singer, "ko-ENUNU.ini");

            semivowelSep = koreanENUNUIniSetting.IsSeparateSemiVowels() ? " ": "";

            // Modify Phoneme Tables
            // First Consonants
            FirstConsonants["ㄱ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㄱ")}";
            FirstConsonants["ㄲ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㄲ")}";
            FirstConsonants["ㄴ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㄴ")}";
            FirstConsonants["ㄷ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㄷ")}";
            FirstConsonants["ㄸ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㄸ")}";
            FirstConsonants["ㄹ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㄹ")}";
            FirstConsonants["ㅁ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㅁ")}";
            FirstConsonants["ㅂ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㅂ")}";
            FirstConsonants["ㅃ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㅃ")}";
            FirstConsonants["ㅅ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㅅ")}";
            FirstConsonants["ㅆ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㅆ")}";
            FirstConsonants["ㅇ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㅇ")}";
            FirstConsonants["ㅈ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㅈ")}";
            FirstConsonants["ㅉ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㅉ")}";
            FirstConsonants["ㅊ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㅊ")}";
            FirstConsonants["ㅋ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㅋ")}";
            FirstConsonants["ㅌ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㅌ")}";
            FirstConsonants["ㅍ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㅍ")}";
            FirstConsonants["ㅎ"][0] = $"{koreanENUNUIniSetting.GetFirstConsonantPhoneme("ㅎ")}";

            
            // Vowels
            MiddleVowels["ㅑ"][1] = koreanENUNUIniSetting.GetSemiVowelPhoneme("y");
            MiddleVowels["ㅒ"][1] = koreanENUNUIniSetting.GetSemiVowelPhoneme("y");
            MiddleVowels["ㅕ"][1] = koreanENUNUIniSetting.GetSemiVowelPhoneme("y");
            MiddleVowels["ㅖ"][1] = koreanENUNUIniSetting.GetSemiVowelPhoneme("y");
            MiddleVowels["ㅘ"][1] = koreanENUNUIniSetting.GetSemiVowelPhoneme("w");
            MiddleVowels["ㅙ"][1] = koreanENUNUIniSetting.GetSemiVowelPhoneme("w");
            MiddleVowels["ㅚ"][1] = koreanENUNUIniSetting.GetSemiVowelPhoneme("w");
            MiddleVowels["ㅛ"][1] = koreanENUNUIniSetting.GetSemiVowelPhoneme("y");
            MiddleVowels["ㅝ"][1] = koreanENUNUIniSetting.GetSemiVowelPhoneme("w");
            MiddleVowels["ㅞ"][1] = koreanENUNUIniSetting.GetSemiVowelPhoneme("w");
            MiddleVowels["ㅟ"][1] = koreanENUNUIniSetting.GetSemiVowelPhoneme("w");
            MiddleVowels["ㅠ"][1] = koreanENUNUIniSetting.GetSemiVowelPhoneme("y");

            MiddleVowels["ㅏ"][2] = $"{koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅏ")}";
            MiddleVowels["ㅐ"][2] = $"{koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅔ/ㅐ")}";
            MiddleVowels["ㅑ"][2] = $" {koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅏ")}";
            MiddleVowels["ㅒ"][2] = $" {koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅔ/ㅐ")}";
            MiddleVowels["ㅓ"][2] = $"{koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅓ")}";
            MiddleVowels["ㅔ"][2] = $"{koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅔ/ㅐ")}";
            MiddleVowels["ㅕ"][2] = $" {koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅓ")}";
            MiddleVowels["ㅖ"][2] = $" {koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅔ/ㅐ")}";
            MiddleVowels["ㅗ"][2] = $"{koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅗ")}";
            MiddleVowels["ㅘ"][2] = $" {koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅏ")}";
            MiddleVowels["ㅙ"][2] = $" {koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅔ/ㅐ")}";
            MiddleVowels["ㅚ"][2] = $" {koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅔ/ㅐ")}";
            MiddleVowels["ㅛ"][2] = $" {koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅗ")}";
            MiddleVowels["ㅜ"][2] = $"{koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅜ")}";
            MiddleVowels["ㅝ"][2] = $" {koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅓ")}";
            MiddleVowels["ㅞ"][2] = $" {koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅔ/ㅐ")}";
            MiddleVowels["ㅟ"][2] = $" {koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅣ")}";
            MiddleVowels["ㅠ"][2] = $" {koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅜ")}";
            MiddleVowels["ㅡ"][2] = $"{koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅡ")}";
            MiddleVowels["ㅢ"][2] = $"{koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅣ")}"; // ㅢ는 ㅣ로 발음
            MiddleVowels["ㅣ"][2] = $"{koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅣ")}";
        
        // final consonants
            LastConsonants["ㄱ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄱ")}";
            LastConsonants["ㄲ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄱ")}";
            LastConsonants["ㄳ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄱ")}";
            LastConsonants["ㄴ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄴ")}";
            LastConsonants["ㄵ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄴ")}";
            LastConsonants["ㄶ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄴ")}";
            LastConsonants["ㄷ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄷ")}";
            LastConsonants["ㄹ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄹ")}";
            LastConsonants["ㄺ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄱ")}";
            LastConsonants["ㄻ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㅁ")}";
            LastConsonants["ㄼ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄹ")}";
            LastConsonants["ㄽ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄹ")}";
            LastConsonants["ㄾ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄹ")}";
            LastConsonants["ㄿ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㅂ")}";
            LastConsonants["ㅀ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄹ")}";
            LastConsonants["ㅁ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㅁ")}";
            LastConsonants["ㅂ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㅂ")}";
            LastConsonants["ㅄ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㅂ")}";
            LastConsonants["ㅅ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄷ")}";
            LastConsonants["ㅆ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄷ")}";
            LastConsonants["ㅇ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㅇ")}";
            LastConsonants["ㅈ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄷ")}";
            LastConsonants["ㅊ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄷ")}";
            LastConsonants["ㅋ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄱ")}";
            LastConsonants["ㅌ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄷ")}";
            LastConsonants["ㅍ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㅂ")}";
            LastConsonants["ㅎ"][0] = $" {koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄷ")}";

        }
        private class KoreanENUNUIniSetting : KoreanPhonemizerUtil.BaseIniManager{
            protected override void IniSetUp(IniFile iniFile) {
                // ko-ENUNU.ini
                SetOrReadThisValue("SETTING", "Separate semivowels, like 'n y a'(otherwise 'ny a')", false); // 반자음 떼기 유무 - 기본값 false
                SetOrReadThisValue("FIRST CONSONANTS", "ㄱ", "g"); // ㄱ 음소 - 기본값 "g"
                SetOrReadThisValue("FIRST CONSONANTS", "ㄲ", "kk"); // ㄲ 음소 - 기본값 "kk"
                SetOrReadThisValue("FIRST CONSONANTS", "ㄴ", "n"); // ㄴ 음소 - 기본값 "n"
                SetOrReadThisValue("FIRST CONSONANTS", "ㄷ", "d"); // ㄷ 음소 - 기본값 "d"
                SetOrReadThisValue("FIRST CONSONANTS", "ㄸ", "tt"); // ㄸ 음소 - 기본값 "tt"
                SetOrReadThisValue("FIRST CONSONANTS", "ㄹ", "r"); // ㄹ 음소 - 기본값 "r"
                SetOrReadThisValue("FIRST CONSONANTS", "ㅁ", "m"); // ㅁ 음소 - 기본값 "m"
                SetOrReadThisValue("FIRST CONSONANTS", "ㅂ", "b"); // ㅂ 음소 - 기본값 "b"
                SetOrReadThisValue("FIRST CONSONANTS", "ㅃ", "pp"); // ㅃ 음소 - 기본값 "pp"
                SetOrReadThisValue("FIRST CONSONANTS", "ㅅ", "s"); // ㅅ 음소 - 기본값 "s"
                SetOrReadThisValue("FIRST CONSONANTS", "ㅆ", "ss"); // ㅆ 음소 - 기본값 "ss"
                SetOrReadThisValue("FIRST CONSONANTS", "ㅇ", ""); // ㅇ 음소 - 기본값 ""
                SetOrReadThisValue("FIRST CONSONANTS", "ㅈ", "j"); // ㅈ 음소 - 기본값 "j"
                SetOrReadThisValue("FIRST CONSONANTS", "ㅉ", "jj"); // ㅉ 음소 - 기본값 "jj"
                SetOrReadThisValue("FIRST CONSONANTS", "ㅊ", "ch"); // ㅊ 음소 - 기본값 "ch"
                SetOrReadThisValue("FIRST CONSONANTS", "ㅋ", "k"); // ㅋ 음소 - 기본값 "k"
                SetOrReadThisValue("FIRST CONSONANTS", "ㅌ", "t"); // ㅌ 음소 - 기본값 "t"
                SetOrReadThisValue("FIRST CONSONANTS", "ㅍ", "p"); // ㅍ 음소 - 기본값 "p"
                SetOrReadThisValue("FIRST CONSONANTS", "ㅎ", "h"); // ㅎ 음소 - 기본값 "h"
                SetOrReadThisValue("PLAIN VOWELS", "ㅏ", "a"); // ㅏ음소 - 기본값 "a"
                SetOrReadThisValue("PLAIN VOWELS", "ㅣ", "i"); // ㅣ음소 - 기본값 "i"
                SetOrReadThisValue("PLAIN VOWELS", "ㅜ", "u"); // ㅜ음소 - 기본값 "u"
                SetOrReadThisValue("PLAIN VOWELS", "ㅔ/ㅐ", "e"); // ㅔ음소 - 기본값 "eu"
                SetOrReadThisValue("PLAIN VOWELS", "ㅗ", "o"); // ㅗ음소 - 기본값 "o"
                SetOrReadThisValue("PLAIN VOWELS", "ㅡ", "eu"); // ㅡ음소 - 기본값 "eu"
                SetOrReadThisValue("PLAIN VOWELS", "ㅓ", "eo"); // ㅓ음소 - 기본값 "eo"
                SetOrReadThisValue("SEMI VOWELS", "w", "w"); // w음소 - 기본값 "w"
                SetOrReadThisValue("SEMI VOWELS", "y", "y"); // y음소 - 기본값 "y"
                SetOrReadThisValue("FINAL CONSONANTS", "ㄱ", "K"); // ㄱ음소 - 기본값 "K"
                SetOrReadThisValue("FINAL CONSONANTS", "ㄴ", "N"); // ㄴ음소 - 기본값 "N"
                SetOrReadThisValue("FINAL CONSONANTS", "ㄷ", "T"); // ㄷ음소 - 기본값 "T"
                SetOrReadThisValue("FINAL CONSONANTS", "ㄹ", "L"); // ㄹ음소 - 기본값 "L"
                SetOrReadThisValue("FINAL CONSONANTS", "ㅁ", "M"); // ㅁ음소 - 기본값 "M"
                SetOrReadThisValue("FINAL CONSONANTS", "ㅂ", "P"); // ㅂ음소 - 기본값 "P"
                SetOrReadThisValue("FINAL CONSONANTS", "ㅇ", "NG"); // ㅇ음소 - 기본값 "NG"
            }

            public string GetFirstConsonantPhoneme(string Phoneme) {
                return iniFile["FIRST CONSONANTS"][Phoneme].ToString();
            }

            public string GetPlainVowelPhoneme(string Phoneme) {
                return iniFile["PLAIN VOWELS"][Phoneme].ToString();
            }

            public string GetSemiVowelPhoneme(string Phoneme) {
                return iniFile["SEMI VOWELS"][Phoneme].ToString();
            }
            
            public string GetFinalConsonantPhoneme(string Phoneme) {
                return iniFile["FINAL CONSONANTS"][Phoneme].ToString();
            }

            public bool IsSeparateSemiVowels() {
                return iniFile["SETTING"]["Separate semivowels, like 'n y a'(otherwise 'ny a')"].ToBool();
            }

        }

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
            
        Dictionary<Note[], Phoneme[]> partResult = new Dictionary<Note[], Phoneme[]>();

        public override void SetUp(Note[][] notes) {
            partResult.Clear();
            if (notes.Length == 0 || singer == null || !singer.Found) {
                return;
            }
            double bpm = timeAxis.GetBpmAtTick(notes[0][0].position);
            ulong hash = HashNoteGroups(notes, bpm);
            var tmpPath = Path.Join(PathManager.Inst.CachePath, $"lab-{hash:x16}");
            var ustPath = tmpPath + ".tmp";
            var enutmpPath = tmpPath + "_enutemp";
            var scorePath = Path.Join(enutmpPath, $"score.lab");
            var timingPath = Path.Join(enutmpPath, $"timing.lab");
            var enunuNotes = NoteGroupsToEnunu(notes);
            if (!File.Exists(scorePath) || !File.Exists(timingPath)) {
                EnunuUtils.WriteUst(enunuNotes, bpm, singer, ustPath);
                var response = EnunuClient.Inst.SendRequest<TimingResponse>(new string[] { "timing", ustPath });
                if (response.error != null) {
                    throw new Exception(response.error);
                }
            }
            var noteIndexes = LabelToNoteIndex(scorePath, enunuNotes);
            var timing = ParseLabel(timingPath);
            timing.Zip(noteIndexes, (phoneme, noteIndex) => Tuple.Create(phoneme, noteIndex))
                .GroupBy(tuple => tuple.Item2)
                .ToList()
                .ForEach(g => {
                    if (g.Key >= 0) {
                        var noteGroup = notes[g.Key];
                        partResult[noteGroup] = g.Select(tu => tu.Item1).ToArray();
                    }
                });
        }
        
        ulong HashNoteGroups(Note[][] notes, double bpm) {
            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(this.PhonemizerType);
                    writer.Write(this.singer.Location);
                    writer.Write(bpm);
                    foreach (var ns in notes) {
                        foreach (var n in ns) {
                            writer.Write(n.lyric);
                            if(n.phoneticHint!= null) {
                                writer.Write("["+n.phoneticHint+"]");
                            }
                            writer.Write(n.position);
                            writer.Write(n.duration);
                            writer.Write(n.tone);
                        }
                    }
                    return XXH64.DigestOf(stream.ToArray());
                }
            }
        }

        static int[] LabelToNoteIndex(string scorePath, EnunuNote[] enunuNotes) {
            var result = new List<int>();
            int lastPos = 0;
            int index = 0;
            var score = ParseLabel(scorePath);
            foreach (var p in score) {
                if (p.position != lastPos) {
                    index++;
                    lastPos = p.position;
                }
                result.Add(enunuNotes[index].noteIndex);
            }
            return result.ToArray();
        }

        static Phoneme[] ParseLabel(string path) {
            var phonemes = new List<Phoneme>();
            using (var reader = new StreamReader(path, Encoding.UTF8)) {
                while (!reader.EndOfStream) {
                    var line = reader.ReadLine();
                    var parts = line.Split();
                    if (parts.Length == 3 &&
                        long.TryParse(parts[0], out long pos) &&
                        long.TryParse(parts[1], out long end)) {
                        phonemes.Add(new Phoneme {
                            phoneme = parts[2],
                            position = (int)(pos / 1000L),
                        });
                    }
                }
            }
            return phonemes.ToArray();
        }

        protected override EnunuNote[] NoteGroupsToEnunu(Note[][] notes) {
            KoreanPhonemizerUtil.RomanizeNotes(notes, FirstConsonants, MiddleVowels, LastConsonants, semivowelSep);
            var result = new List<EnunuNote>();
            int position = 0;
            int index = 0;
            
            while (index < notes.Length) {
                if (position < notes[index][0].position) {
                    result.Add(new EnunuNote {
                        lyric = "R",
                        length = notes[index][0].position - position,
                        noteNum = 60,
                        noteIndex = -1,
                    });
                    position = notes[index][0].position;
                } else {
                    var lyric = notes[index][0].lyric;
                    result.Add(new EnunuNote {
                        lyric = lyric,
                        length = notes[index].Sum(n => n.duration),
                        noteNum = notes[index][0].tone,
                        noteIndex = index,
                    });
                    position += result.Last().length;
                    index++;
                }
            }
            return result.ToArray();
        }

        public void AdjustPos(Phoneme[] phonemes, Note[] prevNote){
            Phoneme? prevPhone = null;
            Phoneme? nextPhone = null;
            Phoneme currPhone;

            int length = phonemes.Last().position;
            int prevLength;
            if (prevNote == null){
                prevLength = length;
            }
            else{
                prevLength = MsToTick(prevNote.Sum(n => n.duration));
            }

            for (int i=0; i < phonemes.Length; i++) {
                currPhone = phonemes[i];
                if (i < phonemes.Length - 1){
                    nextPhone = phonemes[i+1];
                }
                else{
                    nextPhone = null;
                }

                if (i == 0){
                    // 받침 + 자음 오면 받침길이 + 자음길이 / 2의 위치에 자음이 오도록 하기
                    if (isPlainVowel(phonemes[i].phoneme)) {
                        phonemes[i].position = 0;
                    }
                    else if (nextPhone != null && ! isPlainVowel(((Phoneme)nextPhone).phoneme) && ! isSemivowel(((Phoneme)nextPhone).phoneme) && isPlainVowel(((Phoneme)nextPhone).phoneme) && isSemivowel(currPhone.phoneme)) {
                        phonemes[i + 1].position = length / 10;
                    }
                    else if (nextPhone != null && isSemivowel(((Phoneme)nextPhone).phoneme)){
                        if (i + 2 < phonemes.Length){
                            phonemes[i + 2].position = length / 10;
                        }
                        
                    }
                }
                prevPhone = currPhone;
            }
        }

        private bool isPlainVowel(string phoneme){
            if (phoneme == koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅏ") || phoneme == koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅣ") || phoneme == koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅜ") || phoneme == koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅔ") || phoneme == koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅗ") || phoneme == koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅡ") || phoneme == koreanENUNUIniSetting.GetPlainVowelPhoneme("ㅓ")){
                return true;
            }
            return false;
        }

        private bool isBatchim(string phoneme){
            if (phoneme == koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄱ") || phoneme == koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄴ") || phoneme == koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄷ") || phoneme == koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㄹ") || phoneme == koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㅁ") || phoneme == koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㅂ") || phoneme == koreanENUNUIniSetting.GetFinalConsonantPhoneme("ㅇ")){
                return true;
            }
            return false;
        }

        private bool isSemivowel(string phoneme) {
            if (phoneme == koreanENUNUIniSetting.GetSemiVowelPhoneme("w") || phoneme == koreanENUNUIniSetting.GetSemiVowelPhoneme("y")){
                return true;
            }
            return false;
        }
        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs) {
            if (partResult.TryGetValue(notes, out var phonemes)) {
                var phonemes_ = phonemes.Select(p => {
                        double posMs = p.position * 0.1;
                        p.position = MsToTick(posMs) - notes[0].position;
                        return p;
                    }).ToArray();

                AdjustPos(phonemes_, prevs);
                return new Result {
                    phonemes = phonemes_,
                };
            }
            return new Result {
                phonemes = new Phoneme[] {
                    new Phoneme {
                        phoneme = "error",
                    }
                },
            };
        }

        public override void CleanUp() {
            partResult.Clear();
        }

    }
}
