using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {

  /// <summary>
  /// Phonemizer
  /// </summary>
  [Phonemizer("Chinese CMPX Phonemizer", "ZH CMPX", "2xxbin", language: "ZH")]
  public class ChineseCMPXPhonemizer : BaseChinesePhonemizer {
    private USinger? singer;

    private readonly static string[] consonant = new string[] {
      "ch", "zh", "sh", "b", "p", "m", "f", "d", "t", "n", "l", "z", "c", "s", "r", "j", "q", "x", "g", "k", "h"
    };

    private readonly static string[] longVCTimingConsonants = new string[] { "ch", "p","f", "t", "c", "q", "k" };
    private readonly static string[] shortVCTimingConsonants = new string[] { "z", "c", "s", "sh", "f", "x", "s" };

    private readonly static string[] frontSemiVowels = new string[] { "Y", "W", "V" };

    private readonly static Dictionary<string, string[]> vowelPhonemes = new Dictionary<string, string[]> {
      { "a", new string[] { "a" } },
      { "o", new string[] { "o" } },
      { "e", new string[] { "e" } },
      { "i", new string[] { "i" } },
      { "u", new string[] { "u" } },
      { "v", new string[] { "v" } },
      { "er", new string[] { "er" } },
      { "ii", new string[] { "ii" } },
      { "oo", new string[] { "oo" } },
      { "ee", new string[] { "ee" } },
      { "ai", new string[] { "a", ":i" } },
      { "ei", new string[] { "ee", ":i" } },
      { "ao", new string[] { "a", ":o" } },
      { "ou", new string[] { "oo", ":u" } },
      { "ong", new string[] { "oo", ":ng" } },
      { "an", new string[] { "a", ":n" } },
      { "en", new string[] { "e", ":n" } },
      { "ang", new string[] { "a", ":ng" } },
      { "eng", new string[] { "e", ":ng" } },
      { "ia", new string[] { "Y", "a" } },
      { "iao", new string[] { "Y", "a", ":o" } },
      { "ie", new string[] { "Y", "ee" } },
      { "iou", new string[] { "Y", "oo", ":u" } },
      { "ian", new string[] { "Y", "ee", ":n" } },
      { "in", new string[] { "i", ":n" } },
      { "iang", new string[] { "Y", "a", ":ng" } },
      { "ing", new string[] { "i", ":ng" } },
      { "iong", new string[] { "Y", "oo", ":ng" } },
      { "ua", new string[] { "W", "a" } },
      { "uo", new string[] { "W", "oo" } },
      { "uai", new string[] { "W", "a", ":i" } },
      { "uei", new string[] { "W", "ee", ":i" } },
      { "uan", new string[] { "W", "a", ":n" } },
      { "uen", new string[] { "W", "e", ":n" } },
      { "uang", new string[] { "W", "a", ":ng" } },
      { "ueng", new string[] { "W", "e", ":ng" } },
      { "ve", new string[] { "V", "ee" } },
      { "van", new string[] { "V", "ee", ":n" } },
      { "vn", new string[] { "v", ":n" } },
    };

    public readonly static string[] iiVowelConsonants = new string[] { "zh", "ch", "sh", "z", "c", "s" };
    public readonly static string[] initalLiquidConsonants = new string[] { "m", "n", "l", "r" };
    public readonly static string[] initalSibilantConsonants = new string[] { "f", "z", "s", "zh", "ch", "sh", "x" };
    public readonly static string[] endBreaths = new string[] { "R", "-" };
    public readonly static string[] initalCV = new string[] { "h" };

    public readonly static int frontSemiVowelTiming = 50;
    
    public readonly static int initalLiquidCTiming = 25;
    public readonly static int initalSibilantCTiming = 100;

    public override void SetSinger(USinger singer) {
      if (singer == null) return;
      
      this.singer = singer;
    }
  
    static string GetOtoAlias(USinger singer, string phoneme, Note note) {
      var attr = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
      string color = attr.voiceColor ?? string.Empty;
      int? alt = attr.alternate;
      var toneShift = attr.toneShift;

      if (singer.TryGetMappedOto(phoneme + alt, note.tone + toneShift, color, out var otoAlt)) {
        return otoAlt.Alias;
      } else if (singer.TryGetMappedOto(phoneme, note.tone + toneShift, color, out var oto)) {
        return oto.Alias;
      } else if (singer.TryGetMappedOto(phoneme, note.tone, color, out oto)) {
        return oto.Alias;
      }
      return phoneme;
    }

    public string GetPinyinVowel(string pinyin) {
      switch (pinyin) {
        case "a": return "a";
        case "o": return "o";
        case "e": return "e";
        case "yi": return "i";
        case "wu": return "u";
        case "yu": return "v";
        case "er": return "er";
      }

      if (pinyin.StartsWith("yi")) return pinyin.Substring(1);

      if (pinyin.StartsWith("yu")) return "v" + pinyin.Substring(2);
      if (pinyin.StartsWith("y")) return "i" + pinyin.Substring(1);
      if (pinyin.StartsWith("w")) return "u" + pinyin.Substring(1);
      
      if (pinyin.Contains("i") && iiVowelConsonants.Contains(pinyin.Substring(0, 2))) return "ii";
      
      if (pinyin.EndsWith("iu")) return "iou";
      if (pinyin.EndsWith("ui")) return "uei";
      if (pinyin.EndsWith("un")) return "uen";

      foreach (string c in consonant) {
        if (pinyin.StartsWith(c)) {
          return pinyin.Substring(c.Length);
        }
      }
      
      return pinyin;
    }
    
    private string getPinyinConsonant(string pinyin) {
      foreach (string c in consonant) {
        if (pinyin.StartsWith(c)) {
          return c;
        }
      }
      return "";
    }
    
    private int getVCtiming(string pinyin, int duration) {
      string consonant = getPinyinConsonant(pinyin);
      int timing = 60;

      if (longVCTimingConsonants.Contains(consonant)) timing = duration / 2;
      if (shortVCTimingConsonants.Contains(consonant)) timing = duration / 3;

      return timing;
    }

    public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs) {
      List<Phoneme> resultPhonemes = new List<Phoneme>();
      int totalDuration = notes.Sum(n => n.duration);
      Note note = notes[0];
      
      try {

        if (endBreaths.Contains(note.lyric) && prev != null) {
          Note prevNote = (Note) prev;
          string prevPhoneme = vowelPhonemes[GetPinyinVowel(prevNote.lyric)][^1];

          return new Result { phonemes = new Phoneme[] {
            new Phoneme {
              phoneme = GetOtoAlias(singer, prevPhoneme + " " + note.lyric, note),
            }
          }};
        }

        // If a phonetic hint exists.
        if (notes[0].phoneticHint != null) {
            // Phonetic hints are separated by commas.
          var phoneticHints = notes[0].phoneticHint.Split(",");
          var phonemes = new Phoneme[phoneticHints.Length];

          foreach (var phoneticHint in phoneticHints.Select((hint, index) => (hint, index))) {
            phonemes[phoneticHint.index] = new Phoneme {
              phoneme = GetOtoAlias(singer, phoneticHint.hint.Trim(), notes[0]) ,
              // The position is evenly divided into n parts.
              position = totalDuration - ((totalDuration / phoneticHints.Length) * (phoneticHints.Length - phoneticHint.index)),
            };
          }

          return new Result {
            phonemes = phonemes,
          };
        }

        string consonant = getPinyinConsonant(note.lyric);
        string vowel = GetPinyinVowel(note.lyric);
        List<string> vowelPhoneme = vowelPhonemes[vowel].ToList();

        if (prev == null || (prev != null && endBreaths.Contains(prev?.lyric))) {
          if (initalLiquidConsonants.Contains(consonant) || initalSibilantConsonants.Contains(consonant)) { // "- consonant" (- m)
            Debug.WriteLine("- C");
            int initalCTiming = initalLiquidConsonants.Contains(consonant) ? -initalLiquidCTiming : -initalSibilantCTiming;

            resultPhonemes.Add(new Phoneme() { phoneme = GetOtoAlias(singer, $"- {consonant}", note), position = initalCTiming });
          } else if (frontSemiVowels.Contains(vowelPhoneme[0]) && consonant == "") { // "- semivowel" (- Y)
            Debug.WriteLine("- semivowel");
            resultPhonemes.Add(new Phoneme() { phoneme = GetOtoAlias(singer, $"- {vowelPhoneme[0]}", note), position = -frontSemiVowelTiming });
          } else if (initalCV.Contains(consonant)) { // "- CV" (- h)
            Debug.WriteLine("- CV");
            resultPhonemes.Add(new Phoneme() { phoneme = GetOtoAlias(singer, $"- {consonant}{vowelPhoneme[0]}", note), position = 0 });
            vowelPhoneme.RemoveAt(0);
          }
        }
        
        // CV
        if (consonant != "") {
          resultPhonemes.Add(new Phoneme() { phoneme = GetOtoAlias(singer, consonant + vowelPhoneme[0], note), position = 0 });
        } else if (!frontSemiVowels.Contains(vowelPhoneme[0])) { // VV
          string beforeVowel = prev != null ? vowelPhonemes[GetPinyinVowel(prev?.lyric)][^1] : "-";
          string phoneme = vowelPhoneme[0];
          if (phoneme.StartsWith("i")) phoneme = "yi" + phoneme.Substring(1);
          if (phoneme == "u") phoneme = "wu";
          if (phoneme == "v") phoneme = "yu";

          resultPhonemes.Add(new Phoneme() { phoneme = GetOtoAlias(singer, beforeVowel + " " + phoneme, note), position = 0 });
        }

        // VC 타이밍 계산
        Phoneme? VCPhoneme = null;
        int VCtiming = 0;

        if (next != null && !endBreaths.Contains(next?.lyric)) {
          Note nextNote = (Note) next;
          string nextLyric = nextNote.lyric;
          string nextSemiVowel = vowelPhonemes[GetPinyinVowel(nextLyric)][0];
          string nextConsonant = getPinyinConsonant(nextLyric);

          if (frontSemiVowels.Contains(nextSemiVowel) && nextConsonant == "") {
            nextConsonant = nextSemiVowel;
            VCtiming = frontSemiVowelTiming;
          } else if (nextConsonant != "") {
            VCtiming = getVCtiming(nextNote.lyric, totalDuration);
          }

          if (VCtiming != 0) {
            VCPhoneme = new Phoneme() { 
              phoneme = GetOtoAlias(singer, vowelPhoneme[^1] + " " + nextConsonant, note), 
              position = totalDuration - VCtiming 
            };
          }
        }

        // middle vowel
        if (frontSemiVowels.Contains(vowelPhoneme[0])) 
          resultPhonemes.Add(new Phoneme() { phoneme = GetOtoAlias(singer, vowelPhoneme[0] + " " + vowelPhoneme[1], note), position = consonant == "" ? 0 : frontSemiVowelTiming });

        // back semivowel
        if (vowelPhoneme[^1].StartsWith(":")) {
          string backSemiVowel = vowelPhoneme[^1];
          resultPhonemes.Add(new Phoneme() { phoneme = GetOtoAlias(singer, vowelPhoneme[^2] + " " + backSemiVowel, note), position = totalDuration - ((totalDuration / 3) + VCtiming) });
        }

        // VC 추가
        if (VCPhoneme != null) resultPhonemes.Add((Phoneme) VCPhoneme);


      } catch (Exception e) {
        Console.WriteLine("ERROR: " + e);
        resultPhonemes.Add(new Phoneme() { phoneme = "ERROR" });
      }

      return new Result { phonemes = resultPhonemes.ToArray() };
    }
  }
}
