using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NWaves.Audio;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Options;
using NWaves.Filters.Fda;
using NWaves.Utils;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using ScottPlot;
using ScottPlot.Plottable;
using Serilog;

namespace OpenUtau.App.Views {
    public partial class SingersDialog : Window, ICmdSubscriber {
        const int kFftSize = 1024;
        const int kMelSize = 80;

        Color blueFill;
        Color pinkFill;
        Color blueLine;

        double coordToMs;
        double totalDurMs;
        double lastPointerMs;

        WaveFile? wav;
        string? wavPath;
        IPlottable? waveform;
        IPlottable? spectrogram;
        IPlottable? freq;
        List<IPlottable> timingMarks = new List<IPlottable>();
        AxisLimits outerLimits;

        private bool editingCell = false;

        public SingersDialog() {
            try {
                InitializeComponent();

                OtoPlot.Configuration.LockVerticalAxis = true;
                OtoPlot.Configuration.MiddleClickDragZoom = false;
                OtoPlot.Configuration.ScrollWheelZoomFraction = 0.5;
                OtoPlot.RightClicked -= OtoPlot.DefaultRightClickEvent;
                if (Core.Util.Preferences.Default.Theme == 1) {
                    OtoPlot.Plot.Style(ScottPlot.Style.Gray1);
                }
                OtoPlot.Plot.Margins(0, 0);
                OtoPlot.Plot.Frameless();

                int argb = Color.LightBlue.ToArgb();
                argb = argb & 0x00FFFFFF | 0x7F000000;
                blueFill = Color.FromArgb(argb);
                argb = Color.Pink.ToArgb();
                argb = argb & 0x00FFFFFF | 0x7F000000;
                pinkFill = Color.FromArgb(argb);
                blueLine = ColorTranslator.FromHtml("#4EA6EA");
            } catch (Exception e) {
                Log.Error(e, "Failed to initialize component.");
            }
            DocManager.Inst.AddSubscriber(this);
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            DocManager.Inst.RemoveSubscriber(this);
        }

        void OnSingerMenuButton(object sender, RoutedEventArgs args) {
            SingerMenu.PlacementTarget = sender as Button;
            SingerMenu.Open();
        }

        void OnVisitWebsite(object sender, RoutedEventArgs args) {
            var viewModel = (DataContext as SingersViewModel)!;
            if (viewModel.Singer == null) {
                return;
            }
            try {
                OS.OpenWeb(viewModel.Singer.Web);
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(e));
            }
        }

        async void OnSetPortrait(object sender, RoutedEventArgs args) {
            var viewModel = (DataContext as SingersViewModel)!;
            if (viewModel.Singer == null) {
                return;
            }
            var dialog = new OpenFileDialog() {
                AllowMultiple = false,
                Directory = viewModel.Singer.Location,
            };
            var files = await dialog.ShowAsync(this);
            if (files == null || files.Length != 1) {
                return;
            }
            try {
                using (var stream = File.OpenRead(files[0])) {
                    var portrait = new Bitmap(stream);
                    portrait.Dispose();
                }
                viewModel.SetPortrait(Path.GetRelativePath(viewModel.Singer.Location, files[0]));
            } catch (Exception e) {
                Log.Error(e, "Failed to set portrait");
                _ = await MessageBox.ShowError(this, e);
            }
        }

        async void OnEditSubbanksButton(object sender, RoutedEventArgs args) {
            var viewModel = (DataContext as SingersViewModel)!;
            if (viewModel.Singer == null) {
                return;
            }
            var dialog = new EditSubbanksDialog();
            dialog.ViewModel.SetSinger(viewModel.Singer!);
            dialog.RefreshSinger = () => viewModel.RefreshSinger();
            await dialog.ShowDialog(this);
        }

        void OnSelectedSingerChanged(object sender, SelectionChangedEventArgs e) {
            OtoPlot?.Plot.Clear();
            OtoPlot?.Refresh();
        }

        void OnSelectedOtoChanged(object sender, SelectionChangedEventArgs e) {
            var viewModel = (DataContext as SingersViewModel)!;
            if (viewModel.Singer == null || e.AddedItems.Count < 1) {
                return;
            }
            var oto = (Core.Ustx.UOto?)e.AddedItems[0];
            if (oto == null || !File.Exists(oto.File)) {
                OtoPlot?.Plot.Clear();
                OtoPlot?.Refresh();
                wavPath = null;
                return;
            }
            DrawOto(oto, viewModel.ZoomInMel);
        }

