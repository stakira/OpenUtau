using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Korean CVCCV Phonemizer", "KO CVCCV", "RYUUSEI")]
    public class KoreanCVCCVPhonemizer : Phonemizer {
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
            "n=ㄴ",
            "d=ㄷ",
            "r=ㄹ",
            "l=l",
            "m=ㅁ",
            "b=ㅂ",
            "s=ㅅ",
            "=ㅇ,　",
            "j=ㅈ",
            "ch=ㅊ",
            "k=ㅋ,k",
            "t=ㅌ",
            "p=ㅍ",
            "h=ㅎ",
            "gg=ㄲ",
            "dd=ㄸ",
            "bb=ㅃ",
            "ss=ㅆ",
            "jj=ㅉ",
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
            "e=ㅔ,ㅐ",
            "ye=ㅖ,ㅒ",
            "eui=ㅢ",
            "wa=ㅘ",
            "weo=ㅝ",
            "we=ㅞ,ㅙ,ㅚ",
            "wi=ㅟ",
        };

        // V-V의 경우 이전 모음으로 대체
        static readonly string[] subsequentVowels = new string[] {
            "a=ㅏ,ㅑ,ㅘ",
            "eo=ㅓ,ㅕ,ㅝ",
            "o=ㅗ,ㅛ",
            "u=ㅜ,ㅠ",
            "eu=ㅡ",
            "e=ㅔ,ㅐ,ㅞ,ㅙ,ㅚ,ㅖ,ㅒ",
            "i=ㅣ,ㅢ,ㅟ",
        };

        // 끝소리일 경우에만 동작
        static readonly string[] lastConsonants = new string[] {
            "K=ㄱ,ㅋ,ㄲ,ㄳ,ㄺ",
            "n=ㄴ,ㄵ,ㄶ",
            "T=ㄷ,ㅅ,ㅈ,ㅊ,ㅌ,ㅆ,ㅎ",
            "l=ㄹ,ㄼ,l",
            "rl=ㄽ,ㄾ,ㄿ,ㅀ,0",
            "m=ㅁ,ㄻ",
            "P=ㅂ,ㅍ,ㅄ",
            "ng=ㅇ",
        };

        static readonly string[] subsequentLastConsonants = new string[] {
            "-=ㄱ,ㅋ,ㄲ,ㄳ,ㄺ,ㄷ,ㅅ,ㅈ,ㅊ,ㅌ,ㅆ,ㅎ,ㅂ,ㅍ,ㅄ,ㅇ",
            "n=ㄴ,ㄵ,ㄶ",
            "rl=ㄹ",
            "l=ㄼ,ㄽ,ㄾ,ㄿ,ㅀ,l",
            "m=ㅁ,ㄻ",
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
            "ll=ㄹㄹ,ㄴㄹ,ㄵㄹ",

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
            "ㄴㄷ=ㄴㄸ,ㄵㄷ,ㄵㄸ,ㄶㅌ,ㄶㄸ",
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
            "ㅅㅅ=ㅅㅆ,ㅅㅅ,ㅆㅆ,ㅆㅅ,ㅆㅇ",
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
            "ㅇ1=ㅇㅇ",
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
        static readonly Dictionary<string, string> subsequentLastConsonantsLookup;
        static readonly Dictionary<string, string> ruleOfConsonantsLookup;


        static KoreanCVCCVPhonemizer() {
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
            subsequentLastConsonantsLookup = subsequentLastConsonants.ToList()
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

        public override Phoneme[] Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour) {
            var prevLyric = prevNeighbour?.lyric;
            char[] prevKoreanLyrics = { '　', '　', '　' };
            bool isPrevEndV = true;
            if (prevLyric != null && prevLyric[0] >= '가' && prevLyric[0] <= '힣') {
                prevKoreanLyrics = SeparateHangul(prevLyric != null ? prevLyric[0] : '\0');
            }

            var currentLyric = notes[0].lyric;
            if (!(currentLyric[0] >= '가' && currentLyric[0] <= '힣')) {
                return new Phoneme[] {
                    new Phoneme {
                        phoneme = $"{currentLyric}",
                    }};
            }
            var currentKoreanLyrics = SeparateHangul(currentLyric[0]);

            var nextLyric = nextNeighbour?.lyric;
            char[] nextKoreanLyrics = { '　', '　', '　' };
            if (nextLyric != null && nextLyric[0] >= '가' && nextLyric[0] <= '힣') {
                nextKoreanLyrics = SeparateHangul(nextLyric != null ? nextLyric[0] : '\0');
            }

            ruleOfConsonantsLookup.TryGetValue(prevKoreanLyrics[2].ToString() + currentKoreanLyrics[0].ToString(), out var prevCCConsonants);
            ruleOfConsonantsLookup.TryGetValue(currentKoreanLyrics[2].ToString() + nextKoreanLyrics[0].ToString(), out var nextCCConsonants);

            isPrevEndV = (prevKoreanLyrics[2] == '　' && prevKoreanLyrics[0] != '　') || (prevCCConsonants != null && prevCCConsonants[0] == '　');
            var isCurrentEndV = (currentKoreanLyrics[2] == '　' && currentKoreanLyrics[0] != '　') || (nextCCConsonants != null && nextCCConsonants[0] == '　');

            int totalDuration = notes.Sum(n => n.duration);
            int vcLength = 60;

            string CV = "";
            vowelLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentVowel);
            if (prevNeighbour != null) {
                // 앞문자 존재
                if (!isPrevEndV) {
                    // 앞문자 종결이 C
                    initialConsonantLookup.TryGetValue(prevCCConsonants == null ? currentKoreanLyrics[0].ToString() : prevCCConsonants[1].ToString(), out var changedCurrentConsonants);
                    subsequentLastConsonantsLookup.TryGetValue(prevCCConsonants == null ? prevKoreanLyrics[3].ToString() : prevCCConsonants[0].ToString(), out var changedPrevConsonants);
                    if (prevCCConsonants == null) {
                        CV = $"- {changedCurrentConsonants}{currentVowel}";
                    } else {
                        CV = $"{changedPrevConsonants} {changedCurrentConsonants}{currentVowel}";
                    }
                } else {
                    // 앞문자 종결이 V
                    subsequentVowelsLookup.TryGetValue(prevKoreanLyrics[1].ToString(), out var prevVowel);
                    initialConsonantLookup.TryGetValue(prevCCConsonants == null ? currentKoreanLyrics[0].ToString() : prevCCConsonants[1].ToString(), out var currentInitialConsonants);
                    
                    CV = $"{prevVowel} {currentInitialConsonants}{currentVowel}";
                }
            } else {
                // 앞문자 없음
                initialConsonantLookup.TryGetValue(currentKoreanLyrics[0].ToString(), out var currentInitialConsonants);

                CV = $"- {currentInitialConsonants}{currentVowel}";
            }

            string VC = "";
            if (!isCurrentEndV) {
                subsequentVowelsLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentsubVowel);
                lastConsonantsLookup.TryGetValue(nextCCConsonants == null ? currentKoreanLyrics[2].ToString() : nextCCConsonants[0].ToString(), out var changedCurrentConsonants);

                if (nextLyric == null) {
                    VC = $"{currentsubVowel} {changedCurrentConsonants}";
                } else {
                    VC = $"{currentsubVowel} {changedCurrentConsonants}";
                }
            }

            string suffix = "";
            if (currentLyric.Length == 2) {
                suffix = $"{currentVowel} {currentLyric[1]}";
            }


            if (VC == "") {
                if (suffix == "") {
                    return new Phoneme[] {
                        new Phoneme {
                            phoneme = CV,
                        },
                    };
                } else {
                    return new Phoneme[] {
                        new Phoneme {
                            phoneme = CV,
                        },
                        new Phoneme {
                            phoneme = suffix,
                            position = totalDuration - vcLength,
                        },

                    };
                }
            }

            return new Phoneme[] {
                new Phoneme {
                    phoneme = CV,
                },
                new Phoneme {
                    phoneme = VC,
                    position = totalDuration - vcLength,
                },
            };
        }
    }
}
