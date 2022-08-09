using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NWaves.Audio;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Options;
using NWaves.Filters.Fda;
using OpenUtau.App.ViewModels;
using OpenUtau.Core.Ustx;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottable;
using Serilog;

namespace OpenUtau.App.Views {
    public partial class SingersDialog : Window {
        const int fftSize = 1024;
        const int melSize = 80;

        Color blueFill;
        Color pinkFill;
        Color blueLine;

        DataGrid? otoGrid;
        AvaPlot? otoPlot;
        double coordToMs;
        double lastPointerMs;

        WaveFile? wav;
        string? wavPath;
        IPlottable? waveform;
        IPlottable? spectrogram;
        List<IPlottable> timingMarks = new List<IPlottable>();
        AxisLimits outerLimites;

        public SingersDialog() {
            try {
                InitializeComponent();

                otoGrid = this.Find<DataGrid>("OtoGrid");
                otoPlot = this.Find<AvaPlot>("OtoPlot");
                otoPlot.Configuration.LockVerticalAxis = true;
                otoPlot.Configuration.MiddleClickDragZoom = false;
                otoPlot.Configuration.ScrollWheelZoomFraction = 0.5;
                otoPlot.RightClicked -= otoPlot.DefaultRightClickEvent;
                if (Core.Util.Preferences.Default.Theme == 1) {
                    otoPlot.Plot.Style(ScottPlot.Style.Gray1);
                }
                otoPlot.Plot.Margins(0, 0);
                otoPlot.Plot.Frameless();

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
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        void OnSingerMenuButton(object sender, RoutedEventArgs args) {
            var menu = this.FindControl<ContextMenu>("SingerMenu");
            menu.PlacementTarget = sender as Button;
            menu.Open();
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
                _ = await MessageBox.Show(
                     this,
                     e.ToString(),
                     ThemeManager.GetString("errors.caption"),
                     MessageBox.MessageBoxButtons.Ok);
            }
        }

        private void ExportOtoButtonClick(object sender, RoutedEventArgs e) {
            var viewModel = DataContext as SingersViewModel;
            if (viewModel is not null && otoGrid is not null) {

                // get whole grid data for that vb
                var otoGridSingerViewModel = otoGrid.DataContext as SingersViewModel;
                if (otoGridSingerViewModel is null) {
                    return;
                }

                // get location to save otos
                var p = viewModel.Singer?.Location;
                var o = viewModel.Singer?.Otos;

                List<UOto> otosToSave = new();
                otosToSave.AddRange(otoGridSingerViewModel.Otos);

                string savePath = Path.GetDirectoryName(otosToSave[0].File) ?? string.Empty;

                // nothing to save, return early
                if (!otosToSave.Any()) {
                    return;
                }

                // make string from otosToSave
                StringBuilder sb = new();
                otosToSave.ForEach((x) => {
                    sb.Append($"{GetOtoFilePath(p, x.File)},{x.Alias},{x.Offset},{x.Consonant},{x.Cutoff},{x.Preutter},{x.Overlap}\n");
                });
                var generatedOtoString = sb.ToString();

                var sfd = new SaveFileDialog();
                sfd.DefaultExtension = ".ini";
                sfd.InitialFileName = "oto.ini";
                sfd.Directory = savePath;
                sfd.Title = "Export oto text";
                var saveTask = Task.Run(async () => {
                    string? wantSaveLocation = await sfd.ShowAsync(this);
                    if (wantSaveLocation is not null) {
                        await File.WriteAllTextAsync(wantSaveLocation, generatedOtoString);
                    }
                });
                saveTask.Wait();

                return;

            } else {
                throw new Exception("unable to find viewmodel when attempting to save otos!");
            }
        }

        private string GetOtoFilePath(string basePath, string fullWavPath) {
            if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(fullWavPath)) {
                throw new ArgumentException("GetOtoFilePath called and either basePath or fullWavPath was null or empty");
            }
            // +1 to trim leading slash
            return fullWavPath.Substring(basePath.Length + 1).Replace('\\', '/');
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

        void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            var viewModel = (DataContext as SingersViewModel)!;
            if (viewModel.Singer == null || e.AddedItems.Count < 1) {
                return;
            }
            var oto = (Core.Ustx.UOto?)e.AddedItems[0];
            if (oto == null || !File.Exists(oto.File)) {
                return;
            }
            DrawOto(oto, true);
        }

