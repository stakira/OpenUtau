using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using Serilog;

namespace OpenUtau.App.ViewModels {
    public class SingersViewModel : ViewModelBase {
        public IEnumerable<USinger> Singers => DocManager.Inst.SingersOrdered;
        public USinger? Singer {
            get => singer;
            set => this.RaiseAndSetIfChanged(ref singer, value);
        }
        public Bitmap? Avatar {
            get => avatar;
            set => this.RaiseAndSetIfChanged(ref avatar, value);
        }
        public string? Info {
            get => info;
            set => this.RaiseAndSetIfChanged(ref info, value);
        }
        public List<UOto>? Otos {
            get => otos;
            set => this.RaiseAndSetIfChanged(ref otos, value);
        }

        private USinger? singer;
        private Bitmap? avatar;
        public string? info;
        private List<UOto>? otos;

        public SingersViewModel() {
            if (Singers.Count() > 0) {
                Singer = Singers.First();
            }
            this.WhenAnyValue(vm => vm.Singer)
                .WhereNotNull()
                .Subscribe(singer => {
                    Avatar = LoadAvatar(singer);
                    Otos = singer.OtoSets.SelectMany(set => set.Otos.Values).SelectMany(list => list).ToList();
                    Info = $"Author: {singer.Author}\nWeb: {singer.Web}\n{singer.OtherInfo}\n\n{string.Join("\n", singer.OtoSets.SelectMany(set => set.Errors))}";
                });
        }

        Bitmap? LoadAvatar(USinger singer) {
            if (string.IsNullOrWhiteSpace(singer.Avatar)) {
                return null;
            }
            try {
                using (var stream = new FileStream(singer.Avatar, FileMode.Open)) {
                    return Bitmap.DecodeToWidth(stream, 120);
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to load avatar.");
                return null;
            }
        }

        public void OpenLocation() {
            if (Singer != null) {
                OpenFolder(Singer.Location);
            }
        }

        private void OpenFolder(string folderPath) {
            if (Directory.Exists(folderPath)) {
                Process.Start(new ProcessStartInfo {
                    Arguments = folderPath,
                    FileName = "explorer.exe",
                });
            }
        }
    }
}
