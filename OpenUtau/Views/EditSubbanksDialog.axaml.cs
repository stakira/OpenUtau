using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;

namespace OpenUtau.App.Views {
    public partial class EditSubbanksDialog : Window {
        internal readonly EditSubbanksViewModel ViewModel;

        internal Action? RefreshSinger;

        public EditSubbanksDialog() {
            InitializeComponent();
            DataContext = ViewModel = new EditSubbanksViewModel();
        }

        void OnAdd(object sender, RoutedEventArgs e) {
            var dialog = new TypeInDialog() {
                Title = ThemeManager.GetString("singers.subbanks.color.add"),
                onFinish = name => ViewModel.AddSubbank(name),
            };
            dialog.ShowDialog(this);
        }

        void OnRemove(object sender, RoutedEventArgs e) {
            ViewModel.RemoveSubbank();
        }

        void OnRename(object sender, RoutedEventArgs e) {
            if (ViewModel.SelectedColor == null || string.IsNullOrEmpty(ViewModel.SelectedColor.Name)) {
                return;
            }
            var dialog = new TypeInDialog() {
                Title = ThemeManager.GetString("singers.subbanks.color.rename"),
                onFinish = name => ViewModel.RenameSubbank(name),
            };
            dialog.ShowDialog(this);
        }

        void OnSave(object sender, RoutedEventArgs e) {
            ViewModel.SaveSubbanks();
            RefreshSinger?.Invoke();
            Close();
        }

        void OnCancel(object sender, RoutedEventArgs e) {
            Close();
        }

        void OnSelectAll(object sender, RoutedEventArgs e) {
            SuffixGrid.SelectAll();
        }

        void OnSet(object sender, RoutedEventArgs e) {
            ViewModel.Set(SuffixGrid.SelectedItems);
        }

        void OnClear(object sender, RoutedEventArgs e) {
            ViewModel.Clear(SuffixGrid.SelectedItems);
        }

        async void OnImportMap(object sender, RoutedEventArgs args) {
            if (ViewModel.Singer == null || ViewModel.Rows == null) {
                return;
            }
            var file = await FilePicker.OpenFile(this, "singers.subbanks.import", ViewModel.Singer.Location, FilePicker.PrefixMap);
            if (!string.IsNullOrEmpty(file)) {
                try {
                    using (var reader = new StreamReader(file, ViewModel.Singer.TextFileEncoding)) {
                        while (reader.Peek() != -1) {
                            var line = reader.ReadLine()!.Trim();
                            var s = line.Split('\t');
                            if (s.Length == 3) {
                                var row = ViewModel.Rows.First(row => row.Tone == s[0]);
                                if(row != null) {
                                    row.Prefix = s[1];
                                    row.Suffix = s[2];
                                }
                            }
                        }
                    }
                } catch (Exception e) {
                    var customEx = new MessageCustomizableException("Failed to load prefix map", "<translate:errors.failed.load>: prefix map", e);
                    DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(customEx));
                }
            }
        }

        async void OnExportMap(object sender, RoutedEventArgs args) {
            if (ViewModel.Singer == null) {
                return;
            }
            if (ViewModel.Colors.Count > 0 && ViewModel.Colors.First(c => c.Name == string.Empty) is VoiceColor main) {
                var file = await FilePicker.SaveFile(this, "singers.subbanks.export", ViewModel.Singer.Location, "prefix.map", FilePicker.PrefixMap);
                if (!string.IsNullOrEmpty(file)) {
                    try {
                        using (var writer = new StreamWriter(file, false, ViewModel.Singer.TextFileEncoding)) {
                            foreach (var row in main.Rows) {
                                writer.WriteLine($"{row.Tone}\t{row.Prefix}\t{row.Suffix}");
                            }
                        }
                    } catch (Exception e) {
                        var customEx = new MessageCustomizableException("Failed to save prefix map", "<translate:errors.failed.save>: prefix map", e);
                        DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(customEx));
                    }
                }
            }
        }

        void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if(SuffixGrid.SelectedItems.Count > 0 && SuffixGrid.SelectedItems[0] is VoiceColorRow row) {
                ViewModel.Prefix = row.Prefix;
                ViewModel.Suffix = row.Suffix;
            }
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            DocManager.Inst.ExecuteCmd(new VoiceColorRemappingNotification(-1, true));
        }
    }
}
