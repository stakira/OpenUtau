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
                    .Select(v => v.SingerType == USingerType.Enunu
                        ? new Core.Enunu.EnunuSinger(v) as USinger
                        : new ClassicSinger(v) as USinger));
            }
            return singers;
        }
    }
}
