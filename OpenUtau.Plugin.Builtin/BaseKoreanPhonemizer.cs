using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;


namespace OpenUtau.Plugin.Builtin {
    /// <summary>
    /// Base Phonemizer for Korean Phonemizers.
    /// <para>1. Can process Phoneme variation(음운 변동), through Hangeul.Variate().</para>
    /// <para>2. Can find Alias in oto, including Voice color etc, through FindInOto().</para>
    /// <para>3. Can manage .ini configuring, through implementing IniParser at child class. (Usage is in KoreanCVPhonemizer.cs)</para>
    /// <para>4. Can generate phonemes according to Phoneme hints.</para>
    /// </summary>
    public abstract class BaseKoreanPhonemizer : Phonemizer {
        
        protected USinger singer;
        protected int vcLength = 120; // TODO
        protected int vcLengthShort = 90;


        public override void SetSinger(USinger singer) => this.singer = singer;
        public static string? FindInOto(USinger singer, string phoneme, Note note, bool nullIfNotFound = false) {
            // 음소와 노트를 입력받고, 다음계 및 보이스컬러 에일리어스를 적용한다. 
            // nullIfNotFound가 true이면 음소가 찾아지지 않을 때 음소가 아닌 null을 리턴한다.
            // nullIfNotFound가 false면 음소가 찾아지지 않을 때 그대로 음소를 반환
            string phonemeToReturn;
            string color = string.Empty;
            int toneShift = 0;
            int? alt = null;
            if (phoneme.Equals("")) {return phoneme;}

            if (singer.TryGetMappedOto(phoneme + alt, note.tone + toneShift, color, out var otoAlt)) {
                phonemeToReturn = otoAlt.Alias;
            } 
            else if (singer.TryGetMappedOto(phoneme, note.tone + toneShift, color, out var oto)) {
                phonemeToReturn = oto.Alias;
            } 
            else if (singer.TryGetMappedOto(phoneme, note.tone, color, out oto)) {
                phonemeToReturn = oto.Alias;
            } 
            else if (nullIfNotFound) {
                phonemeToReturn = null;
            } 
            else {
                phonemeToReturn = phoneme;
            }

            return phonemeToReturn;
        }
        
        /// <summary>
        /// <para>All child Korean Phonemizer have to do is implementing this (1). </para>
        /// <para> This Function manages phoneme conversion at Notes that are not in last position. </para>
        /// </summary>
        /// <param name="notes"></param>
        /// <param name="prev"></param>
        /// <param name="next"></param>
        /// <param name="prevNeighbour"></param>
        /// <param name="nextNeighbour"></param>
        /// <param name="prevNeighbours"></param>
        /// <returns>Same as BasePhonemizer.Process(), but just manages Notes that are not in last position.</returns>
        public virtual Result ConvertPhonemes(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            // below return is Dummy
            return new Result() {
                phonemes = new Phoneme[] {
                        new Phoneme { phoneme = $""},
                    }
            };
        }

        /// <summary>
        /// <para>All child Korean Phonemizer have to do is implementing this (2). </para>
        /// <para> This Function manages phoneme conversion at Note in last position. </para>
        /// </summary>
        /// <param name="notes"></param>
        /// <param name="prev"></param>
        /// <param name="next"></param>
        /// <param name="prevNeighbour"></param>
        /// <param name="nextNeighbour"></param>
        /// <param name="prevNeighbours"></param>
        /// <returns>Same as BasePhonemizer.Process(), but just manages Note that in last position.</returns>
        public virtual Result GenerateEndSound(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            // below return is Dummy
            return new Result() {
                phonemes = new Phoneme[] {
                        new Phoneme { phoneme = $""},
                    }
            };
        }

        /// <summary>
        /// Returns Result with two input Phonemes. 
        /// <para>Second Phoneme's position is: totalDuration - Math.Min(totalDuration / totalDurationDivider, secondPhonemePosition). </para>
        /// <param name="firstPhoneme"></param>
        /// <param name="secondPhoneme"></param>
        /// <param name="totalDuration"></param>
        /// <param name="totalDurationDivider"></param>
        /// </summary>
        public Result GenerateResult(String firstPhoneme, String secondPhoneme, int totalDuration, int secondPhonemePosition, int totalDurationDivider=3){
            return new Result() {
                phonemes = new Phoneme[] {
                    new Phoneme { phoneme = firstPhoneme },
                    new Phoneme { phoneme = secondPhoneme,
                    position = totalDuration - Math.Min(totalDuration / totalDurationDivider, secondPhonemePosition)},
                }
            };
        }

        /// <summary>
        /// Returns Result with one input Phonemes. 
        /// </summary>
        public Result GenerateResult(String firstPhoneme){
            return new Result() {
                phonemes = new Phoneme[] {
                    new Phoneme { phoneme = firstPhoneme },
                }
            };
        }

