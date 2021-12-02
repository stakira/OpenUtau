using System;
using System.Collections.Generic;
using System.Text;

namespace OpenUtau.Core.Ustx {
    public enum UTagType { Unknown, Flavor, ToneShift }

    public class UTag {
        public UTagType type;
        public string value;
        public int position;
        public int duration;
    }
}
