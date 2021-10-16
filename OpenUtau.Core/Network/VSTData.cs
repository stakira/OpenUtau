using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.Network {
    public struct VSTData {
        public bool playing;
        public double ticks;
        public double ticksPerBeat;
    }
}
