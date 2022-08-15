using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core {
    public class SingerManager : SingletonBase<SingerManager> {
        public Dictionary<string, USinger> Singers { get; private set; } = new Dictionary<string, USinger>();
        public Dictionary<USingerType, List<USinger>> SingerGroups { get; private set; } = new Dictionary<USingerType, List<USinger>>();

        public void Initialize() {
            SearchAllSingers();
        }

        public void SearchAllSingers() {
            try {
                Directory.CreateDirectory(PathManager.Inst.SingersPath);
                var stopWatch = Stopwatch.StartNew();
                var singers = ClassicSingerLoader.FindAllSingers()
                    .Concat(Vogen.VogenSingerLoader.FindAllSingers());
                Singers = singers
                    .ToLookup(s => s.Id)
                    .ToDictionary(g => g.Key, g => g.First());
                SingerGroups = singers
                    .GroupBy(s => s.SingerType)
                    .ToDictionary(s => s.Key, s => s.OrderBy(singer => singer.Name).ToList());
                stopWatch.Stop();
                Log.Information($"Search all singers: {stopWatch.Elapsed}");
            } catch (Exception e) {
                Log.Error(e, "Failed to search singers.");
                Singers = new Dictionary<string, USinger>();
            }
        }

        public USinger GetSinger(string name) {
            Log.Information(name);
            name = name.Replace("%VOICE%", "");
            if (Singers.ContainsKey(name)) {
                return Singers[name];
            }
            return null;
        }
    }
}
