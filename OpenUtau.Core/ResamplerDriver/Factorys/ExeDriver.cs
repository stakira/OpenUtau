using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core.ResamplerDriver.Factorys {
    internal class ExeDriver : DriverModels, IResamplerDriver {
        readonly string ExePath = "";
        readonly bool _isLegalPlugin = false;

        public ExeDriver(string ExePath) {
            if (File.Exists(ExePath)) {
                if (Path.GetExtension(ExePath).ToLower() == ".exe" ||
                    Path.GetExtension(ExePath).ToLower() == ".sh") {
                    this.ExePath = ExePath;
                    FilePath = Path.GetRelativePath(PathManager.Inst.GetEngineSearchPath(), ExePath);
                    _isLegalPlugin = true;
                }
            }
        }
        public string FilePath { get; }
        public bool isLegalPlugin {
            get {
                return _isLegalPlugin;
            }
        }

        public byte[] DoResampler(DriverModels.EngineInput Args, ILogger logger) {
            const bool debugResampler = false;
            byte[] data = new byte[0];
            if (!_isLegalPlugin) {
                return data;
            }
            var threadId = Thread.CurrentThread.ManagedThreadId;
            string tmpFile = Path.GetTempFileName();
            string ArgParam = FormattableString.Invariant(
                $"\"{Args.inputWaveFile}\" \"{tmpFile}\" {Args.NoteString} {Args.Velocity} \"{Args.StrFlags}\" {Args.Offset} {Args.RequiredLength} {Args.Consonant} {Args.Cutoff} {Args.Volume} {Args.Modulation} !{Args.Tempo} {Base64.Base64EncodeInt12(Args.pitchBend)}");
            logger.Information($" > [thread-{threadId}] {ExePath} {ArgParam}");
            using (var proc = new Process()) {
                proc.StartInfo = new ProcessStartInfo(ExePath, ArgParam) {
                    UseShellExecute = false,
                    RedirectStandardOutput = debugResampler,
                    RedirectStandardError = debugResampler,
                    CreateNoWindow = true,
                };
#pragma warning disable CS0162 // Unreachable code detected
                if (debugResampler) {
                    proc.OutputDataReceived += (o, e) => logger.Information($" >>> [thread-{threadId}] {e.Data}");
                    proc.ErrorDataReceived += (o, e) => logger.Error($" >>> [thread-{threadId}] {e.Data}");
                }
                proc.Start();
                if (debugResampler) {
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                }
#pragma warning restore CS0162 // Unreachable code detected
                if (!proc.WaitForExit(60000)) {
                    logger.Warning($"[thread-{threadId}] Timeout, killing...");
                    try {
                        proc.Kill();
                        logger.Warning($"[thread-{threadId}] Killed.");
                    } catch (Exception e) {
                        logger.Error(e, $"[thread-{threadId}] Failed to kill");
                    }
                }
            }
            if (File.Exists(tmpFile)) {
                data = File.ReadAllBytes(tmpFile);
                File.Delete(tmpFile);
            }
            return data;
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

        public DriverModels.EngineInfo GetInfo() {
            DriverModels.EngineInfo ret = new EngineInfo();
            if (!_isLegalPlugin) return ret;
            ret.Name = Path.GetRelativePath(PathManager.Inst.GetEngineSearchPath(), ExePath);
            ret.Usuage = $"Traditional Resample Engine in {ExePath}";
            ret.FlagItem = new EngineFlagItem[0];
            ret.FlagItemCount = 0;
            try {
                if (ExePath.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase)) {
                    string RealFile = ExePath.Substring(0, ExePath.Length - 3) + "ini";
                    if (File.Exists(RealFile)) {
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
                        for (int i = 1; i <= ret.FlagItemCount; i++) {
                            try {
                                EngineFlagItem I = new EngineFlagItem {
                                    Default = double.Parse(IniFile.getKeyValue($"Flag{i}", "Default")),
                                    flagStr = IniFile.getKeyValue($"Flag{i}", "Flag"),
                                    Max = double.Parse(IniFile.getKeyValue($"Flag{i}", "Max")),
                                    Min = double.Parse(IniFile.getKeyValue($"Flag{i}", "Min")),
                                    ThreeLetterName = IniFile.getKeyValue($"Flag{i}", "ThreeLetterName")
                                };
                                Items.Add(I);
                            } catch {; }
                        }
                        ret.FlagItemCount = Items.Count;
                        ret.FlagItem = Items.ToArray();
                    }
                }
            } catch {; }
            return ret;
        }

    }
}
