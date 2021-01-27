using System.Collections.Generic;
using System.IO;
using OpenUtau.Core.ResamplerDriver.Factorys;

namespace OpenUtau.Core.ResamplerDriver {
    internal interface IResamplerDriver {
        byte[] DoResampler(DriverModels.EngineInput Args);
        DriverModels.EngineInfo GetInfo();
    }

    internal class ResamplerDriver {
        public static IResamplerDriver Load(string filePath) {
            string ext = Path.GetExtension(filePath).ToLower();
            if (!File.Exists(filePath)) {
                return null;
            }
            if (ext == ".exe") {
                return new ExeDriver(filePath);
            }
            if (ext == ".dll") {
                CppDriver cppDriver = new CppDriver(filePath);
                if (cppDriver.isLegalPlugin) {
                    return cppDriver;
                }
                SharpDriver csDriver = new SharpDriver(filePath);
                if (csDriver.isLegalPlugin) {
                    return csDriver;
                }
            }
            return null;
        }

        public static List<DriverModels.EngineInfo> Search(string path) {
            var engineInfoList = new List<DriverModels.EngineInfo>();
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var files = Directory.EnumerateFiles(path);
            foreach (var file in files) {
                var driver = Load(file);
                if (driver != null) {
                    engineInfoList.Add(driver.GetInfo());
                }
            }
            return engineInfoList;
        }
    }
}
