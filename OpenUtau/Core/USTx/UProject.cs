using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public class UProject
    {
        public double BPM = 120;
        public int BeatPerBar = 4;
        public int BeatUnit = 4;
        public int PreBar = 1;
        public int Resolution = 480;

        public string FilePath;
        public string Name = "New Project";
        public string Comment = "";
        public string OutputDir = "Vocal";
        public string CacheDir = "UCache";

        public List<UTrack> Tracks = new List<UTrack>();

        public UProject()
        {
        }
    }
}
