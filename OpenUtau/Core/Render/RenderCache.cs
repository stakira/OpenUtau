using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.Render
{
    class RenderCache
    {
        public int Capacity { set; get; }
        public int Count { get { return cache.Count; } }

        Dictionary<uint, CachedSound> cache;

        private RenderCache() { cache = new Dictionary<uint, CachedSound>(); }
        private static RenderCache _s;
        public static RenderCache Inst { get { if (_s == null) { _s = new RenderCache(); } return _s; } }

        public void Clear() { cache.Clear(); }
        public void Put(uint hash, CachedSound sound) { cache.Add(hash, sound); }
        public CachedSound Get(uint hash) { if (cache.ContainsKey(hash)) return cache[hash]; else return null; }
        public int TotalMemSize { get { int size = 0; foreach (var pair in cache) size += pair.Value.MemSize; return size; } }
    }
}
