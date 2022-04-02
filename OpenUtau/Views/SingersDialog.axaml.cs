using System;
using System.Drawing;
using System.IO;
using System.Linq;
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
using ScottPlot.Avalonia;
using Serilog;

namespace OpenUtau.App.Views {
    public partial class SingersDialog : Window {
        Color blueFill;
        Color pinkFill;
        Color blueLine;

        AvaPlot? otoPlot;
        double coordToMs;
        double lastPointerMs;

        public SingersDialog() {
            try {
                InitializeComponent();

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
            otoPlot.Plot.Clear();
            WaveFile wav;
            using (var stream = File.OpenRead(oto.File)) {
                wav = new WaveFile(stream);
            }

            int fftSize = 1024;
            int melSize = 80;
            int hopSize = wav.WaveFmt.SamplingRate / 100;

            var msToCoord = 0.001 * wav.WaveFmt.SamplingRate / hopSize;
            coordToMs = 1.0 / msToCoord;
            var dur = wav.Signals[0].Duration * 1000;
            var samples = wav.Signals[0].Samples.Select(f => (double)f * 20 + 100).ToArray();
            var xs = Enumerable.Range(0, samples.Length).Select(i => i * 1.0 / hopSize).ToArray();
            double cutoff = oto.Cutoff > 0
                ? (wav.Signals[0].Duration * 1000.0 - oto.Cutoff)
                : oto.Offset + oto.Preutter - oto.Cutoff;
            double offsetX = oto.Offset * msToCoord;
            double consonantX = (oto.Offset + oto.Consonant) * msToCoord;
            double preutterX = (oto.Offset + oto.Preutter) * msToCoord;
            double overlapX = (oto.Offset + oto.Overlap) * msToCoord;
            double cutoffX = cutoff * msToCoord;
            double durX = dur * msToCoord;
            if (offsetX > 0) {
                otoPlot.Plot.AddPolygon(
                    new double[] { 0, 0, offsetX, offsetX },
                    new double[] { 0, 120, 120, 0 }, blueFill);
            }
            if (consonantX > offsetX) {
                otoPlot.Plot.AddPolygon(
                    new double[] { offsetX, offsetX, consonantX, consonantX },
                    new double[] { 0, 120, 120, 0 }, pinkFill);
            }
            if (cutoff <= samples.Length) {
                otoPlot.Plot.AddPolygon(
                    new double[] { cutoffX, cutoffX, durX, durX },
                    new double[] { 0, 120, 120, 0 }, blueFill);
            }
            otoPlot.Plot.AddSignalXY(xs, samples, color: Color.Blue);
            otoPlot.Plot.AddLine(offsetX, 80, overlapX, 119, blueLine, 2);
            otoPlot.Plot.AddLine(overlapX, 119, cutoffX, 119, blueLine, 2);
            otoPlot.Plot.AddLine(cutoffX, 119, cutoffX, 80, blueLine, 2);

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
            otoPlot.Plot.AddHeatmap(heatmap, lockScales: false);

            otoPlot.Plot.AddVerticalLine(overlapX, Color.Lime);
            otoPlot.Plot.AddText("OVL", overlapX, 80, color: Color.Lime);
            otoPlot.Plot.AddVerticalLine(preutterX, Color.Red);
            otoPlot.Plot.AddText("PRE", preutterX, 80, color: Color.Red);

            if (fit) {
                double span = (cutoffX - preutterX) * 2;
                if (span <= 0) {
                    span = durX;
                }
                otoPlot.Plot.SetAxisLimitsX(Math.Max(0, preutterX - span), Math.Min(durX, preutterX + span));
                otoPlot.Plot.SetAxisLimitsY(0, 120);
            } else {
                otoPlot.Plot.SetAxisLimits(limites);
            }
            otoPlot.Plot.SetOuterViewLimits(0, durX, 0, 120);
            otoPlot.Refresh();
        }

        void OtoPlot_OnPointerMoved(object sender, PointerEventArgs args) {
            if (otoPlot == null) {
                return;
            }
            var point = args.GetCurrentPoint(otoPlot);
            lastPointerMs = otoPlot.Plot.GetCoordinateX((float)point.Position.X) * coordToMs;
        }

        void OtoPlot_OnKeyDown(object sender, KeyEventArgs args) {
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
                default:
                    args.Handled = false;
                    break;
            }
        }
    }
}
