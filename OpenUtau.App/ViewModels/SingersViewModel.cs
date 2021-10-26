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
        public IEnumerable<USinger> Singers => DocManager.Inst.SingersOrdered;
        [Reactive] public USinger? Singer { get; set; }
        [Reactive] public Bitmap? Avatar { get; set; }
        [Reactive] public string? Info { get; set; }
        [Reactive] public ObservableCollectionExtended<USubbank> Subbanks { get; set; }
        [Reactive] public USubbank? SelectedSubbank { get; set; }
        [Reactive] public List<UOto>? Otos { get; set; }
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
                    Avatar = LoadAvatar(singer);
                    Otos = singer.Otos.Values.ToList();
                    Info = $"Author: {singer.Author}\nWeb: {singer.Web}\n{singer.OtherInfo}\n\n{string.Join("\n", singer.Errors)}";
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

        private void Refresh() {
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
        }

        Bitmap? LoadAvatar(USinger singer) {
            if (string.IsNullOrWhiteSpace(singer.Avatar)) {
                return null;
            }
            try {
                using (var stream = File.OpenRead(singer.Avatar)) {
                    return Bitmap.DecodeToWidth(stream, 120);
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to load avatar.");
                return null;
            }
        }

        public void OpenLocation() {
            if (Singer != null) {
                OS.OpenFolder(Singer.Location);
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

        public void AddSubbank() {
            var subbank = new USubbank(new Subbank());
            Subbanks.Add(subbank);
        }

        public void RemoveSubbank() {
            if (SelectedSubbank != null) {
                Subbanks.Remove(SelectedSubbank);
            }
        }

        public void SaveSubbanks() {
            if (Singer == null) {
                return;
            }
            var yamlFile = Path.Combine(Singer.Location, "character.yaml");
            VoicebankConfig? bankConfig = null;
            try {
                // Load from character.yaml
                if (File.Exists(yamlFile)) {
                    using (var stream = File.OpenRead(yamlFile)) {
                        bankConfig = VoicebankConfig.Load(stream);
                    }
                }
            } catch {
            }
            if (bankConfig == null) {
                bankConfig = new VoicebankConfig();
            }
            bankConfig.Subbanks = Subbanks.Select(subbank => subbank.subbank).ToArray();
            try {
                using (var stream = File.Open(yamlFile, FileMode.Create)) {
                    bankConfig.Save(stream);
                }
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new UserMessageNotification(
                    $"Failed to save subbanks\n\n" + e.ToString()));
            }
            LoadSubbanks();
        }
    }
}
