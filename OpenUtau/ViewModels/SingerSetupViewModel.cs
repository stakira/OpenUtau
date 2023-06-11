using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData.Binding;
using OpenUtau.Core;
using ReactiveUI;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace OpenUtau.App.ViewModels {
    public class SingerSetupViewModel : ViewModelBase {
        public int Step {
            get => step;
            set => this.RaiseAndSetIfChanged(ref step, value);
        }
        public ObservableCollection<string> TextItems => textItems;
        public string ArchiveFilePath {
            get => archiveFilePath;
            set => this.RaiseAndSetIfChanged(ref archiveFilePath, value);
        }
        public Encoding[] Encodings => encodings;
        public Encoding ArchiveEncoding {
            get => archiveEncoding;
            set => this.RaiseAndSetIfChanged(ref archiveEncoding, value);
        }
        public Encoding TextEncoding {
            get => textEncoding;
            set => this.RaiseAndSetIfChanged(ref textEncoding, value);
        }
        public string[] SingerTypes => singerTypes;
        public string SingerType {
            get => singerType;
            set => this.RaiseAndSetIfChanged(ref singerType, value);
        }

        private int step;
        private string[] singerTypes;
        private string singerType;
        private ObservableCollectionExtended<string> textItems;
        private string archiveFilePath;
        private Encoding[] encodings;
        private Encoding archiveEncoding;
        private Encoding textEncoding;

        public SingerSetupViewModel() {
#if DEBUG
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
            singerTypes = new[] { "utau", "enunu" };
            singerType = singerTypes[0];
            archiveFilePath = string.Empty;
            encodings = new Encoding[] {
                Encoding.GetEncoding("shift_jis"),
                Encoding.ASCII,
                Encoding.UTF8,
                Encoding.GetEncoding("gb2312"),
                Encoding.GetEncoding("big5"),
                Encoding.GetEncoding("ks_c_5601-1987"),
                Encoding.GetEncoding("Windows-1252"),
                Encoding.GetEncoding("macintosh"),
            };
            archiveEncoding = encodings[0];
            textEncoding = encodings[0];
            textItems = new ObservableCollectionExtended<string>();

            this.WhenAnyValue(vm => vm.Step, vm => vm.ArchiveEncoding, vm => vm.ArchiveFilePath)
                .Subscribe(_ => RefreshArchiveItems());
            this.WhenAnyValue(vm => vm.Step, vm => vm.TextEncoding)
                .Subscribe(_ => RefreshTextItems());
        }

        public void Back() {
            Step--;
        }

        public void Next() {
            Step++;
        }

        private void RefreshArchiveItems() {
            if (Step != 0) {
                return;
            }
            if (string.IsNullOrEmpty(ArchiveFilePath)) {
                textItems.Clear();
                return;
            }
            var readerOptions = new ReaderOptions {
                ArchiveEncoding = new ArchiveEncoding { Forced = ArchiveEncoding },
            };
            using (var archive = ArchiveFactory.Open(ArchiveFilePath, readerOptions)) {
                textItems.Clear();
                textItems.AddRange(archive.Entries
                    .Select(entry => entry.Key)
                    .ToArray());
            }
        }

        private void RefreshTextItems() {
            if (Step != 1) {
                return;
            }
            if (string.IsNullOrEmpty(ArchiveFilePath)) {
                textItems.Clear();
                return;
            }
            var readerOptions = new ReaderOptions {
                ArchiveEncoding = new ArchiveEncoding { Forced = ArchiveEncoding },
            };
            using (var archive = ArchiveFactory.Open(ArchiveFilePath, readerOptions)) {
                textItems.Clear();
                foreach (var entry in archive.Entries.Where(entry => entry.Key.EndsWith("character.txt") || entry.Key.EndsWith("oto.ini"))) {
                    using (var stream = entry.OpenEntryStream()) {
                        using var reader = new StreamReader(stream, TextEncoding);
                        textItems.Add($"------ {entry.Key} ------");
                        int count = 0;
                        while (count < 256 && !reader.EndOfStream) {
                            string? line = reader.ReadLine();
                            if (!string.IsNullOrWhiteSpace(line)) {
                                textItems.Add(line);
                                count++;
                            }
                        }
                        if (!reader.EndOfStream) {
                            textItems.Add($"...");
                        }
                    }
                }
            }
        }

        class Character {
            public string file;
            public List<string> otoSets = new List<string>();
            public Character(string file) {
                this.file = file;
            }
        }

        public Task Install() {
            string archiveFilePath = ArchiveFilePath;
            var archiveEncoding = ArchiveEncoding;
            var textEncoding = TextEncoding;
            return Task.Run(() => {
                try {
                    var basePath = PathManager.Inst.SingersInstallPath;
                    var installer = new Classic.VoicebankInstaller(basePath, (progress, info) => {
                        DocManager.Inst.ExecuteCmd(new ProgressBarNotification(progress, info));
                    }, archiveEncoding, textEncoding);
                    installer.Install(archiveFilePath, SingerType);
                } finally {
                    new Task(() => {
                        DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, ""));
                        DocManager.Inst.ExecuteCmd(new SingersChangedNotification());
                    }).Start(DocManager.Inst.MainScheduler);
                }
            });
        }
    }
}
