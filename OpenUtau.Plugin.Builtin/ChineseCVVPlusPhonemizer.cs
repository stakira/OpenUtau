using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
  [Serializable]
  public class ChineseCVVPlusConfigYaml {
    public string VowelTailPrefix = "_";
    public bool UseSingleNasalVowel = false;
    public bool UseSingleMultipleVowel = false;
    public string[] SupportedTailBreath = {","};
    public Dictionary<string, string> CustomTailVowel = new Dictionary<string, string>() {
      {"ian", "ian"},
    };
  }


  [Phonemizer("Chinese CVV Plus Phonemizer", "ZH CVV+", "2xxbin", language: "ZH")]
  public class ChineseCVVPlusPhonemizer : BaseChinesePhonemizer {
    static readonly String[] CONSONANTS = {"zh", "ch", "sh", "b", "p", "m", "f", "d", "t", "n", "l", "z", "c", "s", "r", "j", "q", "x", "g", "k", "h"};
    static readonly String[] SINGLE_VOWELS = {"a", "o", "e", "i", "u", "v", "er"};
    static readonly String[] MULTIPLE_VOWELS = {"ai", "ei", "ao", "ou", "ia", "iao", "ie", "iou", "ua", "uo", "uai", "uei", "ve"};
    static readonly String[] NESAL_VOWELS = {"an", "en", "ang", "eng", "ong", "ian", "iang", "ing", "iong", "uan", "uen", "uang", "ueng", "van", "vn"};
    List<String>? CombineVowels;
    List<String>? Vowels;
    static readonly Dictionary<String, String> DEFAULT_TAIL_VOWELS = new Dictionary<string, string>() {
      {"ai", "ai"},
      {"ei", "ei"},
      {"ao", "ao"},
      {"ou", "ou"},
      {"an", "an"},
      {"en", "en"},
      {"ang", "ang"},
      {"eng", "eng"},
      {"ong", "ong"},
      {"ia", "ia"},
      {"iao", "ao"},
      {"ie", "ie"},
      {"iu", "ou"},
      {"iou", "ou"},
      {"ian", "ian"},
      {"in", "in"},
      {"iang", "ang"},
      {"ing", "ing"},
      {"iong", "ong"},
      {"ua", "ua"},
      {"uo", "uo"},
      {"uai", "ai"},
      {"ui", "ei"},
      {"uei", "ei"},
      {"uan", "an"},
      {"un", "uen"},
      {"uang", "ang"},
      {"ueng", "eng"},
      {"ve", "ve"},
      {"van", "en"},
      {"vn", "vn"}
    };
    Dictionary<String, String> tailVowels;
    private USinger? singer;
    ChineseCVVPlusConfigYaml SettingYaml;
    public override void SetSinger(USinger singer) {
      if(singer == null) {
        return;
      }
      this.singer = singer;

      CombineVowels = MULTIPLE_VOWELS.Concat(NESAL_VOWELS).ToList();
      Vowels = SINGLE_VOWELS.Concat(CombineVowels).ToList();

      var configPath = Path.Join(singer.Location, "zhcvvplus.yaml");

      if(!File.Exists(configPath)) {
        Log.Information("Cannot Find zhcvvplus.yaml, creating a new one...");
        var defaultConfig = new ChineseCVVPlusConfigYaml {};
        var configContent = Yaml.DefaultDeserializer.Serialize(defaultConfig);
        File.WriteAllText(configPath, configContent);
        Log.Information("New zhcvvplus.yaml created with default settings.");
      }

      try {
        var configContent = File.ReadAllText(configPath);
        SettingYaml = Yaml.DefaultDeserializer.Deserialize<ChineseCVVPlusConfigYaml>(configContent);
      }catch (Exception e) {
        Log.Error(e, $"Failed to load zhcvvplus.yaml (configPath: '{configPath}')");
      }

      
      tailVowels = new Dictionary<string, string>(DEFAULT_TAIL_VOWELS);
      foreach (var customTailVowel in SettingYaml.CustomTailVowel) {
        tailVowels[customTailVowel.Key] = customTailVowel.Value;
      }

    }

    private string getLryicVowel(string lryic) {
      string prefix = lryic.Substring(0, Math.Min(2, lryic.Length));
      string suffix = lryic.Length > 2 ? lryic.Substring(2) : "";

      foreach (var consonant in CONSONANTS) {
          if (prefix.StartsWith(consonant)) {
              prefix = prefix.Replace(consonant, ""); 
          }
      }

      

      return (prefix + suffix).Replace("yu", "v").Replace("y", "i").Replace("w", "u").Trim();
    }

    public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
      try {
        int totalDuration = notes.Sum(n => n.duration);
        var phoneme = notes[0].lyric;
        var lryicVowel = getLryicVowel(notes[0].lyric);
        
        if (tailVowels.ContainsKey(lryicVowel)) {
          var tailPhoneme = $"{SettingYaml.VowelTailPrefix}{tailVowels[lryicVowel]}";
          
          if ((totalDuration <= 480 && (SettingYaml.UseSingleNasalVowel && NESAL_VOWELS.Contains(lryicVowel) ||
              SettingYaml.UseSingleMultipleVowel && MULTIPLE_VOWELS.Contains(lryicVowel)) ||
              (!SettingYaml.UseSingleNasalVowel && NESAL_VOWELS.Contains(lryicVowel)) ||
              (!SettingYaml.UseSingleMultipleVowel && MULTIPLE_VOWELS.Contains(lryicVowel)))) {
            return new Result() {
              phonemes = new Phoneme[] {
                new Phoneme { phoneme = phoneme },
                new Phoneme { phoneme = tailPhoneme, position = totalDuration - Math.Min(totalDuration / 3, 300) },
              }
            };
          }
        };

        return new Result {
          phonemes = new Phoneme[] {
            new Phoneme() {
              phoneme = phoneme,
            }
          }
        };
      } catch (Exception e) {
        Log.Error(e, "zh cvv+ error");
        return new Result {
          phonemes = new Phoneme[] {
            new Phoneme() {
              phoneme = "ERROR",
            }
          }
        };
      }
    }
  }
}