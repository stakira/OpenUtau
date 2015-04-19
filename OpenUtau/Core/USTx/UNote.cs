using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public class UNote : IComparable
    {
        public int PosTick;
        public int DurTick;
        public int NoteNum;
        public string Lyric;
        public string Phoneme;
        public Dictionary<string, Expression> Expressions = new Dictionary<string, Expression>();
        public int Channel = 0;

        public int EndTick { get { return PosTick + DurTick; } }

        public UNote() { }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            UNote other = obj as UNote;
            if (other == null)
                throw new ArgumentException("CompareTo object is not a Note");

            if (other.Channel < this.Channel)
                return 1;
            else if (other.Channel > this.Channel)
                return -1;
            else if (other.PosTick < this.PosTick)
                return 1;
            else if (other.PosTick > this.PosTick)
                return -1;
            else if (other.GetHashCode() < this.GetHashCode())
                return 1;
            else if (other.GetHashCode() > this.GetHashCode())
                return -1;
            else
                return 0;
        }
    }
}
