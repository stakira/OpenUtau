using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
  [Serializable]
  public class ChineseCVVPlusConfigYaml {
    public string VowelTailPrefix = "_";
    public bool UseSingleNasalVowel = false;
    public bool UseSingleMultipleVowel = false;
    public bool UseRetan = false;
    public string[] SupportedTailBreath = {"-"};
    public string[] ConsonantDict = {"zh", "ch", "sh", "b", "p", "m", "f", "d", "t", "n", "l", "z", "c", "s", "r", "j", "q", "x", "g", "k", "h"};
    public string[] SingleVowelDict = {"a", "o", "e", "i", "u", "v", "er"}; 
    public string[] NesalVowelDict = {"an", "en", "ang", "eng", "ong", "ian", "iang", "ing", "iong", "uan", "uen", "un", "uang", "ueng", "van", "vn"};
    public string[] MultipleVowelDict = {"ai", "ei", "ao", "ou", "ia", "iao", "ie", "iou", "ua", "uo", "uai", "uei", "ui", "ve"};
    
    public int FastTailVowelTimingTick = 100;
    public int SingleVowelsReferenceTimimgTick = 480;
    public Dictionary<string, string> FastTailVowelDict = new Dictionary<string, string>() {
      {"ia", "ia"},
      {"ie", "ie"},
      {"ua", "ua"},
      {"uo", "uo"},
      {"ve", "ve"},
    };
    public Dictionary<string, string> SlowTailVowelDict = new Dictionary<string, string>() {
      {"ai", "ai"},
      {"ei", "ei"},
      {"ao", "ao"},
      {"ou", "ou"},
      {"an", "an"},
      {"en", "en"},
      {"ang", "ang"},
      {"eng", "eng"},
      {"ong", "ong"},
      {"iao", "ao"},
      {"iu", "ou"},
      {"iou", "ou"},
      {"ian", "ian"},
      {"in", "in"},
      {"iang", "ang"},
      {"ing", "ing"},
      {"iong", "ong"},
      {"uai", "ai"},
      {"ui", "ei"},
      {"uei", "ei"},
      {"uan", "an"},
      {"un", "uen"},
      {"uang", "ang"},
      {"ueng", "eng"},
      {"van", "en"},
      {"vn", "vn"},
    };
  }

  class FlowStyleIntegerSequences : ChainedEventEmitter {
      public FlowStyleIntegerSequences(IEventEmitter nextEmitter)
          : base(nextEmitter) {}

      public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter) {
        eventInfo = new SequenceStartEventInfo(eventInfo.Source) {
          Style = YamlDotNet.Core.Events.SequenceStyle.Flow
        };

        nextEmitter.Emit(eventInfo, emitter);
      }
  }


  [Phonemizer("Chinese CVV Plus Phonemizer", "ZH CVV+", "2xxbin", language: "ZH")]
  public class ChineseCVVPlusPhonemizer : BaseChinesePhonemizer {
    String[] Consonants;
    String[] SingleVowels;
    String[] MultipleVowels;
    String[] NesalVowels;
    String[] TailBreaths;
    Dictionary<String, String> FastTailVowels;
    Dictionary<String, String> SlowTailVowels;
    Dictionary<String, String> TailVowels;
    private USinger? singer;
    ChineseCVVPlusConfigYaml SettingYaml;
    public override void SetSinger(USinger singer) {

      if(singer == null) {
        return;
      }

      var configPath = Path.Join(singer.Location, "zhcvvplus.yaml");

      if(!File.Exists(configPath)) {
        Log.Information("Cannot Find zhcvvplus.yaml, creating a new one...");
        var serializer = new SerializerBuilder().WithEventEmitter(next => new FlowStyleIntegerSequences(next)).Build();
        var configContent = serializer.Serialize(new ChineseCVVPlusConfigYaml {});
        File.WriteAllText(configPath, configContent);
        Log.Information("New zhcvvplus.yaml created with default settings.");
      }

      try {
        var configContent = File.ReadAllText(configPath);
        var deserializer = new DeserializerBuilder().Build();
        SettingYaml = deserializer.Deserialize<ChineseCVVPlusConfigYaml>(configContent);
      }catch (Exception e) {
        Log.Error(e, $"Failed to load zhcvvplus.yaml (configPath: '{configPath}')");
      }

      Consonants = SettingYaml.ConsonantDict.OrderByDescending(c => c.Length).ToArray();
      SingleVowels = SettingYaml.SingleVowelDict;
      MultipleVowels = SettingYaml.MultipleVowelDict;
      NesalVowels = SettingYaml.NesalVowelDict;
      FastTailVowels = SettingYaml.FastTailVowelDict;
      SlowTailVowels = SettingYaml.SlowTailVowelDict;
      TailVowels = FastTailVowels.Concat(SlowTailVowels).ToDictionary(g => g.Key, g => g.Value);
      TailBreaths = SettingYaml.SupportedTailBreath;

      this.singer = singer;
    }

    private string getLryicVowel(string lryic) {
      string prefix = lryic.Substring(0, Math.Min(2, lryic.Length));
      string suffix = lryic.Length > 2 ? lryic.Substring(2) : "";

      foreach (var consonant in Consonants) {
          if (prefix.StartsWith(consonant)) {
              prefix = prefix.Replace(consonant, ""); 
          }
      }

      return (prefix + suffix).Replace("yu", "v").Replace("y", "i").Replace("w", "u").Trim();
    }

    public static bool isExistPhonemeInOto(USinger singer, string phoneme, Note note) {
      var color = string.Empty;
      var toneShift = 0;
      int? alt = null;
      if (phoneme.Equals("")) {
        return false;
      }

      if (singer.TryGetMappedOto(phoneme + alt, note.tone + toneShift, color, out var otoAlt)) {
        return true;
      } else if (singer.TryGetMappedOto(phoneme, note.tone + toneShift, color, out var oto)) {
        return true;
      } else if (singer.TryGetMappedOto(phoneme, note.tone, color, out oto)) {
        return true;
      }

      return false;
    }

    public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
      try {
        int totalDuration = notes.Sum(n => n.duration);
        var phoneme = notes[0].lyric;
        var lryicVowel = getLryicVowel(notes[0].lyric);

        if(notes[0].phoneticHint != null) {
          var phoneticHints = notes[0].phoneticHint.Split(",");
          var phonemes = new Phoneme[phoneticHints.Length];

          foreach(var phoneticHint in phoneticHints.Select((hint, index) => (hint, index))) {
            phonemes[phoneticHint.index] = new Phoneme {
              phoneme = phoneticHint.hint.Trim(),
              position = totalDuration - ((totalDuration / phoneticHints.Length) * (phoneticHints.Length - phoneticHint.index)),
            };
          }

          return new Result {
            phonemes = phonemes,
          };
        }

        if(TailBreaths.Contains(phoneme) && prev != null) {
          return new Result {
            phonemes = new Phoneme[] { new Phoneme { phoneme = $"{getLryicVowel(prev?.lyric)} {phoneme}" } }
          };
        }

        if (SettingYaml.UseRetan && prev == null && isExistPhonemeInOto(singer, $"- {phoneme}", notes[0])) {
          phoneme = $"- {phoneme}";
        }

        if (TailVowels.ContainsKey(lryicVowel)) {
          var tailPhoneme = $"{SettingYaml.VowelTailPrefix}{TailVowels[lryicVowel]}";
        
          if ((totalDuration <= SettingYaml.SingleVowelsReferenceTimimgTick && 
              (SettingYaml.UseSingleNasalVowel && NesalVowels.Contains(lryicVowel) || SettingYaml.UseSingleMultipleVowel && MultipleVowels.Contains(lryicVowel)) ||
              (!SettingYaml.UseSingleNasalVowel && NesalVowels.Contains(lryicVowel)) ||
              (!SettingYaml.UseSingleMultipleVowel && MultipleVowels.Contains(lryicVowel)))) {
              
            var tailVowelPosition = totalDuration - totalDuration / 3;
            if (FastTailVowels.ContainsKey(lryicVowel)) {
              tailVowelPosition = SettingYaml.FastTailVowelTimingTick;
            }

            return new Result() {
              phonemes = new Phoneme[] {
                new Phoneme { phoneme = phoneme },
                new Phoneme { phoneme = tailPhoneme, position = tailVowelPosition },
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