        void DrawOto(Core.Ustx.UOto oto, bool fit = false) {
            if (otoPlot == null) {
                return;
            }
            var limites = otoPlot.Plot.GetAxisLimits();
            bool loadWav = wavPath != oto.File;
            if (loadWav) {
                try {
                    using (var stream = File.OpenRead(oto.File)) {
                        wav = new WaveFile(stream);
                        wavPath = oto.File;
                    }
                    double hopSize = GetHopSize(wav.WaveFmt.SamplingRate);
                    outerLimites = new AxisLimits(0, wav.Signals[0].Length / hopSize, 0, 120);
                    otoPlot.Plot.SetOuterViewLimits(
                        outerLimites.XMin, outerLimites.XMax,
                        outerLimites.YMin, outerLimites.YMax);
                    otoPlot.Plot.SetAxisLimitsY(
                        outerLimites.YMin, outerLimites.YMax);
                } catch (Exception e) {
                    Log.Error(e, "failed to load wav");
                    wav = null;
                    wavPath = null;
                }
            }
            if (wav == null) {
                otoPlot.Plot.Clear();
                return;
            }

            if (loadWav) {
                var samples = wav.Signals[0].Samples.Select(f => (double)f * 20 + 100).ToArray();
                var xs = Enumerable.Range(0, samples.Length)
                    .Select(i => i * 1.0 / GetHopSize(wav.WaveFmt.SamplingRate))
                    .ToArray();
                if (waveform != null) {
                    otoPlot.Plot.Remove(waveform);
                }
                waveform = otoPlot.Plot.AddSignalXY(xs, samples, color: Color.Blue);

                var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
                Task.Run(() => {
                    var bands = FilterBanks.MelBands(melSize, wav.WaveFmt.SamplingRate);
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
                            heatmap[melSize - 1 - j, i] = Math.Log(Math.Max(mel[i][j], 1e-4));
                        }
                    }
                    return heatmap;
                }).ContinueWith(heatmap => {
                    if (spectrogram != null) {
                        otoPlot.Plot.Remove(spectrogram);
                        spectrogram = null;
                    }
                    if (heatmap.IsFaulted) {
                        return;
                    }
                    spectrogram = otoPlot.Plot.AddHeatmap(heatmap.Result, lockScales: false);
                    DrawTiming(oto);
                    otoPlot.Refresh();
                }, scheduler);
            }
            DrawTiming(oto);

