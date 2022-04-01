using System.IO;
using System.Linq;
using System.Text;

namespace OpenUtau.Core.Enunu {
    static class EnunuInit {
        const string kScript = "enunu-openutau.py";

        internal static string WorkDir { get; private set; }
        internal static string Python { get; private set; }
        internal static string Script { get; private set; }

        internal static void Init() {
            if (string.IsNullOrEmpty(Python) || string.IsNullOrEmpty(Script)) {
                var plugin = DocManager.Inst.Plugins.FirstOrDefault(
                    plugin => plugin.Name.ToLowerInvariant().Contains("enunu"));
                if (plugin == null || !File.Exists(plugin.Executable)) {
                    throw new FileNotFoundException("enunu plugin not found");
                }
                try {
                    var lines = File.ReadAllLines(plugin.Executable);
                    var line = lines.First(line => line.Contains("python"));
                    var parts = line.Split();
                    WorkDir = Path.GetDirectoryName(plugin.Executable);
                    Python = Path.Join(WorkDir, parts[0]);
                    Script = Path.Join(WorkDir, kScript);
                    File.WriteAllText(Script, Data.EnunuRes.enunu_openutau, Encoding.UTF8);
                } catch {
                    WorkDir = null;
                    Python = null;
                    Script = null;
                    throw;
                }
            }
        }
    }
}
