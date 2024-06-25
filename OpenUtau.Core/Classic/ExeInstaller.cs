using System.IO;
using System.Text;
using System.Threading.Tasks;
using OpenUtau;
using OpenUtau.Core;

namespace Classic {
    public enum ExeType { resampler, wavtool }
    public class ExeInstaller {
        public static void Install(string filePath, ExeType exeType) {
            string fileName = Path.GetFileName(filePath);

            string destPath = exeType == ExeType.wavtool
                ? PathManager.Inst.WavtoolsPath
                : PathManager.Inst.ResamplersPath;
            string destName = Path.Combine(destPath, fileName);
            File.Copy(filePath, destName, true);
            
            if (OS.IsMacOS()) {
                //reference: https://github.com/stakira/OpenUtau/wiki/Resamplers-and-Wavtools#macos
                string MacWrapper = $"#!/bin/sh\r\nRELPATH=\"{fileName}\"\r\n\r\nABSPATH=$(cd \"$(dirname \"$0\")\"; pwd -P)\r\nABSPATH=\"$ABSPATH/$RELPATH\"\r\nif [[ ! -x \"$ABSPATH\" ]]\r\nthen\r\n    chmod +x \"$ABSPATH\"\r\nfi\r\nexec /usr/local/bin/wine32on64 \"$ABSPATH\" \"$@\"";
                File.WriteAllText(Path.ChangeExtension(destName, ".sh"), MacWrapper, new UTF8Encoding(false));
            } else if (OS.IsLinux()) {
                //reference: https://github.com/stakira/OpenUtau/wiki/Resamplers-and-Wavtools#linux
                string LinuxWrapper = $"#!/bin/bash\r\nLANG=\"ja_JP.UTF8\" wine \"{destName}\" \"${{@,-1}}\"";
                File.WriteAllText(Path.ChangeExtension(destName, null), LinuxWrapper, new UTF8Encoding(false));
            }

            new Task(() => {
                DocManager.Inst.ExecuteCmd(new SingersChangedNotification());
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"Installed {fileName}"));
            }).Start(DocManager.Inst.MainScheduler);
        }
    }
}
