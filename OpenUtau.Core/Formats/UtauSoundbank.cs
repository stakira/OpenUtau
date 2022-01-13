using System.Collections.Generic;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Formats {
    public static class UtauSoundbank {
        public static Dictionary<string, USinger> FindAllSingers() {
            Dictionary<string, USinger> singers = new Dictionary<string, USinger>();
            foreach (var path in new string[] {
                PathManager.Inst.SingersPathOld,
                PathManager.Inst.SingersPath,
                PathManager.Inst.AdditionalSingersPath,
            }) {
                var loader = new VoicebankLoader(path);
                var voicebanks = loader.SearchAll();
                foreach (var pair in voicebanks) {
                    singers[pair.Key] = new USinger(pair.Value);
                }
            }
            return singers;
        }
    }
}
