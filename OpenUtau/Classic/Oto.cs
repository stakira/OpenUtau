using System.Linq;

namespace OpenUtau.Classic {

    /// <summary>
    /// A sound in a voice bank. Corresponds to one line in oto.ini file.
    /// </summary>
    public class Oto {
        public string AudioFile;
        public string Alias;
        public int Offset;
        public int Consonant;
        public int Cutoff;
        public int Preutter;
        public int Overlap;

        public static Oto Parse(string line) {
            if (!line.Contains('=')) {
                return null;
            }
            var parts = line.Split('=');
            if (parts.Length != 2) {
                return null;
            }
            var audioClip = parts[0].Trim();
            parts = parts[1].Split(',');
            if (parts.Length != 6) {
                return null;
            }
            var result = new Oto {
                AudioFile = audioClip,
                Alias = parts[0].Trim()
            };
            int.TryParse(parts[1], out result.Offset);
            int.TryParse(parts[2], out result.Consonant);
            int.TryParse(parts[3], out result.Cutoff);
            int.TryParse(parts[4], out result.Preutter);
            int.TryParse(parts[5], out result.Overlap);
            return result;
        }

        public override string ToString() {
            return Alias;
        }
    }
}
