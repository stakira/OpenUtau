using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Avalonia.Controls.Primitives;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using Serilog;
using System.Diagnostics;

namespace OpenUtau.App.Controls {
    public partial class DictionaryEditorControl : UserControl {
        public DictionaryEditorViewModel ViewModel { get; } = new DictionaryEditorViewModel();

        public static readonly StyledProperty<UVoicePart?> PartProperty =
            AvaloniaProperty.Register<DictionaryEditorControl, UVoicePart?>(nameof(Part));

        public UVoicePart? Part {
            get => GetValue(PartProperty);
            set => SetValue(PartProperty, value);
        }
        public DictionaryEditorControl() {
            InitializeComponent();

            ViewModel.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(ViewModel.SelectedCategory)) {
                    RebuildGridColumns(ViewModel.SelectedCategory);
                    
                    if (ViewModel.SelectedCategory != null && ViewModel.SelectedCategory.Columns.Count > 0) {
                        ViewModel.ReplaceColumn = ViewModel.SelectedCategory.Columns[0];
                    }
                }
            };
            
            // Listen for dynamic column additions/removals
            ViewModel.ColumnsChanged += () => {
                RebuildGridColumns(ViewModel.SelectedCategory);
                
                if (ViewModel.SelectedCategory != null && ViewModel.SelectedCategory.Columns.Count > 0) {
                    if (string.IsNullOrEmpty(ViewModel.ReplaceColumn) || !ViewModel.SelectedCategory.Columns.Contains(ViewModel.ReplaceColumn)) {
                        ViewModel.ReplaceColumn = ViewModel.SelectedCategory.Columns[0];
                    }
                } else {
                    ViewModel.ReplaceColumn = null;
                }
            };

            this.Loaded += (s, e) => LoadDictionaryForPart(Part);
        }
        private void EditorGrid_LoadingRow(object? sender, Avalonia.Controls.DataGridRowEventArgs e) {
            e.Row.Header = (e.Row.Index + 1).ToString();
        }
        private void EditorGrid_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e) {
            if (EditorGrid.SelectedItem != null) {
                EditorGrid.ScrollIntoView(EditorGrid.SelectedItem, null);
            }
        }
        private void EditorGrid_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e) {
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed) {
                EditorGrid.SelectedItem = null;
            }
        }
        protected override void OnDataContextChanged(EventArgs e) {
            base.OnDataContextChanged(e);
            
            if (this.DataContext is DictionaryEditorViewModel vm) {
                vm.RefreshIndices = () => {
                    Dispatcher.UIThread.Post(() => {
                        var rows = EditorGrid.GetVisualDescendants().OfType<Avalonia.Controls.DataGridRow>();
                        foreach (var row in rows) {
                            row.Header = (row.Index + 1).ToString();
                        }
                    }, DispatcherPriority.Background);
                };
            }
        }
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);
            if (change.Property == PartProperty) {
                Log.Information("DictionaryEditor: PartProperty changed in UI.");
                LoadDictionaryForPart((UVoicePart?)change.NewValue);
            }
        }
        private void RebuildGridColumns(YamlCategory? category) {
            var grid = this.FindControl<DataGrid>("EditorGrid");
            if (grid == null) return;

            var currentData = grid.ItemsSource;
            grid.ItemsSource = null;

            // Safely clear and rebuild the columns
            grid.Columns.Clear();
            if (category != null) {
                foreach (var colName in category.Columns) {
                    var column = new DataGridTextColumn {
                        Header = colName,
                        Binding = new Binding($"[{colName}]"),
                        Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                    };
                    grid.Columns.Add(column);
                }
            }
            grid.ItemsSource = currentData;
        }
        private void OnRefreshClicked(object? sender, RoutedEventArgs e) {
            Log.Information("DictionaryEditor: Refresh button clicked.");
            LoadDictionaryForPart(Part);
        }

        private void OnOpenFileClicked(object? sender, RoutedEventArgs e) {
            string filePath = ViewModel.GetSelectedFileFullPath();

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath)) {
                try {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                } catch (Exception ex) {
                    Serilog.Log.Error(ex, $"DictionaryEditor: Failed to open file in external editor: {filePath}");
                }
            }
        }

        private void LoadDictionaryForPart(UVoicePart? part) {
            Log.Information("--- DictionaryEditor: Attempting to load dictionary ---");

            if (part == null) {
                Log.Information("DictionaryEditor: ABORT - Part is null.");
                ViewModel.ClearContext();
                return;
            }

            var project = DocManager.Inst.Project;
            if (project == null || part.trackNo >= project.tracks.Count) {
                ViewModel.ClearContext();
                return;
            }

            var track = project.tracks[part.trackNo];
            var singer = track.Singer;

            if (singer == null || string.IsNullOrEmpty(singer.Location) || !Directory.Exists(singer.Location)) {
                ViewModel.ClearContext();
                return;
            }

            Log.Information($"DictionaryEditor: Found singer '{singer.Name}'. Location path is: '{singer.Location}'");

            var allFiles = Directory.GetFiles(singer.Location, "*.*", SearchOption.AllDirectories);
            var excludedFiles = new HashSet<string> { "character.yaml", "dsconfig.yaml", "vocoder.yaml" };

            var validFiles = allFiles
                .Where(f => {
                    string fileName = Path.GetFileName(f).ToLower();
                    bool isValidYaml = fileName.EndsWith(".yaml") && !excludedFiles.Contains(fileName);
                    bool isPresamp = fileName == "presamp.ini";
                    
                    return isValidYaml || isPresamp;
                })
                .ToList();

            // Group by filename to find duplicates
            var groupedFiles = validFiles.GroupBy(f => Path.GetFileName(f).ToLower()).ToList();
            var displayNames = new List<string>();
            var fileMap = new Dictionary<string, string>();

            foreach (var group in groupedFiles) {
                if (group.Count() == 1) {
                    var filePath = group.First();
                    var fileName = Path.GetFileName(filePath);
                    var relativePath = Path.GetRelativePath(singer.Location, filePath);

                    displayNames.Add(fileName);
                    fileMap[fileName] = relativePath;
                } else {
                    foreach (var filePath in group) {
                        var fileName = Path.GetFileName(filePath);
                        var folderName = Path.GetFileName(Path.GetDirectoryName(filePath));
                        var isRoot = Path.GetFullPath(Path.GetDirectoryName(filePath)!) == Path.GetFullPath(singer.Location);

                        string displayName = isRoot ? $"{fileName}" : $"{fileName} ({folderName})";

                        int counter = 1;
                        string finalName = displayName;
                        while (fileMap.ContainsKey(finalName)) {
                            finalName = $"{displayName} ({counter++})";
                        }

                        displayNames.Add(finalName);
                        fileMap[finalName] = Path.GetRelativePath(singer.Location, filePath);
                    }
                }
            }

            Log.Information($"DictionaryEditor: Found {displayNames.Count} valid dictionary/presamp files.");
            ViewModel.SetSingerContext(singer.Location, fileMap);
        }
    }
}