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
        public string Comment = "";

        public List<UPart> Parts = new List<UPart>();

        public UProject Parent;

        public UTrack(UProject parent)
        {
            Parent = parent;
        }
    }
}
