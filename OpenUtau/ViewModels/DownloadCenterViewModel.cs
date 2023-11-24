using System.IO;
using System.Text;
using Avalonia.Controls;
using DynamicData.Binding;
using NAudio.Wave;
using NWaves.Signals;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using OpenUtau.Plugin.Builtin;
using OpenUtau.App.Views;

namespace OpenUtau.App.ViewModels {
    public class DownloadCenterViewModel : ViewModelBase {
        Categories? appliedCategory;
        public Categories? ApplyCategory {
            get => appliedCategory;
            set => this.RaiseAndSetIfChanged(ref appliedCategory, value);
        }

        public DownloadCenterViewModel(){
            DownloadCenterManager downloadCenterManager = new DownloadCenterManager();
            ApplyCategory = downloadCenterManager.categories;
        }

    }
    

    
}