        /// <summary>
        /// Returns Result with three input Phonemes. 
        /// <para>Second Phoneme's position is: totalDuration - Math.Min(totalDuration / secondTotalDurationDivider, secondPhonemePosition). </para>
        /// <para>Third Phoneme's position is: totalDuration - totalDuration / thirdTotalDurationDivider. </para>
        /// </summary>
        /// <param name="firstPhoneme"></param>
        /// <param name="secondPhoneme"></param>
        /// <param name="thirdPhoneme"></param>
        /// <param name="totalDuration"></param>
        /// <param name="secondPhonemePosition"></param>
        /// <param name="secondTotalDurationDivider"></param>
        /// <param name="thirdTotalDurationDivider"></param>
        /// <returns> Result  </returns>
        public Result GenerateResult(String firstPhoneme, String secondPhoneme, String thirdPhoneme, int totalDuration, int secondPhonemePosition, int secondTotalDurationDivider=3, int thirdTotalDurationDivider=8){
            return new Result() {
                phonemes = new Phoneme[] {
                    new Phoneme { phoneme = firstPhoneme},
                    new Phoneme { phoneme = secondPhoneme,
                    position = totalDuration - Math.Min(totalDuration / secondTotalDurationDivider, secondPhonemePosition)},
                    new Phoneme { phoneme = thirdPhoneme,
                    position = totalDuration - totalDuration / thirdTotalDurationDivider},
                }// -음소 있이 이어줌
            };
        }
        /// <summary>
        /// <para> It AUTOMATICALLY generates phonemes based on phoneme hints (each phonemes should be separated by ",". (Example: [a, a i, ya])) </para>
        /// <para> But it can't generate phonemes automatically, so should implement ConvertPhonemes() Method in child class. </para>
        /// <para> Also it can't generate Endsounds automatically, so should implement GenerateEndSound() Method in child class.</para>
        /// </summary>
        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            Note note = notes[0];
            string lyric = note.lyric;
            string phoneticHint = note.phoneticHint;

            Note? prevNote = prevNeighbour; // null or Note
            Note thisNote = note;
            Note? nextNote = nextNeighbour; // null or Note

            int totalDuration = notes.Sum(n => n.duration);

