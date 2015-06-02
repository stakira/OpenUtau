#region
/*
    Resample主引擎调度类
    ResamplerEngine 接口用于实施具体的信息调度
    ResamplerAdapter.LoadEngine过程用于识别并调度引擎，若该文件为可用引擎则返回ResamplerEngine，否则返回null
    引擎DLL开发说明见ResamplerIOModels中
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using OpenUtau.Core.ResamplerDriver;
using OpenUtau.Core.ResamplerDriver.Factorys;

namespace OpenUtau.Core.ResamplerDriver
{
    internal interface IResamplerDriver
    {
        System.IO.Stream DoResampler(DriverModels.EngineInput Args);
        DriverModels.EngineInfo GetInfo();
    }
    internal class ResamplerDriver
    {
        public static IResamplerDriver LoadEngine(string FilePath)
        {
            IResamplerDriver ret = null;
            if (System.IO.File.Exists(FilePath))
            {
                if (Path.GetExtension(FilePath).ToLower() == ".exe")
                {
                    ret = new ExeDriver(FilePath);
                }
                else if (Path.GetExtension(FilePath).ToLower() == ".dll")
                {
                    CppDriver retcpp = new CppDriver(FilePath);
                    if (retcpp.isLegalPlugin)
                    {
                        ret = retcpp;
                    }
                    else
                    {
                        SharpDriver retnet = new SharpDriver(FilePath);
                        if (retnet.isLegalPlugin)
                        {
                            ret = retnet;
                        }
                    }
                }
            }
            return ret;
        }
        public static List<DriverModels.EngineInfo> SearchEngines(string path)
        {
            var engineInfoList = new List<DriverModels.EngineInfo>();
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var files = Directory.EnumerateFiles(path);
            foreach (var file in files)
            {
                var engine = LoadEngine(file);
                if (engine != null) engineInfoList.Add(engine.GetInfo());
            }
            return engineInfoList;
        }
    }
}
