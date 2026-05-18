using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using OpenUtau.Core;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Classic {
    class MorphTrack {
        public string TargetColor = null;
        public string Flag = "";
        public string Abbr = ""; 
        public float[] Weights;
        public int Hash = 17;
    }

    public class ClassicRenderer : IRenderer {
        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Ustx.DYN,
            Ustx.PITD,
            Ustx.CLR,
            Ustx.ENG,
            Ustx.VEL,
            Ustx.VOL,
            Ustx.ATK,
            Ustx.DEC,
            Ustx.MOD,
            Ustx.MODP,
            Ustx.ALT,
            Ustx.DIR,
            Ustx.SHFT
        };

        public USingerType SingerType => USingerType.Classic;

        public bool SupportsRenderPitch => false;

        public bool SupportsExpression(UExpressionDescriptor descriptor) {
            return descriptor.isFlag
                || !string.IsNullOrEmpty(descriptor.flag)
                || supportedExp.Contains(descriptor.abbr)
                || descriptor.type == UExpressionType.MorphingCurve;
        }

        public RenderResult Layout(RenderPhrase phrase) {
            return new RenderResult() {
                leadingMs = phrase.leadingMs,
                positionMs = phrase.positionMs,
                estimatedLengthMs = phrase.durationMs + phrase.leadingMs,
            };
        }

        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, int trackNo, CancellationTokenSource cancellation, bool isPreRender) {
            if (phrase.wavtool == SharpWavtool.nameConvergence || phrase.wavtool == SharpWavtool.nameSimple) {
                return RenderInternal(phrase, progress, trackNo, cancellation, isPreRender);
            } else {
                return RenderExternal(phrase, progress, trackNo, cancellation, isPreRender);
            }
        }

        // INTERNAL PHRASE LEVEL ENGINE 
        public Task<RenderResult> RenderPhraseLevel(RenderPhrase phrase, Progress progress, int trackNo, CancellationTokenSource cancellation, bool isPreRender) {
            return Task.Run(() => {
                var result = Layout(phrase);
                var wavtool = new SharpWavtool(true);

                var otoField = typeof(RenderPhone).GetField("oto", BindingFlags.Public | BindingFlags.Instance);
                var baseOtos = new Dictionary<RenderPhone, UOto>();
                foreach (var phone in phrase.phones) baseOtos[phone] = phone.oto;

                if (TryGetMatrixData(phrase, trackNo, out string baseFlag, out List<MorphTrack> tracks, out var activePart, out int baseHash)) {
                    
                    var morphingFlags = new HashSet<string>();
                    foreach (var t in tracks) if (DocManager.Inst.Project.expressions.TryGetValue(t.Abbr, out var exp) && !string.IsNullOrEmpty(exp.flag)) morphingFlags.Add(exp.flag);

                    var itemsBase = new List<ResamplerItem>();
                    var trackItems = new List<List<ResamplerItem>>();
                    for (int i = 0; i < tracks.Count; i++) trackItems.Add(new List<ResamplerItem>());
                    
                    var itemOtos = new ConcurrentDictionary<ResamplerItem, UOto>();
                    var singer = DocManager.Inst.Project.tracks[trackNo].Singer;

                    foreach (var phone in phrase.phones) {
                        int relativeStartTick = (phrase.position + phone.position) - activePart.position;
                        int centerTick = relativeStartTick + (phone.duration / 2);

                        otoField.SetValue(phone, baseOtos[phone]);
                        var itemBase = new ResamplerItem(phrase, phone);
                        
                        string staticFlags = "";
                        if (itemBase.flags != null) {
                            var filteredFlags = itemBase.flags.Where(f => !morphingFlags.Any(mf => f.Item1.StartsWith(mf)));
                            staticFlags = string.Join("", filteredFlags.Select(f => f.Item1 + (f.Item2.HasValue ? f.Item2.Value.ToString() : "")));
                        }
                        
                        foreach (var c in activePart.curves) {
                            if (!c.IsEmpty && DocManager.Inst.Project.expressions.TryGetValue(c.abbr, out var exp) && !string.IsNullOrEmpty(exp.flag)) {
                                if (!tracks.Any(t => t.Abbr == exp.abbr) && !staticFlags.Contains(exp.flag)) staticFlags += $"{exp.flag}{(int)Math.Round((double)c.Sample(centerTick))}";
                            }
                        }

                        int flagHashBase = HashString(staticFlags + baseFlag);
                        itemBase.flags = new[] { Tuple.Create(staticFlags + baseFlag, (int?)null, (string)null) };
                        itemBase.outputFile = itemBase.outputFile.Replace(".wav", $"_pBase_{flagHashBase:X}.wav");
                        itemsBase.Add(itemBase);
                        itemOtos[itemBase] = baseOtos[phone];

                        for (int i = 0; i < tracks.Count; i++) {
                            UOto targetOto = baseOtos[phone];
                            if (!string.IsNullOrEmpty(tracks[i].TargetColor) && TryHijackOto(singer, phone, tracks[i].TargetColor, out var oto)) targetOto = oto;

                            otoField.SetValue(phone, targetOto); 
                            var tItem = new ResamplerItem(phrase, phone);
                            
                            int flagHashTrk = HashString(staticFlags + tracks[i].Flag);
                            tItem.flags = new[] { Tuple.Create(staticFlags + tracks[i].Flag, (int?)null, (string)null) };
                            tItem.outputFile = tItem.outputFile.Replace(".wav", $"_pTrk{i}_{flagHashTrk:X}.wav");
                            tItem.inputTemp = tItem.inputTemp.Replace(".wav", $"_hij_{flagHashTrk:X}.wav"); 
                            
                            if (targetOto != baseOtos[phone]) tItem.inputFile = targetOto.File;
                            itemOtos[tItem] = targetOto;
                            trackItems[i].Add(tItem);
                        }
                        otoField.SetValue(phone, baseOtos[phone]); 
                    }

                    var allItems = new List<ResamplerItem>();
                    allItems.AddRange(itemsBase);
                    foreach (var list in trackItems) allItems.AddRange(list);

                    int completed = 0;
                    int itemsPerPhone = allItems.Count / phrase.phones.Length;
                    if (itemsPerPhone == 0) itemsPerPhone = 1;

                    Parallel.ForEach(allItems, new ParallelOptions() { MaxDegreeOfParallelism = Preferences.Default.NumRenderThreads }, item => {
                        if (cancellation.IsCancellationRequested) return;
                        lock (item.phone) {
                            otoField.SetValue(item.phone, itemOtos[item]);

                            if (!File.Exists(item.outputFile)) {
                                if (!(item.resampler is WorldlineResampler)) VoicebankFiles.Inst.CopySourceTemp(item.inputFile, item.inputTemp);
                                lock (Renderers.GetCacheLock(item.outputFile)) item.resampler.DoResamplerReturnsFile(item, Log.Logger);
                                if (!File.Exists(item.outputFile)) {
                                    DocManager.Inst.Project.timeAxis.TickPosToBarBeat(item.phrase.position + item.phone.position, out int bar, out int beat, out int tick);
                                    throw new InvalidDataException($"{item.resampler} failed to resample \"{itemOtos[item].Alias}\" at {bar}:{beat}.{string.Format("{0:000}", tick)}");
                                }
                                if (!(item.resampler is WorldlineResampler)) {
                                    VoicebankFiles.Inst.CopyBackMetaFiles(item.inputFile, item.inputTemp);
                                    try { 
                                        if (item.inputTemp.Contains("_hij_") && File.Exists(item.inputTemp)) { 
                                            File.Delete(item.inputTemp); 
                                            CleanUpMetaFiles(item.inputTemp); 
                                        } 
                                    } catch { }
                                }
                            }
                            otoField.SetValue(item.phone, baseOtos[item.phone]); 
                        }
                        // 🌟 OUTPUT ALIAS: Show the exact target color alias in the progress bar
                        if (Interlocked.Increment(ref completed) % itemsPerPhone == 0) progress.Complete(1, $"Track {trackNo + 1}: Morph: {item.resampler} \"{itemOtos[item].Alias}\"");
                    });

                    foreach (var item in itemsBase) otoField.SetValue(item.phone, itemOtos[item]);
                    float[] basePhraseSamples = wavtool.Concatenate(itemsBase, string.Empty, cancellation);

                    var colorAudios = new List<float[]>();
                    var colorCurves = new List<float[]>();

                    for (int i = 0; i < tracks.Count; i++) {
                        foreach (var item in trackItems[i]) otoField.SetValue(item.phone, itemOtos[item]);
                        float[] tSamples = wavtool.Concatenate(trackItems[i], string.Empty, cancellation);
                        colorAudios.Add(tSamples ?? basePhraseSamples);
                        colorCurves.Add(tracks[i].Weights);
                    }

                    if (basePhraseSamples != null) result.samples = SpectralMorpher.MorphN(basePhraseSamples, colorAudios, colorCurves, 44100);
                } else {
                    var standardItems = new List<ResamplerItem>();
                    var project = DocManager.Inst.Project;
                    var part = project.parts.OfType<UVoicePart>().FirstOrDefault(p => p.trackNo == trackNo && p.position <= phrase.position && p.End >= phrase.position);

                    foreach (var phone in phrase.phones) {
                        var item = new ResamplerItem(phrase, phone);

                        string staticFlags = item.flags != null ? string.Join("", item.flags.Select(f => f.Item1 + (f.Item2.HasValue ? f.Item2.Value.ToString() : ""))) : "";
                        if (part != null) {
                            int relativeStartTick = (phrase.position + phone.position) - part.position;
                            int centerTick = relativeStartTick + (phone.duration / 2);
                            foreach (var c in part.curves) {
                                if (!c.IsEmpty && project.expressions.TryGetValue(c.abbr, out var exp) && !string.IsNullOrEmpty(exp.flag) && !staticFlags.Contains(exp.flag)) {
                                    staticFlags += $"{exp.flag}{(int)Math.Round((double)c.Sample(centerTick))}";
                                }
                            }
                        }
                        
                        int flagHashStd = HashString(staticFlags);
                        item.flags = new[] { Tuple.Create(staticFlags, (int?)null, (string)null) };
                        item.outputFile = item.outputFile.Replace(".wav", $"_std_{flagHashStd:X}.wav");
                        standardItems.Add(item);
                    }

                    Parallel.ForEach(standardItems, new ParallelOptions() { MaxDegreeOfParallelism = Preferences.Default.NumRenderThreads }, item => {
                        if (cancellation.IsCancellationRequested) return;
                        if (!File.Exists(item.outputFile)) {
                            if (!(item.resampler is WorldlineResampler)) VoicebankFiles.Inst.CopySourceTemp(item.inputFile, item.inputTemp);
                            lock (Renderers.GetCacheLock(item.outputFile)) item.resampler.DoResamplerReturnsFile(item, Log.Logger);
                            if (!File.Exists(item.outputFile)) {
                                DocManager.Inst.Project.timeAxis.TickPosToBarBeat(item.phrase.position + item.phone.position, out int bar, out int beat, out int tick);
                                throw new InvalidDataException($"{item.resampler} failed to resample \"{item.phone.oto.Alias}\" at {bar}:{beat}.{string.Format("{0:000}", tick)}");
                            }
                            if (!(item.resampler is WorldlineResampler)) VoicebankFiles.Inst.CopyBackMetaFiles(item.inputFile, item.inputTemp);
                        }
                        progress.Complete(1, $"Track {trackNo + 1}: {item.resampler} \"{item.phone.oto.Alias}\"");
                    });
                    result.samples = wavtool.Concatenate(standardItems, string.Empty, cancellation);
                }

                foreach (var phone in phrase.phones) otoField.SetValue(phone, baseOtos[phone]);
                if (result.samples != null) Renderers.ApplyDynamics(phrase, result);
                return result;
            });
        }

        // EXTERNAL PHRASE LEVEL ENGINE
        public Task<RenderResult> RenderExternalPhraseLevel(RenderPhrase phrase, Progress progress, int trackNo, CancellationTokenSource cancellation, bool isPreRender) {
            return Task.Run(() => {
                int universalHash = GetUniversalCurveHash(phrase, trackNo);
                var wavPath = Path.Join(PathManager.Inst.CachePath, $"cat-{phrase.hash:x16}_{universalHash:X}.wav");
                phrase.AddCacheFile(wavPath);
                var result = Layout(phrase);
                if (File.Exists(wavPath)) {
                    try { using (var waveStream = Wave.OpenFile(wavPath)) { result.samples = Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0)); return result; } } catch { }
                }
                
                var wavtool = ToolsManager.Inst.GetWavtool(phrase.wavtool);
                var otoField = typeof(RenderPhone).GetField("oto", BindingFlags.Public | BindingFlags.Instance);
                var baseOtos = new Dictionary<RenderPhone, UOto>();
                foreach (var phone in phrase.phones) baseOtos[phone] = phone.oto;

                if (TryGetMatrixData(phrase, trackNo, out string baseFlag, out List<MorphTrack> tracks, out var activePart, out int baseHash)) {
                    
                    var morphingFlags = new HashSet<string>();
                    foreach (var t in tracks) if (DocManager.Inst.Project.expressions.TryGetValue(t.Abbr, out var exp) && !string.IsNullOrEmpty(exp.flag)) morphingFlags.Add(exp.flag);

                    var itemsBase = new List<ResamplerItem>();
                    var trackItems = new List<List<ResamplerItem>>();
                    for (int i = 0; i < tracks.Count; i++) trackItems.Add(new List<ResamplerItem>());
                    
                    var itemOtos = new Dictionary<ResamplerItem, UOto>();
                    var singer = DocManager.Inst.Project.tracks[trackNo].Singer;

                    foreach (var phone in phrase.phones) {
                        int relativeStartTick = (phrase.position + phone.position) - activePart.position;
                        int centerTick = relativeStartTick + (phone.duration / 2);

                        otoField.SetValue(phone, baseOtos[phone]);
                        var itemBase = new ResamplerItem(phrase, phone);

                        string staticFlags = "";
                        if (itemBase.flags != null) {
                            var filteredFlags = itemBase.flags.Where(f => !morphingFlags.Any(mf => f.Item1.StartsWith(mf)));
                            staticFlags = string.Join("", filteredFlags.Select(f => f.Item1 + (f.Item2.HasValue ? f.Item2.Value.ToString() : "")));
                        }

                        foreach (var c in activePart.curves) {
                            if (!c.IsEmpty && DocManager.Inst.Project.expressions.TryGetValue(c.abbr, out var exp) && !string.IsNullOrEmpty(exp.flag)) {
                                if (!tracks.Any(t => t.Abbr == exp.abbr) && !staticFlags.Contains(exp.flag)) staticFlags += $"{exp.flag}{(int)Math.Round((double)c.Sample(centerTick))}";
                            }
                        }

                        int flagHashBase = HashString(staticFlags + baseFlag);
                        itemBase.flags = new[] { Tuple.Create(staticFlags + baseFlag, (int?)null, (string)null) };
                        itemBase.outputFile = itemBase.outputFile.Replace(".wav", $"_pBase_{flagHashBase:X}.wav");
                        itemsBase.Add(itemBase);
                        itemOtos[itemBase] = baseOtos[phone];

                        for (int i = 0; i < tracks.Count; i++) {
                            UOto targetOto = baseOtos[phone];
                            if (!string.IsNullOrEmpty(tracks[i].TargetColor) && TryHijackOto(singer, phone, tracks[i].TargetColor, out var oto)) targetOto = oto;

                            otoField.SetValue(phone, targetOto);
                            var tItem = new ResamplerItem(phrase, phone);
                            
                            int flagHashTrk = HashString(staticFlags + tracks[i].Flag);
                            tItem.flags = new[] { Tuple.Create(staticFlags + tracks[i].Flag, (int?)null, (string)null) };
                            tItem.outputFile = tItem.outputFile.Replace(".wav", $"_pTrk{i}_{flagHashTrk:X}.wav");
                            tItem.inputTemp = tItem.inputTemp.Replace(".wav", $"_hij_{flagHashTrk:X}.wav");
                            
                            if (targetOto != baseOtos[phone]) tItem.inputFile = targetOto.File;
                            itemOtos[tItem] = targetOto;
                            trackItems[i].Add(tItem);
                        }
                        otoField.SetValue(phone, baseOtos[phone]);
                    }

                    var allItems = new List<ResamplerItem>();
                    allItems.AddRange(itemsBase);
                    foreach (var list in trackItems) allItems.AddRange(list);

                    Parallel.ForEach(allItems, new ParallelOptions() { MaxDegreeOfParallelism = Preferences.Default.NumRenderThreads }, item => {
                        if (cancellation.IsCancellationRequested) return;
                        lock (item.phone) {
                            otoField.SetValue(item.phone, itemOtos[item]);
                            if (!File.Exists(item.outputFile)) {
                                if (!(item.resampler is WorldlineResampler)) VoicebankFiles.Inst.CopySourceTemp(item.inputFile, item.inputTemp);
                                lock (Renderers.GetCacheLock(item.outputFile)) item.resampler.DoResamplerReturnsFile(item, Log.Logger);
                                if (!File.Exists(item.outputFile)) {
                                    DocManager.Inst.Project.timeAxis.TickPosToBarBeat(item.phrase.position + item.phone.position, out int bar, out int beat, out int tick);
                                    throw new InvalidDataException($"{item.resampler} failed to resample \"{itemOtos[item].Alias}\" at {bar}:{beat}.{string.Format("{0:000}", tick)}");
                                }
                                if (!(item.resampler is WorldlineResampler)) {
                                    VoicebankFiles.Inst.CopyBackMetaFiles(item.inputFile, item.inputTemp);
                                    try { 
                                        if (item.inputTemp.Contains("_hij_") && File.Exists(item.inputTemp)) { 
                                            File.Delete(item.inputTemp); 
                                            CleanUpMetaFiles(item.inputTemp); 
                                        } 
                                    } catch { }
                                }
                            }
                            otoField.SetValue(item.phone, baseOtos[item.phone]); 
                        }
                        // 🌟 OUTPUT ALIAS
                        progress.Complete(1, $"Track {trackNo + 1}: Morph: {phrase.wavtool} \"{itemOtos[item].Alias}\"");
                    });

                    string wavPathBase = wavPath.Replace(".wav", $"_base_{baseHash:X}.wav");
                    
                    var colorPaths = new List<string>();
                    for (int i = 0; i < tracks.Count; i++) {
                        string p = wavPath.Replace(".wav", $"_trk{i}_{tracks[i].Hash:X}.wav");
                        colorPaths.Add(p);
                    }

                    foreach (var item in itemsBase) otoField.SetValue(item.phone, itemOtos[item]);
                    float[] basePhraseSamples = wavtool.Concatenate(itemsBase, wavPathBase, cancellation);
                    
                    // Fallback to read memory for external wavtools returning null
                    if (basePhraseSamples == null && File.Exists(wavPathBase)) {
                        basePhraseSamples = ReadWavToFloats(wavPathBase);
                    }
                    
                    var colorAudios = new List<float[]>();
                    var colorCurves = new List<float[]>();

                    for (int i = 0; i < tracks.Count; i++) {
                        foreach (var item in trackItems[i]) otoField.SetValue(item.phone, itemOtos[item]);
                        float[] tSamples = wavtool.Concatenate(trackItems[i], colorPaths[i], cancellation);
                        
                        // Fallback to read memory for external wavtools returning null
                        if (tSamples == null && File.Exists(colorPaths[i])) {
                            tSamples = ReadWavToFloats(colorPaths[i]);
                        }
                        
                        colorAudios.Add(tSamples ?? basePhraseSamples);
                        colorCurves.Add(tracks[i].Weights);
                    }

                    if (basePhraseSamples != null) {
                        result.samples = SpectralMorpher.MorphN(basePhraseSamples, colorAudios, colorCurves, 44100);
                        WriteFloatsToWav(result.samples, wavPath); 
                        
                        try {
                            if (File.Exists(wavPathBase)) { 
                                File.Delete(wavPathBase); 
                                CleanUpMetaFiles(wavPathBase); 
                            }
                            foreach (var p in colorPaths) { 
                                if (File.Exists(p)) { 
                                    File.Delete(p); 
                                    CleanUpMetaFiles(p); 
                                } 
                            }
                        } catch { }
                    }
                } else {
                    var standardItems = new List<ResamplerItem>();
                    var project = DocManager.Inst.Project;
                    var part = project.parts.OfType<UVoicePart>().FirstOrDefault(p => p.trackNo == trackNo && p.position <= phrase.position && p.End >= phrase.position);

                    foreach (var phone in phrase.phones) {
                        var item = new ResamplerItem(phrase, phone);
                        
                        string staticFlags = item.flags != null ? string.Join("", item.flags.Select(f => f.Item1 + (f.Item2.HasValue ? f.Item2.Value.ToString() : ""))) : "";
                        if (part != null) {
                            int relativeStartTick = (phrase.position + phone.position) - part.position;
                            int centerTick = relativeStartTick + (phone.duration / 2);
                            foreach (var c in part.curves) {
                                if (!c.IsEmpty && project.expressions.TryGetValue(c.abbr, out var exp) && !string.IsNullOrEmpty(exp.flag) && !staticFlags.Contains(exp.flag)) {
                                    staticFlags += $"{exp.flag}{(int)Math.Round((double)c.Sample(centerTick))}";
                                }
                            }
                        }
                        
                        int flagHashStd = HashString(staticFlags);
                        item.flags = new[] { Tuple.Create(staticFlags, (int?)null, (string)null) };
                        item.outputFile = item.outputFile.Replace(".wav", $"_std_{flagHashStd:X}.wav");
                        standardItems.Add(item);
                    }

                    foreach (var item in standardItems) VoicebankFiles.Inst.CopySourceTemp(item.inputFile, item.inputTemp);
                    result.samples = wavtool.Concatenate(standardItems, wavPath, cancellation);
                    foreach (var item in standardItems) VoicebankFiles.Inst.CopyBackMetaFiles(item.inputFile, item.inputTemp);
                    progress.Complete(phrase.phones.Length, $"Track {trackNo + 1} : {phrase.wavtool}");
                }

                foreach (var phone in phrase.phones) otoField.SetValue(phone, baseOtos[phone]);
                if (result.samples != null) Renderers.ApplyDynamics(phrase, result);
                return result;
            });
        }

        // INTERNAL ALIAS LEVEL ENGINE 
        public Task<RenderResult> RenderInternal(RenderPhrase phrase, Progress progress, int trackNo, CancellationTokenSource cancellation, bool isPreRender) {
            if (Preferences.Default.PhraseLevelMorphing) return RenderPhraseLevel(phrase, progress, trackNo, cancellation, isPreRender);
            
            return Task.Run(() => {
                var otoField = typeof(RenderPhone).GetField("oto", BindingFlags.Public | BindingFlags.Instance);
                var baseOtos = new Dictionary<RenderPhone, UOto>();
                foreach (var phone in phrase.phones) baseOtos[phone] = phone.oto;

                var finalItems = new List<ResamplerItem>();

                Parallel.ForEach(phrase.phones, new ParallelOptions() { MaxDegreeOfParallelism = Preferences.Default.NumRenderThreads }, phone => {
                    if (cancellation.IsCancellationRequested) return;
                    
                    if (TryGetMatrixDataAlias(phrase, phone, trackNo, out string baseFlag, out List<MorphTrack> tracks, out var activePart, out int baseHash)) {
                        
                        var morphingFlags = new HashSet<string>();
                        foreach (var t in tracks) if (DocManager.Inst.Project.expressions.TryGetValue(t.Abbr, out var exp) && !string.IsNullOrEmpty(exp.flag)) morphingFlags.Add(exp.flag);

                        int relativeStartTick = (phrase.position + phone.position) - activePart.position;
                        int centerTick = relativeStartTick + (phone.duration / 2);

                        otoField.SetValue(phone, baseOtos[phone]);
                        var itemBase = new ResamplerItem(phrase, phone);
                        string staticFlags = "";
                        if (itemBase.flags != null) {
                            var filteredFlags = itemBase.flags.Where(f => !morphingFlags.Any(mf => f.Item1.StartsWith(mf)));
                            staticFlags = string.Join("", filteredFlags.Select(f => f.Item1 + (f.Item2.HasValue ? f.Item2.Value.ToString() : "")));
                        }
                        foreach (var c in activePart.curves) {
                            if (!c.IsEmpty && DocManager.Inst.Project.expressions.TryGetValue(c.abbr, out var exp) && !string.IsNullOrEmpty(exp.flag)) {
                                if (!tracks.Any(t => t.Abbr == exp.abbr) && !staticFlags.Contains(exp.flag)) staticFlags += $"{exp.flag}{(int)Math.Round((double)c.Sample(centerTick))}";
                            }
                        }

                        itemBase.flags = new[] { Tuple.Create(staticFlags + baseFlag, (int?)null, (string)null) };
                        string masterMorphPath = itemBase.outputFile.Replace(".wav", $"_morphN_{baseHash:X}.wav");
                        lock (itemBase.phrase) { itemBase.phrase.AddCacheFile(masterMorphPath); }
                        
                        var masterItem = new ResamplerItem(phrase, phone) { outputFile = masterMorphPath };
                        lock(finalItems) { finalItems.Add(masterItem); }

                        if (!File.Exists(masterMorphPath)) {
                            string fileBase = masterMorphPath + ".base.wav";
                            progress.Complete(0, $"Track {trackNo + 1}: Morph: {itemBase.resampler} \"{baseOtos[phone].Alias}\"");
                            RunResamplerWithFlag(itemBase, staticFlags + baseFlag, fileBase);

                            var colorFiles = new List<string>();
                            var singer = DocManager.Inst.Project.tracks[trackNo].Singer;

                            for (int i = 0; i < tracks.Count; i++) {
                                string fileTrk = masterMorphPath + $".trk{i}.wav";
                                colorFiles.Add(fileTrk);

                                UOto targetOto = baseOtos[phone];
                                if (!string.IsNullOrEmpty(tracks[i].TargetColor) && TryHijackOto(singer, phone, tracks[i].TargetColor, out var oto)) targetOto = oto;

                                otoField.SetValue(phone, targetOto);
                                var tItem = new ResamplerItem(phrase, phone);
                                tItem.flags = new[] { Tuple.Create(staticFlags + tracks[i].Flag, (int?)null, (string)null) };
                                tItem.inputTemp = tItem.inputTemp.Replace(".wav", $"_hij_{tracks[i].Hash:X}.wav");
                                if (targetOto != baseOtos[phone]) tItem.inputFile = targetOto.File;
                                progress.Complete(0, $"Track {trackNo + 1}: Morph: {tItem.resampler} \"{targetOto.Alias}\"");
                                RunResamplerWithFlag(tItem, staticFlags + tracks[i].Flag, fileTrk);
                            }
                            otoField.SetValue(phone, baseOtos[phone]);

                            if (File.Exists(fileBase)) {
                                float[] baseSamples = ReadWavToFloats(fileBase);
                                var colorAudios = new List<float[]>();
                                var colorCurves = new List<float[]>();

                                for (int i = 0; i < tracks.Count; i++) {
                                    colorAudios.Add(File.Exists(colorFiles[i]) ? ReadWavToFloats(colorFiles[i]) : baseSamples);
                                    colorCurves.Add(tracks[i].Weights);
                                }
                                WriteFloatsToWav(SpectralMorpher.MorphN(baseSamples, colorAudios, colorCurves, 44100), masterMorphPath);

                                try {
                                    if (File.Exists(fileBase)) { 
                                        File.Delete(fileBase); 
                                        CleanUpMetaFiles(fileBase); 
                                    }
                                    foreach (var f in colorFiles) { 
                                        if (File.Exists(f)) { 
                                            File.Delete(f); 
                                            CleanUpMetaFiles(f); 
                                        } 
                                    }
                                } catch { }
                            }
                        }
                        progress.Complete(1, $"Track {trackNo + 1}: Morph: {itemBase.resampler} \"{phone.oto.Alias}\"");
                    } else {
                        otoField.SetValue(phone, baseOtos[phone]);
                        var item = new ResamplerItem(phrase, phone);
                        
                        var project = DocManager.Inst.Project;
                        int phoneAbsoluteTick = phrase.position + phone.position;
                        var part = project.parts.OfType<UVoicePart>().FirstOrDefault(p => p.trackNo == trackNo && p.position <= phoneAbsoluteTick && p.End >= phoneAbsoluteTick);
                        string staticFlags = item.flags != null ? string.Join("", item.flags.Select(f => f.Item1 + (f.Item2.HasValue ? f.Item2.Value.ToString() : ""))) : "";
                        
                        if (part != null) {
                            int relativeStartTick = phoneAbsoluteTick - part.position;
                            int centerTick = relativeStartTick + (phone.duration / 2);
                            foreach (var c in part.curves) {
                                if (!c.IsEmpty && project.expressions.TryGetValue(c.abbr, out var exp) && !string.IsNullOrEmpty(exp.flag) && !staticFlags.Contains(exp.flag)) {
                                    staticFlags += $"{exp.flag}{(int)Math.Round((double)c.Sample(centerTick))}";
                                }
                            }
                        }
                        
                        int flagHash = HashString(staticFlags);
                        item.flags = new[] { Tuple.Create(staticFlags, (int?)null, (string)null) };
                        item.outputFile = item.outputFile.Replace(".wav", $"_std_{flagHash:X}.wav");
                        
                        lock(finalItems) { finalItems.Add(item); }

                        if (!File.Exists(item.outputFile)) {
                            if (!(item.resampler is WorldlineResampler)) VoicebankFiles.Inst.CopySourceTemp(item.inputFile, item.inputTemp);
                            if(!item.phone.direct) lock (Renderers.GetCacheLock(item.outputFile)) item.resampler.DoResamplerReturnsFile(item, Log.Logger);
                            if (!File.Exists(item.outputFile)) {
                                DocManager.Inst.Project.timeAxis.TickPosToBarBeat(item.phrase.position + item.phone.position, out int bar, out int beat, out int tick);
                                throw new InvalidDataException($"{item.resampler} failed to resample \"{item.phone.oto.Alias}\" at {bar}:{beat}.{string.Format("{0:000}", tick)}");
                            }
                            if (!(item.resampler is WorldlineResampler)) VoicebankFiles.Inst.CopyBackMetaFiles(item.inputFile, item.inputTemp);
                        }
                        progress.Complete(1, $"Track {trackNo + 1}: {item.resampler} \"{phone.oto.Alias}\"");
                    }
                });

                foreach (var phone in phrase.phones) otoField.SetValue(phone, baseOtos[phone]);
                finalItems = finalItems.OrderBy(i => i.phone.position).ToList();
                
                var result = Layout(phrase);
                result.samples = new SharpWavtool(true).Concatenate(finalItems, string.Empty, cancellation);
                if (result.samples != null) Renderers.ApplyDynamics(phrase, result);
                return result;
            });
        }

        // EXTERNAL ALIAS LEVEL ENGINE
        public Task<RenderResult> RenderExternal(RenderPhrase phrase, Progress progress, int trackNo, CancellationTokenSource cancellation, bool isPreRender) {
            if (Preferences.Default.PhraseLevelMorphing) return RenderExternalPhraseLevel(phrase, progress, trackNo, cancellation, isPreRender);
            
            int universalHash = GetUniversalCurveHash(phrase, trackNo);
            return Task.Run(() => {
                var wavPath = Path.Join(PathManager.Inst.CachePath, $"cat-{phrase.hash:x16}_{universalHash:X}.wav");
                phrase.AddCacheFile(wavPath);
                
                var result = Layout(phrase);
                if (File.Exists(wavPath)) {
                    try { using (var waveStream = Wave.OpenFile(wavPath)) { result.samples = Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0)); return result; } } catch { }
                }

                var otoField = typeof(RenderPhone).GetField("oto", BindingFlags.Public | BindingFlags.Instance);
                var baseOtos = new Dictionary<RenderPhone, UOto>();
                foreach (var phone in phrase.phones) baseOtos[phone] = phone.oto;

                var finalItems = new List<ResamplerItem>();

                Parallel.ForEach(phrase.phones, new ParallelOptions() { MaxDegreeOfParallelism = Preferences.Default.NumRenderThreads }, phone => {
                    if (cancellation.IsCancellationRequested) return;

                    if (TryGetMatrixDataAlias(phrase, phone, trackNo, out string baseFlag, out List<MorphTrack> tracks, out var activePart, out int baseHash)) {
                        
                        var morphingFlags = new HashSet<string>();
                        foreach (var t in tracks) if (DocManager.Inst.Project.expressions.TryGetValue(t.Abbr, out var exp) && !string.IsNullOrEmpty(exp.flag)) morphingFlags.Add(exp.flag);

                        int relativeStartTick = (phrase.position + phone.position) - activePart.position;
                        int centerTick = relativeStartTick + (phone.duration / 2);

                        otoField.SetValue(phone, baseOtos[phone]);
                        var itemBase = new ResamplerItem(phrase, phone);
                        string staticFlags = "";
                        if (itemBase.flags != null) {
                            var filteredFlags = itemBase.flags.Where(f => !morphingFlags.Any(mf => f.Item1.StartsWith(mf)));
                            staticFlags = string.Join("", filteredFlags.Select(f => f.Item1 + (f.Item2.HasValue ? f.Item2.Value.ToString() : "")));
                        }
                        foreach (var c in activePart.curves) {
                            if (!c.IsEmpty && DocManager.Inst.Project.expressions.TryGetValue(c.abbr, out var exp) && !string.IsNullOrEmpty(exp.flag)) {
                                if (!tracks.Any(t => t.Abbr == exp.abbr) && !staticFlags.Contains(exp.flag)) staticFlags += $"{exp.flag}{(int)Math.Round((double)c.Sample(centerTick))}";
                            }
                        }

                        itemBase.flags = new[] { Tuple.Create(staticFlags + baseFlag, (int?)null, (string)null) };
                        string masterMorphPath = itemBase.outputFile.Replace(".wav", $"_morphN_{baseHash:X}.wav");
                        lock (itemBase.phrase) { itemBase.phrase.AddCacheFile(masterMorphPath); }
                        
                        var masterItem = new ResamplerItem(phrase, phone) { outputFile = masterMorphPath };
                        lock(finalItems) { finalItems.Add(masterItem); }

                        if (!File.Exists(masterMorphPath)) {
                            string fileBase = masterMorphPath + ".base.wav";
                            progress.Complete(0, $"Track {trackNo + 1}: Morph: {itemBase.resampler} \"{baseOtos[phone].Alias}\"");
                            RunResamplerWithFlag(itemBase, staticFlags + baseFlag, fileBase);

                            var colorFiles = new List<string>();
                            var singer = DocManager.Inst.Project.tracks[trackNo].Singer;

                            for (int i = 0; i < tracks.Count; i++) {
                                string fileTrk = masterMorphPath + $".trk{i}.wav";
                                colorFiles.Add(fileTrk);

                                UOto targetOto = baseOtos[phone];
                                if (!string.IsNullOrEmpty(tracks[i].TargetColor) && TryHijackOto(singer, phone, tracks[i].TargetColor, out var oto)) targetOto = oto;

                                otoField.SetValue(phone, targetOto);
                                var tItem = new ResamplerItem(phrase, phone);
                                tItem.flags = new[] { Tuple.Create(staticFlags + tracks[i].Flag, (int?)null, (string)null) };
                                tItem.inputTemp = tItem.inputTemp.Replace(".wav", $"_hij_{tracks[i].Hash:X}.wav");
                                if (targetOto != baseOtos[phone]) tItem.inputFile = targetOto.File;
                                progress.Complete(0, $"Track {trackNo + 1}: Morph: {tItem.resampler} \"{targetOto.Alias}\"");
                                RunResamplerWithFlag(tItem, staticFlags + tracks[i].Flag, fileTrk);
                            }
                            otoField.SetValue(phone, baseOtos[phone]);

                            if (File.Exists(fileBase)) {
                                float[] baseSamples = ReadWavToFloats(fileBase);
                                var colorAudios = new List<float[]>();
                                var colorCurves = new List<float[]>();

                                for (int i = 0; i < tracks.Count; i++) {
                                    colorAudios.Add(File.Exists(colorFiles[i]) ? ReadWavToFloats(colorFiles[i]) : baseSamples);
                                    colorCurves.Add(tracks[i].Weights);
                                }
                                WriteFloatsToWav(SpectralMorpher.MorphN(baseSamples, colorAudios, colorCurves, 44100), masterMorphPath);

                                try {
                                    if (File.Exists(fileBase)) { 
                                        File.Delete(fileBase); 
                                        CleanUpMetaFiles(fileBase); 
                                    }
                                    foreach (var f in colorFiles) { 
                                        if (File.Exists(f)) { 
                                            File.Delete(f); 
                                            CleanUpMetaFiles(f); 
                                        } 
                                    }
                                } catch { }
                            }
                        }
                        progress.Complete(1, $"Track {trackNo + 1}: Morph: {phrase.wavtool} \"{phone.oto.Alias}\"");
                    } else {
                        otoField.SetValue(phone, baseOtos[phone]);
                        var item = new ResamplerItem(phrase, phone);
                        
                        var project = DocManager.Inst.Project;
                        int phoneAbsoluteTick = phrase.position + phone.position;
                        var part = project.parts.OfType<UVoicePart>().FirstOrDefault(p => p.trackNo == trackNo && p.position <= phoneAbsoluteTick && p.End >= phoneAbsoluteTick);
                        string staticFlags = item.flags != null ? string.Join("", item.flags.Select(f => f.Item1 + (f.Item2.HasValue ? f.Item2.Value.ToString() : ""))) : "";
                        
                        if (part != null) {
                            int relativeStartTick = phoneAbsoluteTick - part.position;
                            int centerTick = relativeStartTick + (phone.duration / 2);
                            foreach (var c in part.curves) {
                                if (!c.IsEmpty && project.expressions.TryGetValue(c.abbr, out var exp) && !string.IsNullOrEmpty(exp.flag) && !staticFlags.Contains(exp.flag)) {
                                    staticFlags += $"{exp.flag}{(int)Math.Round((double)c.Sample(centerTick))}";
                                }
                            }
                        }
                        
                        int flagHash = HashString(staticFlags);
                        item.flags = new[] { Tuple.Create(staticFlags, (int?)null, (string)null) };
                        item.outputFile = item.outputFile.Replace(".wav", $"_std_{flagHash:X}.wav");
                        
                        lock(finalItems) { finalItems.Add(item); }

                        if (!File.Exists(item.outputFile)) {
                            if (!(item.resampler is WorldlineResampler)) VoicebankFiles.Inst.CopySourceTemp(item.inputFile, item.inputTemp);
                            if(!item.phone.direct) lock (Renderers.GetCacheLock(item.outputFile)) item.resampler.DoResamplerReturnsFile(item, Log.Logger);
                            if (!File.Exists(item.outputFile)) {
                                DocManager.Inst.Project.timeAxis.TickPosToBarBeat(item.phrase.position + item.phone.position, out int bar, out int beat, out int tick);
                                throw new InvalidDataException($"{item.resampler} failed to resample \"{item.phone.oto.Alias}\" at {bar}:{beat}.{string.Format("{0:000}", tick)}");
                            }
                            if (!(item.resampler is WorldlineResampler)) VoicebankFiles.Inst.CopyBackMetaFiles(item.inputFile, item.inputTemp);
                        }
                        progress.Complete(1, $"Track {trackNo + 1}: {phrase.wavtool} \"{phone.oto.Alias}\"");
                    }
                });

                foreach (var phone in phrase.phones) otoField.SetValue(phone, baseOtos[phone]);
                finalItems = finalItems.OrderBy(i => i.phone.position).ToList();
                result.samples = ToolsManager.Inst.GetWavtool(phrase.wavtool).Concatenate(finalItems, wavPath, cancellation);
                
                if (result.samples != null) Renderers.ApplyDynamics(phrase, result);
                return result;
            });
        }

        public RenderPitchResult LoadRenderedPitch(RenderPhrase phrase) { return null; }

        public UExpressionDescriptor[] GetSuggestedExpressions(USinger singer, URenderSettings renderSettings) {
            var expressions = new List<UExpressionDescriptor>();
            var manifest = renderSettings.Resampler.Manifest;
            if (manifest != null && manifest.expressions != null) expressions.AddRange(manifest.expressions.Values);

            if (singer != null && singer.Subbanks != null) {
                var uniqueColors = new HashSet<string>();
                foreach (var sub in singer.Subbanks) if (!string.IsNullOrEmpty(sub.Color)) uniqueColors.Add(sub.Color);
                int colorIndex = 1;
                foreach (var colorName in uniqueColors) {
                    expressions.Add(new UExpressionDescriptor { name = $"voice color {colorIndex:D2} {colorName}", abbr = $"cl{colorIndex:D2}", type = UExpressionType.MorphingCurve, min = 0, max = 100, defaultValue = 0, isFlag = false, flag = "" });
                    colorIndex++;
                }
            }
            return expressions.ToArray();
        }

        public override string ToString() => Renderers.CLASSIC;

        private bool TryHijackOto(USinger singer, RenderPhone phone, string targetColor, out UOto targetOto) {
            targetOto = null;
            if (singer == null || singer.Subbanks == null) return false;

            var targetSubbanks = singer.Subbanks.Where(b => b.Color == targetColor).ToList();
            if (targetSubbanks.Count == 0) return false;

            if (singer.TryGetMappedOto(phone.phoneme, phone.tone, targetColor, out targetOto)) {
                bool isCorrectColor = false;
                foreach(var tb in targetSubbanks) {
                    if ((string.IsNullOrEmpty(tb.Prefix) || targetOto.Alias.StartsWith(tb.Prefix)) && 
                        (string.IsNullOrEmpty(tb.Suffix) || targetOto.Alias.EndsWith(tb.Suffix))) {
                        isCorrectColor = true; break;
                    }
                }
                if (isCorrectColor) return true;
            }

            string rawAlias = phone.oto.Alias;
            var baseSubbank = singer.Subbanks.OrderByDescending(b => (b.Prefix?.Length ?? 0) + (b.Suffix?.Length ?? 0))
                                             .FirstOrDefault(b => (string.IsNullOrEmpty(b.Prefix) || rawAlias.StartsWith(b.Prefix)) && 
                                                                  (string.IsNullOrEmpty(b.Suffix) || rawAlias.EndsWith(b.Suffix)));

            string strippedAlias = rawAlias;
            if (baseSubbank != null) {
                if (!string.IsNullOrEmpty(baseSubbank.Prefix) && strippedAlias.StartsWith(baseSubbank.Prefix)) strippedAlias = strippedAlias.Substring(baseSubbank.Prefix.Length);
                if (!string.IsNullOrEmpty(baseSubbank.Suffix) && strippedAlias.EndsWith(baseSubbank.Suffix)) strippedAlias = strippedAlias.Substring(0, strippedAlias.Length - baseSubbank.Suffix.Length);
                foreach(var tb in targetSubbanks) if (singer.TryGetOto(tb.Prefix + strippedAlias + tb.Suffix, out targetOto)) return true;
            }

            string rawPhoneme = phone.phoneme;
            if (baseSubbank != null) {
                if (!string.IsNullOrEmpty(baseSubbank.Prefix) && rawPhoneme.StartsWith(baseSubbank.Prefix)) rawPhoneme = rawPhoneme.Substring(baseSubbank.Prefix.Length);
                if (!string.IsNullOrEmpty(baseSubbank.Suffix) && rawPhoneme.EndsWith(baseSubbank.Suffix)) rawPhoneme = rawPhoneme.Substring(0, rawPhoneme.Length - baseSubbank.Suffix.Length);
            }

            var bestSubbank = targetSubbanks.OrderBy(b => {
                if (b.toneSet == null || b.toneSet.Count == 0) return 999;
                return Math.Abs(b.toneSet.FirstOrDefault() - phone.tone);
            }).FirstOrDefault();

            if (bestSubbank != null) {
                if (singer.TryGetOto(bestSubbank.Prefix + rawPhoneme + bestSubbank.Suffix, out targetOto)) return true;
                if (singer.TryGetOto(bestSubbank.Prefix + strippedAlias + bestSubbank.Suffix, out targetOto)) return true;
            }

            foreach(var tb in targetSubbanks) if (singer.TryGetOto(tb.Prefix + rawPhoneme + tb.Suffix, out targetOto)) return true;
            return false;
        }

        private float[] ReadWavToFloats(string filePath) {
            using (var waveStream = Wave.OpenFile(filePath)) { return Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0)); }
        }

        private void WriteFloatsToWav(float[] samples, string filePath) {
            var waveFormat = new NAudio.Wave.WaveFormat(44100, 16, 1); 
            using (var writer = new NAudio.Wave.WaveFileWriter(filePath, waveFormat)) {
                short[] shortSamples = new short[samples.Length];
                for (int i = 0; i < samples.Length; i++) {
                    float sample = samples[i] * 32768f;
                    if (sample > 32767f) sample = 32767f;
                    if (sample < -32768f) sample = -32768f;
                    shortSamples[i] = (short)sample;
                }
                byte[] byteBuffer = new byte[shortSamples.Length * 2];
                Buffer.BlockCopy(shortSamples, 0, byteBuffer, 0, byteBuffer.Length);
                writer.Write(byteBuffer, 0, byteBuffer.Length);
            }
        }

        private void RunResamplerWithFlag(ResamplerItem baseItem, string tempFlag, string tempOutputPath) {
            if (!(baseItem.resampler is WorldlineResampler)) VoicebankFiles.Inst.CopySourceTemp(baseItem.inputFile, baseItem.inputTemp);
            var originalFlags = baseItem.flags;
            string originalOutput = baseItem.outputFile;
            try {
                baseItem.flags = new[] { Tuple.Create(tempFlag, (int?)null, (string)null) };
                baseItem.outputFile = tempOutputPath;
                lock (Renderers.GetCacheLock(tempOutputPath)) baseItem.resampler.DoResamplerReturnsFile(baseItem, Log.Logger);
                if (!File.Exists(tempOutputPath)) {
                    DocManager.Inst.Project.timeAxis.TickPosToBarBeat(baseItem.phrase.position + baseItem.phone.position, out int bar, out int beat, out int tick);
                    throw new InvalidDataException($"{baseItem.resampler} failed to resample \"{baseItem.phone.oto.Alias}\" at {bar}:{beat}.{string.Format("{0:000}", tick)}");
                }
            } finally { 
                baseItem.flags = originalFlags; 
                baseItem.outputFile = originalOutput; 
                if (!(baseItem.resampler is WorldlineResampler)) {
                    VoicebankFiles.Inst.CopyBackMetaFiles(baseItem.inputFile, baseItem.inputTemp);
                    try { 
                        if (baseItem.inputTemp.Contains("_hij_") && File.Exists(baseItem.inputTemp)) { 
                            File.Delete(baseItem.inputTemp); 
                            CleanUpMetaFiles(baseItem.inputTemp); 
                        } 
                    } catch { }
                }
            }
        }

        private int HashString(string str) {
            if (string.IsNullOrEmpty(str)) return 0;
            unchecked {
                int hash = 23;
                foreach (char c in str) hash = hash * 31 + c;
                return hash;
            }
        }

        private int GetUniversalCurveHash(RenderPhrase phrase, int trackNo) {
            int hash = 17;
            var part = DocManager.Inst.Project.parts.OfType<UVoicePart>().FirstOrDefault(p => p.trackNo == trackNo && p.position <= phrase.position && p.End >= phrase.position);
            if (part != null) {
                int startTick = phrase.position - part.position;
                int endTick = startTick + phrase.duration;
                for (int tick = startTick; tick <= endTick; tick += 15) {
                    foreach (var c in part.curves) if (!c.IsEmpty) {
                        hash = unchecked(hash * 31 + HashString(c.abbr)); 
                        hash = unchecked(hash * 31 + (int)Math.Round((double)c.Sample(tick)));
                    }
                }
            }
            return hash;
        }

        private bool TryGetMatrixData(RenderPhrase phrase, int trackNo, out string baseFlag, out List<MorphTrack> tracks, out UVoicePart activePart, out int baseHash) {
            baseFlag = ""; tracks = new List<MorphTrack>(); baseHash = 17;
            var project = DocManager.Inst.Project;
            activePart = project.parts.OfType<UVoicePart>().FirstOrDefault(p => p.trackNo == trackNo && p.position <= phrase.position && p.End >= phrase.position);
            if (activePart == null) return false;

            int startTick = phrase.position - activePart.position;
            int endTick = startTick + phrase.duration;
            double phraseStartMs = phrase.positionMs - phrase.leadingMs;
            int sampleCount = (int)((phrase.positionMs + phrase.durationMs + 200.0 - phraseStartMs) / 5.0) + 1;

            var morphingExps = project.expressions.Values.Where(exp => exp.type == UExpressionType.MorphingCurve);
            var dynamicExps = new List<(UExpressionDescriptor exp, UCurve c, float cMin, float cMax, string color)>();

            foreach (var exp in morphingExps) {
                var c = activePart.curves.FirstOrDefault(x => x.abbr == exp.abbr);
                if (c == null || c.IsEmpty) continue;

                float cMin = float.MaxValue, cMax = float.MinValue;
                for (int tick = startTick; tick <= endTick; tick += 15) {
                    float v = c.Sample(tick);
                    if (v < cMin) cMin = v;
                    if (v > cMax) cMax = v;
                }

                string matchedColor = null;
                var singer = project.tracks[trackNo].Singer;
                if (singer != null && singer.Subbanks != null) {
                    foreach (var sub in singer.Subbanks) if (!string.IsNullOrEmpty(sub.Color) && exp.name.IndexOf(sub.Color, StringComparison.OrdinalIgnoreCase) >= 0) matchedColor = sub.Color;
                }

                if (matchedColor != null) {
                    if (cMax > 0.05f) dynamicExps.Add((exp, c, cMin, cMax, matchedColor));
                } else {
                    float defVal = exp.defaultValue;
                    if (cMax > defVal + 0.1f || cMin < defVal - 0.1f) {
                        dynamicExps.Add((exp, c, cMin, cMax, null));
                        string resFlag = string.IsNullOrEmpty(exp.flag) ? exp.abbr : exp.flag;
                        baseFlag += $"{resFlag}{(int)Math.Round((double)defVal)}"; 
                        baseHash = unchecked(baseHash * 31 + HashString(exp.abbr)); 
                    }
                }
            }

            // Protect consonant phase integrity
            bool[] isVoiced = new bool[sampleCount];
            for (int i = 0; i < sampleCount; i++) isVoiced[i] = true;
            foreach (var phone in phrase.phones) {
                double startMs = phone.positionMs - phone.leadingMs;
                double preutterMs = phone.positionMs;
                int startIdx = Math.Max(0, (int)((startMs - phraseStartMs) / 5.0));
                int endIdx = Math.Min(sampleCount - 1, (int)((preutterMs - phraseStartMs) / 5.0));
                for (int i = startIdx; i <= endIdx; i++) isVoiced[i] = false;
            }

            foreach (var data in dynamicExps) {
                if (data.color != null) {
                    float[] rawWeights = new float[sampleCount];
                    float[] finalWeights = new float[sampleCount];
                    int trackHash = 17;
                    trackHash = unchecked(trackHash * 31 + HashString(data.color)); 

                    for (int i = 0; i < sampleCount; i++) {
                        int tick = project.timeAxis.MsPosToTickPos(phraseStartMs + (i * 5.0)) - activePart.position;
                        float val = Math.Clamp(data.c.Sample(tick), 0, 100);
                        rawWeights[i] = isVoiced[i] ? val : (val > 50f ? 100f : 0f); 
                    }
                    
                    // 5-tap moving average to stop STFT cliff popping
                    for (int i = 0; i < sampleCount; i++) {
                        if (i < 2 || i >= sampleCount - 2) finalWeights[i] = rawWeights[i];
                        else finalWeights[i] = (rawWeights[i - 2] + rawWeights[i - 1] + rawWeights[i] + rawWeights[i + 1] + rawWeights[i + 2]) / 5f;
                        trackHash = unchecked(trackHash * 31 + (int)Math.Round(finalWeights[i]));
                    }

                    baseHash = unchecked(baseHash * 31 + trackHash);
                    tracks.Add(new MorphTrack { TargetColor = data.color, Abbr = data.exp.abbr, Flag = baseFlag, Weights = finalWeights, Hash = trackHash });
                } else {
                    float defVal = data.exp.defaultValue;

                    if (data.cMax > defVal + 0.1f) {
                        float[] rawWeights = new float[sampleCount];
                        float[] finalWeights = new float[sampleCount];
                        int trackHash = 17;
                        trackHash = unchecked(trackHash * 31 + HashString(data.exp.abbr)); 

                        for (int i = 0; i < sampleCount; i++) {
                            int tick = project.timeAxis.MsPosToTickPos(phraseStartMs + (i * 5.0)) - activePart.position;
                            float val = data.c.Sample(tick);
                            float rw = val > defVal ? Math.Clamp(((val - defVal) / Math.Max(0.001f, data.exp.max - defVal)) * 100f, 0, 100) : 0;
                            rawWeights[i] = isVoiced[i] ? rw : (rw > 50f ? 100f : 0f);
                        }

                        for (int i = 0; i < sampleCount; i++) {
                            if (i < 2 || i >= sampleCount - 2) finalWeights[i] = rawWeights[i];
                            else finalWeights[i] = (rawWeights[i - 2] + rawWeights[i - 1] + rawWeights[i] + rawWeights[i + 1] + rawWeights[i + 2]) / 5f;
                            trackHash = unchecked(trackHash * 31 + (int)Math.Round(finalWeights[i]));
                        }

                        string trkFlag = "";
                        foreach(var d2 in dynamicExps) {
                            if (d2.color != null) continue;
                            string r2 = string.IsNullOrEmpty(d2.exp.flag) ? d2.exp.abbr : d2.exp.flag;
                            trkFlag += (d2.exp.abbr == data.exp.abbr) ? $"{r2}{(int)Math.Round((double)data.cMax)}" : $"{r2}{(int)Math.Round((double)d2.exp.defaultValue)}";
                        }
                        baseHash = unchecked(baseHash * 31 + trackHash);
                        tracks.Add(new MorphTrack { TargetColor = null, Abbr = data.exp.abbr, Flag = trkFlag, Weights = finalWeights, Hash = trackHash });
                    }
                    if (data.cMin < defVal - 0.1f) {
                        float[] rawWeights = new float[sampleCount];
                        float[] finalWeights = new float[sampleCount];
                        int trackHash = 17;
                        trackHash = unchecked(trackHash * 31 + HashString(data.exp.abbr));

                        for (int i = 0; i < sampleCount; i++) {
                            int tick = project.timeAxis.MsPosToTickPos(phraseStartMs + (i * 5.0)) - activePart.position;
                            float val = data.c.Sample(tick);
                            float rw = val < defVal ? Math.Clamp(((defVal - val) / Math.Max(0.001f, defVal - data.exp.min)) * 100f, 0, 100) : 0;
                            rawWeights[i] = isVoiced[i] ? rw : (rw > 50f ? 100f : 0f);
                        }

                        for (int i = 0; i < sampleCount; i++) {
                            if (i < 2 || i >= sampleCount - 2) finalWeights[i] = rawWeights[i];
                            else finalWeights[i] = (rawWeights[i - 2] + rawWeights[i - 1] + rawWeights[i] + rawWeights[i + 1] + rawWeights[i + 2]) / 5f;
                            trackHash = unchecked(trackHash * 31 + (int)Math.Round(finalWeights[i]));
                        }

                        string trkFlag = "";
                        foreach(var d2 in dynamicExps) {
                            if (d2.color != null) continue;
                            string r2 = string.IsNullOrEmpty(d2.exp.flag) ? d2.exp.abbr : d2.exp.flag;
                            trkFlag += (d2.exp.abbr == data.exp.abbr) ? $"{r2}{(int)Math.Round((double)data.cMin)}" : $"{r2}{(int)Math.Round((double)d2.exp.defaultValue)}";
                        }
                        baseHash = unchecked(baseHash * 31 + trackHash);
                        tracks.Add(new MorphTrack { TargetColor = null, Abbr = data.exp.abbr, Flag = trkFlag, Weights = finalWeights, Hash = trackHash });
                    }
                }
            }
            return tracks.Count > 0;
        }

        private bool TryGetMatrixDataAlias(RenderPhrase phrase, RenderPhone phone, int trackNo, out string baseFlag, out List<MorphTrack> tracks, out UVoicePart activePart, out int baseHash) {
            baseFlag = ""; tracks = new List<MorphTrack>(); baseHash = 17;
            var project = DocManager.Inst.Project;
            int phoneAbsoluteTick = phrase.position + phone.position;
            activePart = project.parts.OfType<UVoicePart>().FirstOrDefault(p => p.trackNo == trackNo && p.position <= phoneAbsoluteTick && p.End >= phoneAbsoluteTick);
            if (activePart == null) return false;

            int startTick = (phrase.position + phone.position) - activePart.position;
            int endTick = startTick + phone.duration;
            double phonePosMs = phone.positionMs - phone.leadingMs; 
            int sampleCount = (int)((phone.endMs - phonePosMs) / 5.0) + 1;

            var morphingExps = project.expressions.Values.Where(exp => exp.type == UExpressionType.MorphingCurve);
            var dynamicExps = new List<(UExpressionDescriptor exp, UCurve c, float cMin, float cMax, string color)>();

            foreach (var exp in morphingExps) {
                var c = activePart.curves.FirstOrDefault(x => x.abbr == exp.abbr);
                if (c == null || c.IsEmpty) continue;

                float cMin = float.MaxValue, cMax = float.MinValue;
                for (int tick = startTick; tick <= endTick; tick += 15) {
                    float v = c.Sample(tick);
                    if (v < cMin) cMin = v;
                    if (v > cMax) cMax = v;
                }

                string matchedColor = null;
                var singer = project.tracks[trackNo].Singer;
                if (singer != null && singer.Subbanks != null) {
                    foreach (var sub in singer.Subbanks) if (!string.IsNullOrEmpty(sub.Color) && exp.name.IndexOf(sub.Color, StringComparison.OrdinalIgnoreCase) >= 0) matchedColor = sub.Color;
                }

                if (matchedColor != null) {
                    if (cMax > 0.05f) dynamicExps.Add((exp, c, cMin, cMax, matchedColor));
                } else {
                    float defVal = exp.defaultValue;
                    if (cMax > defVal + 0.1f || cMin < defVal - 0.1f) {
                        dynamicExps.Add((exp, c, cMin, cMax, null));
                        string resFlag = string.IsNullOrEmpty(exp.flag) ? exp.abbr : exp.flag;
                        baseFlag += $"{resFlag}{(int)Math.Round((double)defVal)}";
                        baseHash = unchecked(baseHash * 31 + HashString(exp.abbr)); 
                    }
                }
            }

            // isVoiced for Alias level
            bool[] isVoiced = new bool[sampleCount];
            for (int i = 0; i < sampleCount; i++) isVoiced[i] = true;
            double startMs = phone.positionMs - phone.leadingMs;
            double preutterMs = phone.positionMs;
            int startIdx = Math.Max(0, (int)((startMs - phonePosMs) / 5.0));
            int endIdx = Math.Min(sampleCount - 1, (int)((preutterMs - phonePosMs) / 5.0));
            for (int i = startIdx; i <= endIdx; i++) isVoiced[i] = false;

            foreach (var data in dynamicExps) {
                if (data.color != null) {
                    float[] rawWeights = new float[sampleCount];
                    float[] finalWeights = new float[sampleCount];
                    int trackHash = 17;
                    trackHash = unchecked(trackHash * 31 + HashString(data.color)); 

                    for (int i = 0; i < sampleCount; i++) {
                        int tick = project.timeAxis.MsPosToTickPos(phonePosMs + (i * 5.0)) - activePart.position;
                        float val = Math.Clamp(data.c.Sample(tick), 0, 100);
                        rawWeights[i] = isVoiced[i] ? val : (val > 50f ? 100f : 0f);
                    }

                    for (int i = 0; i < sampleCount; i++) {
                        if (i < 2 || i >= sampleCount - 2) finalWeights[i] = rawWeights[i];
                        else finalWeights[i] = (rawWeights[i - 2] + rawWeights[i - 1] + rawWeights[i] + rawWeights[i + 1] + rawWeights[i + 2]) / 5f;
                        trackHash = unchecked(trackHash * 31 + (int)Math.Round(finalWeights[i]));
                    }

                    baseHash = unchecked(baseHash * 31 + trackHash);
                    tracks.Add(new MorphTrack { TargetColor = data.color, Abbr = data.exp.abbr, Flag = baseFlag, Weights = finalWeights, Hash = trackHash });
                } else {
                    float defVal = data.exp.defaultValue;

                    if (data.cMax > defVal + 0.1f) { 
                        float[] rawWeights = new float[sampleCount];
                        float[] finalWeights = new float[sampleCount];
                        int trackHash = 17;
                        trackHash = unchecked(trackHash * 31 + HashString(data.exp.abbr)); 

                        for (int i = 0; i < sampleCount; i++) {
                            int tick = project.timeAxis.MsPosToTickPos(phonePosMs + (i * 5.0)) - activePart.position;
                            float val = data.c.Sample(tick);
                            float rw = val > defVal ? Math.Clamp(((val - defVal) / Math.Max(0.001f, data.exp.max - defVal)) * 100f, 0, 100) : 0;
                            rawWeights[i] = isVoiced[i] ? rw : (rw > 50f ? 100f : 0f);
                        }

                        for (int i = 0; i < sampleCount; i++) {
                            if (i < 2 || i >= sampleCount - 2) finalWeights[i] = rawWeights[i];
                            else finalWeights[i] = (rawWeights[i - 2] + rawWeights[i - 1] + rawWeights[i] + rawWeights[i + 1] + rawWeights[i + 2]) / 5f;
                            trackHash = unchecked(trackHash * 31 + (int)Math.Round(finalWeights[i]));
                        }

                        string trkFlag = "";
                        foreach(var d2 in dynamicExps) {
                            if (d2.color != null) continue;
                            string r2 = string.IsNullOrEmpty(d2.exp.flag) ? d2.exp.abbr : d2.exp.flag;
                            trkFlag += (d2.exp.abbr == data.exp.abbr) ? $"{r2}{(int)Math.Round((double)data.cMax)}" : $"{r2}{(int)Math.Round((double)d2.exp.defaultValue)}";
                        }
                        baseHash = unchecked(baseHash * 31 + trackHash);
                        tracks.Add(new MorphTrack { TargetColor = null, Abbr = data.exp.abbr, Flag = trkFlag, Weights = finalWeights, Hash = trackHash });
                    }
                    if (data.cMin < defVal - 0.1f) { 
                        float[] rawWeights = new float[sampleCount];
                        float[] finalWeights = new float[sampleCount];
                        int trackHash = 17;
                        trackHash = unchecked(trackHash * 31 + HashString(data.exp.abbr)); 

                        for (int i = 0; i < sampleCount; i++) {
                            int tick = project.timeAxis.MsPosToTickPos(phonePosMs + (i * 5.0)) - activePart.position;
                            float val = data.c.Sample(tick);
                            float rw = val < defVal ? Math.Clamp(((defVal - val) / Math.Max(0.001f, defVal - data.exp.min)) * 100f, 0, 100) : 0;
                            rawWeights[i] = isVoiced[i] ? rw : (rw > 50f ? 100f : 0f);
                        }

                        for (int i = 0; i < sampleCount; i++) {
                            if (i < 2 || i >= sampleCount - 2) finalWeights[i] = rawWeights[i];
                            else finalWeights[i] = (rawWeights[i - 2] + rawWeights[i - 1] + rawWeights[i] + rawWeights[i + 1] + rawWeights[i + 2]) / 5f;
                            trackHash = unchecked(trackHash * 31 + (int)Math.Round(finalWeights[i]));
                        }

                        string trkFlag = "";
                        foreach(var d2 in dynamicExps) {
                            if (d2.color != null) continue;
                            string r2 = string.IsNullOrEmpty(d2.exp.flag) ? d2.exp.abbr : d2.exp.flag;
                            trkFlag += (d2.exp.abbr == data.exp.abbr) ? $"{r2}{(int)Math.Round((double)data.cMin)}" : $"{r2}{(int)Math.Round((double)d2.exp.defaultValue)}";
                        }
                        baseHash = unchecked(baseHash * 31 + trackHash);
                        tracks.Add(new MorphTrack { TargetColor = null, Abbr = data.exp.abbr, Flag = trkFlag, Weights = finalWeights, Hash = trackHash });
                    }
                }
            }
            return tracks.Count > 0;
        }
        // Garbage collector for morphing cache, auto deletes them to prevent cache folder inflation
        private void CleanUpMetaFiles(string filePath) {
            if (string.IsNullOrEmpty(filePath)) return;
            string ext = Path.GetExtension(filePath);
            string noExt = filePath.Substring(0, filePath.Length - ext.Length);
            string frqExt = ext.Replace('.', '_') + ".frq";
            
            string[] pathsToDelete = new string[] {
                noExt + frqExt,
                filePath + ".llsm",
                filePath + ".uspec",
                filePath + ".dio",
                filePath + ".star",
                filePath + ".platinum",
                filePath + ".frc",
                filePath + ".pmk",
                filePath + ".vs4ufrq",
                noExt + ".rudb",
                noExt + ".sc.npz",
                noExt + ".sc",
                noExt + ".hifi.npz"
            };

            foreach (string p in pathsToDelete) {
                try { if (File.Exists(p)) File.Delete(p); } catch { }
            }
        }
    }
}