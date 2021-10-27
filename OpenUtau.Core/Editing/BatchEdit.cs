using System.Collections.Generic;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Editing {
    public interface BatchEdit {
        string Name { get; }
        void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager);
    }
}
