using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace OpenUtau.App.ViewModels {
    public class DynamicYamlRow : ReactiveObject {
        private readonly Dictionary<string, string> _data = new();

        public string this[string key] {
            get => _data.ContainsKey(key) ? _data[key] : string.Empty;
            set {
                _data[key] = value;
                this.RaisePropertyChanged("Item");
            }
        }
        public Dictionary<string, string> GetData() => _data;
    }

    public class YamlCategory : ReactiveObject {
        public string Name { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new();
        public HashSet<string> ListColumns { get; set; } = new();
        public bool IsDictionaryFormat { get; set; } = false;
        public ObservableCollection<DynamicYamlRow> Rows { get; } = new();
    }

    public class DictionaryEditorViewModel : ViewModelBase {
        private string _currentDirectory = string.Empty;
        private Dictionary<string, string> _filePaths = new();
        public ObservableCollection<string> AvailableFiles { get; } = new();
        [Reactive] public string SelectedFile { get; set; } = string.Empty;

        // Dynamic categories for new tables
        public ObservableCollection<YamlCategory> Categories { get; } = new();
        [Reactive] public YamlCategory? SelectedCategory { get; set; }
        public event Action? ColumnsChanged;
        [Reactive] public DynamicYamlRow? SelectedRow { get; set; }
        [Reactive] public bool IsCreatingNewCategory { get; set; } = false;
        [Reactive] public string NewCategoryName { get; set; } = string.Empty;
        [Reactive] public string NewCategoryColumns { get; set; } = string.Empty;
        [Reactive] public bool IsManagingColumns { get; set; } = false;
        [Reactive] public string ManageColumnName { get; set; } = string.Empty;
        [Reactive] public bool IsConfirmingDelete { get; set; } = false;
        [Reactive] public bool IsCreatingNewFile { get; set; } = false;
        [Reactive] public string NewFileName { get; set; } = string.Empty;

        public DictionaryEditorViewModel() {
            this.WhenAnyValue(x => x.SelectedFile)
                .Subscribe(file => {
                    if (!string.IsNullOrEmpty(file) && !string.IsNullOrEmpty(_currentDirectory)) {
                        if (_filePaths.TryGetValue(file, out string? relativePath) && relativePath != null) {
                            LoadYaml(Path.Combine(_currentDirectory, relativePath));
                        }
                    }
                });
        }

        public void ToggleNewFilePanel() {
            IsCreatingNewFile = !IsCreatingNewFile;
            IsCreatingNewCategory = false;
            IsManagingColumns = false;
            IsConfirmingDelete = false;
            NewFileName = string.Empty;
        }

        public void ToggleConfirmDeletePanel() {
            if (string.IsNullOrEmpty(SelectedFile)) return;
            IsConfirmingDelete = !IsConfirmingDelete;
            IsCreatingNewFile = false;
            IsCreatingNewCategory = false;
            IsManagingColumns = false;
        }

        public void ToggleNewCategoryPanel() {
            IsCreatingNewCategory = !IsCreatingNewCategory;
            IsCreatingNewFile = false;
            IsManagingColumns = false;
            IsConfirmingDelete = false;
            NewCategoryName = string.Empty;
            NewCategoryColumns = string.Empty;
        }

        public void ToggleManageColumnsPanel() {
            IsManagingColumns = !IsManagingColumns;
            IsCreatingNewFile = false;
            IsCreatingNewCategory = false;
            IsConfirmingDelete = false;
            ManageColumnName = string.Empty;
        }

        // File Actions
        public void ConfirmNewFile() {
            if (string.IsNullOrWhiteSpace(NewFileName) || string.IsNullOrEmpty(_currentDirectory)) return;

            string fileName = NewFileName.Trim();
            if (!fileName.EndsWith(".yaml") && !fileName.EndsWith(".yml")) {
                fileName += ".yaml";
            }
            string filePath = Path.Combine(_currentDirectory, fileName);
            if (!File.Exists(filePath)) {
                File.WriteAllText(filePath, "Metadata:\n  Created: True\n");
                AvailableFiles.Add(fileName);
            }
            SelectedFile = fileName;
            ToggleNewFilePanel();
        }

        public void DeleteSelectedFile() {
            if (string.IsNullOrEmpty(SelectedFile) || string.IsNullOrEmpty(_currentDirectory)) return;

            if (_filePaths.TryGetValue(SelectedFile, out string? relativePath) && relativePath != null) {
                string filePath = Path.Combine(_currentDirectory, relativePath);
                if (File.Exists(filePath)) {
                    File.Delete(filePath);
                }
            }

            AvailableFiles.Remove(SelectedFile);
            if (AvailableFiles.Count > 0) SelectedFile = AvailableFiles[0];
            else ClearContext();
        }

        public void ConfirmDeleteFile() {
            if (string.IsNullOrEmpty(SelectedFile) || string.IsNullOrEmpty(_currentDirectory)) return;

            if (_filePaths.TryGetValue(SelectedFile, out string? relativePath) && relativePath != null) {
                string filePath = Path.Combine(_currentDirectory, relativePath);
                if (File.Exists(filePath)) {
                    File.Delete(filePath);
                }
            }

            AvailableFiles.Remove(SelectedFile);

            if (AvailableFiles.Count > 0) SelectedFile = AvailableFiles[0];
            else ClearContext();

            IsConfirmingDelete = false;
        }

        // Object Actions
        public void ConfirmNewCategory() {
            if (string.IsNullOrWhiteSpace(NewCategoryName) || string.IsNullOrWhiteSpace(NewCategoryColumns)) return;

            var columns = NewCategoryColumns.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList();
            if (columns.Count == 0) return;

            var newCat = new YamlCategory { Name = NewCategoryName.Trim(), Columns = columns };
            Categories.Add(newCat);
            SelectedCategory = newCat;
            ToggleNewCategoryPanel();
        }

        public void DeleteSelectedCategory() {
            if (SelectedCategory != null) {
                Categories.Remove(SelectedCategory);
                SelectedCategory = Categories.FirstOrDefault();
            }
        }

        // Column Actions
        public void AddNewColumn() {
            if (SelectedCategory == null || string.IsNullOrWhiteSpace(ManageColumnName)) return;
            string col = ManageColumnName.Trim();
            if (!SelectedCategory.Columns.Contains(col)) {
                SelectedCategory.Columns.Add(col);
                ColumnsChanged?.Invoke();
            }
            ManageColumnName = string.Empty;
        }

        public void RemoveColumn() {
            if (SelectedCategory == null || string.IsNullOrWhiteSpace(ManageColumnName)) return;
            string col = ManageColumnName.Trim();
            if (SelectedCategory.Columns.Contains(col)) {
                SelectedCategory.Columns.Remove(col);
                ColumnsChanged?.Invoke();
            }
            ManageColumnName = string.Empty;
        }

        // Row Actions
        public void AddNewRow() {
            if (SelectedCategory == null) return;
            var newRow = new DynamicYamlRow();

            // If a row is clicked, insert the new one directly below it
            if (SelectedRow != null) {
                int index = SelectedCategory.Rows.IndexOf(SelectedRow);
                if (index >= 0) {
                    SelectedCategory.Rows.Insert(index + 1, newRow);
                    SelectedRow = newRow;
                    return;
                }
            }
            // Otherwise, just drop it at the bottom
            SelectedCategory.Rows.Add(newRow);
            SelectedRow = newRow;
        }

        public void DeleteSelectedRow() {
            if (SelectedCategory != null && SelectedRow != null) {
                SelectedCategory.Rows.Remove(SelectedRow);
            }
        }

        public void SetSingerContext(string dir, Dictionary<string, string> fileMap) {
            _currentDirectory = dir;
            _filePaths = fileMap;

            AvailableFiles.Clear();
            foreach (var name in fileMap.Keys) {
                AvailableFiles.Add(name);
            }

            if (AvailableFiles.Count > 0) {
                SelectedFile = AvailableFiles[0];
            }
        }

        public void ClearContext() {
            _currentDirectory = string.Empty;
            AvailableFiles.Clear();
            Categories.Clear();
        }

        public void LoadYaml(string filePath) {
            Categories.Clear();
            if (!File.Exists(filePath)) return;

            try {
                var deserializer = new DeserializerBuilder().Build();
                var yamlContent = File.ReadAllText(filePath);

                var rawData = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
                if (rawData == null) return;

                YamlCategory? metaCategory = null;

                foreach (var kvp in rawData) {
                    string rootKey = kvp.Key;
                    object rootValue = kvp.Value;

                    if (rootValue is List<object> rows) {
                        var category = new YamlCategory { Name = rootKey };
                        var allColumns = new HashSet<string>();

                        foreach (var rowObj in rows) {
                            if (rowObj is Dictionary<object, object> rowDict)
                                foreach (var key in rowDict.Keys)
                                    if (key != null) allColumns.Add(key.ToString() ?? "");
                        }
                        category.Columns = allColumns.ToList();

                        if (category.Columns.Count > 0) {
                            foreach (var rowObj in rows) {
                                if (rowObj is Dictionary<object, object> rowDict) {
                                    var row = new DynamicYamlRow();
                                    foreach (var col in category.Columns) {
                                        var keyMatch = rowDict.Keys.FirstOrDefault(k => k?.ToString() == col);
                                        if (keyMatch != null) {
                                            var val = rowDict[keyMatch];
                                            if (val is List<object> list) {
                                                // Re-wrap list items in quotes if they contain spaces or commas
                                                var formattedList = list.Select(x => {
                                                    string s = x?.ToString() ?? "";
                                                    return (s.Contains(" ") || s.Contains(",")) ? $"\"{s}\"" : s;
                                                });
                                                row[col] = string.Join(" ", formattedList);
                                                category.ListColumns.Add(col);
                                            } else {
                                                row[col] = val?.ToString() ?? "";
                                            }
                                        }
                                    }
                                    category.Rows.Add(row);
                                }
                            }
                        }
                        Categories.Add(category);
                    } else if (rootValue is Dictionary<object, object> dictData) {
                        var category = new YamlCategory {
                            Name = rootKey,
                            Columns = new List<string> { "Key", "Value" },
                            IsDictionaryFormat = true
                        };

                        foreach (var innerKvp in dictData) {
                            var row = new DynamicYamlRow();
                            row["Key"] = innerKvp.Key?.ToString() ?? "";

                            if (innerKvp.Value is List<object> list) {
                                var formattedList = list.Select(x => {
                                    string s = x?.ToString() ?? "";
                                    return (s.Contains(" ") || s.Contains(",")) ? $"\"{s}\"" : s;
                                });
                                row["Value"] = string.Join(" ", formattedList);
                                category.ListColumns.Add("Value");
                            } else {
                                row["Value"] = innerKvp.Value?.ToString() ?? "";
                            }
                            category.Rows.Add(row);
                        }
                        Categories.Add(category);
                    } else {
                        if (metaCategory == null) {
                            metaCategory = new YamlCategory { Name = "Metadata", Columns = new List<string> { "Key", "Value" } };
                            Categories.Insert(0, metaCategory);
                        }
                        var row = new DynamicYamlRow();
                        row["Key"] = rootKey;
                        row["Value"] = rootValue?.ToString() ?? "";
                        metaCategory.Rows.Add(row);
                    }
                }

                if (Categories.Count > 0) SelectedCategory = Categories[0];
            } catch (Exception ex) {
                Serilog.Log.Error(ex, $"Failed to parse YAML: {filePath}");
                Categories.Clear();
            }
        }

        public void SaveYaml() {
            if (string.IsNullOrEmpty(SelectedFile) || string.IsNullOrEmpty(_currentDirectory)) return;

            var dictToSave = new Dictionary<string, object>();

            foreach (var cat in Categories) {
                if (cat.Name == "Metadata") {
                    foreach (var row in cat.Rows) {
                        string key = row["Key"];
                        string val = row["Value"];
                        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(val)) {
                            if (double.TryParse(val, out double numVal)) dictToSave[key] = numVal;
                            else dictToSave[key] = val;
                        }
                    }
                } else if (cat.IsDictionaryFormat) {
                    var dictNode = new Dictionary<string, object>();
                    foreach (var row in cat.Rows) {
                        string key = row["Key"];
                        string val = row["Value"];

                        if (string.IsNullOrWhiteSpace(key)) continue;

                        if (cat.ListColumns.Contains("Value") && !string.IsNullOrWhiteSpace(val)) {
                            var matches = System.Text.RegularExpressions.Regex.Matches(val, @"\""[^\""]*\""|[^ ,]+");
                            dictNode[key] = matches.Cast<System.Text.RegularExpressions.Match>()
                                                   .Select(m => m.Value.Trim('"'))
                                                   .ToList();
                        } else {
                            dictNode[key] = val;
                        }
                    }
                    dictToSave[cat.Name] = dictNode;
                } else {
                    var rowList = new List<Dictionary<string, object>>();
                    foreach (var row in cat.Rows) {
                        var newRow = new Dictionary<string, object>();
                        foreach (var col in cat.Columns) {
                            string val = row[col];
                            if (string.IsNullOrWhiteSpace(val)) continue;

                            if (cat.ListColumns.Contains(col)) {
                                var matches = System.Text.RegularExpressions.Regex.Matches(val, @"\""[^\""]*\""|[^ ,]+");
                                newRow[col] = matches.Cast<System.Text.RegularExpressions.Match>()
                                                     .Select(m => m.Value.Trim('"'))
                                                     .ToList();
                            } else {
                                newRow[col] = val;
                            }
                        }
                        if (newRow.Count > 0) rowList.Add(newRow);
                    }
                    dictToSave[cat.Name] = rowList;
                }
            }

            var serializer = new SerializerBuilder()
                .DisableAliases()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .WithIndentedSequences()
                .WithEventEmitter(next => new BracketStyleEmitter(next))
                .Build();

            if (_filePaths.TryGetValue(SelectedFile, out string? relativePath) && relativePath != null) {
                File.WriteAllText(Path.Combine(_currentDirectory, relativePath), serializer.Serialize(dictToSave));
            }
        }
    }

    public class BracketStyleEmitter : ChainedEventEmitter {
        private int _depth = 0;

        public BracketStyleEmitter(IEventEmitter nextEmitter) : base(nextEmitter) { }

        public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter) {
            _depth++;
            // Depth 1 is the Root, Depth 2 is the Category List.
            // Depth 3+ is the inner row data, which we want wrapped in inline brackets { }
            eventInfo.Style = _depth >= 3 ? MappingStyle.Flow : MappingStyle.Block;

            base.Emit(eventInfo, emitter);
        }

        public override void Emit(MappingEndEventInfo eventInfo, IEmitter emitter) {
            _depth--;
            base.Emit(eventInfo, emitter);
        }
        public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter) {
            _depth++;
            // Depth 3+ is a list inside a row (like phonemes), which we want wrapped in brackets [ ]
            eventInfo.Style = _depth >= 3 ? SequenceStyle.Flow : SequenceStyle.Block;
            base.Emit(eventInfo, emitter);
        }
        public override void Emit(SequenceEndEventInfo eventInfo, IEmitter emitter) {
            _depth--;
            base.Emit(eventInfo, emitter);
        }
    }
}