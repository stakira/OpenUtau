using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using SharpCompress;

namespace OpenUtau.App.ViewModels {
    class SingerConfigViewModel : ViewModelBase {
        [Reactive] public string Name { get; set; }
        [Reactive] public ObservableCollection<LocalizedName> LocalizedNames { get; set; }
        [Reactive] public string SingerType { get; set; } = string.Empty;
        [Reactive] public string TextFileEncoding { get; set; } = string.Empty;
        [Reactive] public string Image { get; set; }
        [Reactive] public string Portrait { get; set; } = string.Empty;
        [Reactive] public string PortraitOpacity { get; set; } = string.Empty;
        [Reactive] public string PortraitHeight { get; set; } = string.Empty;
        [Reactive] public string Author { get; set; }
        [Reactive] public string Voice { get; set; }
        [Reactive] public string Web { get; set; }
        [Reactive] public string Version { get; set; }
        [Reactive] public string Sample { get; set; }
        [Reactive] public string OtherInfo { get; set; }
        [Reactive] public string DefaultPhonemizer { get; set; } = string.Empty;
        [Reactive] public bool UseFilenameAsAlias { get; set; } = false;
        [Reactive] public int TxtOutputOption { get; set; } = 0;

        public List<string> SingerTypeList { get; }
        public List<string> TextFileEncodingList { get; }
        public List<string> PhonemizerList { get; }
        public string Location => singer.Location;

        private Encoding[] encodings;
        private USinger singer;

        public SingerConfigViewModel(USinger singer) {
            this.singer = singer;

            // Lists
            LocalizedNames = new ObservableCollection<LocalizedName>();
            var languageList = App.GetLanguages().Keys
                .Select(lang => CultureInfo.GetCultureInfo(lang));
            languageList.ForEach(language => LocalizedNames.Add(new LocalizedName(language.Name, language.DisplayName, string.Empty)));
            SingerTypeList = SingerTypeUtils.SingerTypeFromName.Keys.ToList();
            SingerTypeList.Insert(0, string.Empty);
            encodings = [
                            Encoding.GetEncoding("shift_jis"),
                            Encoding.ASCII,
                            Encoding.UTF8,
                            Encoding.GetEncoding("gb2312"),
                            Encoding.GetEncoding("big5"),
                            Encoding.GetEncoding("ks_c_5601-1987"),
                            Encoding.GetEncoding("Windows-1252"),
                            Encoding.GetEncoding("macintosh"),
                        ];
            TextFileEncodingList = encodings.Select(e => e.WebName).ToList();
            TextFileEncodingList.Insert(0, string.Empty);
            PhonemizerList = DocManager.Inst.PhonemizerFactories.Select(factory => factory.ToString()).ToList();
            PhonemizerList.Insert(0, string.Empty);

            // Set character.txt Settings
            Name = singer.Name;
            Image = singer.Avatar != null ? Path.GetRelativePath(singer.Location, singer.Avatar) : string.Empty;
            Author = singer.Author ?? string.Empty;
            Voice = singer.Voice ?? string.Empty;
            Web = singer.Web ?? string.Empty;
            Version = singer.Version ?? string.Empty;
            Sample = singer.Sample != null ? Path.GetRelativePath(singer.Location, singer.Sample) : string.Empty;
            OtherInfo = singer.OtherInfo ?? string.Empty;

            // Set OU Settings
            var yamlFile = Path.Combine(singer.Location, "character.yaml");
            VoicebankConfig? config = null;
            if (File.Exists(yamlFile)) {
                try {
                    using (var stream = File.OpenRead(yamlFile)) {
                        config = VoicebankConfig.Load(stream);
                    }
                } catch (Exception e) {
                    Log.Error(e, $"Failed to load yaml {yamlFile}");
                }
            }
            if (config != null) {
                config.LocalizedNames?.ForEach(name => {
                    var localizedName = LocalizedNames.FirstOrDefault(language => language.Language == name.Key);
                    if (localizedName != null) {
                        localizedName.Name = name.Value;
                    } else {
                        CultureInfo culture = CultureInfo.GetCultureInfo(name.Key);
                        LocalizedNames.Add(new LocalizedName(name.Key, culture.DisplayName, name.Value));
                    }
                });
                if (config.SingerType != null && SingerTypeList.Contains(config.SingerType)) {
                    SingerType = config.SingerType;
                }
                TextFileEncoding = config.TextFileEncoding ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(config.Portrait)) {
                    var portrait = Path.Combine(singer.Location, config.Portrait);
                    Portrait = Path.GetRelativePath(singer.Location, portrait);
                }
                PortraitOpacity = config.PortraitOpacity?.ToString() ?? string.Empty;
                PortraitHeight = config.PortraitHeight?.ToString() ?? string.Empty;
                var factory = DocManager.Inst.PhonemizerFactories.FirstOrDefault(factory => factory.type.FullName == config.DefaultPhonemizer);
                DefaultPhonemizer = factory?.ToString() ?? string.Empty;
                if (singer is ClassicSinger classic) {
                    UseFilenameAsAlias = config.UseFilenameAsAlias ?? false;
                }
            }

