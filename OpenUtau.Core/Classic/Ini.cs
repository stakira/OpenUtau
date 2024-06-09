using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenUtau.Classic {
    public class IniLine {
        public string file;
        public int lineNumber;
        public string line;

        public override string ToString() {
            return $"\"{file}\"\nat line {lineNumber + 1}:\n\"{line}\"";
        }
    }

    public class IniBlock {
        public string header;
        public List<IniLine> lines = new List<IniLine>();
    }

    public static class Ini {
        public static List<IniBlock> ReadBlocks(StreamReader reader, string file, string headerPattern, bool trim = true) {
            var headerRegex = new Regex(headerPattern);
            var blocks = new List<IniBlock>();
            var lineNumber = -1;
            while (!reader.EndOfStream) {
                string line;
                if (trim) {
                    line = reader.ReadLine().Trim();
                } else {
                    line = reader.ReadLine();
                }
                lineNumber++;
                if (string.IsNullOrEmpty(line)) {
                    continue;
                }
                if (headerRegex.IsMatch(line)) {
                    blocks.Add(new IniBlock());
                }
                if (blocks.Count == 0) {
                    throw new FileFormatException("Unexpected beginning of ust file.");
                }
                blocks.Last().lines.Add(new IniLine {
                    file = file,
                    line = line,
                    lineNumber = lineNumber
                });
            }
            foreach (var block in blocks) {
                block.header = block.lines[0].line;
                block.lines.RemoveAt(0);
            }
            return blocks;
        }

        public static bool TryGetLines(List<IniBlock> blocks, string header, out List<IniLine> lines) {
            if (blocks.Any(block => block.header == header)) {
                lines = blocks.Find(block => block.header == header).lines;
                if (lines == null || lines.Count < 0) {
                    return false;
                } else {
                    return true;
                }
            } else {
                lines = null;
                return false;
            }

        }
    }
}
