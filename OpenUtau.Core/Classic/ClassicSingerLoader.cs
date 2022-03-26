using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Classic {
    public static class ClassicSingerLoader {
        public static IEnumerable<USinger> FindAllSingers() {
            List<USinger> singers = new List<USinger>();
            foreach (var path in new string[] {
                PathManager.Inst.SingersPathOld,
                PathManager.Inst.SingersPath,
                PathManager.Inst.AdditionalSingersPath,
            }) {
                var loader = new VoicebankLoader(path);
                singers.AddRange(loader.SearchAll()
                    .Select(v => new ClassicSinger(v)));
            }
            return singers;
        }
    }
}