            if (loadWav) {
                ZoomAll();
            } else {
                otoPlot.Plot.SetAxisLimits(limites);
            }
            otoPlot.Refresh();
        }

        void DrawTiming(Core.Ustx.UOto oto) {
            if (otoPlot == null || wav == null) {
                return;
            }
            foreach (var plottable in timingMarks) {
                otoPlot.Plot.Remove(plottable);
            }
            timingMarks.Clear();

            int hopSize = GetHopSize(wav.WaveFmt.SamplingRate);
            var msToCoord = 0.001 * wav.WaveFmt.SamplingRate / hopSize;
            coordToMs = 1.0 / msToCoord;
            double cutoff = oto.Cutoff > 0
                ? (wav.Signals[0].Duration * 1000.0 - oto.Cutoff)
                : oto.Offset - oto.Cutoff;
            double offsetX = oto.Offset * msToCoord;
            double consonantX = (oto.Offset + oto.Consonant) * msToCoord;
            double preutterX = (oto.Offset + oto.Preutter) * msToCoord;
            double overlapX = (oto.Offset + oto.Overlap) * msToCoord;
            double cutoffX = cutoff * msToCoord;
            double durX = outerLimites.XMax;

            if (offsetX > 0) {
                timingMarks.Add(otoPlot.Plot.AddPolygon(
                    new double[] { 0, 0, offsetX, offsetX },
                    new double[] { 80, 120, 120, 80 }, blueFill));
            }
            if (consonantX > offsetX) {
                timingMarks.Add(otoPlot.Plot.AddPolygon(
                    new double[] { offsetX, offsetX, consonantX, consonantX },
                    new double[] { 80, 120, 120, 80 }, pinkFill));
            }
            if (cutoff <= wav.Signals[0].Length) {
                timingMarks.Add(otoPlot.Plot.AddPolygon(
                    new double[] { cutoffX, cutoffX, durX, durX },
                    new double[] { 80, 120, 120, 80 }, blueFill));
            }

            timingMarks.Add(otoPlot.Plot.AddLine(offsetX, 80, overlapX, 119, blueLine, 2));
            timingMarks.Add(otoPlot.Plot.AddLine(overlapX, 119, cutoffX, 119, blueLine, 2));
            timingMarks.Add(otoPlot.Plot.AddLine(cutoffX, 119, cutoffX, 80, blueLine, 2));

            timingMarks.Add(otoPlot.Plot.AddVerticalLine(overlapX, Color.Lime));
            timingMarks.Add(otoPlot.Plot.AddText("OVL", overlapX, 80, color: Color.Lime));
            timingMarks.Add(otoPlot.Plot.AddVerticalLine(preutterX, Color.Red));
            timingMarks.Add(otoPlot.Plot.AddText("PRE", preutterX, 80, color: Color.Red));
        }

        void ZoomAll() {
            if (otoPlot == null || wav == null) {
                return;
            }
            otoPlot.Plot.SetAxisLimitsX(outerLimites.XMin, outerLimites.XMax);
        }

        int GetHopSize(int sampleRate) {
            return sampleRate / 400;
        }

        void OtoPlot_OnPointerMoved(object sender, PointerEventArgs args) {
            if (otoPlot == null) {
                return;
            }
            var point = args.GetCurrentPoint(otoPlot);
            lastPointerMs = otoPlot.Plot.GetCoordinateX((float)point.Position.X) * coordToMs;
        }

        void OnKeyDown(object sender, KeyEventArgs args) {
            if (otoPlot == null) {
                return;
            }
            var viewModel = DataContext as SingersViewModel;
            if (viewModel == null) {
                return;
            }
            args.Handled = true;
            switch (args.Key) {
                case Key.D1:
                    viewModel.SetOffset(lastPointerMs);
                    DrawOto(viewModel.SelectedOto);
                    break;
                case Key.D2:
                    viewModel.SetOverlap(lastPointerMs);
                    DrawOto(viewModel.SelectedOto);
                    break;
                case Key.D3:
                    viewModel.SetPreutter(lastPointerMs);
                    DrawOto(viewModel.SelectedOto);
                    break;
                case Key.D4:
                    viewModel.SetFixed(lastPointerMs);
                    DrawOto(viewModel.SelectedOto);
                    break;
                case Key.D5:
                    viewModel.SetCutoff(lastPointerMs);
                    DrawOto(viewModel.SelectedOto);
                    break;
                case Key.W: {
                        var limites = otoPlot.Plot.GetAxisLimits();
                        double span = limites.XSpan / 2;
                        otoPlot.Plot.SetAxisLimitsX(
                            limites.XCenter - span * 0.5,
                            limites.XCenter + span * 0.5);
                        otoPlot.Refresh();
                        break;
                    }
                case Key.S: {
                        var limites = otoPlot.Plot.GetAxisLimits();
                        double span = limites.XSpan * 2;
                        otoPlot.Plot.SetAxisLimitsX(
                            limites.XCenter - span * 0.5,
                            limites.XCenter + span * 0.5);
                        otoPlot.Refresh();
                        break;
                    }
                case Key.A: {
                        var limites = otoPlot.Plot.GetAxisLimits();
                        double shift = limites.XSpan * 0.5;
                        otoPlot.Plot.SetAxisLimitsX(
                            limites.XMin - shift,
                            limites.XMax - shift);
                        otoPlot.Refresh();
                        break;
                    }
                case Key.D: {
                        var limites = otoPlot.Plot.GetAxisLimits();
                        double shift = limites.XSpan * 0.5;
                        otoPlot.Plot.SetAxisLimitsX(
                            limites.XMin + shift,
                            limites.XMax + shift);
                        otoPlot.Refresh();
                        break;
                    }
                case Key.Q: {
                        if (otoGrid != null) {
                            otoGrid.SelectedIndex = Math.Max(0, otoGrid.SelectedIndex - 1);
                            otoGrid.ScrollIntoView(otoGrid.SelectedItem, otoGrid.Columns[0]);
                        }
                        break;
                    }
                case Key.E: {
                        if (otoGrid != null) {
                            otoGrid.SelectedIndex++;
                            otoGrid.ScrollIntoView(otoGrid.SelectedItem, otoGrid.Columns[0]);
                        }
                        break;
                    }
                case Key.F:
                    ZoomAll();
                    otoPlot.Refresh();
                    break;
                default:
                    args.Handled = false;
                    break;
            }
        }
    }
}
