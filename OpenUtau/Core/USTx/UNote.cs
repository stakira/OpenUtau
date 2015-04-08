using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public class UNote : IComparable
    {
        public const double minLength = 4.0 / 64;  // Actual minimal possible note length is 1/64 note

        public int PosTick;
        public int DurTick;
        public int NoteNum;
        public int Velocity;
        public string Lyric;
        public string Phoneme;

        public OpenUtau.UI.Controls.NoteControl noteControl;

        int _channel = 0;
        bool _error = false;
        bool _selected = false;

        public int Channel { set { _channel = value; } get { return _channel; } }
        public bool Error { set { _error = value; } get { return _error; } }
        public bool Selected { set { _selected = value; } get { return _selected; } }
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
