using OpenUtau.Core.ResamplerDriver;
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
        public DriverModels.EngineInformation GetResamplerInformation()
        {
            DriverModels.EngineInformation ret = new EngineInformation();
            ret.Version = "Error";
            if (!_isLegalPlugin) return ret;
            ret.Author = "Unknown";
            ret.Name = System.IO.Path.GetFileName(ExePath);
            ret.Version = "Unknown";
            ret.Usuage = "Traditional Resample Engine in "+ExePath;
            return ret;
        }
    }
}
