using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Korean CVVC Phonemizer", "KO CVVC", "RYUUSEI", language:"KO")]
    public class KoreanCVVCStandardPronunciationPhonemizer : Phonemizer {
        static readonly string initialConsonantsTable = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
        static readonly string vowelsTable = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
        static readonly string lastConsonantsTable = "　ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";
        static readonly ushort unicodeKoreanBase = 0xAC00;
        static readonly ushort unicodeKoreanLast = 0xD79F;

        private char[] SeparateHangul(char letter) {
            if (letter == 0) return new char[] { '　', '　', '　' };
            var u16 = Convert.ToUInt16(letter);

            if (u16 < unicodeKoreanBase || u16 > unicodeKoreanLast)
                return new char[] { letter };

            u16 -= unicodeKoreanBase;

            var i = u16 / (21 * 28);
            u16 %= 21 * 28;
            var v = u16 / 28;
            u16 %= 28;
            var l = u16;

            return new char[] { initialConsonantsTable[i], vowelsTable[v], lastConsonantsTable[l] };
        }

        // 초성
        static readonly string[] initialConsonants = new string[] {
            "g=ㄱ",
            "kk=ㄲ",
            "k=ㅋ",
            "n=ㄴ",
            "d=ㄷ",
            "tt=ㄸ",
            "t=ㅌ",
            "r=ㄹ",
            "m=ㅁ",
            "b=ㅂ",
            "pp=ㅃ",
            "s=ㅅ",
            "ss=ㅆ",
            "=ㅇ,　",
            "j=ㅈ",
            "jj=ㅉ",
            "ch=ㅊ",
            "p=ㅍ",
            "h=ㅎ",
        };

        // 일반 모음
        static readonly string[] vowels = new string[] {
            "a=ㅏ",
            "ya=ㅑ",
            "eo=ㅓ",
            "yeo=ㅕ",
            "o=ㅗ",
            "yo=ㅛ",
            "u=ㅜ",
            "yu=ㅠ",
            "eu=ㅡ",
            "i=ㅣ",
            "e=ㅔ",
            "ye=ㅖ",
            "ae=ㅐ",
            "yae=ㅒ",
            "eui=ㅢ",
            "we=ㅞ,ㅙ,ㅚ",
            "weo=ㅝ",
            "wa=ㅘ",
            "wi=ㅟ",
        };

        // V-V의 경우 이전 모음으로 대체
        static readonly string[] subsequentVowels = new string[] {
            "a=ㅏ,ㅑ,ㅘ",
            "eo=ㅓ,ㅕ,ㅝ",
            "o=ㅗ,ㅛ",
            "u=ㅜ,ㅠ",
            "eu=ㅡ",
            "e=ㅔ,ㅐ,ㅞ,ㅙ",
            "i=ㅣ,ㅢ,ㅟ",
        };

        // 끝소리일 경우에만 동작
        static readonly string[] lastConsonants = new string[] {
            "k=ㄱ,ㅋ,ㄲ,ㄳ,ㄺ",
            "n=ㄴ,ㄵ,ㄶ",
            "t=ㄷ,ㅅ,ㅈ,ㅊ,ㅌ,ㅆ,ㅎ",
            "l=ㄹ,ㄼ,ㄽ,ㄾ,ㄿ,ㅀ",
            "m=ㅁ,ㄻ",
            "p=ㅂ,ㅍ,ㅄ",
            "ng=ㅇ",
        };

        // 표준발음법 적용
        static readonly string[] ruleOfConsonants = new string[] {
            // 자음동화: (비음화, 유음화)
            "ㅇㄴ=ㅇㄴ,ㄱㄴ,ㄱㄹ,ㅇㄹ",
            "ㅇㄱ=ㅇㄱ,ㄱㅁ",
            "ㄴㄴ=ㄴㄴ,ㄷㄴ,ㄵㄴ",
            "ㄴㅁ=ㄴㅁ,ㄷㅁ,ㄵㅁ",
            "ㅁㄴ=ㅁㄴ,ㅂㄴ,ㅂㄹ,ㅁㄹ",
            "ㅁㅁ=ㅁㅁ,ㅂㅁ",
            "ㄹㄹ=ㄹㄹ,ㄴㄹ,ㄵㄹ",

            // 구개음화
            "　ㅈㅣ=ㄷㅇㅣ",
            "　ㅈㅓ=ㄷㅇㅓ",
            "　ㅈㅓ=ㄷㅇㅕ",
            "　ㅊㅣ=ㄷㅎㅣ",
            "　ㅊㅓ=ㄷㅎㅓ",
            "　ㅊㅓ=ㄷㅎㅕ",
            "　ㅊㅣ=ㅌㅇㅣ",
            "　ㅊㅓ=ㅌㅇㅓ",
            "　ㅊㅓ=ㅌㅇㅕ",
            "　ㅊㅣ=ㅌㅎㅣ",
            "　ㅊㅓ=ㅌㅎㅓ",
            "　ㅊㅓ=ㅌㅎㅕ",
            "ㄹㅊㅣ=ㄾㅇㅣ",
            "ㄹㅊㅓ=ㄾㅇㅓ",
            "ㄹㅊㅓ=ㄾㅇㅕ",
            "ㄹㅊㅣ=ㄾㅎㅣ",
            "ㄹㅊㅓ=ㄾㅎㅓ",
            "ㄹㅊㅓ=ㄾㅎㅕ",

            // 경음화
            "ㄱㄲ=ㄱㄲ,ㄱㄱ,ㄲㄱ",
            "ㄱㄸ=ㄱㄸ,ㄱㄷ,ㄺㄷ,ㄺㅌ,ㄺㄸ",
            "ㄱㅃ=ㄱㅃ,ㄱㅂ",
            "ㄱㅆ=ㄱㅆ,ㄱㅅ",
            "ㄱㅉ=ㄱㅉ,ㄱㅈ",
            "ㄴㄸ=ㄴㄸ,ㄵㄷ,ㄵㄸ,ㄶㅌ,ㄶㄸ",
            "ㄷㄲ=ㄷㄲ,ㄷㄱ",
            "ㄷㄸ=ㄷㄸ,ㄷㄷ",
            "ㄷㅃ=ㄷㅃ,ㄷㅂ",
            "ㄷㅆ=ㄷㅆ,ㄷㅅ",
            "ㄷㅉ=ㄷㅉ",
            "ㅁㄸ=ㅁㄸ,ㄻㄷ,ㄻㅌ,ㄻㄸ",
            "ㅂㄲ=ㅂㄲ,ㅂㄱ,ㅄㄲ,ㅄㄱ,ㄼㄱ,ㄼㅋ,ㄼㄲ",
            "ㅂㄸ=ㅂㄸ,ㅂㄷ,ㅄㄸ,ㅄㄷ",
            "ㅂㅃ=ㅂㅃ,ㅂㅂ,ㅄㅃ,ㅄㅂ",
            "ㅂㅆ=ㅂㅆ,ㄼㅅ,ㄼㅆ,ㅂㅅ,ㅄㅆ,ㅄㅅ",
            "ㅂㅉ=ㅂㅉ,ㅂㅈ,ㅄㅉ,ㅄㅈ",
            "ㅅㄲ=ㅅㄲ,ㅅㄱ,ㅆㄲ,ㅆㄱ",
            "ㅅㄸ=ㅅㄸ,ㅅㄷ,ㅆㄸ,ㅆㄷ",
            "ㅅㅃ=ㅅㅃ,ㅅㅂ,ㅆㅃ,ㅆㅂ",
            "ㅅㅆ=ㅅㅆ,ㅅㅅ,ㅆㅆ,ㅆㅅ",
            "ㅅㅉ=ㅅㅉ,ㅅㅈ,ㅆㅉ,ㅆㅈ",
            "ㅈㄲ=ㅈㄲ,ㅈㄱ",
            "ㅈㄸ=ㅈㄸ,ㅈㄷ",
            "ㅈㅃ=ㅈㅃ,ㅈㅂ",
            "ㅈㅉ=ㅈㅉ,ㅈㅈ",
            "ㅈㅆ=ㅈㅆ,ㅈㅅ",

            // 자음 축약
            "　ㅋ=ㄱㅎ",
            "　ㅌ=ㄷㅎ",
            "　ㅍ=ㅂㅎ",
            "　ㅊ=ㅈㅎ",
            "ㄴㅊ=ㄴㅊ,ㄵㅎ",

            // 탈락
            "ㄴㅌ=ㄴㅌ,ㄶㄷ",
            "　ㄴ=ㄶㅇ",
            "ㄹㅌ=ㄹㅌ,ㄼㄷ,ㄼㅌ,ㄽㄷ,ㄾㅌ,ㄾㄷ,ㄽㅌ,ㄿㄷ,ㄿㅌ,ㅀㄷ,ㄾㅇ",
            "ㄹㄸ=ㄹㄸ,ㅀㅌ,ㅀㄸ",
            "　ㄹ=ㅀㅇ",

            // 연음
            "　ㄱ=ㄱㅇ",
            "　ㄲ=ㄲㅇ",
            "ㄱㅅ=ㄳㅇ",
            "　ㄴ=ㄴㅇ",
            "ㄴㅈ=ㄴㅈ,ㄵㅇ",
            "　ㄹ=ㄹㅇ",
            "ㄹㄱ=ㄹㄱ,ㄺㅇ",
            "ㄹㅁ=ㄹㅁ,ㄻㅇ",
            "ㄹㅂ=ㄹㅂ,ㄼㅇ",
            "ㄹㅅ=ㄹㅅ,ㄽㅇ",
            "ㄹㅍ=ㄹㅍ,ㄿㅇ,ㄺㅂ,ㄻㅂ,ㄼㅂ,ㄽㅂ,ㄾㅂ,ㄿㅂ,ㅀㅂ",
            "　ㅁ=ㅁㅇ",
            "　ㅂ=ㅂㅇ",
            "ㅂㅅ=ㅄㅇ",
            "　ㅅ=ㅅㅇ",
            "　ㅈ=ㅈㅇ",
            "　ㅊ=ㅊㅇ",
            "　ㅋ=ㅋㅇ",
            "　ㅌ=ㅌㅇ",
            "　ㅍ=ㅍㅇ",
            "　ㅎ=ㅎㅇ",

            // 이 외
            "ㄱㅋ=ㄱㅋ",
            "ㄱㅌ=ㄱㅌ",
            "ㄱㅊ=ㄱㅊ",
            "ㄱㅍ=ㄱㅍ",
            "ㄲㅂ=ㄲㅂ",
            "ㄲㅃ=ㄲㅃ",
            "ㄲㅈ=ㄲㅈ",
            "ㄲㅉ=ㄲㅉ",
            "ㄲㅅ=ㄲㅅ",
            "ㄲㅆ=ㄲㅆ",
            "ㄲㅁ=ㄲㅁ",
            "ㄲㄴ=ㄲㄴ",
            "ㄲㄹ=ㄲㄹ",
            "ㄲㅋ=ㄲㅋ",
            "ㄲㅌ=ㄲㅌ",
            "ㄲㅊ=ㄲㅊ",
            "ㄲㅍ=ㄲㅍ",
            "ㄴㅂ=ㄴㅂ,ㄵㅂ,ㄶㅂ",
            "ㄴㅃ=ㄴㅃ,ㄵㅃ,ㄶㅃ",
            "ㄴㄷ=ㄴㄷ",
            "ㄴㄱ=ㄴㄱ,ㄵㄱ,ㄶㄱ",
            "ㄴㄲ=ㄴㄲ,ㄵㄲ,ㄶㄲ",
            "ㄴㅅ=ㄴㅅ,ㄵㅅ,ㄶㅅ",
            "ㄴㅆ=ㄴㅆ,ㄵㅆ,ㄶㅆ",
            "ㄴㅎ=ㄴㅎ,ㄶㅎ",
            "ㄴㅋ=ㄴㅋ,ㄵㅋ,ㄶㅋ",
            "ㄴㅍ=ㄴㅍ,ㄵㅍ,ㄶㅍ",
            "ㄷㄹ=ㄷㄹ",
            "ㄷㅋ=ㄷㅋ",
            "ㄷㅌ=ㄷㅌ",
            "ㄷㅊ=ㄷㅊ",
            "ㄷㅍ=ㄷㅍ",
            "ㄹㅃ=ㄹㅃ,ㄺㅃ,ㄻㅃ,ㄼㅃ,ㄽㅃ,ㄾㅃ,ㄿㅃ,ㅀㅃ",
            "ㄹㅈ=ㄹㅈ,ㄺㅈ,ㄻㅈ,ㄼㅈ,ㄽㅈ,ㄾㅈ,ㄿㅈ,ㅀㅈ",
            "ㄹㅉ=ㄹㅉ,ㄺㅉ,ㄻㅉ,ㄼㅉ,ㄽㅉ,ㄾㅉ,ㄿㅉ,ㅀㅉ",
            "ㄹㄷ=ㄹㄷ",
            "ㄹㄲ=ㄹㄲ,ㄺㄲ,ㄻㄲ,ㄽㄲ,ㄾㄲ,ㄿㄲ,ㅀㄲ",
            "ㄹㄴ=ㄹㄴ,ㄺㄴ,ㄻㄴ,ㄼㄴ,ㄽㄴ,ㄾㄴ,ㄿㄴ,ㅀㄴ",
            "ㄹㅎ=ㄹㅎ,ㄺㅎ,ㄻㅎ,ㄼㅎ,ㄽㅎ,ㄾㅎ,ㄿㅎ,ㅀㅎ",
            "ㄹㅋ=ㄹㅋ,ㄺㅋ,ㄻㅋ,ㄽㅋ,ㄾㅋ,ㄿㅋ,ㅀㅋ",
            "ㄹㅊ=ㄹㅊ,ㄺㅊ,ㄻㅊ,ㄼㅊ,ㄽㅊ,ㄾㅊ,ㄿㅊ,ㅀㅊ",
            "ㅁㅂ=ㅁㅂ",
            "ㅁㅃ=ㅁㅃ",
            "ㅁㅈ=ㅁㅈ",
            "ㅁㅉ=ㅁㅉ",
            "ㅁㄷ=ㅁㄷ",
            "ㅁㅅ=ㅁㅅ",
            "ㅁㅆ=ㅁㅆ",
            "ㅁㅋ=ㅁㅋ",
            "ㅁㅌ=ㅁㅌ",
            "ㅁㅊ=ㅁㅊ",
            "ㅁㅍ=ㅁㅍ",
            "ㅂㅋ=ㅂㅋ,ㅄㅋ",
            "ㅂㅌ=ㅂㅌ,ㅄㅌ",
            "ㅂㅊ=ㅂㅊ,ㅄㅊ",
            "ㅂㅍ=ㅂㅍ,ㅄㅍ",
            "ㅅㅁ=ㅅㅁ,ㅆㅁ",
            "ㅅㄴ=ㅅㄴ,ㅆㄴ",
            "ㅅㄹ=ㅅㄹ,ㅆㄹ",
            "ㅅㅋ=ㅅㅋ,ㅆㅋ",
            "ㅅㅌ=ㅅㅌ,ㅆㅌ",
            "ㅅㅊ=ㅅㅊ,ㅆㅊ",
            "ㅅㅍ=ㅅㅍ,ㅆㅍ",
            "ㅅㅎ=ㅅㅎ,ㅆㅎ",
            "ㅇㅂ=ㅇㅂ",
            "ㅇㅃ=ㅇㅃ",
            "ㅇㅈ=ㅇㅈ",
            "ㅇㅉ=ㅇㅉ",
            "ㅇㄷ=ㅇㄷ",
            "ㅇㄸ=ㅇㄸ",
            "ㅇㄲ=ㅇㄲ",
            "ㅇㅅ=ㅇㅅ",
            "ㅇㅆ=ㅇㅆ",
            "ㅇㅁ=ㅇㅁ",
            "ㅇㅇ=ㅇㅇ",
            "ㅇㅎ=ㅇㅎ",
            "ㅇㅋ=ㅇㅋ",
            "ㅇㅌ=ㅇㅌ",
            "ㅇㅊ=ㅇㅊ",
            "ㅇㅍ=ㅇㅍ",
            "ㅈㅁ=ㅈㅁ",
            "ㅈㄴ=ㅈㄴ",
            "ㅈㄹ=ㅈㄹ",
            "ㅈㅋ=ㅈㅋ",
            "ㅈㅌ=ㅈㅌ",
            "ㅈㅊ=ㅈㅊ",
            "ㅈㅍ=ㅈㅍ",
            "ㅊㅃ=ㅊㅃ",
            "ㅊㅂ=ㅊㅂ",
            "ㅊㅉ=ㅊㅉ",
            "ㅊㅈ=ㅊㅈ",
            "ㅊㄸ=ㅊㄸ",
            "ㅊㄷ=ㅊㄷ",
            "ㅊㄲ=ㅊㄲ",
            "ㅊㄱ=ㅊㄱ",
            "ㅊㅆ=ㅊㅆ",
            "ㅊㅅ=ㅊㅅ",
            "ㅊㅁ=ㅊㅁ",
            "ㅊㄴ=ㅊㄴ",
            "ㅊㄹ=ㅊㄹ",
            "ㅊㅋ=ㅊㅋ",
            "ㅊㅌ=ㅊㅌ",
            "ㅊㅊ=ㅊㅊ",
            "ㅊㅍ=ㅊㅍ",
            "ㅋㅃ=ㅋㅃ",
            "ㅋㅂ=ㅋㅂ",
            "ㅋㅉ=ㅋㅉ",
            "ㅋㅈ=ㅋㅈ",
            "ㅋㄸ=ㅋㄸ",
            "ㅋㄷ=ㅋㄷ",
            "ㅋㄲ=ㅋㄲ",
            "ㅋㄱ=ㅋㄱ",
            "ㅋㅁ=ㅋㅁ",
            "ㅋㄴ=ㅋㄴ",
            "ㅋㄹ=ㅋㄹ",
            "ㅋㅋ=ㅋㅋ",
            "ㅋㅌ=ㅋㅌ",
            "ㅋㅊ=ㅋㅊ",
            "ㅋㅍ=ㅋㅍ",
            "ㅌㅃ=ㅌㅃ",
            "ㅌㅂ=ㅌㅂ",
            "ㅌㅉ=ㅌㅉ",
            "ㅌㅈ=ㅌㅈ",
            "ㅌㄸ=ㅌㄸ",
            "ㅌㄷ=ㅌㄷ",
            "ㅌㄲ=ㅌㄲ",
            "ㅌㄱ=ㅌㄱ",
            "ㅌㅆ=ㅌㅆ",
            "ㅌㅅ=ㅌㅅ",
            "ㅌㅁ=ㅌㅁ",
            "ㅌㄴ=ㅌㄴ",
            "ㅌㄹ=ㅌㄹ",
            "ㅌㅋ=ㅌㅋ",
            "ㅌㅌ=ㅌㅌ",
            "ㅌㅊ=ㅌㅊ",
            "ㅌㅍ=ㅌㅍ",
            "ㅍㅃ=ㅍㅃ",
            "ㅍㅂ=ㅍㅂ",
            "ㅍㅉ=ㅍㅉ",
            "ㅍㅈ=ㅍㅈ",
            "ㅍㄸ=ㅍㄸ",
            "ㅍㄷ=ㅍㄷ",
            "ㅍㄲ=ㅍㄲ",
            "ㅍㄱ=ㅍㄱ",
            "ㅍㅆ=ㅍㅆ",
            "ㅍㅅ=ㅍㅅ",
            "ㅍㅁ=ㅍㅁ",
            "ㅍㄴ=ㅍㄴ",
            "ㅍㄹ=ㅍㄹ",
            "ㅍㅋ=ㅍㅋ",
            "ㅍㅌ=ㅍㅌ",
            "ㅍㅊ=ㅍㅊ",
            "ㅍㅍ=ㅍㅍ",
            "ㅎㅃ=ㅎㅃ",
            "ㅎㅂ=ㅎㅂ",
            "ㅎㅉ=ㅎㅉ",
            "ㅎㅈ=ㅎㅈ",
            "ㅎㄸ=ㅎㄸ",
            "ㅎㄷ=ㅎㄷ",
            "ㅎㄲ=ㅎㄲ",
            "ㅎㄱ=ㅎㄱ",
            "ㅎㅆ=ㅎㅆ",
            "ㅎㅅ=ㅎㅅ",
            "ㅎㅁ=ㅎㅁ",
            "ㅎㄴ=ㅎㄴ",
            "ㅎㄹ=ㅎㄹ",
            "ㅎㅎ=ㅎㅎ",
            "ㅎㅋ=ㅎㅋ",
            "ㅎㅌ=ㅎㅌ",
            "ㅎㅊ=ㅎㅊ",
            "ㅎㅍ=ㅎㅍ",
        };


        static readonly Dictionary<string, string> initialConsonantLookup;
        static readonly Dictionary<string, string> vowelLookup;
        static readonly Dictionary<string, string> subsequentVowelsLookup;
        static readonly Dictionary<string, string> lastConsonantsLookup;
        static readonly Dictionary<string, string> ruleOfConsonantsLookup;


        static KoreanCVVCStandardPronunciationPhonemizer() {
            initialConsonantLookup = initialConsonants.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
            vowelLookup = vowels.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
            subsequentVowelsLookup = subsequentVowels.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
            lastConsonantsLookup = lastConsonants.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
            ruleOfConsonantsLookup = ruleOfConsonants.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);

        }

        // Store singer in field, will try reading presamp.ini later
        private USinger singer;
        public override void SetSinger(USinger singer) => this.singer = singer;
        
        // Legacy mapping. Might adjust later to new mapping style.
		public override bool LegacyMapping => true;

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var prevLyric = prevNeighbour?.lyric;
            char[] prevKoreanLyrics = { '　', '　', '　' };
            bool isPrevEndV = true;
            if (prevLyric != null && prevLyric[0] >= '가' && prevLyric[0] <= '힣') {
                prevKoreanLyrics = SeparateHangul(prevLyric != null ? prevLyric[0] : '\0');
                isPrevEndV = prevKoreanLyrics[2] == '　' && prevKoreanLyrics[0] != '　';
            }

            var currentLyric = notes[0].lyric;
            if (!(currentLyric[0] >= '가' && currentLyric[0] <= '힣')) {
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme {
                            phoneme = $"{currentLyric}",
                        }
                    },
                };
            }
            var currentKoreanLyrics = SeparateHangul(currentLyric[0]);
            var isCurrentEndV = currentKoreanLyrics[2] == '　' && currentKoreanLyrics[0] != '　';

            var nextLyric = nextNeighbour?.lyric;
            char[] nextKoreanLyrics = { '　', '　', '　' };
            if (nextLyric != null && nextLyric[0] >= '가' && nextLyric[0] <= '힣') {
                nextKoreanLyrics = SeparateHangul(nextLyric != null ? nextLyric[0] : '\0');
            }

            int totalDuration = notes.Sum(n => n.duration);
            int vcLength = 60;

            string CV = "";
            if (prevNeighbour != null) {
                // 앞문자 존재
                if (!isPrevEndV) {
                    // 앞문자 종결이 C
                    ruleOfConsonantsLookup.TryGetValue(prevKoreanLyrics[2].ToString() + currentKoreanLyrics[0].ToString(), out var CCConsonants);
                    vowelLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentVowel);
                    initialConsonantLookup.TryGetValue(CCConsonants == null ? currentKoreanLyrics[0].ToString() : CCConsonants[1].ToString(), out var changedCurrentConsonants);
                    CV = $"{changedCurrentConsonants}{currentVowel}";

                } else {
                    // 앞문자 종결이 V
                    initialConsonantLookup.TryGetValue(currentKoreanLyrics[0].ToString(), out var currentInitialConsonants);
                    vowelLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentVowel);

                    CV = $"{currentInitialConsonants}{currentVowel}";
                }
            } else {
                // 앞문자 없음
                initialConsonantLookup.TryGetValue(currentKoreanLyrics[0].ToString(), out var currentInitialConsonants);
                vowelLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentVowel);

                CV = $"{currentInitialConsonants}{currentVowel}";
            }
            //System.Diagnostics.Debug.WriteLine(CV);


            string VC = "";
            if (isCurrentEndV) {
                // 이번 문자 종결이 CV
                if (nextLyric == null || !(nextLyric[0] >= '가' && nextLyric[0] <= '힣')) {
                    // 다음 문자가 없는 경우
                } else {
                    // 다음 문자가 있는 경우(V + C or V)
                    subsequentVowelsLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentVowel);
                    initialConsonantLookup.TryGetValue(nextKoreanLyrics[0].ToString(), out var nextInitialConsonants);
                    if (nextInitialConsonants == "") {
                        // VV인 경우
                        vowelLookup.TryGetValue(nextKoreanLyrics[1].ToString(), out var nextVowel);
                        // VC = $"{currentVowel} {nextVowel}";
                    } else {
                        // VC인 경우
                        VC = $"{currentVowel} {nextInitialConsonants}";
                    }
                }
            } else {
                // 이번 문자 종결이 CVC

                subsequentVowelsLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentVowels);
                if (nextLyric == null || !(nextLyric[0] >= '가' && nextLyric[0] <= '힣')) {
                    // 다음 문자가 없는 경우
                    lastConsonantsLookup.TryGetValue(currentKoreanLyrics[2].ToString(), out var lastConsonants);
                    VC = $"{currentVowels} {lastConsonants}";
                } else {
                    // 다음 문자가 있는 경우(C + C or V)
                    ruleOfConsonantsLookup.TryGetValue(currentKoreanLyrics[2].ToString() + nextKoreanLyrics[0].ToString(), out var ruleVC);
                    if (ruleVC[0] == '　') {
                        // 현재 노트가 CVC에서 CV로 바뀌는 경우
                        subsequentVowelsLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentVowel);
                        initialConsonantLookup.TryGetValue(ruleVC[1].ToString(), out var nextInitialConsonants);
                        VC = $"{currentVowel} {nextInitialConsonants}";
                    } else {
                        // 현재 노트가 CVC가 유지되는 경우
                        lastConsonantsLookup.TryGetValue(ruleVC[0].ToString(), out var lastConsonants);
                        VC = $"{currentVowels} {lastConsonants}";
                    }
                }
            }

            if (VC == "") {
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme {
                            phoneme = CV,
                        },
                    },
                };
            }
            return new Result {
                phonemes = new Phoneme[] {
                    new Phoneme {
                        phoneme = CV,
                    },
                    new Phoneme {
                        phoneme = VC,
                        position = totalDuration - vcLength,
                    },
                },
            };
        }
    }
}
