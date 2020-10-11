using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core.ResamplerDriver.Factorys
{
    internal class ExeDriver : DriverModels, IResamplerDriver
    {
        string ExePath = "";
        bool _isLegalPlugin = false;

        public ExeDriver(string ExePath)
        {
            if (System.IO.File.Exists(ExePath))
            {
                if (Path.GetExtension(ExePath).ToLower() == ".exe")
                {
                    this.ExePath = ExePath;
                    _isLegalPlugin = true;
                }
            }
        }
        public bool isLegalPlugin
        {
            get
            {
                return _isLegalPlugin;
            }
        }

        public System.IO.Stream DoResampler(DriverModels.EngineInput Args)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            if (!_isLegalPlugin) return ms;
            try
            {
                string tmpFile = System.IO.Path.GetTempFileName();
                string ArgParam = FormattableString.Invariant(
                    $"\"{Args.inputWaveFile}\" \"{tmpFile}\" {Args.NoteString} {Args.Velocity} \"{Args.StrFlags}\" {Args.Offset} {Args.RequiredLength} {Args.Consonant} {Args.Cutoff} {Args.Volume} {Args.Modulation} !{Args.Tempo} {Base64.Base64EncodeInt12(Args.pitchBend)}");

                var p = Process.Start(new ProcessStartInfo(ExePath, ArgParam) { UseShellExecute = false, CreateNoWindow = true });
                p.WaitForExit();
                if (p != null)
                {
                    p.Close();
                    p.Dispose();
                    p = null;
                }

                if (System.IO.File.Exists(tmpFile))
                {
                    byte[] Dat = System.IO.File.ReadAllBytes(tmpFile);
                    ms = new MemoryStream(Dat);
                    try
                    {
                        System.IO.File.Delete(tmpFile);
                    }
                    catch { ;}
                }
            }
            catch (Exception) { ;}
            return ms;
        }
        /*
         付：外挂ini配置文件格式：
         [Information]
         Name=Resampler
         Author=Unknown
         Version=1.0
         
         [Usuage]
         This is all the Usuage Text,A Mono Text
          
         [FlagsSetting]
         ItemCount=2
          
         [Flag1]
         Flag=B
         ThreeLetterName=BRI
         Min=-100
         Max=100
         Default=1
         
         [Flag2]
         Flag=b
         ThreeLetterName=bre
         Min=-100
         Max=100
         Default=10
         */

        public DriverModels.EngineInfo GetInfo()
        {
            DriverModels.EngineInfo ret = new EngineInfo
            {
                Version = "Error"
            };
            if (!_isLegalPlugin) return ret;
            ret.Author = "Unknown";
            ret.Name = System.IO.Path.GetFileName(ExePath);
            ret.Version = "Unknown";
            ret.Usuage = $"Traditional Resample Engine in {ExePath}";
            ret.FlagItem = new EngineFlagItem[0];
            ret.FlagItemCount = 0;
            try
            {
                if (ExePath.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase))
                {
                    string RealFile = ExePath.Substring(0, ExePath.Length-3) + "ini";
                    if (System.IO.File.Exists(RealFile))
                    {
                        IniFileClass IniFile = new IniFileClass(RealFile);
                        string Name = IniFile.getKeyValue("Information", "Name");
                        if (Name != string.Empty) ret.Name = Name;
                        string Author = IniFile.getKeyValue("Information", "Author");
                        if (Author != string.Empty) ret.Author = Author;
                        string Version = IniFile.getKeyValue("Information", "Version");
                        if (Version != string.Empty) ret.Version = Version;
                        StringBuilder Usuage = new StringBuilder();
                        Usuage.Append(IniFile.SectionValues("Usuage"));
                        if (Usuage.Length > 10) ret.Usuage = Usuage.ToString();
                        string FlagItemCount = IniFile.getKeyValue("FlagsSetting", "ItemCount");
                        int.TryParse(FlagItemCount, out ret.FlagItemCount);
                        List<EngineFlagItem> Items = new List<EngineFlagItem>();
                        for (int i = 1; i <= ret.FlagItemCount; i++)
                        {
                            try
                            {
                                EngineFlagItem I = new EngineFlagItem
                                {
                                    Default = double.Parse(IniFile.getKeyValue($"Flag{i}", "Default")),
                                    flagStr = IniFile.getKeyValue($"Flag{i}", "Flag"),
                                    Max = double.Parse(IniFile.getKeyValue($"Flag{i}", "Max")),
                                    Min = double.Parse(IniFile.getKeyValue($"Flag{i}", "Min")),
                                    ThreeLetterName = IniFile.getKeyValue($"Flag{i}", "ThreeLetterName")
                                };
                                Items.Add(I);
                            }
                            catch { ;}
                        }
                        ret.FlagItemCount = Items.Count;
                        ret.FlagItem = Items.ToArray();
                    }
                }
            }
            catch { ;}
            return ret;
        }

    }
}
