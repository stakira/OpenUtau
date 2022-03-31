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
        static readonly string[] specialConsonantsTable = { "gg", "dd", "bb", "ss", "jj", "t", "k", "ch", "p" };
        static readonly string[] makeVowelconsonantsTable = { "g", "d", "b", "n", "s", "h", "m", "r" };
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

        private bool CheckSpecialCC(string consonants) {
            return specialConsonantsTable.Contains(consonants);
        }

        private bool CheckMakeVowelCC(string consonants) {
            return makeVowelconsonantsTable.Contains(consonants);
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
            "k=ㄱ,ㅋ,ㄲ,ㄳ,ㄺ",
            "n=ㄴ,ㄵ,ㄶ",
            "t=ㄷ,ㅈ,ㅊ,ㅌ,ㅎ,ㄸ,ㅅ",
            "s=ㅆ",
            "l=ㄹ,ㄼ,l",
            "rl=ㄽ,ㄾ,ㄿ,ㅀ,0",
            "m=ㅁ,ㄻ",
            "p=ㅂ,ㅍ,ㅄ,ㅃ",
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
            "ㄱㅆ=ㄱㅆ,ㄱㅅ,ㄳㅇ",
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
            "ㅂㅆ=ㅂㅆ,ㄼㅅ,ㄼㅆ,ㅂㅅ,ㅄㅆ,ㅄㅅ,ㅄㅇ",
            "ㅂㅉ=ㅂㅉ,ㅂㅈ,ㅄㅉ,ㅄㅈ",
            "ㅅㄲ=ㅅㄲ,ㅅㄱ,ㅆㄲ,ㅆㄱ",
            "ㅅㄸ=ㅅㄸ,ㅅㄷ,ㅆㄸ,ㅆㄷ",
            "ㅅㅃ=ㅅㅃ,ㅅㅂ,ㅆㅃ,ㅆㅂ",
            "ㅅㅆ=ㅅㅆ,ㅅㅅ,ㅆㅆ,ㅆㅅ,ㅆㅇ",
            "ㅅㅉ=ㅅㅉ,ㅅㅈ,ㅆㅉ,ㅆㅈ",
            "ㅈㄲ=ㅈㄲ,ㅈㄱ",
            "ㅈㄸ=ㅈㄸ,ㅈㄷ",
            "ㅈㅃ=ㅈㅃ,ㅈㅂ",
            "ㅈㅉ=ㅈㅉ,ㅈㅈ",
            "ㅈㅅ=ㅈㅆ,ㅈㅅ",

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
            "　ㄴ=ㄴㅇ",
            "ㄴㅈ=ㄴㅈ,ㄵㅇ",
            "　ㄹ=ㄹㅇ",
            "ㄹㄱ=ㄹㄱ,ㄺㅇ",
            "ㄹㅁ=ㄹㅁ,ㄻㅇ",
            "ㄹㅂ=ㄹㅂ,ㄼㅇ",
            "ㄹㅆ=ㄹㅅ,ㄽㅇ,ㄹㅆ",
            "ㄹㅍ=ㄹㅍ,ㄿㅇ,ㄺㅂ,ㄻㅂ,ㄼㅂ,ㄽㅂ,ㄾㅂ,ㄿㅂ,ㅀㅂ,ㄹㅃ,ㄺㅃ,ㄻㅃ,ㄼㅃ,ㄽㅃ,ㄾㅃ,ㄿㅃ,ㅀㅃ",
            "　ㅁ=ㅁㅇ",
            "　ㅂ=ㅂㅇ",
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
            "ㄲㅈ=ㄲㅈ",
            "ㄲㅉ=ㄲㅉ",
            "ㄲㅅ=ㄲㅅ,ㄲㅆ",
            "ㄲㅁ=ㄲㅁ",
            "ㄲㄴ=ㄲㄴ",
            "ㄲㄹ=ㄲㄹ",
            "ㄲㅋ=ㄲㅋ",
            "ㄲㅌ=ㄲㅌ",
            "ㄲㅊ=ㄲㅊ",
            "ㄲㅍ=ㄲㅍ,ㄲㅃ",
            "ㄴㅂ=ㄴㅂ,ㄵㅂ,ㄶㅂ",
            "ㄴㄷ=ㄴㄷ",
            "ㄴㄱ=ㄴㄱ,ㄵㄱ,ㄶㄱ",
            "ㄴㅆ=ㄴㅆ,ㄵㅆ,ㄶㅆ",
            "ㄴㅅ=ㄴㅅ,ㄵㅅ,ㄶㅅ",
            "ㄴㅎ=ㄴㅎ,ㄶㅎ",
            "ㄴㅋ=ㄴㅋ,ㄵㅋ,ㄶㅋ,ㄴㄲ,ㄵㄲ,ㄶㄲ",
            "ㄴㅃ=ㄴㅃ,ㄵㅃ,ㄶㅃ",
            "ㄴㅍ=ㄴㅍ,ㄵㅍ,ㄶㅍ,",
            "ㄷㄹ=ㄷㄹ",
            "ㄷㅋ=ㄷㅋ",
            "ㄷㅌ=ㄷㅌ",
            "ㄷㅊ=ㄷㅊ",
            "ㄷㅍ=ㄷㅍ",
            "ㄹㅈ=ㄹㅈ,ㄺㅈ,ㄻㅈ,ㄼㅈ,ㄽㅈ,ㄾㅈ,ㄿㅈ,ㅀㅈ",
            "ㄹㅉ=ㄹㅉ,ㄺㅉ,ㄻㅉ,ㄼㅉ,ㄽㅉ,ㄾㅉ,ㄿㅉ,ㅀㅉ",
            "ㄹㄷ=ㄹㄷ",
            "ㄹㄴ=ㄹㄴ,ㄺㄴ,ㄻㄴ,ㄼㄴ,ㄽㄴ,ㄾㄴ,ㄿㄴ,ㅀㄴ",
            "ㄹㅎ=ㄹㅎ,ㄺㅎ,ㄻㅎ,ㄼㅎ,ㄽㅎ,ㄾㅎ,ㄿㅎ,ㅀㅎ",
            "ㄹㅋ=ㄹㅋ,ㄺㅋ,ㄻㅋ,ㄽㅋ,ㄾㅋ,ㄿㅋ,ㅀㅋ,ㄹㄲ,ㄺㄲ,ㄻㄲ,ㄽㄲ,ㄾㄲ,ㄿㄲ,ㅀㄲ",
            "ㄹㅊ=ㄹㅊ,ㄺㅊ,ㄻㅊ,ㄼㅊ,ㄽㅊ,ㄾㅊ,ㄿㅊ,ㅀㅊ",
            "ㅁㄱ=ㅁㄱ",
            "ㅁㅂ=ㅁㅂ",
            "ㅁㅈ=ㅁㅈ",
            "ㅁㅉ=ㅁㅉ",
            "ㅁㄷ=ㅁㄷ",
            "ㅁㅅ=ㅁㅅ,ㅁㅆ",
            "ㅁㅋ=ㅁㅋ",
            "ㅁㅌ=ㅁㅌ",
            "ㅁㅊ=ㅁㅊ",
            "ㅁㅍ=ㅁㅍ,ㅁㅃ",
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
            "ㅇㅈ=ㅇㅈ",
            "ㅇㅉ=ㅇㅉ",
            "ㅇㄷ=ㅇㄷ",
            "ㅇㅅ=ㅇㅅ,ㅇㅆ",
            "ㅇㅁ=ㅇㅁ",
            "ㅇ1=ㅇㅇ",
            "ㅇㅎ=ㅇㅎ",
            "ㅇㄲ=ㅇㄲ",
            "ㅇㅋ=ㅇㅋ",
            "ㅇㅌ=ㅇㅌ,ㅇㄸ",
            "ㅇㅊ=ㅇㅊ",
            "ㅇㅃ=ㅇㅃ",
            "ㅇㅍ=ㅇㅍ",
            "ㅈㅁ=ㅈㅁ",
            "ㅈㄴ=ㅈㄴ",
            "ㅈㄹ=ㅈㄹ",
            "ㅈㅋ=ㅈㅋ",
            "ㅈㅌ=ㅈㅌ",
            "ㅈㅊ=ㅈㅊ",
            "ㅈㅍ=ㅈㅍ",
            "ㅊㅂ=ㅊㅂ",
            "ㅊㅉ=ㅊㅉ",
            "ㅊㅈ=ㅊㅈ",
            "ㅊㄷ=ㅊㄷ",
            "ㅊㄱ=ㅊㄱ",
            "ㅊㅅ=ㅊㅅ,ㅊㅆ",
            "ㅊㅁ=ㅊㅁ",
            "ㅊㄴ=ㅊㄴ",
            "ㅊㄹ=ㅊㄹ",
            "ㅊㅋ=ㅊㅋ,ㅊㄲ",
            "ㅊㅌ=ㅊㅌ,ㅊㄸ",
            "ㅊㅊ=ㅊㅊ",
            "ㅊㅍ=ㅊㅍ,ㅊㅃ",
            "ㅋㅂ=ㅋㅂ",
            "ㅋㅉ=ㅋㅉ",
            "ㅋㅈ=ㅋㅈ",
            "ㅋㄷ=ㅋㄷ",
            "ㅋㄱ=ㅋㄱ",
            "ㅋㅁ=ㅋㅁ",
            "ㅋㄴ=ㅋㄴ",
            "ㅋㄹ=ㅋㄹ",
            "ㅋㅋ=ㅋㅋ,ㅋㄲ",
            "ㅋㅌ=ㅋㅌ,ㅋㄸ",
            "ㅋㅊ=ㅋㅊ",
            "ㅋㅍ=ㅋㅍ,ㅋㅃ",
            "ㅌㅂ=ㅌㅂ",
            "ㅌㅉ=ㅌㅉ",
            "ㅌㅈ=ㅌㅈ",
            "ㅌㄷ=ㅌㄷ",
            "ㅌㄱ=ㅌㄱ",
            "ㅌㅅ=ㅌㅅ,ㅌㅆ",
            "ㅌㅁ=ㅌㅁ",
            "ㅌㄴ=ㅌㄴ",
            "ㅌㄹ=ㅌㄹ",
            "ㅌㅋ=ㅌㅋ,ㅌㄲ",
            "ㅌㅌ=ㅌㅌ,ㅌㄸ",
            "ㅌㅊ=ㅌㅊ",
            "ㅌㅍ=ㅌㅍ,ㅌㅃ",
            "ㅍㅂ=ㅍㅂ",
            "ㅍㅉ=ㅍㅉ",
            "ㅍㅈ=ㅍㅈ",
            "ㅍㄷ=ㅍㄷ",
            "ㅍㄱ=ㅍㄱ",
            "ㅍㅅ=ㅍㅅ,ㅍㅆ",
            "ㅍㅁ=ㅍㅁ",
            "ㅍㄴ=ㅍㄴ",
            "ㅍㄹ=ㅍㄹ",
            "ㅍㅋ=ㅍㅋ,ㅍㄲ",
            "ㅍㅌ=ㅍㅌ,ㅍㄸ",
            "ㅍㅊ=ㅍㅊ",
            "ㅍㅍ=ㅍㅍ,ㅍㅃ",
            "ㅎㅂ=ㅎㅂ",
            "ㅎㅉ=ㅎㅉ",
            "ㅎㅈ=ㅎㅈ",
            "ㅎㄷ=ㅎㄷ",
            "ㅎㄱ=ㅎㄱ",
            "ㅎㅅ=ㅎㅅ,ㅎㅆ",
            "ㅎㅁ=ㅎㅁ",
            "ㅎㄴ=ㅎㄴ",
            "ㅎㄹ=ㅎㄹ",
            "ㅎㅎ=ㅎㅎ",
            "ㅎㅋ=ㅎㅋ,ㅎㄲ",
            "ㅎㅌ=ㅎㅌ,ㅎㄸ",
            "ㅎㅊ=ㅎㅊ",
            "ㅎㅍ=ㅎㅍ,ㅎㅃ",
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
        
        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs) {
            var prevLyric = prevNeighbour?.lyric;
            char[] prevKoreanLyrics = { '　', '　', '　' };
            bool isPrevEndV = true;
            if (prevLyric != null && prevLyric[0] >= '가' && prevLyric[0] <= '힣') {
                prevKoreanLyrics = SeparateHangul(prevLyric != null ? prevLyric[0] : '\0');
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
                        if (CheckSpecialCC(changedCurrentConsonants)) {
                            CV = $"{changedCurrentConsonants}{currentVowel}";
                        } else {
                            CV = $"{changedPrevConsonants} {changedCurrentConsonants}{currentVowel}";
                        }
                            
                    }
                } else {
                    // 앞문자 종결이 V
                    subsequentVowelsLookup.TryGetValue(prevKoreanLyrics[1].ToString(), out var prevVowel);
                    initialConsonantLookup.TryGetValue(prevCCConsonants == null ? currentKoreanLyrics[0].ToString() : prevCCConsonants[1].ToString(), out var currentInitialConsonants);

                    if (CheckSpecialCC(currentInitialConsonants)) {
                        CV = $"- {currentInitialConsonants}{currentVowel}";
                    } else {
                        CV = $"{prevVowel} {currentInitialConsonants}{currentVowel}";
                    }
                }
            } else {
                // 앞문자 없음
                initialConsonantLookup.TryGetValue(currentKoreanLyrics[0].ToString(), out var currentInitialConsonants);

                CV = $"- {currentInitialConsonants}{currentVowel}";
            }

            string VC = "";
            subsequentVowelsLookup.TryGetValue(currentKoreanLyrics[1].ToString(), out var currentsubVowel);
            lastConsonantsLookup.TryGetValue(nextCCConsonants == null ? currentKoreanLyrics[2].ToString() : nextCCConsonants[0].ToString(), out var changedNextConsonants);
            lastConsonantsLookup.TryGetValue(nextCCConsonants == null ? nextKoreanLyrics[0].ToString() : nextCCConsonants[1].ToString(), out var nextInitialConsonants);
            if (!isCurrentEndV) {
                if (currentLyric.Length == 2) {
                    if (nextLyric == null) {
                        VC = $"{currentsubVowel}{changedNextConsonants} {currentLyric[1]}";
                    } else {
                        VC = $"{currentsubVowel}{changedNextConsonants} {currentLyric[1]}";
                    }
                } else {
                    if (CheckSpecialCC(nextInitialConsonants) || nextInitialConsonants == null) {
                        if (CheckSpecialCC(changedNextConsonants) || nextInitialConsonants == null) {
                            VC = $"{currentsubVowel} {changedNextConsonants}";
                        } else {
                            VC = $"{currentsubVowel}{changedNextConsonants} {nextInitialConsonants}";
                        }
                    } else {
                        VC = $"{currentsubVowel} {changedNextConsonants}";
                    }
                }
                
            } else {
                if (nextInitialConsonants == null) {
                    if (currentLyric.Length == 2) {
                        VC = $"{currentsubVowel} {currentLyric[1]}";
                    } else {
                        VC = $"{currentsubVowel} -";
                    }
                } else {
                    if (CheckMakeVowelCC(nextInitialConsonants)) {
                        VC = $"{currentsubVowel} {nextInitialConsonants}";
                    }
                }
            }

            string suffix = "";
            if (currentLyric.Length == 2) {
                suffix = $"{currentVowel} {currentLyric[1]}";
            }


            if (VC == "") {
                if (suffix == "") {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme {
                                phoneme = CV,
                            },
                        }
                    };
                } else {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme {
                                phoneme = CV,
                            },
                            new Phoneme {
                                phoneme = suffix,
                                position = totalDuration - vcLength,
                            },
                        }
                    };
                }
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
                }
            };
        }

    }
}