        void OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e) {
            editingCell = true;
        }

        void OnCellEditEnded(object sender, DataGridCellEditEndedEventArgs e) {
            var viewModel = (DataContext as SingersViewModel)!;
            if (e.EditAction == DataGridEditAction.Commit) {
                viewModel?.NotifyOtoChanged();
            }
            editingCell = false;
        }

        void GotoSourceFile(object sender, RoutedEventArgs args) {
            var oto = OtoGrid?.SelectedItem as Core.Ustx.UOto;
            if (oto == null) {
                return;
            }
            try {
                OS.GotoFile(oto.File);
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(e));
            }
        }

        void GotoVLabelerOto(object sender, RoutedEventArgs args) {
            var viewModel = (DataContext as SingersViewModel)!;
            if (viewModel.Singer == null) {
                return;
            }
            var oto = OtoGrid?.SelectedItem as Core.Ustx.UOto;
            if (oto == null) {
                return;
            }
            if (viewModel.Singer != null) {
                OpenInVLabeler(viewModel.Singer, oto);
            }
        }

        void OnEditInVLabeler(object sender, RoutedEventArgs args) {
            var viewModel = (DataContext as SingersViewModel)!;
            if (viewModel.Singer != null) {
                OpenInVLabeler(viewModel.Singer, null);
            }
        }

        private void OpenInVLabeler(Core.Ustx.USinger singer, Core.Ustx.UOto? oto) {
            string path = Core.Util.Preferences.Default.VLabelerPath;
            if (string.IsNullOrEmpty(path) || !OS.AppExists(path)) {
                MessageBox.Show(
                    this,
                    ThemeManager.GetString("singers.editoto.setvlabelerpath"),
                    ThemeManager.GetString("errors.caption"),
                    MessageBox.MessageBoxButtons.Ok);
                return;
            }
            try {
                Integrations.VLabelerClient.Inst.GotoOto(singer, oto);
            } catch (Exception e) {
                MessageBox.Show(
                    this,
                    e.ToString(),
                    ThemeManager.GetString("errors.caption"),
                    MessageBox.MessageBoxButtons.Ok);
            }
        }

        void RegenFrq(object sender, RoutedEventArgs args) {
            if (OtoGrid != null &&
                sender is Control control &&
                DataContext is SingersViewModel viewModel) {
                string[] files = OtoGrid.SelectedItems
                    .Cast<Core.Ustx.UOto>()
                    .Select(oto => oto.File)
                    .ToHashSet()
                    .ToArray();
                MessageBox? msgbox = null;
                string text = ThemeManager.GetString("singers.editoto.regenfrq.regenerating");
                if (files.Length > 1) {
                    msgbox = MessageBox.ShowModal(this, text, text);
                }
                var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
                viewModel.RegenFrq(files, control.Tag as string, count => {
                    msgbox?.SetText(string.Format("{0}\n{1} / {2}", text, count, files.Length));
                }).ContinueWith(task => {
                    msgbox?.Close();
                    if (task.IsFaulted && task.Exception != null) {
                        MessageBox.ShowError(this, task.Exception);
                    } else {
                        DrawOto(viewModel.SelectedOto, viewModel.ZoomInMel, true);
                    }
                }, scheduler);
            }
        }

        void DrawOto(Core.Ustx.UOto? oto, bool zoomInMel, bool forceRedraw = false) {
            if (OtoPlot == null || oto == null) {
                return;
            }
            var limits = OtoPlot.Plot.GetAxisLimits();
            OtoPlot.Plot.SetAxisLimitsY(0, 120);
            bool loadWav = wavPath != oto.File || forceRedraw;
            if (loadWav) {
                try {
                    using (var memStream = new MemoryStream()) {
                        using (var waveStream = Core.Format.Wave.OpenFile(oto.File)) {
                            NAudio.Wave.WaveFileWriter.WriteWavFileToStream(memStream, waveStream);
                        }
                        memStream.Seek(0, SeekOrigin.Begin);
                        wav = new WaveFile(memStream);
                        wavPath = oto.File;
                    }
                    double hopSize = GetHopSize(wav.WaveFmt.SamplingRate);
                    outerLimits = new AxisLimits(0, wav.Signals[0].Length / hopSize, 0, 120);
                    OtoPlot.Plot.SetOuterViewLimits(
                        outerLimits.XMin, outerLimits.XMax,
                        outerLimits.YMin, outerLimits.YMax);
                } catch (Exception e) {
                    Log.Error(e, "failed to load wav");
                    wav = null;
                    wavPath = null;
                }
            }
            if (wav == null) {
                OtoPlot.Plot.Clear();
                return;
            }

            if (loadWav) {
                var samples = wav.Signals[0].Samples.Select(f => (double)f * 20 + 100).ToArray();
                var xs = Enumerable.Range(0, samples.Length)
                    .Select(i => i * 1.0 / GetHopSize(wav.WaveFmt.SamplingRate))
                    .ToArray();
                if (waveform != null) {
                    OtoPlot.Plot.Remove(waveform);
                }
                waveform = OtoPlot.Plot.AddSignalXY(xs, samples, color: Color.Blue);

                int zoomIn = zoomInMel ? 8 : 1;
                var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
                Task.Run(() => {
                    return Tuple.Create<double[,], Tuple<double[], double[]>>(LoadMel(wav, zoomIn), LoadF0(wav, oto.File, zoomIn));
                }).ContinueWith((Action<Task<Tuple<double[,], Tuple<double[], double[]>>>>)(task => {
                    if (spectrogram != null) {
                        this.OtoPlot.Plot.Remove(spectrogram);
                        spectrogram = null;
                    }
                    if (freq != null) {
                        this.OtoPlot.Plot.Remove(freq);
                        freq = null;
                    }
                    if (task.IsFaulted) {
                        return;
                    }
                    var mel = task.Result.Item1;
                    spectrogram = this.OtoPlot.Plot.AddHeatmap(mel, lockScales: (bool?)false);
                    if (task.Result.Item2 != null) {
                        var frqX = task.Result.Item2.Item1;
                        var frqY = task.Result.Item2.Item2;
                        freq = this.OtoPlot.Plot.AddSignalXY(frqX, frqY, color: Color.White);
                    }
                    DrawTiming(oto);
                    this.OtoPlot.Refresh();
                }), scheduler);
            }
            DrawTiming(oto);

            if (loadWav) {
                ZoomAll();
            } else {
                OtoPlot.Plot.SetAxisLimitsX(limits.XMin, limits.XMax);
            }
            OtoPlot.Refresh();
        }

        double[,] LoadMel(WaveFile wav, int zoomIn) {
            var bands = FilterBanks.MelBands(kMelSize, wav.WaveFmt.SamplingRate, highFreq: wav.WaveFmt.SamplingRate / 2 / zoomIn);
            int fftSize = kFftSize * Math.Max(1, zoomIn / 4);
            var extractor = new FilterbankExtractor(
               new FilterbankOptions {
                   SamplingRate = wav.WaveFmt.SamplingRate,
                   FrameSize = fftSize,
                   FftSize = fftSize,
                   HopSize = GetHopSize(wav.WaveFmt.SamplingRate),
                   Window = NWaves.Windows.WindowType.Hann,
                   FilterBank = FilterBanks.Triangular(
                       fftSize, wav.WaveFmt.SamplingRate, bands),
               });
            var padded = new float[fftSize / 2].Concat(wav.Signals[0].Samples).Concat(new float[fftSize / 2]).ToArray();
            var mel = extractor.ComputeFrom(padded);
            var heatmap = new double[mel[0].Length, mel.Count];
            for (int i = 0; i < mel.Count; i++) {
                for (int j = 0; j < mel[i].Length; j++) {
                    heatmap[kMelSize - 1 - j, i] = Math.Log(Math.Max(mel[i][j], 1e-4));
                }
            }
            return heatmap;
        }

        Tuple<double[], double[]>? LoadF0(WaveFile wav, string filepath, int zoomIn) {
            double[]? frqX = null;
            double[]? frqY = null;
            string frqFile = Classic.VoicebankFiles.GetFrqFile(filepath);
            if (File.Exists(frqFile)) {
                var frq = new Classic.Frq();
                using (var fileStream = File.OpenRead(frqFile)) {
                    frq.Load(fileStream);
                }
                frqX = Enumerable.Range(0, frq.f0.Length)
                    .Select(v => (double)v * frq.hopSize / wav.WaveFmt.SamplingRate * 400).ToArray();
                double high = Scale.HerzToMel(wav.WaveFmt.SamplingRate / 2 / zoomIn);
                double low = Scale.HerzToMel(0);
                double resolution = (high - low) / (kMelSize + 1);
                frqY = frq.f0.Select(v => Scale.HerzToMel(v) / resolution).ToArray();
            }
            return frqX == null || frqY == null ? null : Tuple.Create(frqX, frqY);
        }

        void DrawTiming(Core.Ustx.UOto oto) {
            if (OtoPlot == null || wav == null) {
                return;
            }
            foreach (var plottable in timingMarks) {
                OtoPlot.Plot.Remove(plottable);
            }
            timingMarks.Clear();

            int hopSize = GetHopSize(wav.WaveFmt.SamplingRate);
            var msToCoord = 0.001 * wav.WaveFmt.SamplingRate / hopSize;
            coordToMs = 1.0 / msToCoord;
            totalDurMs = wav.Signals[0].Duration * 1000.0;
            double cutoff = oto.Cutoff >= 0
                ? totalDurMs - oto.Cutoff
                : oto.Offset - oto.Cutoff;
            double offsetX = oto.Offset * msToCoord;
            double consonantX = (oto.Offset + oto.Consonant) * msToCoord;
            double preutterX = (oto.Offset + oto.Preutter) * msToCoord;
            double overlapX = (oto.Offset + oto.Overlap) * msToCoord;
            double cutoffX = cutoff * msToCoord;
            double durX = outerLimits.XMax;

            if (offsetX > 0) {
                timingMarks.Add(OtoPlot.Plot.AddPolygon(
                    new double[] { 0, 0, offsetX, offsetX },
                    new double[] { 80, 120, 120, 80 }, blueFill));
            }
            if (consonantX > offsetX) {
                timingMarks.Add(OtoPlot.Plot.AddPolygon(
                    new double[] { offsetX, offsetX, consonantX, consonantX },
                    new double[] { 80, 120, 120, 80 }, pinkFill));
            }
            if (cutoff <= wav.Signals[0].Length) {
                timingMarks.Add(OtoPlot.Plot.AddPolygon(
                    new double[] { cutoffX, cutoffX, durX, durX },
                    new double[] { 80, 120, 120, 80 }, blueFill));
            }

            timingMarks.Add(OtoPlot.Plot.AddLine(offsetX, 80, overlapX, 119, blueLine, 2));
            timingMarks.Add(OtoPlot.Plot.AddLine(overlapX, 119, cutoffX, 119, blueLine, 2));
            timingMarks.Add(OtoPlot.Plot.AddLine(cutoffX, 119, cutoffX, 80, blueLine, 2));

            timingMarks.Add(OtoPlot.Plot.AddVerticalLine(overlapX, Color.Lime));
            timingMarks.Add(OtoPlot.Plot.AddText("OVL", overlapX, 80, color: Color.Lime));
            timingMarks.Add(OtoPlot.Plot.AddVerticalLine(preutterX, Color.Red));
            timingMarks.Add(OtoPlot.Plot.AddText("PRE", preutterX, 80, color: Color.Red));
        }

        void ZoomAll() {
            if (OtoPlot == null || wav == null) {
                return;
            }
            OtoPlot.Plot.SetAxisLimitsX(outerLimits.XMin, outerLimits.XMax);
            OtoPlot.Plot.SetAxisLimitsY(0, 120);
        }

        int GetHopSize(int sampleRate) {
            return sampleRate / 400;
        }

        void OtoPlot_OnPointerMoved(object sender, PointerEventArgs args) {
            if (OtoPlot == null) {
                return;
            }
            var point = args.GetCurrentPoint(OtoPlot);
            lastPointerMs = OtoPlot.Plot.GetCoordinateX((float)point.Position.X) * coordToMs;
            lastPointerMs = Math.Clamp(lastPointerMs, 0, totalDurMs);
        }

        void OnZoomInMelChecked(object sender, RoutedEventArgs args) {
            if (DataContext is SingersViewModel viewModel) {
                DrawOto(viewModel.SelectedOto, true, true);
            }
        }

        void OnZoomInMelUnchecked(object sender, RoutedEventArgs args) {
            if (DataContext is SingersViewModel viewModel) {
                DrawOto(viewModel.SelectedOto, false, true);
            }
        }

        void OnKeyDown(object sender, KeyEventArgs args) {
            if (args.Handled || editingCell || (FocusManager?.GetFocusedElement() is TextBox)) {
                return;
            }
            var viewModel = DataContext as SingersViewModel;
            if (viewModel == null || OtoPlot == null) {
                return;
            }
            args.Handled = true;
            switch (args.Key) {
                case Key.D1:
                    viewModel.SetOffset(lastPointerMs, totalDurMs);
                    break;
                case Key.D2:
                    viewModel.SetOverlap(lastPointerMs, totalDurMs);
                    break;
                case Key.D3:
                    viewModel.SetPreutter(lastPointerMs, totalDurMs);
                    break;
                case Key.D4:
                    viewModel.SetFixed(lastPointerMs, totalDurMs);
                    break;
                case Key.D5:
                    viewModel.SetCutoff(lastPointerMs, totalDurMs);
                    break;
                case Key.W: {
                        var limites = OtoPlot.Plot.GetAxisLimits();
                        double span = limites.XSpan / 2;
                        OtoPlot.Plot.SetAxisLimitsX(
                            limites.XCenter - span * 0.5,
                            limites.XCenter + span * 0.5);
                        OtoPlot.Refresh();
                        break;
                    }
                case Key.S: {
                        var limites = OtoPlot.Plot.GetAxisLimits();
                        double span = limites.XSpan * 2;
                        OtoPlot.Plot.SetAxisLimitsX(
                            limites.XCenter - span * 0.5,
                            limites.XCenter + span * 0.5);
                        OtoPlot.Refresh();
                        break;
                    }
                case Key.A: {
                        var limites = OtoPlot.Plot.GetAxisLimits();
                        double shift = limites.XSpan * 0.5;
                        OtoPlot.Plot.SetAxisLimitsX(
                            limites.XMin - shift,
                            limites.XMax - shift);
                        OtoPlot.Refresh();
                        break;
                    }
                case Key.D: {
                        var limites = OtoPlot.Plot.GetAxisLimits();
                        double shift = limites.XSpan * 0.5;
                        OtoPlot.Plot.SetAxisLimitsX(
                            limites.XMin + shift,
                            limites.XMax + shift);
                        OtoPlot.Refresh();
                        break;
                    }
                case Key.Q: {
                        if (OtoGrid != null) {
                            OtoGrid.SelectedIndex = Math.Max(0, OtoGrid.SelectedIndex - 1);
                            OtoGrid.ScrollIntoView(OtoGrid.SelectedItem, null);
                        }
                        break;
                    }
                case Key.E: {
                        if (OtoGrid != null) {
                            OtoGrid.SelectedIndex++;
                            OtoGrid.ScrollIntoView(OtoGrid.SelectedItem, null);
                        }
                        break;
                    }
                case Key.F:
                    ZoomAll();
                    OtoPlot.Refresh();
                    break;
                default:
                    args.Handled = false;
                    break;
            }
        }

        #region ICmdSubscriber

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is OtoChangedNotification otoChanged) {
                var viewModel = DataContext as SingersViewModel;
                if (viewModel == null) {
                    return;
                }
                if (otoChanged.external) {
                    viewModel.RefreshSinger();
                }
                DrawOto(viewModel.SelectedOto, viewModel.ZoomInMel);
            } else if (cmd is GotoOtoNotification editOto) {
                var viewModel = DataContext as SingersViewModel;
                if (viewModel == null) {
                    return;
                }
                viewModel.GotoOto(editOto.singer, editOto.oto);
                OtoGrid?.ScrollIntoView(OtoGrid.SelectedItem, null);
                Activate();
            }
        }

        #endregion
    }
}
