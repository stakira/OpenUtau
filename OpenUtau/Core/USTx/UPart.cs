using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public class UPart
    {
        public string Name = "New Part";
        public string Comment = "";

        public List<UNote> Notes = new List<UNote>();

        public int PosTick;
        public int DurTick;
        public int TrackNo;

        public UPart() { }
    }
}
