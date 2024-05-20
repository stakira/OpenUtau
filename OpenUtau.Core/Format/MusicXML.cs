using System.Collections.Generic;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Format {
    public static class MusicXML {
      static public UProject LoadProject(string file) {
        UProject uproject = new UProject();
        return uproject;
      }
      static public List<UVoicePart> Load(string file, UProject project) {
        List<UVoicePart> resultParts = new List<UVoicePart>();
        return resultParts;
      }
    }
}