            this.WhenAnyValue(x => x.PortraitOpacity)
                .Subscribe(value => {
                    if (!float.TryParse(value, out float f) || f <= 0 || 1 < f) {
                        PortraitOpacity = string.Empty;
                    }
                }
            );
            this.WhenAnyValue(x => x.PortraitHeight)
                .Subscribe(value => {
                    if (!int.TryParse(value, out int i) || i < 0) {
                        PortraitHeight = string.Empty;
                    }
                }
            );
        }

        public bool Save() {
            try {
                // character.yaml
                var yamlFile = Path.Combine(singer.Location, "character.yaml");
                VoicebankConfig? config = null;
                if (File.Exists(yamlFile)) {
                    try {
                        using (var stream = File.OpenRead(yamlFile)) {
                            config = VoicebankConfig.Load(stream);
                        }
                    } catch (Exception e) {
                        Log.Error(e, $"Failed to load yaml {yamlFile}");
                    }
                }
                if (config == null) {
                    config = new VoicebankConfig();
                }

                config.Name = !string.IsNullOrWhiteSpace(Name) ? Name : null;
                var dict = new Dictionary<string, string>();
                LocalizedNames.ForEach(n => {
                    if (!string.IsNullOrWhiteSpace(n.Name)) {
                        dict.Add(n.Language, n.Name);
                    }
                });
                config.LocalizedNames = dict.Count != 0 ? dict : null;
                config.SingerType = !string.IsNullOrWhiteSpace(SingerType) ? SingerType : null;
                config.TextFileEncoding = !string.IsNullOrWhiteSpace(TextFileEncoding) ? TextFileEncoding : null; // WebName?
                config.Image = !string.IsNullOrWhiteSpace(Image) ? Image : null;
                config.Portrait = !string.IsNullOrWhiteSpace(Portrait) ? Portrait : null;
                if (float.TryParse(PortraitOpacity, out float f) && 0 < f && f <= 1) {
                    config.PortraitOpacity = f;
                } else config.PortraitOpacity = null;
                if (int.TryParse(PortraitHeight, out int i) && 0 <= i) {
                    config.PortraitHeight = i;
                } else config.PortraitHeight = null;
                config.Author = !string.IsNullOrWhiteSpace(Author) ? Author : null;
                config.Voice = !string.IsNullOrWhiteSpace(Voice) ? Voice : null;
                config.Web = !string.IsNullOrWhiteSpace(Web) ? Web : null;
                config.Version = !string.IsNullOrWhiteSpace(Version) ? Version : null;
                config.Sample = !string.IsNullOrWhiteSpace(Sample) ? Sample : null;
                config.OtherInfo = !string.IsNullOrWhiteSpace(OtherInfo) ? OtherInfo : null;
                var factory = DocManager.Inst.PhonemizerFactories.FirstOrDefault(factory => factory.ToString() == DefaultPhonemizer);
                config.DefaultPhonemizer = factory?.type.FullName ?? null;
                config.UseFilenameAsAlias = UseFilenameAsAlias ? true : null;

                using (var stream = File.Open(yamlFile, FileMode.Create)) {
                    config.Save(stream);
                }

                // character.txt
                Encoding encoding = Encoding.GetEncoding("shift_jis");
                switch (TxtOutputOption) {
                    case 2:
                        break;
                    case 1:
                        try {
                            if (!string.IsNullOrWhiteSpace(TextFileEncoding)) {
                                encoding = Encoding.GetEncoding(TextFileEncoding);
                            }
                        } catch { }
                        goto case 0;
                    case 0:
                        var txtFile = Path.Combine(singer.Location, "character.txt");
                        var lines = new List<string>();
                        if (!string.IsNullOrWhiteSpace(Name)) {
                            lines.Add($"name={Name}");
                        }
                        if (!string.IsNullOrWhiteSpace(Image)) {
                            lines.Add($"image={Image}");
                        }
                        if (!string.IsNullOrWhiteSpace(Author)) {
                            lines.Add($"author={Author}");
                        }
                        if (!string.IsNullOrWhiteSpace(Sample)) {
                            lines.Add($"sample={Sample}");
                        }
                        if (!string.IsNullOrWhiteSpace(Web)) {
                            lines.Add($"web={Web}");
                        }
                        if (!string.IsNullOrWhiteSpace(OtherInfo)) {
                            lines.Add(OtherInfo);
                        }
                        File.WriteAllLines(txtFile, lines, encoding);
                        break;
                }
                return true;
            } catch (Exception e) {
                var customEx = new MessageCustomizableException("Failed to save singer config", "<translate:errors.failed.savesingerconfig>", e);
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(customEx));
                return false;
            }
        }
    }

    public class LocalizedName {
        public string Language { get; }
        public string DisplayName { get; }
        public string Name { get; set; }

        public LocalizedName(string language, string displayName, string name) {
            Language = language;
            DisplayName = displayName;
            Name = name;
        }
    }
}
