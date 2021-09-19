using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using ReactiveUI;
using static OpenUtau.Core.ResamplerDriver.DriverModels;

namespace OpenUtau.App.ViewModels {
    public class PreferencesViewModel : ViewModelBase {
        public List<EngineInfo>? Resamplers {
            get => resamplers;
            set => this.RaiseAndSetIfChanged(ref resamplers, value);
        }
        public EngineInfo? PreviewResampler {
            get => previewResampler;
            set => this.RaiseAndSetIfChanged(ref previewResampler, value);
        }
        public EngineInfo? ExportResampler {
            get => exportResampler;
            set => this.RaiseAndSetIfChanged(ref exportResampler, value);
        }

        private List<EngineInfo>? resamplers;
        private EngineInfo? previewResampler;
        private EngineInfo? exportResampler;

        public PreferencesViewModel() {
            Resamplers = Core.ResamplerDriver.ResamplerDriver.Search(PathManager.Inst.GetEngineSearchPath());
            if (Resamplers.Count() > 0) {
                int index = Resamplers.FindIndex(resampler => resampler.Name == Preferences.Default.ExternalPreviewEngine);
                if (index > 0) {
                    PreviewResampler = Resamplers[index];
                } else {
                    PreviewResampler = null;
                }
                index = Resamplers.FindIndex(resampler => resampler.Name == Preferences.Default.ExternalExportEngine);
                if (index > 0) {
                    ExportResampler = Resamplers[index];
                } else {
                    ExportResampler = null;
                }
            }
            this.WhenAnyValue(vm => vm.PreviewResampler)
                .WhereNotNull()
                .Subscribe(resampler => {
                    Preferences.Default.ExternalPreviewEngine = resampler?.Name;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.ExportResampler)
                .WhereNotNull()
                .Subscribe(resampler => {
                    Preferences.Default.ExternalExportEngine = resampler?.Name;
                    Preferences.Save();
                });
        }
    }
}
