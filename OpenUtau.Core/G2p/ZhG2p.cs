using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace G2p {
    public class ZhG2p {
        private Dictionary<string, string> PhrasesMap = new Dictionary<string, string>();
        private Dictionary<string, string> TransDict = new Dictionary<string, string>();
        private Dictionary<string, string> WordDict = new Dictionary<string, string>();
        private Dictionary<string, string> PhrasesDict = new Dictionary<string, string>();

        public ZhG2p(string language) {
            string dictDir;
            if (language == "mandarin") {
                dictDir = "Dicts.mandarin";
            } else {
                dictDir = "Dicts.cantonese";
            }

            LoadDict(dictDir, "phrases_map.txt", PhrasesMap);
            LoadDict(dictDir, "phrases_dict.txt", PhrasesDict);
            LoadDict(dictDir, "user_dict.txt", PhrasesDict);
            LoadDict(dictDir, "word.txt", WordDict);
            LoadDict(dictDir, "trans_word.txt", TransDict);
        }

        public static bool LoadDict(string dictDir, string fileName, Dictionary<string, string> resultMap) {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "OpenUtau.Core.G2p." + dictDir + "." + fileName;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName)) {
                if (stream != null) {
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8)) {
                        string content = reader.ReadToEnd();
                        string[] lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                        foreach (string line in lines) {
                            string trimmedLine = line.Trim();
                            string[] keyValuePair = trimmedLine.Split(':');

                            if (keyValuePair.Length == 2) {
                                string key = keyValuePair[0];
                                string value = keyValuePair[1];
                                resultMap[key] = value;
                            }
                        }

                        return true;
                    }
                } else {
                    Console.WriteLine($"Resource {fileName} not found.");
                    return false;
                }
            }

        }

        private static readonly Dictionary<string, string> NumMap = new Dictionary<string, string>
        {
            {"0", "零"},
            {"1", "一"},
            {"2", "二"},
            {"3", "三"},
            {"4", "四"},
            {"5", "五"},
            {"6", "六"},
            {"7", "七"},
            {"8", "八"},
            {"9", "九"}
        };

        static List<string> SplitString(string input) {
            List<string> res = new List<string>();

            // 正则表达式模式
            string pattern = @"(?![ー゜])([a-zA-Z]+|[+-]|[0-9]|[\u4e00-\u9fa5]|[\u3040-\u309F\u30A0-\u30FF][ャュョゃゅょァィゥェォぁぃぅぇぉ]?)";

            // 使用正则表达式匹配
            MatchCollection matches = Regex.Matches(input, pattern);

            foreach (Match match in matches) {
                res.Add(match.Value);
            }

            return res;
        }

        private static string ResetZH(List<string> input, List<string> res, List<int> positions) {
            var result = input;
            for (var i = 0; i < positions.Count; i++) {
                result[positions[i]] = res[i];
            }

            return string.Join(" ", result);
        }

        private static void AddString(string text, List<string> res) {
            var temp = text.Split(' ');
            res.AddRange(temp);
        }

        private static void RemoveElements(List<string> list, int start, int n) {
            if (start >= 0 && start < list.Count && n > 0) {
                int countToRemove = Math.Min(n, list.Count - start);
                list.RemoveRange(start, countToRemove);
            }
        }

        private void ZhPosition(List<string> input, List<string> res, List<int> positions) {
            for (int i = 0; i < input.Count; i++) {
                if (WordDict.ContainsKey(input[i]) || TransDict.ContainsKey(input[i])) {
                    res.Add(input[i]);
                    positions.Add(i);
                }
            }
        }

        public string Convert(string input, bool tone, bool covertNum) {
            return Convert(SplitString(input), tone, covertNum);
        }

        public string Convert(List<string> input, bool tone, bool convertNum) {
            var inputList = new List<string>();
            var inputPos = new List<int>();
            ZhPosition(input, inputList, inputPos);
            var result = new List<string>();
            var cursor = 0;

            while (cursor < inputList.Count) {
                var rawCurrentChar = inputList[cursor];
                var currentChar = TradToSim(rawCurrentChar);

                if (convertNum && NumMap.ContainsKey(currentChar)) {
                    result.Add(NumMap[currentChar]);
                    cursor++;
                }

                if (!WordDict.ContainsKey(currentChar)) {
                    result.Add(currentChar);
                    cursor++;
                    continue;
                }

                if (!IsPolyphonic(currentChar)) {
                    result.Add(GetDefaultPinyin(currentChar));
                    cursor++;
                } else {
                    var found = false;
                    for (var length = 4; length >= 2 && !found; length--) {
                        if (cursor + length <= inputList.Count) {
                            // cursor: 地, subPhrase: 地久天长
                            var subPhrase = string.Join("", inputList.GetRange(cursor, length));
                            if (PhrasesDict.ContainsKey(subPhrase)) {
                                AddString(PhrasesDict[subPhrase], result);
                                cursor += length;
                                found = true;
                            }

                            if (cursor >= 1 && !found) {
                                // cursor: 重, subPhrase_1: 语重心长
                                var subPhrase_1 = string.Join("", inputList.GetRange(cursor - 1, length));
                                if (PhrasesDict.ContainsKey(subPhrase_1)) {
                                    result.RemoveAt(result.Count - 1);
                                    AddString(PhrasesDict[subPhrase_1], result);
                                    cursor += length - 1;
                                    found = true;
                                }
                            }
                        }

                        if (cursor + 1 - length >= 0 && !found && cursor + 1 <= inputList.Count) {
                            // cursor: 好, xSubPhrase: 各有所好
                            var xSubPhrase = string.Join("", inputList.GetRange(cursor + 1 - length, length));
                            if (PhrasesDict.ContainsKey(xSubPhrase)) {
                                var pos = xSubPhrase.LastIndexOf(currentChar);
                                RemoveElements(result, cursor + 1 - length, pos);
                                AddString(PhrasesDict[xSubPhrase], result);
                                cursor += 1;
                                found = true;
                            }
                        }

                        if (cursor + 2 - length >= 0 && cursor + 2 <= inputList.Count && !found) {
                            // cursor: 好, xSubPhrase: 叶公好龙
                            var xSubPhrase_1 = string.Join("", inputList.GetRange(cursor + 2 - length, length));
                            if (PhrasesDict.ContainsKey(xSubPhrase_1)) {
                                var pos = xSubPhrase_1.LastIndexOf(currentChar);
                                RemoveElements(result, cursor + 2 - length, pos);
                                AddString(PhrasesDict[xSubPhrase_1], result);
                                cursor += 2;
                                found = true;
                            }
                        }
                    }

                    if (!found) {
                        result.Add(GetDefaultPinyin(currentChar));
                        cursor++;
                    }
                }
            }

            if (!tone) {
                for (var i = 0; i < result.Count; i++) {
                    result[i] = System.Text.RegularExpressions.Regex.Replace(result[i], "[^a-z]", "");
                }
            }

            return ResetZH(input, result, inputPos);
        }

        bool IsPolyphonic(string text) {
            return PhrasesMap.ContainsKey(text);
        }

        string TradToSim(string text) {
            return TransDict.ContainsKey(text) ? TransDict[text] : text;
        }

        string GetDefaultPinyin(string text) {
            return WordDict.ContainsKey(text) ? WordDict[text] : string.Empty;
        }


    }
}
