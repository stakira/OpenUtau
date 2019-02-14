using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using OpenUtau.Core.ResamplerDriver;

namespace OpenUtau.Core.ResamplerDriver.Factorys
{
    internal class SharpDriver:DriverModels,IResamplerDriver
    {
        static Dictionary<string, Assembly> LoadTable = new Dictionary<string, Assembly>();
        bool _isLegalPlugin = false;

        #region 对象转换接口
        /// <summary>
        /// 格式转换过程，用于强制转换结构体而不需要引用公关类
        /// </summary>
        /// <param name="SourceStruct"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        protected static object CopyObjectToNewType(object SourceStruct, Type t)
        {
            int StructSize = Marshal.SizeOf(SourceStruct);
            IntPtr structPtr = Marshal.AllocHGlobal(StructSize);
            Marshal.StructureToPtr(SourceStruct, structPtr, false);
            object ret = Marshal.PtrToStructure(structPtr, t);
            Marshal.FreeHGlobal(structPtr);
            return ret;
        }
        #endregion

        Assembly asm = null;
        MethodInfo DoResamplerMethod = null;
        MethodInfo GetInformationMethod = null;
        public SharpDriver(string DllPath)
        {
            if (LoadTable.ContainsKey(DllPath))
            {
                asm = LoadTable[DllPath];
            }
            else
            {
                try
                {
                    asm = Assembly.LoadFrom(DllPath);
                }
                catch
                {

                }
                LoadTable.Add(DllPath,asm);
            }
            if (asm == null) _isLegalPlugin = false;
            else
            {
                foreach (Type t in asm.GetExportedTypes())
                {
                    if (DoResamplerMethod == null)
                    {
                        MethodInfo m = t.GetMethod("DoResampler");
                        if (m != null && m.IsStatic && m.GetParameters().Length == 1)
                        {
                            DoResamplerMethod = m;
                        }
                    }
                    if (GetInformationMethod == null)
                    {
                        MethodInfo m = t.GetMethod("GetInformation");
                        if (m != null && m.IsStatic && m.GetParameters().Length == 0)
                        {
                            GetInformationMethod = m;
                        }
                    }
                    if ((GetInformationMethod != null) && (DoResamplerMethod != null))
                    {
                        _isLegalPlugin = true;
                        break;
                    }
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
            if (DoResamplerMethod != null)
            {
                object inputarg = CopyObjectToNewType(Args,DoResamplerMethod.GetParameters()[0].ParameterType);
                object ret = DoResamplerMethod.Invoke(null, new object[1] { inputarg });
                DriverModels.EngineOutput Out = (DriverModels.EngineOutput)CopyObjectToNewType(ret, typeof(DriverModels.EngineOutput));
                ms = new System.IO.MemoryStream(Out.wavData);
            }
            return ms;
        }
        public DriverModels.EngineInfo GetInfo()
        {
            EngineInfo ret = new EngineInfo
            {
                Version = "Error"
            };
            if (!_isLegalPlugin) return ret;
            if (GetInformationMethod != null)
            {
                object Ret = GetInformationMethod.Invoke(null, new object[0]);
                if (Ret != null)
                {
                    ret.Author = (string)Ret.GetType().GetField("Author").GetValue(Ret);
                    ret.Name = (string)Ret.GetType().GetField("Name").GetValue(Ret);
                    ret.Usuage = (string)Ret.GetType().GetField("Usuage").GetValue(Ret);
                    ret.Version = (string)Ret.GetType().GetField("Version").GetValue(Ret);
                    ret.Author = (string)Ret.GetType().GetField("Author").GetValue(Ret);
                    ret.FlagItemCount = (int)Ret.GetType().GetField("FlagItemCount").GetValue(Ret);
                    Array ItemArray = (Ret.GetType().GetField("FlagItem").GetValue(Ret)) as Array;
                    ret.FlagItem = new EngineFlagItem[ret.FlagItemCount];
                    for (int i = 0; i < ret.FlagItemCount; i++)
                    {
                        ret.FlagItem[i] = (EngineFlagItem)CopyObjectToNewType(ItemArray.GetValue(i), typeof(EngineFlagItem));
                    }
                }
            }
            return ret;
        }
    }
}