            if (phoneticHint != null) {
                // if there are phonetic hint
                // 발음 힌트가 있음 
                // 냥[nya2, ang]
                string[] phoneticHints = phoneticHint.Split(','); // phonemes are seperated by ','.
                int phoneticHintsLength = phoneticHints.Length;

                Phoneme[] phonemes = new Phoneme[phoneticHintsLength];

                Dictionary<string, string> VVdictionary = new Dictionary<string, string>() { };

                string[] VVsource = new string[] { "a", "i", "u", "e", "o", "eo", "eu" };

                for (int i = 0; i < 7; i++) {
                    // VV 딕셔너리를 채운다
                    // 나중에 발음기호에 ["a a"]를 입력하고 만일 음원에게 "a a"가 없을 경우, 자동으로 VVDictionary에서 "a a"에 해당하는 값인 "a"를 호출해 사용
                    // (반대도 똑같이 적용)

                    // VVDictionary 예시: {"a a", "a"} ...
                    for (int j = 6; j >= 0; j--) {
                        VVdictionary[$"{VVsource[i]} {VVsource[j]}"] = $"{VVsource[j]}"; // CV/CVC >> CBNN 호환용
                        VVdictionary[$"{VVsource[j]}"] = $"{VVsource[i]} {VVsource[j]}"; // CBNN >> CV/CVC 호환용
                    }
                }

                for (int i = 0; i < phoneticHintsLength; i++) {
                    string? alias = FindInOto(singer, phoneticHints[i].Trim(), note, true); // alias if exists, otherwise null

                    if (alias != null) {
                        // 발음기호에 입력된 phoneme이 음원에 존재함

                        if (i == 0) {
                            // first syllable
                            phonemes[i] = new Phoneme { phoneme = alias };
                        } 
                        else if ((i == phoneticHintsLength - 1) && ((phoneticHints[i].Trim().EndsWith('-')) || phoneticHints[i].Trim().EndsWith('R'))) {
                            // 마지막 음소이고 끝음소(ex: a -, a R)일 경우, VCLengthShort에 맞춰 음소를 배치
                            phonemes[i] = new Phoneme {
                                phoneme = alias,
                                position = totalDuration - Math.Min(vcLengthShort, totalDuration / 8)
                                // 8등분한 길이로 끝에 숨소리 음소 배치, n등분했을 때의 음소 길이가 이보다 작다면 n등분했을 때의 길이로 간다
                            };
                        } 
                        else if (phoneticHintsLength == 2) {
                            // 입력되는 발음힌트가 2개일 경우, 2등분되어 음소가 배치된다.
                            // 이 경우 부자연스러우므로 3등분해서 음소 배치하게 조정
                            phonemes[i] = new Phoneme {
                                phoneme = alias,
                                position = totalDuration - totalDuration / 3
                                // 3등분해서 음소가 배치됨
                            };
                        } 
                        else {
                            phonemes[i] = new Phoneme {
                                phoneme = alias,
                                position = totalDuration - ((totalDuration / phoneticHintsLength) * (phoneticHintsLength - i))
                                // 균등하게 n등분해서 음소가 배치됨
                            };
                        }
                    } else if (VVdictionary.ContainsKey(phoneticHints[i].Trim())) {
                        // 입력 실패한 음소가 VV 혹은 V일 때
                        if (phoneticHintsLength == 2) {
                            // 입력되는 발음힌트가 2개일 경우, 2등분되어 음소가 배치된다.
                            // 이 경우 부자연스러우므로 3등분해서 음소 배치하게 조정
                            phonemes[i] = new Phoneme {
                                phoneme = FindInOto(singer, VVdictionary[phoneticHints[i].Trim()], note),
                                position = totalDuration - totalDuration / 3
                                // 3등분해서 음소가 배치됨
                            };
                        } 
                        else {
                            phonemes[i] = new Phoneme {
                                phoneme = FindInOto(singer, VVdictionary[phoneticHints[i].Trim()], note),
                                position = totalDuration - ((totalDuration / phoneticHintsLength) * (phoneticHintsLength - i))
                                // 균등하게 n등분해서 음소가 배치됨
                            };
                        }
                    } else {
                        // 그냥 음원에 음소가 없음
                        phonemes[i] = new Phoneme {
                            phoneme = phoneticHints[i].Trim(),
                            position = totalDuration - ((totalDuration / phoneticHintsLength) * (phoneticHintsLength - i))
                            // 균등하게 n등분해서 음소가 배치됨
                        };
                    }
                }

                return new Result() {
                    phonemes = phonemes
                };
            } 
            else if (KoreanPhonemizerUtil.IsHangeul(lyric) && (!lyric.Equals("-")) && (!lyric.Equals("R"))) {
                return ConvertPhonemes(notes, prev, next, prevNeighbour, nextNeighbour, prevNeighbours);
            } 
            else {
                return GenerateEndSound(notes, prev, next, prevNeighbour, nextNeighbour, prevNeighbours);
            }
        }
    }

    

    /// <summary>
    /// abstract class for BaseIniManager(https://github.com/Enichan/Ini/blob/master/Ini.cs)
    /// <para>
    /// Note: This class will be NOT USED when implementing child korean phonemizers. This class is only for BaseIniManager.
    /// </para>
    /// </summary>
    public abstract class IniParser
    {
        public struct IniValue {
            private static bool TryParseInt(string text, out int value) {
                int res;
                if (Int32.TryParse(text,
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out res)) {
                    value = res;
                    return true;
                }
                value = 0;
                return false;
            }
            private static bool TryParseDouble(string text, out double value) {
                double res;
                if (Double.TryParse(text,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out res)) {
                    value = res;
                    return true;
                }
                value = Double.NaN;
                return false;
            }
            public string Value;
            public IniValue(object value) {
                var formattable = value as IFormattable;
                if (formattable != null) {
                    Value = formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
                } else {
                    Value = value != null ? value.ToString() : null;
                }
            }
            public IniValue(string value) {
                Value = value;
            }
            public bool ToBool(bool valueIfInvalid = false) {
                bool res;
                if (TryConvertBool(out res)) {
                    return res;
                }
                return valueIfInvalid;
            }
            public bool TryConvertBool(out bool result) {
                if (Value == null) {
                    result = default(bool);
                    return false;
                }
                var boolStr = Value.Trim().ToLowerInvariant();
                if (boolStr == "true") {
                    result = true;
                    return true;
                } else if (boolStr == "false") {
                    result = false;
                    return true;
                }
                result = default(bool);
                return false;
            }
            public int ToInt(int valueIfInvalid = 0) {
                int res;
                if (TryConvertInt(out res)) {
                    return res;
                }
                return valueIfInvalid;
            }
            public bool TryConvertInt(out int result) {
                if (Value == null) {
                    result = default(int);
                    return false;
                }
                if (TryParseInt(Value.Trim(), out result)) {
                    return true;
                }
                return false;
            }
            public double ToDouble(double valueIfInvalid = 0) {
                double res;
                if (TryConvertDouble(out res)) {
                    return res;
                }
                return valueIfInvalid;
            }
            public bool TryConvertDouble(out double result) {
                if (Value == null) {
                    result = default(double);
                    return false; ;
                }
                if (TryParseDouble(Value.Trim(), out result)) {
                    return true;
                }
                return false;
            }
            public string GetString() {
                return GetString(true, false);
            }
            public string GetString(bool preserveWhitespace) {
                return GetString(true, preserveWhitespace);
            }
            public string GetString(bool allowOuterQuotes, bool preserveWhitespace) {
                if (Value == null) {
                    return "";
                }
                var trimmed = Value.Trim();
                if (allowOuterQuotes && trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"') {
                    var inner = trimmed.Substring(1, trimmed.Length - 2);
                    return preserveWhitespace ? inner : inner.Trim();
                } else {
                    return preserveWhitespace ? Value : Value.Trim();
                }
            }
            public override string ToString() {
                return Value;
            }
            public static implicit operator IniValue(byte o) {
                return new IniValue(o);
            }
            public static implicit operator IniValue(short o) {
                return new IniValue(o);
            }
            public static implicit operator IniValue(int o) {
                return new IniValue(o);
            }
            public static implicit operator IniValue(sbyte o) {
                return new IniValue(o);
            }
            public static implicit operator IniValue(ushort o) {
                return new IniValue(o);
            }
            public static implicit operator IniValue(uint o) {
                return new IniValue(o);
            }
            public static implicit operator IniValue(float o) {
                return new IniValue(o);
            }
            public static implicit operator IniValue(double o) {
                return new IniValue(o);
            }
            public static implicit operator IniValue(bool o) {
                return new IniValue(o);
            }
            public static implicit operator IniValue(string o) {
                return new IniValue(o);
            }
            private static readonly IniValue _default = new IniValue();
            public static IniValue Default { get { return _default; } }
        }
        public class IniFile : IEnumerable<KeyValuePair<string, IniSection>>, IDictionary<string, IniSection> {
            private Dictionary<string, IniSection> sections;
            public IEqualityComparer<string> StringComparer;
            public bool SaveEmptySections;
            public IniFile()
                : this(DefaultComparer) {
            }
            public IniFile(IEqualityComparer<string> stringComparer) {
                StringComparer = stringComparer;
                sections = new Dictionary<string, IniSection>(StringComparer);
            }
            public void Save(string path, FileMode mode = FileMode.Create) {
                using (var stream = new FileStream(path, mode, FileAccess.Write)) {
                    Save(stream);
                }
            }
            public void Save(Stream stream) {
                using (var writer = new StreamWriter(stream)) {
                    Save(writer);
                }
            }
            public void Save(StreamWriter writer) {
                foreach (var section in sections) {
                    if (section.Value.Count > 0 || SaveEmptySections) {
                        writer.WriteLine(string.Format("[{0}]", section.Key.Trim()));
                        foreach (var kvp in section.Value) {
                            writer.WriteLine(string.Format("{0}={1}", kvp.Key, kvp.Value));
                        }
                        writer.WriteLine("");
                    }
                }
            }
            public void Load(string path, bool ordered = false) {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                    Load(stream, ordered);
                }
            }
            public void Load(Stream stream, bool ordered = false) {
                using (var reader = new StreamReader(stream)) {
                    Load(reader, ordered);
                }
            }
            public void Load(StreamReader reader, bool ordered = false) {
                IniSection section = null;
                while (!reader.EndOfStream) {
                    var line = reader.ReadLine();
                    if (line != null) {
                        var trimStart = line.TrimStart();
                        if (trimStart.Length > 0) {
                            if (trimStart[0] == '[') {
                                var sectionEnd = trimStart.IndexOf(']');
                                if (sectionEnd > 0) {
                                    var sectionName = trimStart.Substring(1, sectionEnd - 1).Trim();
                                    section = new IniSection(StringComparer) { Ordered = ordered };
                                    sections[sectionName] = section;
                                }
                            } else if (section != null && trimStart[0] != ';') {
                                string key;
                                IniValue val;
                                if (LoadValue(line, out key, out val)) {
                                    section[key] = val;
                                }
                            }
                        }
                    }
                }
            }
            private bool LoadValue(string line, out string key, out IniValue val) {
                var assignIndex = line.IndexOf('=');
                if (assignIndex <= 0) {
                    key = null;
                    val = null;
                    return false;
                }
                key = line.Substring(0, assignIndex).Trim();
                var value = line.Substring(assignIndex + 1);
                val = new IniValue(value);
                return true;
            }
            public bool ContainsSection(string section) {
                return sections.ContainsKey(section);
            }
            public bool TryGetSection(string section, out IniSection result) {
                return sections.TryGetValue(section, out result);
            }
            bool IDictionary<string, IniSection>.TryGetValue(string key, out IniSection value) {
                return TryGetSection(key, out value);
            }
            public bool Remove(string section) {
                return sections.Remove(section);
            }
            public IniSection Add(string section, Dictionary<string, IniValue> values, bool ordered = false) {
                return Add(section, new IniSection(values, StringComparer) { Ordered = ordered });
            }
            public IniSection Add(string section, IniSection value) {
                if (value.Comparer != StringComparer) {
                    value = new IniSection(value, StringComparer);
                }
                sections.Add(section, value);
                return value;
            }
            public IniSection Add(string section, bool ordered = false) {
                var value = new IniSection(StringComparer) { Ordered = ordered };
                sections.Add(section, value);
                return value;
            }
            void IDictionary<string, IniSection>.Add(string key, IniSection value) {
                Add(key, value);
            }
            bool IDictionary<string, IniSection>.ContainsKey(string key) {
                return ContainsSection(key);
            }
            public ICollection<string> Keys {
                get { return sections.Keys; }
            }
            public ICollection<IniSection> Values {
                get { return sections.Values; }
            }
            void ICollection<KeyValuePair<string, IniSection>>.Add(KeyValuePair<string, IniSection> item) {
                ((IDictionary<string, IniSection>)sections).Add(item);
            }
            public void Clear() {
                sections.Clear();
            }
            bool ICollection<KeyValuePair<string, IniSection>>.Contains(KeyValuePair<string, IniSection> item) {
                return ((IDictionary<string, IniSection>)sections).Contains(item);
            }
            void ICollection<KeyValuePair<string, IniSection>>.CopyTo(KeyValuePair<string, IniSection>[] array, int arrayIndex) {
                ((IDictionary<string, IniSection>)sections).CopyTo(array, arrayIndex);
            }
            public int Count {
                get { return sections.Count; }
            }
            bool ICollection<KeyValuePair<string, IniSection>>.IsReadOnly {
                get { return ((IDictionary<string, IniSection>)sections).IsReadOnly; }
            }
            bool ICollection<KeyValuePair<string, IniSection>>.Remove(KeyValuePair<string, IniSection> item) {
                return ((IDictionary<string, IniSection>)sections).Remove(item);
            }
            public IEnumerator<KeyValuePair<string, IniSection>> GetEnumerator() {
                return sections.GetEnumerator();
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
            public IniSection this[string section] {
                get {
                    IniSection s;
                    if (sections.TryGetValue(section, out s)) {
                        return s;
                    }
                    s = new IniSection(StringComparer);
                    sections[section] = s;
                    return s;
                }
                set {
                    var v = value;
                    if (v.Comparer != StringComparer) {
                        v = new IniSection(v, StringComparer);
                    }
                    sections[section] = v;
                }
            }
            public string GetContents() {
                using (var stream = new MemoryStream()) {
                    Save(stream);
                    stream.Flush();
                    var builder = new StringBuilder(Encoding.UTF8.GetString(stream.ToArray()));
                    return builder.ToString();
                }
            }
            public static IEqualityComparer<string> DefaultComparer = new CaseInsensitiveStringComparer();
            class CaseInsensitiveStringComparer : IEqualityComparer<string> {
                public bool Equals(string x, string y) {
                    return String.Compare(x, y, true) == 0;
                }
                public int GetHashCode(string obj) {
                    return obj.ToLowerInvariant().GetHashCode();
                }

#if JS
        public new bool Equals(object x, object y) {
            var xs = x as string;
            var ys = y as string;
            if (xs == null || ys == null) {
                return xs == null && ys == null;
            }
            return Equals(xs, ys);
        }

        public int GetHashCode(object obj) {
            if (obj is string) {
                return GetHashCode((string)obj);
            }
            return obj.ToStringInvariant().ToLowerInvariant().GetHashCode();
        }
#endif
                }
            }

            public class IniSection : IEnumerable<KeyValuePair<string, IniValue>>, IDictionary<string, IniValue> {
                private Dictionary<string, IniValue> values;

                #region Ordered
                private List<string> orderedKeys;

                public int IndexOf(string key) {
                    if (!Ordered) {
                        throw new InvalidOperationException("Cannot call IndexOf(string) on IniSection: section was not ordered.");
                    }
                    return IndexOf(key, 0, orderedKeys.Count);
                }

                public int IndexOf(string key, int index) {
                    if (!Ordered) {
                        throw new InvalidOperationException("Cannot call IndexOf(string, int) on IniSection: section was not ordered.");
                    }
                    return IndexOf(key, index, orderedKeys.Count - index);
                }

                public int IndexOf(string key, int index, int count) {
                    if (!Ordered) {
                        throw new InvalidOperationException("Cannot call IndexOf(string, int, int) on IniSection: section was not ordered.");
                    }
                    if (index < 0 || index > orderedKeys.Count) {
                        throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
                    }
                    if (count < 0) {
                        throw new IndexOutOfRangeException("Count cannot be less than zero." + Environment.NewLine + "Parameter name: count");
                    }
                    if (index + count > orderedKeys.Count) {
                        throw new ArgumentException("Index and count were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
                    }
                    var end = index + count;
                    for (int i = index; i < end; i++) {
                        if (Comparer.Equals(orderedKeys[i], key)) {
                            return i;
                        }
                    }
                    return -1;
                }

                public int LastIndexOf(string key) {
                    if (!Ordered) {
                        throw new InvalidOperationException("Cannot call LastIndexOf(string) on IniSection: section was not ordered.");
                    }
                    return LastIndexOf(key, 0, orderedKeys.Count);
                }

                public int LastIndexOf(string key, int index) {
                    if (!Ordered) {
                        throw new InvalidOperationException("Cannot call LastIndexOf(string, int) on IniSection: section was not ordered.");
                    }
                    return LastIndexOf(key, index, orderedKeys.Count - index);
                }

                public int LastIndexOf(string key, int index, int count) {
                    if (!Ordered) {
                        throw new InvalidOperationException("Cannot call LastIndexOf(string, int, int) on IniSection: section was not ordered.");
                    }
                    if (index < 0 || index > orderedKeys.Count) {
                        throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
                    }
                    if (count < 0) {
                        throw new IndexOutOfRangeException("Count cannot be less than zero." + Environment.NewLine + "Parameter name: count");
                    }
                    if (index + count > orderedKeys.Count) {
                        throw new ArgumentException("Index and count were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
                    }
                    var end = index + count;
                    for (int i = end - 1; i >= index; i--) {
                        if (Comparer.Equals(orderedKeys[i], key)) {
                            return i;
                        }
                    }
                    return -1;
                }

                public void Insert(int index, string key, IniValue value) {
                    if (!Ordered) {
                        throw new InvalidOperationException("Cannot call Insert(int, string, IniValue) on IniSection: section was not ordered.");
                    }
                    if (index < 0 || index > orderedKeys.Count) {
                        throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
                    }
                    values.Add(key, value);
                    orderedKeys.Insert(index, key);
                }

                public void InsertRange(int index, IEnumerable<KeyValuePair<string, IniValue>> collection) {
                    if (!Ordered) {
                        throw new InvalidOperationException("Cannot call InsertRange(int, IEnumerable<KeyValuePair<string, IniValue>>) on IniSection: section was not ordered.");
                    }
                    if (collection == null) {
                        throw new ArgumentNullException("Value cannot be null." + Environment.NewLine + "Parameter name: collection");
                    }
                    if (index < 0 || index > orderedKeys.Count) {
                        throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
                    }
                    foreach (var kvp in collection) {
                        Insert(index, kvp.Key, kvp.Value);
                        index++;
                    }
                }

                public void RemoveAt(int index) {
                    if (!Ordered) {
                        throw new InvalidOperationException("Cannot call RemoveAt(int) on IniSection: section was not ordered.");
                    }
                    if (index < 0 || index > orderedKeys.Count) {
                        throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
                    }
                    var key = orderedKeys[index];
                    orderedKeys.RemoveAt(index);
                    values.Remove(key);
                }

                public void RemoveRange(int index, int count) {
                    if (!Ordered) {
                        throw new InvalidOperationException("Cannot call RemoveRange(int, int) on IniSection: section was not ordered.");
                    }
                    if (index < 0 || index > orderedKeys.Count) {
                        throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
                    }
                    if (count < 0) {
                        throw new IndexOutOfRangeException("Count cannot be less than zero." + Environment.NewLine + "Parameter name: count");
                    }
                    if (index + count > orderedKeys.Count) {
                        throw new ArgumentException("Index and count were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
                    }
                    for (int i = 0; i < count; i++) {
                        RemoveAt(index);
                    }
                }

                public void Reverse() {
                    if (!Ordered) {
                        throw new InvalidOperationException("Cannot call Reverse() on IniSection: section was not ordered.");
                    }
                    orderedKeys.Reverse();
                }

                public void Reverse(int index, int count) {
                    if (!Ordered) {
                        throw new InvalidOperationException("Cannot call Reverse(int, int) on IniSection: section was not ordered.");
                    }
                    if (index < 0 || index > orderedKeys.Count) {
                        throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
                    }
                    if (count < 0) {
                        throw new IndexOutOfRangeException("Count cannot be less than zero." + Environment.NewLine + "Parameter name: count");
                    }
                    if (index + count > orderedKeys.Count) {
                        throw new ArgumentException("Index and count were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
                    }
                    orderedKeys.Reverse(index, count);
                }

                public ICollection<IniValue> GetOrderedValues() {
                    if (!Ordered) {
                        throw new InvalidOperationException("Cannot call GetOrderedValues() on IniSection: section was not ordered.");
                    }
                    var list = new List<IniValue>();
                    for (int i = 0; i < orderedKeys.Count; i++) {
                        list.Add(values[orderedKeys[i]]);
                    }
                    return list;
                }

                public IniValue this[int index] {
                    get {
                        if (!Ordered) {
                            throw new InvalidOperationException("Cannot index IniSection using integer key: section was not ordered.");
                        }
                        if (index < 0 || index >= orderedKeys.Count) {
                            throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
                        }
                        return values[orderedKeys[index]];
                    }
                    set {
                        if (!Ordered) {
                            throw new InvalidOperationException("Cannot index IniSection using integer key: section was not ordered.");
                        }
                        if (index < 0 || index >= orderedKeys.Count) {
                            throw new IndexOutOfRangeException("Index must be within the bounds." + Environment.NewLine + "Parameter name: index");
                        }
                        var key = orderedKeys[index];
                        values[key] = value;
                    }
                }

                public bool Ordered {
                    get {
                        return orderedKeys != null;
                    }
                    set {
                        if (Ordered != value) {
                            orderedKeys = value ? new List<string>(values.Keys) : null;
                        }
                    }
                }
                #endregion

                public IniSection()
                    : this(IniFile.DefaultComparer) {
                }

                public IniSection(IEqualityComparer<string> stringComparer) {
                    this.values = new Dictionary<string, IniValue>(stringComparer);
                }

                public IniSection(Dictionary<string, IniValue> values)
                    : this(values, IniFile.DefaultComparer) {
                }

                public IniSection(Dictionary<string, IniValue> values, IEqualityComparer<string> stringComparer) {
                    this.values = new Dictionary<string, IniValue>(values, stringComparer);
                }

                public IniSection(IniSection values)
                    : this(values, IniFile.DefaultComparer) {
                }

                public IniSection(IniSection values, IEqualityComparer<string> stringComparer) {
                    this.values = new Dictionary<string, IniValue>(values.values, stringComparer);
                }

                public void Add(string key, IniValue value) {
                    values.Add(key, value);
                    if (Ordered) {
                        orderedKeys.Add(key);
                    }
                }

                public bool ContainsKey(string key) {
                    return values.ContainsKey(key);
                }

                /// <summary>
                /// Returns this IniSection's collection of keys. If the IniSection is ordered, the keys will be returned in order.
                /// </summary>
                public ICollection<string> Keys {
                    get { return Ordered ? (ICollection<string>)orderedKeys : values.Keys; }
                }

                public bool Remove(string key) {
                    var ret = values.Remove(key);
                    if (Ordered && ret) {
                        for (int i = 0; i < orderedKeys.Count; i++) {
                            if (Comparer.Equals(orderedKeys[i], key)) {
                                orderedKeys.RemoveAt(i);
                                break;
                            }
                        }
                    }
                    return ret;
                }

                public bool TryGetValue(string key, out IniValue value) {
                    return values.TryGetValue(key, out value);
                }

                /// <summary>
                /// Returns the values in this IniSection. These values are always out of order. To get ordered values from an IniSection call GetOrderedValues instead.
                /// </summary>
                public ICollection<IniValue> Values {
                    get {
                        return values.Values;
                    }
                }

                void ICollection<KeyValuePair<string, IniValue>>.Add(KeyValuePair<string, IniValue> item) {
                    ((IDictionary<string, IniValue>)values).Add(item);
                    if (Ordered) {
                        orderedKeys.Add(item.Key);
                    }
                }

                public void Clear() {
                    values.Clear();
                    if (Ordered) {
                        orderedKeys.Clear();
                    }
                }

                bool ICollection<KeyValuePair<string, IniValue>>.Contains(KeyValuePair<string, IniValue> item) {
                    return ((IDictionary<string, IniValue>)values).Contains(item);
                }

                void ICollection<KeyValuePair<string, IniValue>>.CopyTo(KeyValuePair<string, IniValue>[] array, int arrayIndex) {
                    ((IDictionary<string, IniValue>)values).CopyTo(array, arrayIndex);
                }

                public int Count {
                    get { return values.Count; }
                }

                bool ICollection<KeyValuePair<string, IniValue>>.IsReadOnly {
                    get { return ((IDictionary<string, IniValue>)values).IsReadOnly; }
                }

                bool ICollection<KeyValuePair<string, IniValue>>.Remove(KeyValuePair<string, IniValue> item) {
                    var ret = ((IDictionary<string, IniValue>)values).Remove(item);
                    if (Ordered && ret) {
                        for (int i = 0; i < orderedKeys.Count; i++) {
                            if (Comparer.Equals(orderedKeys[i], item.Key)) {
                                orderedKeys.RemoveAt(i);
                                break;
                            }
                        }
                    }
                    return ret;
                }

                public IEnumerator<KeyValuePair<string, IniValue>> GetEnumerator() {
                    if (Ordered) {
                        return GetOrderedEnumerator();
                    } else {
                        return values.GetEnumerator();
                    }
                }

                private IEnumerator<KeyValuePair<string, IniValue>> GetOrderedEnumerator() {
                    for (int i = 0; i < orderedKeys.Count; i++) {
                        yield return new KeyValuePair<string, IniValue>(orderedKeys[i], values[orderedKeys[i]]);
                    }
                }

                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
                    return GetEnumerator();
                }

                public IEqualityComparer<string> Comparer { get { return values.Comparer; } }

                public IniValue this[string name] {
                    get {
                        IniValue val;
                        if (values.TryGetValue(name, out val)) {
                            return val;
                        }
                        return IniValue.Default;
                    }
                    set {
                        if (Ordered && !orderedKeys.Contains(name, Comparer)) {
                            orderedKeys.Add(name);
                        }
                        values[name] = value;
                    }
                }

                public static implicit operator IniSection(Dictionary<string, IniValue> dict) {
                    return new IniSection(dict);
                }

                public static explicit operator Dictionary<string, IniValue>(IniSection section) {
                    return section.values;
                }
            }
        }

    /// <summary>
    /// abstract class for Ini Management
    /// To use, child phonemizer should implement this class(BaseIniManager) with its own setting values!
    /// </summary>
    public abstract class BaseIniManager : IniParser{
        protected USinger singer;
        protected IniFile iniFile = new IniFile();
        protected string iniFileName;

        public BaseIniManager() { }

        /// <summary>
        /// if no [iniFileName] in Singer Directory, it makes new [iniFileName] with settings in [IniSetUp(iniFile)].
        /// </summary>
        /// <param name="singer"></param>
        /// <param name="iniFileName"></param>
        public void Initialize(USinger singer, string iniFileName) {
            this.singer = singer;
            this.iniFileName = iniFileName;
            try {
                iniFile.Load($"{singer.Location}/{iniFileName}");
                IniSetUp(iniFile); // you can override IniSetUp() to use.
            } 
            catch {
                IniSetUp(iniFile); // you can override IniSetUp() to use.
            }
       }

        /// <summary>
        /// <para>you can override this method with your own values. </para> 
        /// !! when implement this method, you have to use [SetOrReadThisValue(string sectionName, string keyName, bool/string/int/double value)] when setting or reading values.
        /// <para>(ex)
        /// SetOrReadThisValue("sectionName", "keyName", true);</para>
        /// </summary>
       protected virtual void IniSetUp(IniFile iniFile) {
       }

       /// <summary>
       /// <param name="sectionName"> section's name in .ini config file. </param>
       /// <param name="keyName"> key's name in .ini config file's [sectionName] section. </param>
       /// <param name="defaultValue"> default value to overwrite if there's no valid value in config file. </param>
       /// inputs section name & key name & default value. If there's valid boolean vaule, nothing happens. But if there's no valid boolean value, overwrites current value with default value.
       /// 섹션과 키 이름을 입력받고, bool 값이 존재하면 넘어가고 존재하지 않으면 defaultValue 값으로 덮어씌운다 
       /// </summary>
       protected void SetOrReadThisValue(string sectionName, string keyName, bool defaultValue) {
           iniFile[sectionName][keyName] = iniFile[sectionName][keyName].ToBool(defaultValue);
           iniFile.Save($"{singer.Location}/{iniFileName}");
       }

       /// <summary>
       /// <param name="sectionName"> section's name in .ini config file. </param>
       /// <param name="keyName"> key's name in .ini config file's [sectionName] section. </param>
       /// <param name="defaultValue"> default value to overwrite if there's no valid value in config file. </param>
       /// inputs section name & key name & default value. If there's valid string vaule, nothing happens. But if there's no valid string value, overwrites current value with default value.
       /// 섹션과 키 이름을 입력받고, string 값이 존재하면 넘어가고 존재하지 않으면 defaultValue 값으로 덮어씌운다 
       /// </summary>
       protected void SetOrReadThisValue(string sectionName, string keyName, string defaultValue) {
           if (!iniFile[sectionName].ContainsKey(keyName)) {
               // 키가 존재하지 않으면 새로 값을 넣는다
               iniFile[sectionName][keyName] = defaultValue;
               iniFile.Save($"{singer.Location}/{iniFileName}");
           }
           // 키가 존재하면 그냥 스킵
       }

       /// <summary>
       /// 
       /// <param name="sectionName"> section's name in .ini config file. </param>
       /// <param name="keyName"> key's name in .ini config file's [sectionName] section. </param>
       /// <param name="defaultValue"> default value to overwrite if there's no valid value in config file. </param>
       /// inputs section name & key name & default value. If there's valid int vaule, nothing happens. But if there's no valid int value, overwrites current value with default value.
       /// 섹션과 키 이름을 입력받고, int 값이 존재하면 넘어가고 존재하지 않으면 defaultValue 값으로 덮어씌운다 
       /// </summary>
       protected void SetOrReadThisValue(string sectionName, string keyName, int defaultValue) {
           iniFile[sectionName][keyName] = iniFile[sectionName][keyName].ToInt(defaultValue);
           iniFile.Save($"{singer.Location}/{iniFileName}");
       }

       /// <summary>
       /// <param name="sectionName"> section's name in .ini config file. </param>
       /// <param name="keyName"> key's name in .ini config file's [sectionName] section. </param>
       /// <param name="defaultValue"> default value to overwrite if there's no valid value in config file. </param>
       /// inputs section name & key name & default value. If there's valid double vaule, nothing happens. But if there's no valid double value, overwrites current value with default value.
       /// 섹션과 키 이름을 입력받고, double 값이 존재하면 넘어가고 존재하지 않으면 defaultValue 값으로 덮어씌운다 
       /// </summary>
       protected void SetOrReadThisValue(string sectionName, string keyName, double defaultValue) {
           iniFile[sectionName][keyName] = iniFile[sectionName][keyName].ToDouble(defaultValue);
           iniFile.Save($"{singer.Location}/{iniFileName}");
       }
    }
}