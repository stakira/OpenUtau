using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using OpenUtau.Classic;
using OpenUtau.Core.USTx;

namespace OpenUtau.Core.Formats {
    public static class UtauSoundbank {
        public static Dictionary<string, USinger> FindAllSingers() {
            var loader = new VoicebankLoader(PathManager.Inst.InstalledSingersPath);
            var voicebanks = loader.LoadAll();
            Dictionary<string, USinger> singers = new Dictionary<string, USinger>();
            foreach (var pair in voicebanks) {
                singers[pair.Key] = new USinger(pair.Value, PathManager.Inst.InstalledSingersPath);
            }
            return singers;
        }
    }
}
