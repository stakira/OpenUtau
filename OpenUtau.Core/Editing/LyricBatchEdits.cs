using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Ustx;
using TinyPinyin;
using WanaKanaNet;

namespace OpenUtau.Core.Editing {

    public abstract class SingleNoteLyricEdit : BatchEdit {
        public abstract string Name { get; }
        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes.ToArray() : part.notes.ToArray();
            if (notes.Length == 0) {
                return;
            }
            var lyrics = notes.Select(note => Transform(note.lyric)).ToArray();
            docManager.StartUndoGroup(true);
            docManager.ExecuteCmd(new ChangeNoteLyricCommand(part, notes, lyrics));
            docManager.EndUndoGroup();
        }
        protected abstract string Transform(string lyric);
    }

    public class RomajiToHiragana : SingleNoteLyricEdit {
        static Dictionary<string, string> mapping = new Dictionary<string, string>() {
            {".", "."}, {",", ","}, {":", ":"}, {"/", "/"}, {"!", "!"}, {"?", "?"},
            {"~", "~"}, {"-", "-"}, {"‘", "‘"}, {"’", "’"}, {"“", "“"}, {"”", "”"},
            {"[", "["}, {"]", "]"}, {"(", "("}, {")", ")"}, {"{", "{"}, {"}", "}"},
        };
        private WanaKanaOptions option = new WanaKanaOptions() { CustomKanaMapping = mapping };
        public override string Name => "pianoroll.menu.lyrics.romajitohiragana";
        protected override string Transform(string lyric) {
            return WanaKana.ToHiragana(lyric, option);
        }
    }

    public class HiraganaToRomaji : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.hiraganatoromaji";
        protected override string Transform(string lyric) {
            return WanaKana.ToRomaji(lyric);
        }
    }

    public class JapaneseVCVtoCV : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.javcvtocv";
        protected override string Transform(string lyric) {
            if (lyric.Length > 2 && lyric[1] == ' ') {
                // When the lyric is like "a あ", "a R" or "- あ", cut off the first two characters.
                return lyric.Substring(2);
            } else {
                // Otherwise cannot recognize VCV, return as is.
                return lyric;
            }
        }
    }

    // Removes suffix like "C4", "C#4" or "Cb4"
    public class RemoveToneSuffix : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.removetonesuffix";
        protected override string Transform(string lyric) {
            if (lyric.Length <= 2) {
                return lyric;
            }
            string suffix = lyric.Substring(lyric.Length - 2);
            if ((suffix[0] == 'b' || suffix[0] == '#') && lyric.Length > 3) {
                suffix = lyric.Substring(lyric.Length - 3);
            }
            if (suffix[0] >= 'A' && suffix[0] <= 'G' && suffix.Last() >= '0' && suffix.Last() <= '9') {
                return lyric.Substring(0, lyric.Length - suffix.Length);
            }
            return lyric;
        }
    }

    public class RemoveLetterSuffix : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.removelettersuffix";
        protected override string Transform(string lyric) {
            int pos = lyric.Length - 1;
            while (pos >= 0 && ShouldRemove(lyric[pos])) {
                pos--;
            }
            return lyric.Substring(0, pos + 1);
        }

        private bool ShouldRemove(char c) {
            return (c == '_' || c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z') && c != 'R' && c != 'r';
        }
    }

    public class DashToPlus : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.dashtoplus";
        protected override string Transform(string lyric) {
            if (lyric == "-") {
                return lyric.Replace("-", "+");
            } else {
                return lyric;
            }
        }
    }
}
