using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using Avalonia.Media.Imaging;
using DynamicData.Binding;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace OpenUtau.App.ViewModels {
    public class SingersViewModel : ViewModelBase {
        public IEnumerable<USinger> Singers => DocManager.Inst.SingerGroups.Values.SelectMany(l => l);
        [Reactive] public USinger? Singer { get; set; }
        [Reactive] public Bitmap? Avatar { get; set; }
        [Reactive] public string? Info { get; set; }
        [Reactive] public ObservableCollectionExtended<USubbank> Subbanks { get; set; }
        [Reactive] public List<UOto>? Otos { get; set; }
        [Reactive] public UOto SelectedOto { get; set; }
        [Reactive] public List<MenuItemViewModel> SetEncodingMenuItems { get; set; }

        private ReactiveCommand<Encoding, Unit> setEncodingCommand;

        public SingersViewModel() {
            Subbanks = new ObservableCollectionExtended<USubbank>();
#if DEBUG
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
            if (Singers.Count() > 0) {
                Singer = Singers.First();
            }
            this.WhenAnyValue(vm => vm.Singer)
                .WhereNotNull()
                .Subscribe(singer => {
                    singer.Reload();
                    Avatar = LoadAvatar(singer);
                    Otos = singer.Otos.Values.ToList();
                    Info = $"Author: {singer.Author}\nVoice: {singer.Voice}\nWeb: {singer.Web}\nVersion: {singer.Version}\n{singer.OtherInfo}\n\n{string.Join("\n", singer.Errors)}";
                    LoadSubbanks();
                });


            setEncodingCommand = ReactiveCommand.Create<Encoding>(encoding => {
                SetEncoding(encoding);
                Refresh();
            });
            var encodings = new Encoding[] {
                Encoding.GetEncoding("shift_jis"),
                Encoding.ASCII,
                Encoding.UTF8,
                Encoding.GetEncoding("gb2312"),
                Encoding.GetEncoding("big5"),
                Encoding.GetEncoding("ks_c_5601-1987"),
                Encoding.GetEncoding("Windows-1252"),
                Encoding.GetEncoding("macintosh"),
            };
            SetEncodingMenuItems = encodings.Select(encoding =>
                new MenuItemViewModel() {
                    Header = encoding.EncodingName,
                    Command = setEncodingCommand,
                    CommandParameter = encoding,
                }
            ).ToList();
        }

        private void SetEncoding(Encoding encoding) {
            if (Singer == null) {
                return;
            }
            try {
                var yamlFile = Path.Combine(Singer.Location, "character.yaml");
                VoicebankConfig? bankConfig = null;
                if (File.Exists(yamlFile)) {
                    using (var stream = File.OpenRead(yamlFile)) {
                        bankConfig = VoicebankConfig.Load(stream);
                    }
                }
                if (bankConfig == null) {
                    bankConfig = new VoicebankConfig();
                }
                bankConfig.TextFileEncoding = encoding.WebName;
                using (var stream = File.Open(yamlFile, FileMode.Create)) {
                    bankConfig.Save(stream);
                }
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new UserMessageNotification(
                    $"Failed to set encoding\n\n" + e.ToString()));
            }
        }

        public void Refresh() {
            if (Singer == null) {
                return;
            }
            var singerId = Singer.Id;
            DocManager.Inst.SearchAllSingers();
            this.RaisePropertyChanged(nameof(Singers));
            if (DocManager.Inst.Singers.TryGetValue(singerId, out var singer)) {
                Singer = singer;
            } else {
                Singer = Singers.FirstOrDefault();
            }
            DocManager.Inst.ExecuteCmd(new SingersRefreshedNotification());
        }

        Bitmap? LoadAvatar(USinger singer) {
            if (singer.AvatarData == null) {
                return null;
            }
            try {
                using (var stream = new MemoryStream(singer.AvatarData)) {
                    return new Bitmap(stream);
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to load avatar.");
                return null;
            }
        }

        public void OpenLocation() {
            try {
                if (Singer != null) {
                    OS.OpenFolder(Singer.Location);
                }
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new UserMessageNotification(e.ToString()));
            }
        }

        public void LoadSubbanks() {
            Subbanks.Clear();
            if (Singer == null) {
                return;
            }
            try {
                Subbanks.AddRange(Singer.Subbanks);
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new UserMessageNotification(
                    $"Failed to load subbanks\n\n" + e.ToString()));
            }
        }

        public void RefreshSinger() {
            Singer?.Reload();
            LoadSubbanks();
            DocManager.Inst.ExecuteCmd(new SingersRefreshedNotification());
        }

        public void SetOffset(double value) {
            if (SelectedOto != null) {
                var delta = value - SelectedOto.Offset;
                SelectedOto.Offset += delta;
                SelectedOto.Consonant -= delta;
                SelectedOto.Preutter -= delta;
                SelectedOto.Overlap -= delta;
            }
            this.RaisePropertyChanged(nameof(SelectedOto));
        }

        public void SetOverlap(double value) {
            if (SelectedOto != null) {
                SelectedOto.Overlap = value - SelectedOto.Offset;
            }
        }

        public void SetPreutter(double value) {
            if (SelectedOto != null) {
                var delta = value - SelectedOto.Offset - SelectedOto.Preutter;
                if (SelectedOto.Cutoff < 0) {
                    SelectedOto.Cutoff += delta;
                }
                SelectedOto.Preutter += delta;
            }
        }

        public void SetFixed(double value) {
            if (SelectedOto != null) {
                SelectedOto.Consonant = value - SelectedOto.Offset;
            }
        }

        public void SetCutoff(double value) {
            if (SelectedOto != null) {
                SelectedOto.Cutoff = -(value - SelectedOto.Offset - SelectedOto.Preutter);
            }
        }
    }
}
