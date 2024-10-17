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
    /// <summary>
    /// zhcvvplus.yaml 클래스
    /// </summary> 
    [Serializable]
    public class ChineseCVVPlusConfigYaml {

        /// <summary>
        /// 접모음의 접두사 지정. 기본값 "_"
        /// </summary>
        public string VowelTailPrefix = "_";

        /// <summary>
        /// 일정 길이 이상의 비운모를 접모음 없이 사용할 건지에 대한 여부
        /// </summary>
        public bool UseSingleNasalVowel = false;

        /// <summary>
        /// 일정 길이 이상의 복합운모를 접모음 없이 사용할 건지에 대한 여부
        /// </summary>
        public bool UseSingleMultipleVowel = false;

        /// <summary>
        /// 연단음 여부. True로 설정시 첫번째 노트의 가사에 "- " 추가
        /// </summary>
        public bool UseRetan = false;

        /// <summary>
        /// 어미숨 종류. 여러개 사용 가능
        /// </summary>
        public string[] SupportedTailBreath = { "-" };

        /// <summary>
        /// 성모 지정. 추가 자음이 생길 상황을 대비해 커스텀 가능하게 yaml에 분리
        /// </summary>
        public string[] ConsonantDict = { "zh", "ch", "sh", "b", "p", "m", "f", "d", "t", "n", "l", "z", "c", "s", "r", "j", "q", "x", "g", "k", "h" };

        /// <summary>
        /// 운모 지정. 위와 동일한 이유로 yaml에 분리
        /// </summary>
        public string[] SingleVowelDict = { "a", "o", "e", "i", "u", "v", "er" };

        /// <summary>
        /// 비운모 지정. 위와 동일한 이유로 yaml에 분리
        /// </summary>
        public string[] NasalVowelDict = { "an", "en", "ang", "eng", "ong", "ian", "iang", "ing", "iong", "uan", "uen", "un", "uang", "ueng", "van", "vn" };

        /// <summary>
        /// 복합운모 지정. 위와 동일한 이유로 yaml에 분리
        /// </summary>
        public string[] MultipleVowelDict = { "ai", "ei", "ao", "ou", "ia", "iao", "ie", "iou", "ua", "uo", "uai", "uei", "ui", "ve" };

        /// <summary>
        /// 빠른 접모음의 위치 (tick 단위).
        /// </summary>
        public int FastTailVowelTimingTick = 100;
        /// <summary>
        /// UseSingleNasalVowel 혹은 UseSingleMultipleVowel 가 True 일때, 단독 사용의 판단 기준 (tick 단위)
        /// </summary>
        public int SingleVowelsReferenceTimimgTick = 480;
        /// <summary>
        /// 빠른 복합운모. 만일 빠른 복합운모의 비운모가 필요가 없다면 이부분은 비워두고 전부 SlowTailVowelDict 로 옮겨두면 됨
        /// </summary>
        public Dictionary<string, string> FastTailVowelDict = new Dictionary<string, string>() {
            {"ia", "ia"},
            {"ie", "ie"},
            {"ua", "ua"},
            {"uo", "uo"},
            {"ve", "ve"},
        };
        /// <summary>
        /// 느린 복합운모. 느린 복합운모의 포지션은 노트의 1/3로 계산 됨
        /// <br></br>
        /// {"모음의 기본형": "접모음의 접두사를 제외한 표기"}
        /// </summary>
        public Dictionary<string, string> SlowTailVowelDict = new Dictionary<string, string>()
        {
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


        [YamlIgnore]
        public Dictionary<string, string> TailVowels {
            get {
                return FastTailVowelDict.Concat(SlowTailVowelDict).ToDictionary(g => g.Key, g => g.Value);
            }
        }


        [YamlIgnore]
        public string[] Consonants {
            get {
                return ConsonantDict.OrderByDescending(c => c.Length).ToArray();
            }
        }
    }

    /// <summary>
    /// yaml을 직렬화 할 때, 배열을 인라인 스타일로 만들기 위한 커스텀 이벤트
    /// </summary>
    class FlowStyleIntegerSequences : ChainedEventEmitter {
        public FlowStyleIntegerSequences(IEventEmitter nextEmitter)
            : base(nextEmitter) { }

        public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter) {
            eventInfo = new SequenceStartEventInfo(eventInfo.Source) {
                Style = YamlDotNet.Core.Events.SequenceStyle.Flow
            };

            nextEmitter.Emit(eventInfo, emitter);
        }
    }


    /// <summary>
    /// 포네마이저
    /// </summary>
    [Phonemizer("Chinese CVV Plus Phonemizer", "ZH CVV+", "2xxbin", language: "ZH")]
    public class ChineseCVVPlusPhonemizer : BaseChinesePhonemizer {
        private USinger? singer;
        /// <summary>
        /// zhcvvplus.yaml를 담아두는 변수
        /// </summary>
        ChineseCVVPlusConfigYaml Config;
        public override void SetSinger(USinger singer) {

            if (singer == null) {
                return;
            }

            // zhcvvplus.yaml 경로 지정
            var configPath = Path.Join(singer.Location, "zhcvvplus.yaml");

            // 만약 없다면, 새로 제작해 추가
            if (!File.Exists(configPath)) {
                CreateConfigChineseCVVPlus(configPath);
            }

            // zhcvvplus.yaml 읽기
            try {
                var configContent = File.ReadAllText(configPath);
                var deserializer = new DeserializerBuilder().Build();
                Config = deserializer.Deserialize<ChineseCVVPlusConfigYaml>(configContent);
            } catch (Exception e) {
                Log.Error(e, $"Failed to load zhcvvplus.yaml (configPath: '{configPath}')");
                try {
                    CreateConfigChineseCVVPlus(configPath);
                } catch (Exception e2) {
                    Log.Error(e2, "Failed to create zhcvvplus.yaml");
                }
            }

            // 음원 지정
            this.singer = singer;

            if (Config == null) {
                Log.Error("Failed to load zhcvvplus.yaml, using default settings.");
                Config = new ChineseCVVPlusConfigYaml();
            }
        }

        // 노트의 가사를 받아 모음을 반환하는 메소드.
        private string GetLyricVowel(string lyric) {
            string initialPrefix = string.Empty;

            // Handle the case that first character is not an alphabet(e.g "- qian") - remove it until the first alphabet apears, otherwise GetLyricVowel will return its lyric as it is.
            while (!char.IsLetter(lyric.First())) {
                initialPrefix += lyric.First();
                lyric = lyric.Remove(0, 1);
                if (lyric.Length == 0) {
                    return lyric;
                }
            }

            // 자음 뿐만이 아닌 모음도 제거가 되는 문제(ian -> ia 등) 을 방지하기 위해 가사의 앞 2글자 분리
            string prefix = lyric.Substring(0, Math.Min(2, lyric.Length));
            string suffix = lyric.Length > 2 ? lyric.Substring(2) : string.Empty;

            // 자음 리스트를 순서대로 선회하며 replace 됨.
            foreach (var consonant in Config.Consonants) {
                if (prefix.StartsWith(consonant)) {
                    prefix = prefix.Replace(consonant, string.Empty);
                }
            }

            // 모음 표기를 일반 표기로 변경
            return $"{initialPrefix}{(prefix + suffix).Replace("yu", "v").Replace("y", "i").Replace("w", "u").Trim()}";
        }

        // oto.ini 안에 해당 에일리어스가 있는지 확인하는 메소드.
        public static bool isExistPhonemeInOto(USinger singer, string phoneme, Note note) {

            var attr = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
            string color = attr.voiceColor ?? string.Empty;

            var toneShift = 0;
            int? alt = null;
            if (phoneme.Equals(string.Empty)) {
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

        void CreateConfigChineseCVVPlus(string configPath) {
            Log.Information("Cannot Find zhcvvplus.yaml, creating a new one...");
            var serializer = new SerializerBuilder().WithEventEmitter(next => new FlowStyleIntegerSequences(next)).Build();
            var configContent = serializer.Serialize(new ChineseCVVPlusConfigYaml { });
            File.WriteAllText(configPath, configContent);
            Log.Information("New zhcvvplus.yaml created with default values.");
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            try {
                
                int totalDuration = notes.Sum(n => n.duration);
                string phoneme = notes[0].lyric;
                string? lryicVowel = GetLyricVowel(notes[0].lyric);

                // 만약 발음 힌트가 존재 한다면.
                if (notes[0].phoneticHint != null) {
                    // 발음 힌트는 쉼표 기준으로 분리 됨.
                    var phoneticHints = notes[0].phoneticHint.Split(",");
                    var phonemes = new Phoneme[phoneticHints.Length];

                    foreach (var phoneticHint in phoneticHints.Select((hint, index) => (hint, index))) {
                        phonemes[phoneticHint.index] = new Phoneme {
                            phoneme = GetOtoAlias(singer, phoneticHint.hint.Trim(), notes[0]) ,
                            // 포지션은 균등하게 n등분
                            position = totalDuration - ((totalDuration / phoneticHints.Length) * (phoneticHints.Length - phoneticHint.index)),
                        };
                    }

                    return new Result {
                        phonemes = phonemes,
                    };
                }

                // 만약 노트가 끝 어미숨 노트라면
                if (Config.SupportedTailBreath.Contains(phoneme) && prev != null) {
                    phoneme = GetOtoAlias(singer, $"{GetLyricVowel(prev?.lyric)} {phoneme}", notes[0]);
                    
                    return new Result {
                        // "모음의 기본 형태 + 가사로 작성한 어미숨" 형태로 출력
                        phonemes = new Phoneme[] { new Phoneme { phoneme = phoneme } }
                    };
                }

                // 만약 zhcvvplus.yaml에서 연단음 여부가 True고, 앞 노트가 없으면서, oto.ini에 "- 가사" 에일리어스가 존재한다면
                if (Config.UseRetan && prev == null && isExistPhonemeInOto(singer, $"- {phoneme}", notes[0])) {
                    // 가사를 "- 가사"로 변경
                    phoneme = $"- {phoneme}";
                    phoneme = GetOtoAlias(singer, phoneme, notes[0]);
                }

                // 만약 접모음이 필요한 가사라면
                if (Config.TailVowels.ContainsKey(lryicVowel)) {
                    // 접노트 가사 선언
                    var tailPhoneme = $"{Config.VowelTailPrefix}{Config.TailVowels[lryicVowel]}";

                    // 1. 노트의 길이가 zhcvvplus.yaml의 판단 틱보다 작거나 같은 동시에 
                    // 1-1. 가사가 비운모면서 zhcvvplus.yaml의 비운모 단독 사용 여부가 True 거나
                    // 1-2. 가사가 복합 운모면서 zhcvvplus.yaml의 복합운모 단독 사용 여부가 True 일때
                    // 2. 혹은 zhcvvplus.yaml의 비운모 단독 사용 여부가 False 이면서 가사가 비운모일때
                    // 3. 혹은 zhcvvplus.yaml의 복합운모 단독 사용 여부가 False 이면서 가사가 복합운모일때 
                    if ((totalDuration <= Config.SingleVowelsReferenceTimimgTick &&
                        (Config.UseSingleNasalVowel && Config.NasalVowelDict.Contains(lryicVowel)
                        || Config.UseSingleMultipleVowel && Config.MultipleVowelDict.Contains(lryicVowel)) ||
                        (!Config.UseSingleNasalVowel && Config.NasalVowelDict.Contains(lryicVowel)) ||
                        (!Config.UseSingleMultipleVowel && Config.MultipleVowelDict.Contains(lryicVowel)))) {

                        // 자연스러움을 위해 접운모의 위치는 노트의 1/3로 지정
                        var tailVowelPosition = totalDuration - totalDuration / 3;

                        // 만약 빠른 접모음 이라면
                        if (Config.FastTailVowelDict.ContainsKey(lryicVowel)) {
                            // zhcvvplus.yaml에서 지정한 포지션으로 변경
                            tailVowelPosition = Config.FastTailVowelTimingTick;
                        }
                        phoneme = GetOtoAlias(singer, phoneme, notes[0]);
                        tailPhoneme = GetOtoAlias(singer, tailPhoneme, notes[0]);
                        return new Result() {
                            phonemes = new Phoneme[] {
                                new Phoneme { phoneme = phoneme }, // 원 노트 가사
                                new Phoneme { phoneme = tailPhoneme, position = tailVowelPosition}, // 접모음
                            }
                        };
                    }
                };

                // 위 if문중 어디에도 해당하지 않는다면
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme() {
                            phoneme = phoneme, // 입력한 가사로 출력
                        }
                    }
                };
            } catch (Exception e) { 
                Log.Error(e, "An error occurred during the phoneme processing in zh cvv+ module."); // 로깅

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
