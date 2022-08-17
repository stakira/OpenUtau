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
        public IEnumerable<USinger> Singers => SingerManager.Inst.SingerGroups.Values.SelectMany(l => l);
        [Reactive] public USinger? Singer { get; set; }
        [Reactive] public Bitmap? Avatar { get; set; }
        [Reactive] public string? Info { get; set; }
        [Reactive] public ObservableCollectionExtended<USubbank> Subbanks { get; set; }
        [Reactive] public ObservableCollectionExtended<UOto> Otos { get; set; }
        [Reactive] public UOto? SelectedOto { get; set; }
        [Reactive] public int SelectedIndex { get; set; }
        [Reactive] public List<MenuItemViewModel> SetEncodingMenuItems { get; set; }
        [Reactive] public List<MenuItemViewModel> SetDefaultPhonemizerMenuItems { get; set; }

        private ReactiveCommand<Encoding, Unit> setEncodingCommand;
        private ReactiveCommand<Api.PhonemizerFactory, Unit> setDefaultPhonemizerCommand;

        public SingersViewModel() {
            Subbanks = new ObservableCollectionExtended<USubbank>();
            Otos = new ObservableCollectionExtended<UOto>();
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
                    Otos.Clear();
                    Otos.AddRange(singer.Otos.Values);
                    Info = $"Author: {singer.Author}\nVoice: {singer.Voice}\nWeb: {singer.Web}\nVersion: {singer.Version}\n{singer.OtherInfo}\n\n{string.Join("\n", singer.Errors)}";
                    LoadSubbanks();
                    DocManager.Inst.ExecuteCmd(new OtoChangedNotification());
                });


            setEncodingCommand = ReactiveCommand.Create<Encoding>(encoding => {
                SetEncoding(encoding);
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

            setDefaultPhonemizerCommand = ReactiveCommand.Create<Api.PhonemizerFactory>(factory => {
                SetDefaultPhonemizer(factory);
            });
            SetDefaultPhonemizerMenuItems = DocManager.Inst.PhonemizerFactories.Select(factory => new MenuItemViewModel() {
                Header = factory.ToString(),
                Command = setDefaultPhonemizerCommand,
                CommandParameter = factory,
            }).ToList();
        }

        private void SetEncoding(Encoding encoding) {
            if (Singer == null) {
                return;
            }
            try {
                ModifyConfig(Singer, config => config.TextFileEncoding = encoding.WebName);
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new UserMessageNotification(
                    $"Failed to set encoding\n\n" + e.ToString()));
            }
            Refresh();
        }

        public void SetPortrait(string filepath) {
            if (Singer == null) {
                return;
            }
            try {
                ModifyConfig(Singer, config => config.Portrait = filepath);
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new UserMessageNotification(
                    $"Failed to set portrait\n\n" + e.ToString()));
            }
            Refresh();
        }

        private void SetDefaultPhonemizer(Api.PhonemizerFactory factory) {
            if (Singer == null) {
                return;
            }
            try {
                ModifyConfig(Singer, config => config.DefaultPhonemizer = factory.type.FullName);
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new UserMessageNotification(
                    $"Failed to set portrait\n\n" + e.ToString()));
            }
            Refresh();
        }

        private static void ModifyConfig(USinger singer, Action<VoicebankConfig> modify) {
            var yamlFile = Path.Combine(singer.Location, "character.yaml");
            VoicebankConfig? config = null;
            if (File.Exists(yamlFile)) {
                using (var stream = File.OpenRead(yamlFile)) {
                    config = VoicebankConfig.Load(stream);
                }
            }
            if (config == null) {
                config = new VoicebankConfig();
            }
            modify(config);
            using (var stream = File.Open(yamlFile, FileMode.Create)) {
                config.Save(stream);
            }
        }

        public void Refresh() {
            if (Singer == null) {
                return;
            }
            var singerId = Singer.Id;
            SingerManager.Inst.SearchAllSingers();
            this.RaisePropertyChanged(nameof(Singers));
            if (SingerManager.Inst.Singers.TryGetValue(singerId, out var singer)) {
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
            DocManager.Inst.ExecuteCmd(new OtoChangedNotification());
        }

        public void SetOffset(double value) {
            if (SelectedOto == null) {
                return;
            }
            var delta = value - SelectedOto.Offset;
            SelectedOto.Offset += delta;
            SelectedOto.Consonant -= delta;
            SelectedOto.Preutter -= delta;
            SelectedOto.Overlap -= delta;
            if (SelectedOto.Cutoff < 0) {
                SelectedOto.Cutoff += delta;
            }
            DocManager.Inst.ExecuteCmd(new OtoChangedNotification());
        }

        public void SetOverlap(double value) {
            if (SelectedOto == null) {
                return;
            }
            SelectedOto.Overlap = value - SelectedOto.Offset;
            DocManager.Inst.ExecuteCmd(new OtoChangedNotification());
        }

        public void SetPreutter(double value) {
            if (SelectedOto == null) {
                return;
            }
            var delta = value - SelectedOto.Offset - SelectedOto.Preutter;
            SelectedOto.Preutter += delta;
            DocManager.Inst.ExecuteCmd(new OtoChangedNotification());
        }

        public void SetFixed(double value) {
            if (SelectedOto == null) {
                return;
            }
            SelectedOto.Consonant = value - SelectedOto.Offset;
            DocManager.Inst.ExecuteCmd(new OtoChangedNotification());
        }

        public void SetCutoff(double value) {
            if (SelectedOto == null) {
                return;
            }
            if (value < SelectedOto.Offset) {
                return;
            }
            SelectedOto.Cutoff = -(value - SelectedOto.Offset);
            DocManager.Inst.ExecuteCmd(new OtoChangedNotification());
        }
    }
}
