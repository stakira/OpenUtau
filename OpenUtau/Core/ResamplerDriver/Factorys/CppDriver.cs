using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using OpenUtau.Core.ResamplerDriver;

namespace OpenUtau.Core.ResamplerDriver.Factorys
{
    internal class CppDriver : DriverModels, IResamplerDriver
    {
        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary",SetLastError=true)]
        static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);
        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
        static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);
        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
        static extern bool FreeLibrary(IntPtr hModule);

        #region CppIO模块
        /// <summary>
        /// Cpp指针转换用中间层
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct EngineOutput_Cpp
        {
            public int nWavData;
            public IntPtr wavData;
        }
        /// <summary>
        /// Cpp指针转换过程-EngineOutput
        /// </summary>
        /// <param name="Ptr"></param>
        /// <returns></returns>
        protected static EngineOutput Intptr2EngineOutput(IntPtr Ptr)
        {
            EngineOutput Ret = new EngineOutput();
            Ret.nWavData = 0;
            try
            {
                if (Ptr != IntPtr.Zero)
                {
                    EngineOutput_Cpp CTry = (EngineOutput_Cpp)Marshal.PtrToStructure(Ptr, typeof(EngineOutput_Cpp));
                    if (CTry.nWavData > 0)
                    {
                        Ret.wavData = new byte[CTry.nWavData];
                        Marshal.Copy(CTry.wavData, Ret.wavData, 0, CTry.nWavData);
                        Ret.nWavData = CTry.nWavData;
                    }
                }
            }
            catch { ;}
            return Ret;
        }
        /// <summary>
        /// Cpp指针转换过程-EngineInformation
        /// </summary>
        /// <param name="Ptr"></param>
        /// <returns></returns>
        protected static EngineInformation Intptr2EngineInformation(IntPtr Ptr)
        {
            EngineInformation Ret = new EngineInformation();
            Ret.Version = "Error";
            try
            {
                EngineInformation ret = (EngineInformation)Marshal.PtrToStructure(Ptr, typeof(EngineInformation));
                if (ret.Name != "")
                {
                    Ret = ret;
                }
            }
            catch { ;}
            return Ret;
        }

        /// <summary>
        /// 信息获取执行委托
        /// </summary>
        /// <returns></returns>
        delegate IntPtr GetInformationDelegate();
        /// <summary>
        /// 引擎执行过程委托
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        delegate IntPtr DoResamplerDelegate(DriverModels.EngineInput Input);

        #endregion

        string DllPath = "";
        bool _isLegalPlugin = false;

        public CppDriver(string DllPath)
        {
            IntPtr hModule = LoadLibrary(DllPath);
            if (hModule == IntPtr.Zero)
            {
                _isLegalPlugin = false;
            }
            else
            {
                IntPtr Resp = GetProcAddress(hModule, "DoResampler");
                IntPtr Infp = GetProcAddress(hModule, "GetInformation");
                if (Resp != IntPtr.Zero && Infp != IntPtr.Zero)
                {
                    this.DllPath = DllPath;
                    _isLegalPlugin = true;
                }
                FreeLibrary(hModule);
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
                IntPtr hModule = LoadLibrary(DllPath);
                if (hModule == IntPtr.Zero) _isLegalPlugin = false;
                else
                {
                    IntPtr m = GetProcAddress(hModule, "DoResampler");
                    if (m != IntPtr.Zero)
                    {
                        DoResamplerDelegate g = (DoResamplerDelegate)Marshal.GetDelegateForFunctionPointer(m, typeof(DoResamplerDelegate));
                        DriverModels.EngineOutput Output = Intptr2EngineOutput(g(Args));
                        ms = new System.IO.MemoryStream(Output.wavData);
                    }
                    FreeLibrary(hModule);
                }
            }
            catch { ;}
            return ms;
        }
        public DriverModels.EngineInformation GetResamplerInformation()
        {
            DriverModels.EngineInformation ret = new EngineInformation();
            ret.Version = "Error";
            if (!_isLegalPlugin) return ret;
            try
            {
                IntPtr hModule = LoadLibrary(DllPath);
                if (hModule == IntPtr.Zero) _isLegalPlugin = false;
                else
                {
                    IntPtr m = GetProcAddress(hModule, "GetInformation");
                    if (m != IntPtr.Zero)
                    {
                        GetInformationDelegate g = (GetInformationDelegate)Marshal.GetDelegateForFunctionPointer(m, typeof(GetInformationDelegate));
                        ret = Intptr2EngineInformation(g());
                    }
                    FreeLibrary(hModule);
                }
            }
            catch { ;}
            return ret;
        }
    }
}
