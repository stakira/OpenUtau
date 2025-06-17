﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using NAudio.Wave;
using OpenUtau.Core;
using OpenUtau.Core.Format;
using OpenUtau.Core.Util;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Classic {
    internal class ExeResampler : IResampler {
        public string Name { get; private set; }
        public string FilePath { get; private set; }
        public bool isLegalPlugin => _isLegalPlugin;
        public ResamplerManifest Manifest { get; private set; }
        readonly string _name;
        readonly bool _isLegalPlugin = false;
        readonly string winePath;
        readonly bool useWine;


        public ResamplerManifest LoadManifest() {
            try {
                var ManifestPath = Path.ChangeExtension(FilePath, ".yaml");
                if (!File.Exists(ManifestPath)) {
                    //TODO: Write Resampler Manifests shipped by OpenUtau
                    return new ResamplerManifest();
                }
                return ResamplerManifest.Load(ManifestPath);
            } catch (Exception ex) {
                Log.Error($"Failed loading resampler manifest for {_name}: {ex}");
                return new ResamplerManifest();
            }
        }

        void FixMoreConfig(string moreConfigPath) {
            var lines = new List<string> { };
            if (File.Exists(moreConfigPath)) {
                lines = File.ReadAllLines(moreConfigPath).ToList();
            }
            for (int i = 0; i < lines.Count; i++) {
                if (lines[i].StartsWith("resampler-compatibility")) {
                    if(lines[i] == "resampler-compatibility on"){
                        //moreconfig.txt is correct
                        return;
                    } else {
                        lines[i] = "resampler-compatibility on";
                        File.WriteAllLines(moreConfigPath, lines);
                        return;
                    }
                }
            }
            lines.Add("resampler-compatibility on");
            File.WriteAllLines(moreConfigPath, lines);
        }

        public ExeResampler(string filePath, string basePath) {
            if (File.Exists(filePath)) {
                FilePath = filePath;
                _name = Path.GetRelativePath(basePath, filePath);
                _isLegalPlugin = true;
            }
            //Check if should use wine
            string ext = Path.GetExtension(filePath).ToLower();
            winePath = Preferences.Default.WinePath;
            useWine = !OS.IsWindows() && !string.IsNullOrEmpty(winePath) && (ext == ".exe" || ext == ".bat");
            //Load Resampler Manifest
            Manifest = LoadManifest();
            //Make moresampler happy
            try{
                if(Path.GetFileNameWithoutExtension(filePath) == "moresampler"){
                    //Load moreconfig.txt under the same folder with filePath
                    var moreConfigPath = Path.Combine(Path.GetDirectoryName(filePath), "moreconfig.txt");
                    FixMoreConfig(moreConfigPath);
                }
            } catch (Exception ex){
                Log.Error($"Failed fixing moreconfig.txt for {filePath}: {ex}");
            }
        }

        public float[] DoResampler(ResamplerItem args, ILogger logger) {
            string tmpFile = DoResamplerReturnsFile(args, logger);
            if (string.IsNullOrEmpty(tmpFile) || File.Exists(tmpFile)) {
                using (var waveStream = Wave.OpenFile(tmpFile)) {
                    return Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0));
                }
            }
            return new float[0];
        }

        public string DoResamplerReturnsFile(ResamplerItem args, ILogger logger) {
            if (!_isLegalPlugin) {
                return null;
            }
            var threadId = Thread.CurrentThread.ManagedThreadId;
            string tmpFile = args.outputFile;
            string ArgParam = FormattableString.Invariant(
                $"\"{args.inputTemp}\" \"{tmpFile}\" {MusicMath.GetToneName(args.tone)} {args.velocity} \"{args.GetFlagsString()}\" {args.offset} {args.durRequired} {args.consonant} {args.cutoff} {args.volume} {args.modulation} !{args.tempo} {Base64.Base64EncodeInt12(args.pitches)}");
            logger.Information($" > [thread-{threadId}] {FilePath} {ArgParam}");
            if (useWine) {
                ProcessRunner.Run(winePath, $"{FilePath} {ArgParam}", logger);
            } else {
                ProcessRunner.Run(FilePath, ArgParam, logger);
            }
            return tmpFile;
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);

        public void CheckPermissions() {
            if (OS.IsWindows() || !File.Exists(FilePath)) {
                return;
            }
            int mode = (7 << 6) | (5 << 3) | 5;
            chmod(FilePath, mode);
        }

        public bool SupportsFlag(string abbr) {
            if(Manifest == null || !Manifest.expressionFilter){
                return true;
            }
            return Manifest.expressions.ContainsKey(abbr);
        }

        public override string ToString() => _name;
    }
}
