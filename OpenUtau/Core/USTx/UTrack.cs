using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public class UTrack : IComparable<UTrack>
    {
        public string Name = "New Track";
        public string Comment = "";
        public USinger Singer;

        public int TrackNo;

        public UTrack() { }

        public int CompareTo(UTrack other)
        {
            return TrackNo - other.TrackNo;
        }
    }
}
