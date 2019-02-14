using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public struct UOto
    {
        public string Alias { set; get; }
        public string File { set; get; }
        public double Offset { set; get; }
        public double Consonant { set; get; }
        public double Cutoff { set; get; }
        public double Preutter { set; get; }
        public double Overlap { set; get; }
    }

    public class USinger
    {
        public string Name = string.Empty;
        public string DisplayName { get { return Loaded ? Name : $"{Name}[Unloaded]"; } }
        public string Path = string.Empty;
        public string Author;
        public string Website;
        public string Language;

        public bool Loaded = false;

        public System.Windows.Media.Imaging.BitmapImage Avatar;

        public Encoding FileEncoding;
        public Encoding PathEncoding;

        public Dictionary<string, string> PitchMap = new Dictionary<string, string>();
        public Dictionary<string, UOto> AliasMap = new Dictionary<string, UOto>();
    }
}
