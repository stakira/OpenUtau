using System.IO;
using System.Threading.Tasks;

namespace OpenUtau.Core.Api
{
    public class PhonemizerInstaller
    {
        public static void Install(string filePath) {
            string fileName = Path.GetFileName(filePath);
            string destName = Path.Combine(PathManager.Inst.PluginsPath, fileName);
            File.Copy(filePath, destName, true);
            new Task(() => {
                DocManager.Inst.ExecuteCmd(new SingersChangedNotification());
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"Installed {fileName}"));
            }).Start(DocManager.Inst.MainScheduler);
        }
    }
}