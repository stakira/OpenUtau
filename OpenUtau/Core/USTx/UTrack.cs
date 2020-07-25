using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public class UTrack
    {
        public string Name = "New Track";
        public string Comment = string.Empty;
        public USinger Singer;

        public string SingerName { get { if (Singer != null) return Singer.DisplayName; else return "[No Singer]"; } }
        public int TrackNo { set; get; }
        public int DisplayTrackNo { get { return TrackNo + 1; } }
        public bool Mute { set; get; }
        public bool Solo { set; get; }
        public double Volume { set; get; }
        public double Pan { set; get; }

        public UTrack() { }
    }
}
