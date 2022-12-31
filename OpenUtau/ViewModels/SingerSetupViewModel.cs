using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData.Binding;
using OpenUtau.Core;
using OpenUtau.Core.Util;
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
        public bool CanInstall {
            get => canInstall;
            set => this.RaiseAndSetIfChanged(ref canInstall, value);
        }
        public bool CreateRootDirectory {
            get => createRootDirectory;
            set => this.RaiseAndSetIfChanged(ref createRootDirectory, value);
        }
        public bool CreateCharacterTxt {
            get => createCharacterTxt;
            set => this.RaiseAndSetIfChanged(ref createCharacterTxt, value);
        }
        public string CreateRootDirectoryName {
            get => createRootDirectoryName;
            set => this.RaiseAndSetIfChanged(ref createRootDirectoryName, value);
        }
        public string CreateCharacterTxtName {
            get => createCharacterTxtName;
            set => this.RaiseAndSetIfChanged(ref createCharacterTxtName, value);
        }

        private int step;
        private ObservableCollectionExtended<string> textItems;
        private string archiveFilePath;
        private Encoding[] encodings;
        private Encoding archiveEncoding;
        private Encoding textEncoding;

        private bool canInstall;
        private bool createRootDirectory;
        private bool createCharacterTxt;
        private string createRootDirectoryName = string.Empty;
        private string createCharacterTxtName = string.Empty;

        public SingerSetupViewModel() {
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

            this.WhenAnyValue(vm => vm.Step)
                .Subscribe(_ => OnStep());
            this.WhenAnyValue(vm => vm.Step, vm => vm.ArchiveEncoding, vm => vm.ArchiveFilePath)
                .Subscribe(_ => RefreshArchiveItems());
            this.WhenAnyValue(vm => vm.Step, vm => vm.TextEncoding)
                .Subscribe(_ => RefreshTextItems());
            this.WhenAnyValue(
                vm => vm.CreateRootDirectory,
                vm => vm.CreateRootDirectoryName,
                vm => vm.CreateCharacterTxt,
                vm => vm.CreateCharacterTxtName)
                .Subscribe(_ => CanInstall =
                    (!CreateRootDirectory || !string.IsNullOrWhiteSpace(CreateRootDirectoryName)) &&
                    (!CreateCharacterTxt || !string.IsNullOrWhiteSpace(CreateCharacterTxtName)));
        }

        private void Back() {
            Step--;
        }

        private void Next() {
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

        private void OnStep() {
            if (Step == 2) {
                BuildTree();
            }
        }

        private void BuildTree() {
            var readerOptions = new ReaderOptions {
                ArchiveEncoding = new ArchiveEncoding(ArchiveEncoding, ArchiveEncoding),
            };
            using var archive = ArchiveFactory.Open(ArchiveFilePath, readerOptions);
            var banks = archive.Entries
                .Where(entry => !entry.IsDirectory && Path.GetFileName(entry.Key) == "character.txt")
                .OrderByDescending(bank => bank.Key.Length)
                .Select(bank => new Character(bank.Key))
                .ToList();
            var otoSets = archive.Entries
                .Where(entry => !entry.IsDirectory && Path.GetFileName(entry.Key) == "oto.ini")
                .ToArray();
            var prefixMaps = archive.Entries
                .Where(entry => !entry.IsDirectory && Path.GetFileName(entry.Key) == "prefix.map")
                .ToArray();
            var bankDirs = banks
                .Select(bank => Path.GetDirectoryName(bank.file)!)
                .ToArray();
            var unknownBank = new Character("(Unknown)");
            foreach (var otoSet in otoSets) {
                var dir = Path.GetDirectoryName(otoSet.Key);
                bool foundBank = false;
                for (int i = 0; i < bankDirs.Length; ++i) {
                    if (dir.StartsWith(bankDirs[i])) {
                        string relPath = Path.GetRelativePath(bankDirs[i], Path.GetDirectoryName(otoSet.Key));
                        if (relPath == ".") {
                            relPath = string.Empty;
                        }
                        banks[i].otoSets.Add(otoSet.Key);
                        foundBank = true;
                        break;
                    }
                }
                if (!foundBank) {
                    unknownBank.otoSets.Add(otoSet.Key);
                }
            }
            textItems.Clear();
            foreach (var bank in banks) {
                textItems.Add($"{bank.file}");
                textItems.AddRange(bank.otoSets.Select(set => $"      | {set}"));
            }

            if (unknownBank.otoSets.Count > 0) {
                CreateCharacterTxt = true;
                banks.Add(unknownBank);
            }

            List<string> dirs = banks.Select(b => Path.GetDirectoryName(b.file)).ToList();
            dirs.AddRange(otoSets.Select(set => Path.GetDirectoryName(set.Key)));
            string root = dirs.OrderBy(dir => dir.Length).FirstOrDefault();
            if (string.IsNullOrEmpty(root) || root == ".") {
                CreateRootDirectory = true;
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
                    installer.LoadArchive(archiveFilePath);
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
