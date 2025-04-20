using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Util;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class FileInfo {
        public string Name;
        public string DirectoryName;
        public DateTime LastWriteTime;
        public string LastWriteTimeStr;

        public FileInfo(string directoryName) {
            DirectoryName = directoryName;
            Name = System.IO.Path.GetFileName(directoryName);
            LastWriteTime = System.IO.File.GetLastWriteTime(directoryName);
            LastWriteTimeStr = LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    public class WelcomePageViewModel : ViewModelBase {
        [Reactive] public bool IsVisible { get; set; }
        [Reactive] public List<FileInfo> RecentFiles { get; set; }

        public WelcomePageViewModel() {
            RecentFiles = Preferences.Default.RecentFiles
                .Select(file => new FileInfo(file))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();
        }
    }
}