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

using System;

namespace OpenUtau.Classic {
    class ExeWavtool : IWavtool {
        static object tempBatLock = new object();
        static object tempShLock = new object();

        readonly StringBuilder sb = new StringBuilder();
        readonly string filePath;
        readonly string name;
        private Encoding osEncoding;

        public ExeWavtool(string filePath, string basePath) {
            this.filePath = filePath;
            name = Path.GetRelativePath(basePath, filePath);
            osEncoding = OS.IsWindows() ? Encoding.GetEncoding(0) : Encoding.UTF8;
        }

        public float[] Concatenate(List<ResamplerItem> resamplerItems, string tempPath, CancellationTokenSource cancellation) {
            if (cancellation.IsCancellationRequested) {
                return null;
            }
            //The builtin worldline resampler can't be called from bat script,
            //so we need to call it directly from C#
            foreach(var item in resamplerItems){
                if(!(item.resampler is ExeResampler) && !cancellation.IsCancellationRequested && !File.Exists(item.outputFile)){
                    lock (Renderers.GetCacheLock(item.outputFile)) {
                        item.resampler.DoResamplerReturnsFile(item, Log.Logger);
                    }
                }
            }
            PrepareHelper();
            string batPath = Path.Combine(PathManager.Inst.CachePath, "temp.bat");
            lock (tempBatLock) {
                using (var stream = File.Open(batPath, FileMode.Create)) {
                    UTF8Encoding noBomEncoding = new UTF8Encoding(false);
                    using (var writer = new StreamWriter(stream, OS.IsLinux() ? noBomEncoding : osEncoding)) {
                        WriteSetUp(writer, resamplerItems, tempPath);
                        for (var i = 0; i < resamplerItems.Count; i++) {
                            WriteItem(writer, resamplerItems[i], i, resamplerItems.Count);
                        }
                        WriteTearDown(writer);
                    }
                }

                if (OS.IsLinux()) {
                    //Because you can't run .bat files directly on linux, we have to create a shell script wrapper
                    string shPath = PrepareSh();
                    ProcessRunner.Run(shPath, "", Log.Logger, workDir: PathManager.Inst.CachePath, timeoutMs: 5 * 60 * 1000);
                }
                else {
                    ProcessRunner.Run(batPath, "", Log.Logger, workDir: PathManager.Inst.CachePath, timeoutMs: 5 * 60 * 1000);
                }
            }
            if (string.IsNullOrEmpty(tempPath) || File.Exists(tempPath)) {
                using (var wavStream = Core.Format.Wave.OpenFile(tempPath)) {
                    return Core.Format.Wave.GetSamples(wavStream.ToSampleProvider().ToMono(1, 0));
                }
            }
            return new float[0];
        }

        string PrepareSh () {
            string shPath = Path.Join(PathManager.Inst.CachePath, "temp.sh");
            lock(tempShLock) {
                if (!File.Exists(shPath)) {
                    using (FileStream stream = File.Open(shPath, FileMode.Create)) {
                        //Making a new encoding here that does not have a byte order mark
                        //The byte order mark at the front of an shell script causes an exec format error
                        UTF8Encoding noBomEncoding = new UTF8Encoding(false);
                        using (StreamWriter writer = new StreamWriter(stream, noBomEncoding)) {
                            WriteSh(writer);
                        }
                    }
                    int mode = (7 << 6) | (5 << 3) | 5;
                    chmod(shPath, mode);
                }
            }
            return shPath;
        }

        void WriteSh (StreamWriter writer) {
            string batPath = Path.Combine(PathManager.Inst.CachePath, "temp.bat");
            writer.WriteLine("#!/bin/bash");
            writer.WriteLine("LANG=\"ja_JP.UTF8\" wine \"" + batPath + "\" \"${@,-1}\"");
        }

