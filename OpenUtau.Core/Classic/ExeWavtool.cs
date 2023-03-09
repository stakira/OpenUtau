using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using NAudio.Wave;
using OpenUtau.Core;
using OpenUtau.Core.Render;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Classic {
    class ExeWavtool : IWavtool {
        static object tempBatLock = new object();

        readonly StringBuilder sb = new StringBuilder();
        readonly string filePath;
        readonly string name;

        public ExeWavtool(string filePath, string basePath) {
            this.filePath = filePath;
            name = Path.GetRelativePath(basePath, filePath);
        }

        public float[] Concatenate(List<ResamplerItem> resamplerItems, string tempPath, CancellationTokenSource cancellation) {
            if (cancellation.IsCancellationRequested) {
                return null;
            }
            PrepareHelper();
            string batPath = Path.Combine(PathManager.Inst.CachePath, "temp.bat");
            lock (tempBatLock) {
                using (var stream = File.Open(batPath, FileMode.Create)) {
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) {
                        WriteSetUp(writer, resamplerItems, tempPath);
                        for (var i = 0; i < resamplerItems.Count; i++) {
                            WriteItem(writer, resamplerItems[i], i, resamplerItems.Count);
                        }
                        WriteTearDown(writer);
                    }
                }
                ProcessRunner.Run(batPath, "", Log.Logger, workDir: PathManager.Inst.CachePath, timeoutMs: 5 * 60 * 1000);
            }
            if (string.IsNullOrEmpty(tempPath) || File.Exists(tempPath)) {
                using (var wavStream = Core.Format.Wave.OpenFile(tempPath)) {
                    return Core.Format.Wave.GetSamples(wavStream.ToSampleProvider().ToMono(1, 0));
                }
            }
            return new float[0];
        }

        void PrepareHelper() {
            string tempHelper = Path.Join(PathManager.Inst.CachePath, "temp_helper.bat");
            lock (Renderers.GetCacheLock(tempHelper)) {
                if (!File.Exists(tempHelper)) {
                    using (var stream = File.Open(tempHelper, FileMode.Create)) {
                        using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) {
                            WriteHelper(writer);
                        }
                    }
                }
            }
        }

        void WriteHelper(StreamWriter writer) {
            // writes temp_helper.bat
            writer.WriteLine("@if exist %temp% goto A");
            writer.WriteLine("@\"%resamp%\" %1 %temp% %2 %vel% %flag% %5 %6 %7 %8 %params%");
            writer.WriteLine(":A");
            writer.WriteLine("@\"%tool%\" \"%output%\" %temp% %stp% %3 %env%");
        }

        void WriteSetUp(StreamWriter writer, List<ResamplerItem> resamplerItems, string tempPath) {
            string globalFlags = "";

            writer.WriteLine("@rem project=");
            writer.WriteLine("@set loadmodule=");
            writer.WriteLine($"@set tempo={resamplerItems[0].tempo}");
            writer.WriteLine($"@set samples={44100}");
            writer.WriteLine($"@set oto={PathManager.Inst.CachePath}");
            writer.WriteLine($"@set tool={filePath}");
            string tempFile = Path.GetRelativePath(PathManager.Inst.CachePath, tempPath);
            writer.WriteLine($"@set output={tempFile}");
            writer.WriteLine("@set helper=temp_helper.bat");
            writer.WriteLine($"@set cachedir={PathManager.Inst.CachePath}");
            writer.WriteLine($"@set flag=\"{globalFlags}\"");
            writer.WriteLine("@set env=0 5 35 0 100 100 0");
            writer.WriteLine("@set stp=0");
            writer.WriteLine("");
            writer.WriteLine("@del \"%output%\" 2>nul");
            writer.WriteLine("@mkdir \"%cachedir%\" 2>nul");
            writer.WriteLine("");
        }

        void WriteItem(StreamWriter writer, ResamplerItem item, int index, int total) {
            writer.WriteLine($"@set resamp={item.resampler.FilePath}");
            writer.WriteLine($"@set params={item.volume} {item.modulation} !{item.tempo.ToString("G999")} {Base64.Base64EncodeInt12(item.pitches)}");
            writer.WriteLine($"@set flag=\"{item.GetFlagsString()}\"");
            writer.WriteLine($"@set env={GetEnvelope(item)}");
            writer.WriteLine($"@set stp={item.skipOver}");
            writer.WriteLine($"@set vel={item.velocity}");
            string relOutputFile = Path.GetRelativePath(PathManager.Inst.CachePath, item.outputFile);
            writer.WriteLine($"@set temp=\"%cachedir%\\{relOutputFile}\"");
            string toneName = MusicMath.GetToneName(item.tone);
            string dur = $"{item.phone.duration.ToString("G999")}@{item.phone.adjustedTempo.ToString("G999")}{(item.durCorrection >= 0 ? "+" : "")}{item.durCorrection}";
            string relInputTemp = Path.GetRelativePath(PathManager.Inst.CachePath, item.inputTemp);
            writer.WriteLine($"@echo {MakeProgressBar(index + 1, total)}");
            writer.WriteLine($"@call %helper% \"%oto%\\{relInputTemp}\" {toneName} {dur} {item.preutter} {item.offset} {item.durRequired} {item.consonant} {item.cutoff} {index}");
        }

        string MakeProgressBar(int index, int total) {
            const int kWidth = 40;
            int fill = index * kWidth / total;
            return $"{new string('#', fill)}{new string('-', kWidth - fill)}({index}/{total})";
        }

        string GetEnvelope(ResamplerItem item) {
            var env = item.phone.envelope;
            sb.Clear()
                .Append(env[0].X - env[0].X).Append(' ')
                .Append(env[1].X - env[0].X).Append(' ')
                .Append(env[4].X - env[3].X).Append(' ')
                .Append(env[0].Y).Append(' ')
                .Append(env[1].Y).Append(' ')
                .Append(env[3].Y).Append(' ')
                .Append(env[4].Y).Append(' ')
                .Append(item.overlap).Append(' ')
                .Append(env[4].X - env[4].X).Append(' ')
                .Append(env[2].X - env[1].X).Append(' ')
                .Append(env[2].Y);
            return sb.ToString();
        }

        void WriteTearDown(StreamWriter writer) {
            writer.WriteLine("@if not exist \"%output%.whd\" goto E");
            writer.WriteLine("@if not exist \"%output%.dat\" goto E");
            writer.WriteLine("copy /Y \"%output%.whd\" /B + \"%output%.dat\" /B \"%output%\"");
            writer.WriteLine("del \"%output%.whd\"");
            writer.WriteLine("del \"%output%.dat\"");
            writer.WriteLine(":E");
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);

        public void CheckPermissions() {
            if (OS.IsWindows() || !File.Exists(filePath)) {
                return;
            }
            int mode = (7 << 6) | (5 << 3) | 5;
            chmod(filePath, mode);
        }

        public override string ToString() => name;
    }
}
