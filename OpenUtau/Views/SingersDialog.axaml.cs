using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NWaves.Audio;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Options;
using NWaves.Filters.Fda;
using OpenUtau.App.ViewModels;
using ScottPlot.Avalonia;

namespace OpenUtau.App.Views {
    public partial class SingersDialog : Window {
        Color blueFill;
        Color pinkFill;

        AvaPlot wavePlot;
        AvaPlot specPlot;

        public SingersDialog() {
            InitializeComponent();
            wavePlot = this.Find<AvaPlot>("WavePlot");
            wavePlot.Configuration.Pan = false;
            wavePlot.Configuration.Zoom = false;
            if (Core.Util.Preferences.Default.Theme == 1) {
                wavePlot.Plot.Style(ScottPlot.Style.Gray1);
            }
            wavePlot.Plot.Margins(0, 0);
            wavePlot.Plot.Frameless();
            specPlot = this.Find<AvaPlot>("SpecPlot");
            specPlot.Configuration.Pan = false;
            specPlot.Configuration.Zoom = false;
            if (Core.Util.Preferences.Default.Theme == 1) {
                specPlot.Plot.Style(ScottPlot.Style.Gray1);
            }
            specPlot.Plot.Margins(0, 0);
            specPlot.Plot.Frameless();
            int argb = Color.LightBlue.ToArgb();
            argb = argb & 0x00FFFFFF | 0x7F000000;
            blueFill = Color.FromArgb(argb);
            argb = Color.Pink.ToArgb();
            argb = argb & 0x00FFFFFF | 0x7F000000;
            pinkFill = Color.FromArgb(argb);
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
            if (oto == null || !File.Exists(oto.Value.File)) {
                return;
            }
            DrawOto(oto.Value);
        }

        void DrawOto(Core.Ustx.UOto oto) {
            wavePlot.Plot.Clear();
            specPlot.Plot.Clear();
            using var stream = File.OpenRead(oto.File);
            var wav = new WaveFile(stream);
            int hopSize = wav.WaveFmt.SamplingRate / 100;
            var samples = wav.Signals[0].Samples.Select(f => (double)f).ToArray();

            double msToX = wav.WaveFmt.SamplingRate / 1000;
            double offset = oto.Offset * msToX;
            double consonant = (oto.Offset + oto.Consonant) * msToX;
            double cutoffMs = oto.Cutoff > 0
                ? (wav.Signals[0].Duration * 1000.0 - oto.Cutoff)
                : oto.Offset + oto.Preutter - oto.Cutoff;
            double cutoff = cutoffMs * msToX;
            wavePlot.Plot.AddPolygon(
                new double[] { 0, 0, offset, offset },
                new double[] { -1, 1, 1, -1 }, blueFill);
            wavePlot.Plot.AddPolygon(
                new double[] { offset, offset, consonant, consonant },
                new double[] { -1, 1, 1, -1 }, pinkFill);
            if (cutoff <= samples.Length) {
                wavePlot.Plot.AddPolygon(
                    new double[] { cutoff, cutoff, samples.Length, samples.Length },
                    new double[] { -1, 1, 1, -1 }, blueFill);
            }
            wavePlot.Plot.AddSignal(samples, color: Color.Blue);
            wavePlot.Plot.AddVerticalLine((oto.Offset + oto.Overlap) * msToX, Color.Lime);
            wavePlot.Plot.AddVerticalLine((oto.Offset + oto.Preutter) * msToX, Color.Red);

            wavePlot.Plot.SetAxisLimitsY(-1, 1);
            wavePlot.Refresh();

            int fftSize = 1024;
            int melSize = 80;
            var bands = FilterBanks.MelBands(melSize, wav.WaveFmt.SamplingRate);
            var extractor = new FilterbankExtractor(
               new FilterbankOptions {
                   SamplingRate = wav.WaveFmt.SamplingRate,
                   FrameSize = fftSize,
                   FftSize = fftSize,
                   HopSize = hopSize,
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
            specPlot.Plot.AddHeatmap(heatmap, lockScales: false);

            msToX = wav.WaveFmt.SamplingRate * 0.001 / hopSize;
            specPlot.Plot.AddVerticalLine(oto.Offset * msToX, Color.White);
            specPlot.Plot.AddVerticalLine((oto.Offset + oto.Overlap) * msToX, Color.Lime);
            specPlot.Plot.AddVerticalLine((oto.Offset + oto.Preutter) * msToX, Color.Red);
            if (cutoff <= samples.Length) {
                specPlot.Plot.AddVerticalLine(cutoffMs * msToX, Color.White);
            }

            specPlot.Refresh();
        }
    }
}
