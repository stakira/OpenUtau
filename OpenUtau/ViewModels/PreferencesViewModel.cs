using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Markup.Xaml.MarkupExtensions;
using OpenUtau.Audio;
using OpenUtau.Core;
using OpenUtau.Core.ResamplerDriver;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class PreferencesViewModel : ViewModelBase {
        public List<AudioOutputDevice>? AudioOutputDevices {
            get => audioOutputDevices;
            set => this.RaiseAndSetIfChanged(ref audioOutputDevices, value);
        }
        public AudioOutputDevice? AudioOutputDevice {
            get => audioOutputDevice;
            set => this.RaiseAndSetIfChanged(ref audioOutputDevice, value);
        }
        [Reactive] public int PreferPortAudio { get; set; }
        public string AdditionalSingersPath => PathManager.Inst.AdditionalSingersPath;
        public List<int> PrerenderThreadsItems { get; }
        public int PrerenderThreads {
            get => prerenderThreads;
            set => this.RaiseAndSetIfChanged(ref prerenderThreads, value);
        }
        public List<IResamplerDriver>? Resamplers { get; }
        public IResamplerDriver? PreviewResampler {
            get => previewResampler;
            set => this.RaiseAndSetIfChanged(ref previewResampler, value);
        }
        public IResamplerDriver? ExportResampler {
            get => exportResampler;
            set => this.RaiseAndSetIfChanged(ref exportResampler, value);
        }
        [Reactive] public int Theme { get; set; }
        [Reactive] public int ResamplerLogging { get; set; }
        public List<CultureInfo?>? Languages { get; }
        public CultureInfo? Language {
            get => language;
            set => this.RaiseAndSetIfChanged(ref language, value);
        }
        public bool MoresamplerSelected => moresamplerSelected.Value;

        private List<AudioOutputDevice>? audioOutputDevices;
        private AudioOutputDevice? audioOutputDevice;
        private int prerenderThreads;
        private IResamplerDriver? previewResampler;
        private IResamplerDriver? exportResampler;
        private CultureInfo? language;
        private readonly ObservableAsPropertyHelper<bool> moresamplerSelected;

        public PreferencesViewModel() {
            var audioOutput = PlaybackManager.Inst.AudioOutput;
            if (audioOutput != null) {
                AudioOutputDevices = audioOutput.GetOutputDevices();
                int deviceNumber = audioOutput.DeviceNumber;
                var device = AudioOutputDevices.FirstOrDefault(d => d.deviceNumber == deviceNumber);
                if (device != null) {
                    AudioOutputDevice = device;
                }
            }
            PreferPortAudio = Preferences.Default.PreferPortAudio ? 1 : 0;
            PrerenderThreadsItems = Enumerable.Range(1, 16).ToList();
            PrerenderThreads = Preferences.Default.PrerenderThreads;
            ResamplerDrivers.Search();
            Resamplers = ResamplerDrivers.GetResamplers();
            if (Resamplers.Count > 0) {
                int index = Resamplers.FindIndex(resampler => resampler.Name == Preferences.Default.ExternalPreviewEngine);
                if (index >= 0) {
                    previewResampler = Resamplers[index];
                } else {
                    previewResampler = null;
                }
                index = Resamplers.FindIndex(resampler => resampler.Name == Preferences.Default.ExternalExportEngine);
                if (index >= 0) {
                    exportResampler = Resamplers[index];
                } else {
                    exportResampler = null;
                }
            }
            var pattern = new Regex(@"Strings\.([\w-]+)\.axaml");
            Languages = Application.Current.Resources.MergedDictionaries
                .Select(res => (ResourceInclude)res)
                .OfType<ResourceInclude>()
                .Select(res => pattern.Match(res.Source!.OriginalString))
                .Where(m => m.Success)
                .Select(m => m.Groups[1].Value)
                .Select(lang => CultureInfo.GetCultureInfo(lang))
                .ToList();
            Languages.Insert(0, CultureInfo.GetCultureInfo("en-US"));
            Languages.Insert(0, null);
            Language = string.IsNullOrEmpty(Preferences.Default.Language)
                ? null
                : CultureInfo.GetCultureInfo(Preferences.Default.Language);
            Theme = Preferences.Default.Theme;
            ResamplerLogging = Preferences.Default.ResamplerLogging ? 1 : 0;

            this.WhenAnyValue(vm => vm.AudioOutputDevice)
                .WhereNotNull()
                .SubscribeOn(RxApp.MainThreadScheduler)
                .Subscribe(device => {
                    if (PlaybackManager.Inst.AudioOutput != null) {
                        try {
                            PlaybackManager.Inst.AudioOutput.SelectDevice(device.guid, device.deviceNumber);
                        } catch (Exception e) {
                            DocManager.Inst.ExecuteCmd(new UserMessageNotification($"Failed to select device {device.name}\n{e}"));
                        }
                    }
                });
            this.WhenAnyValue(vm => vm.PreferPortAudio)
                .Subscribe(index => {
                    Preferences.Default.PreferPortAudio = index > 0;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.PrerenderThreads)
                .Subscribe(threads => {
                    Preferences.Default.PrerenderThreads = threads;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.PreviewResampler)
                .WhereNotNull()
                .Subscribe(resampler => {
                    if (resampler != null) {
                        Preferences.Default.ExternalPreviewEngine = resampler!.Name;
                        Preferences.Save();
                        resampler!.CheckPermissions();
                    }
                });
            this.WhenAnyValue(vm => vm.ExportResampler)
                .WhereNotNull()
                .Subscribe(resampler => {
                    if (resampler != null) {
                        Preferences.Default.ExternalExportEngine = resampler!.Name;
                        Preferences.Save();
                        resampler!.CheckPermissions();
                    }
                });
            this.WhenAnyValue(vm => vm.PreviewResampler, vm => vm.ExportResampler)
                .Select(engines =>
                    (engines.Item1?.Name?.Contains("moresampler", StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                    (engines.Item2?.Name?.Contains("moresampler", StringComparison.InvariantCultureIgnoreCase) ?? false))
                .ToProperty(this, x => x.MoresamplerSelected, out moresamplerSelected);
            this.WhenAnyValue(vm => vm.Language)
                .Subscribe(lang => {
                    Preferences.Default.Language = lang?.Name ?? string.Empty;
                    Preferences.Save();
                    App.SetLanguage(Preferences.Default.Language);
                });
            this.WhenAnyValue(vm => vm.Theme)
                .Subscribe(theme => {
                    Preferences.Default.Theme = theme;
                    Preferences.Save();
                    App.SetTheme();
                });
            this.WhenAnyValue(vm => vm.ResamplerLogging)
                .Subscribe(v => {
                    Preferences.Default.ResamplerLogging = v != 0;
                    Preferences.Save();
                });
        }

        public void TestAudioOutputDevice() {
            PlaybackManager.Inst.PlayTestSound();
        }

        public void OpenResamplerLocation() {
            string path = PathManager.Inst.ResamplersPath;
            Directory.CreateDirectory(path);
            OS.OpenFolder(path);
        }

        public void SetAddlSingersPath(string path) {
            Preferences.Default.AdditionalSingerPath = path;
            Preferences.Save();
            this.RaisePropertyChanged(nameof(AdditionalSingersPath));
        }
    }
}
