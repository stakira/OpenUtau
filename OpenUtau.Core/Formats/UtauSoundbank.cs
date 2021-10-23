using System.Collections.Generic;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Formats {
    public static class UtauSoundbank {
        public static Dictionary<string, USinger> FindAllSingers() {
            var loader = new VoicebankLoader(PathManager.Inst.SingersPathOld);
            var voicebanks = loader.LoadAll();
            Dictionary<string, USinger> singers = new Dictionary<string, USinger>();
            foreach (var pair in voicebanks) {
                singers[pair.Key] = new USinger(pair.Value);
            }

            loader = new VoicebankLoader(PathManager.Inst.SingersPath);
            voicebanks = loader.LoadAll();
            foreach (var pair in voicebanks) {
                singers[pair.Key] = new USinger(pair.Value);
            }

            return singers;
        }
    }
}