        void PrepareHelper() {
            string tempHelper = Path.Join(PathManager.Inst.CachePath, "temp_helper.bat");
            lock (Renderers.GetCacheLock(tempHelper)) {
                if (!File.Exists(tempHelper)) {
                    using (var stream = File.Open(tempHelper, FileMode.Create)) {
                        //BOM also causes problems when running .bat files through wine
                        UTF8Encoding noBomEncoding = new UTF8Encoding(false);
                        using (var writer = new StreamWriter(stream, OS.IsLinux() ? noBomEncoding : osEncoding)) {
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
            writer.WriteLine($"@set oto={ConvertIfNeeded(PathManager.Inst.CachePath)}");
            string toolPath = OS.IsLinux() ? ResolveResamplerExePathLinux(filePath) : filePath;
            writer.WriteLine($"@set tool={ConvertIfNeeded(toolPath)}");
            string tempFile = Path.GetRelativePath(PathManager.Inst.CachePath, tempPath);
            writer.WriteLine($"@set output={ConvertIfNeeded(tempFile)}");
            writer.WriteLine("@set helper=temp_helper.bat");
            writer.WriteLine($"@set cachedir={ConvertIfNeeded(PathManager.Inst.CachePath)}");
            writer.WriteLine($"@set flag=\"{globalFlags}\"");
            writer.WriteLine("@set env=0 5 35 0 100 100 0");
            writer.WriteLine("@set stp=0");
            writer.WriteLine("");
            writer.WriteLine("@del \"%output%\" 2>nul");
            writer.WriteLine("@mkdir \"%cachedir%\" 2>nul");
            writer.WriteLine("");
        }

        void WriteItem(StreamWriter writer, ResamplerItem item, int index, int total) {
            string resampPath = OS.IsLinux() ? ResolveResamplerExePathLinux(item.resampler.FilePath) : item.resampler.FilePath;
            writer.WriteLine($"@set resamp={ConvertIfNeeded(resampPath)}");
            writer.WriteLine($"@set params={item.volume} {item.modulation} !{item.tempo:G999} {Base64.Base64EncodeInt12(item.pitches)}");
            writer.WriteLine($"@set flag=\"{item.GetFlagsString()}\"");
            writer.WriteLine($"@set env={GetEnvelope(item)}");
            writer.WriteLine($"@set stp={item.skipOver}");
            writer.WriteLine($"@set vel={item.velocity}");
            string relOutputFile = Path.GetRelativePath(PathManager.Inst.CachePath, item.outputFile);
            writer.WriteLine($"@set temp=\"%cachedir%\\{ConvertIfNeeded(relOutputFile)}\"");
            string toneName = MusicMath.GetToneName(item.tone);
            string dur = $"{item.phone.duration:G999}@{item.phone.adjustedTempo:G999}{(item.durCorrection >= 0 ? "+" : "")}{item.durCorrection}";
            string relInputTemp = Path.GetRelativePath(PathManager.Inst.CachePath, item.inputTemp);
            writer.WriteLine($"@echo {MakeProgressBar(index + 1, total)}");
            writer.WriteLine($"@call %helper% \"%oto%\\{ConvertIfNeeded(relInputTemp)}\" {toneName} {dur} {item.preutter} {item.offset} {item.durRequired} {item.consonant} {item.cutoff} {index}");
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

        string ConvertIfNeeded(string path) {
            if (OS.IsLinux()) return ConvertToWindowsPath(path);
            else return path;
        } 

        string ConvertToWindowsPath (string linuxPath) {
            List<char> path = new List<char>(linuxPath.ToCharArray());
            bool absolutePath = false;
            if (path[0] == '/') absolutePath = true;
            for (int i = path.Count - 1; i > 0; i--) {
                if (path[i] == ' ' && path[i-1] == '\\') {
                    path.RemoveAt(i-1);
                    i--;
                }
            }
            for (int i = 0; i < path.Count; i++) {
                if (path[i] == '/') path[i] = '\\';
            }
            string windowsPath = new string(path.ToArray());
            if (absolutePath) windowsPath = "Z:" + windowsPath;
            return windowsPath;
        }

        //Parse the wrapper shell script created by the user during the resampler install process on linux for the path
        //to the resampler's exe file. Should work for most paths and files that people would make, but there may be edge cases.
        //Intended only for use on linux
        string ResolveResamplerExePathLinux (string wrapperPath) {
            using (FileStream stream = File.Open(wrapperPath, FileMode.Open)) {
                using (StreamReader reader = new StreamReader(stream)) {
                    string line;
                    int start = -1;
                    int end = -1;
                    for (line = reader.ReadLine(); line != null; line = reader.ReadLine()) {
                        if (line[0] == '#') continue; //ignore comments in the file
                        start = line.IndexOf("wine ") + 5;
                        if (start == -1) continue;

                        end = -1;
                        if (line[start] == '"') { //if path is enclosed by quotation marks
                            start++;
                            end = line.IndexOf('"', start + 1);
                        }
                        else { //if path is not enclosed by quotation marks (potential for "\ " in string)
                            int lastChecked = start;
                            do {
                                end = line.IndexOf(' ', lastChecked);
                                if (line[end - 1] != '\\') break;

                                lastChecked = end + 1;
                            } while (end != -1);
                        }
                        if (end != -1) break;
                    }
                    if (line == null) 
                        throw new InvalidDataException("Shell script wrapper for exe resampler is empty");
                    else if (start == -1 || end == -1) 
                        throw new InvalidDataException("Could not find path to .exe resampler in shell script wrapper");
                    else
                        return line.Substring(start, end - start);
                }
            }
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
