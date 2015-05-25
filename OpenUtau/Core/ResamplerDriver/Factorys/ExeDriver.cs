using OpenUtau.Core.ResamplerDriver;
using OpenUtau.Core.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenUtau.Core.ResamplerDriver.Factorys
{
    internal class ExeDriver : DriverModels, IResamplerDriver
    {
        string ExePath = "";
        bool _isLegalPlugin = false;

        #region PIT曲线 Base64编码器
        private const string intToBase64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        static string Base64EncodeInt12(int data)
        {
            if (data < 0) data += 4096;
            char[] base64 = new char[2];
            base64[0] = intToBase64[(data >> 6) & 0x003F];
            base64[1] = intToBase64[data & 0x003F];
            return new String(base64);
        }
        static string Base64EncodeInt12(int[] data)
        {
            List<string> l = new List<string>();
            foreach (int d in data) l.Add(Base64EncodeInt12(d));
            StringBuilder base64 = new StringBuilder();
            string last = "";
            int dups = 0;
            foreach (string b in l)
            {
                if (last == b) dups++;
                else if (dups == 0) base64.Append(b);
                else
                {
                    base64.Append('#');
                    base64.Append(dups + 1);
                    base64.Append('#');
                    dups = 0;
                    base64.Append(b);
                }
                last = b;
            }
            if (dups != 0)
            {
                base64.Append('#');
                base64.Append(dups + 1);
                base64.Append('#');
            }
            return base64.ToString();
        }
        #endregion

        public ExeDriver(string ExePath)
        {
            if (System.IO.File.Exists(ExePath))
            {
                if (Path.GetExtension(ExePath).ToLower()==".exe")
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
                string ArgParam = string.Format(
                    "\"{0}\" \"{1}\" {2} {3} \"{4}\" {5} {6} {7} {8} {9} {10} !{11} {12}",
                    Args.inputWaveFile,
                    tmpFile,
                    Args.NoteString,
                    Args.Velocity,
                    Args.StrFlags,
                    Args.Offset,
                    Args.RequiredLength,
                    Args.Consonant,
                    Args.Cutoff,
                    Args.Volume,
                    Args.Modulation,
                    Args.Tempo,
                    Base64EncodeInt12(Args.pitchBend));

                Process p = Process.Start(ExePath, ArgParam);
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
            catch(Exception e) { ;}
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

        public DriverModels.EngineInformation GetResamplerInformation()
        {
            DriverModels.EngineInformation ret = new EngineInformation();
            ret.Version = "Error";
            if (!_isLegalPlugin) return ret;
            ret.Author = "Unknown";
            ret.Name = System.IO.Path.GetFileName(ExePath);
            ret.Version = "Unknown";
            ret.Usuage = "Traditional Resample Engine in "+ExePath;
            ret.FlagItem = new EngineFlagItem[0];
            ret.FlagItemCount = 0;
            try
            {
                if(ExePath.ToLower().EndsWith(".exe"))
                {
                    string RealFile=ExePath.Substring(0,ExePath.Length-3)+"ini";
                    if (System.IO.File.Exists(RealFile))
                    {
                        IniFileClass IniFile = new IniFileClass(RealFile);
                        string Name=IniFile.getKeyValue("Information", "Name");
                        if (Name != "") ret.Name = Name;
                        string Author = IniFile.getKeyValue("Information", "Author");
                        if (Author != "") ret.Author = Author;
                        string Version = IniFile.getKeyValue("Information", "Version");
                        if (Version != "") ret.Version = Version;
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
                                EngineFlagItem I = new EngineFlagItem();
                                I.Default = double.Parse(IniFile.getKeyValue("Flag" + i.ToString(), "Default"));
                                I.flagStr = IniFile.getKeyValue("Flag" + i.ToString(), "Flag");
                                I.Max = double.Parse(IniFile.getKeyValue("Flag" + i.ToString(), "Max"));
                                I.Min = double.Parse(IniFile.getKeyValue("Flag" + i.ToString(), "Min"));
                                I.ThreeLetterName = IniFile.getKeyValue("Flag" + i.ToString(), "ThreeLetterName");
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
