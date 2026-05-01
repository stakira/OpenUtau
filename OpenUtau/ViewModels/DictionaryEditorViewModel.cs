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
        private System.Text.Encoding _currentPresampEncoding = System.Text.Encoding.UTF8;
        private Dictionary<string, string> _filePaths = new();
        public ObservableCollection<string> AvailableFiles { get; } = new();
        [Reactive] public string SelectedFile { get; set; } = string.Empty;
        public string CurrentFileType => !string.IsNullOrEmpty(SelectedFile) && SelectedFile.EndsWith(".ini", StringComparison.OrdinalIgnoreCase) ? "ini" : "yaml";

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
        public Action? RefreshIndices { get; set; }
        [Reactive] public string? ReplaceColumn { get; set; }
        [Reactive] public string FindText { get; set; } = string.Empty;
        [Reactive] public string ReplaceText { get; set; } = string.Empty;
        [Reactive] public bool UseRegex { get; set; } = false;
        private List<DynamicYamlRow> _internalClipboard = new();

        public void DeselectAll() {
            SelectedRow = null;
        }
        private DynamicYamlRow CloneRow(DynamicYamlRow original) {
            var clone = new DynamicYamlRow();
            if (SelectedCategory != null) {
                foreach (var col in SelectedCategory.Columns) {
                    clone[col] = original[col];
                }
            }
            return clone;
        }
        public void CopyRow(object? parameter) {
            _internalClipboard.Clear();
            if (parameter is System.Collections.IList selectedItems && selectedItems.Count > 0) {
                foreach (var item in selectedItems.Cast<DynamicYamlRow>()) {
                    _internalClipboard.Add(CloneRow(item));
                }
            } 
            else if (SelectedRow != null) {
                _internalClipboard.Add(CloneRow(SelectedRow));
            }
        }
        public void CutRow(object? parameter) {
            CopyRow(parameter);
            DeleteSelectedRow(parameter); 
        }

        public void PasteRow() {
            if (SelectedCategory == null || _internalClipboard.Count == 0) return;
            int insertIndex = SelectedCategory.Rows.Count;
            if (SelectedRow != null) {
                insertIndex = SelectedCategory.Rows.IndexOf(SelectedRow) + 1;
            }

            foreach (var copiedItem in _internalClipboard) {
                var newRow = CloneRow(copiedItem);
                SelectedCategory.Rows.Insert(insertIndex, newRow);
                insertIndex++;
                
                SelectedRow = newRow;
            }

            RefreshIndices?.Invoke(); 
        }

        public DictionaryEditorViewModel() {
            this.WhenAnyValue(x => x.SelectedFile)
                .Subscribe(file => {
                    this.RaisePropertyChanged(nameof(CurrentFileType)); 
                    if (!string.IsNullOrEmpty(file) && !string.IsNullOrEmpty(_currentDirectory)) {
                        LoadSelectedFile(); 
                    }
                });
        }
        private void Find(bool searchUp) {
            if (SelectedCategory == null || string.IsNullOrEmpty(ReplaceColumn) || string.IsNullOrEmpty(FindText)) return;

            int startIndex = 0;
            if (SelectedRow != null) {
                startIndex = SelectedCategory.Rows.IndexOf(SelectedRow);
                startIndex += searchUp ? -1 : 1;
            }

            int count = SelectedCategory.Rows.Count;
            if (count == 0) return;

            // Loop through all rows, wrapping around the top/bottom if necessary
            for (int i = 0; i < count; i++) {
                int offset = searchUp ? -i : i;
                int index = (startIndex + offset) % count;
                if (index < 0) index += count;

                var row = SelectedCategory.Rows[index];
                string currentVal = row[ReplaceColumn];

                if (string.IsNullOrEmpty(currentVal)) continue;

                bool isMatch = false;
                if (UseRegex) {
                    try { isMatch = System.Text.RegularExpressions.Regex.IsMatch(currentVal, FindText); } catch { }
                } else {
                    isMatch = currentVal.Contains(FindText);
                }

                if (isMatch) {
                    SelectedRow = row;
                    return; 
                }
            }
        }
        public void ExecuteFindNext() => Find(searchUp: false);
        public void ExecuteFindPrevious() => Find(searchUp: true);
        public void ExecuteFindAll(object? parameter) {
            if (SelectedCategory == null || string.IsNullOrEmpty(ReplaceColumn) || string.IsNullOrEmpty(FindText)) return;

            if (parameter is System.Collections.IList selectedItems) {
                selectedItems.Clear(); // Deselect everything first

                foreach (var row in SelectedCategory.Rows) {
                    string currentVal = row[ReplaceColumn];
                    if (string.IsNullOrEmpty(currentVal)) continue;

                    bool isMatch = false;
                    if (UseRegex) {
                        try { isMatch = System.Text.RegularExpressions.Regex.IsMatch(currentVal, FindText); } catch { }
                    } else {
                        isMatch = currentVal.Contains(FindText);
                    }

                    if (isMatch) {
                        selectedItems.Add(row);
                    }
                }
            }
        }
        public void ExecuteReplace() {
            if (SelectedCategory == null || SelectedRow == null || string.IsNullOrEmpty(ReplaceColumn) || string.IsNullOrEmpty(FindText)) return;

            string currentVal = SelectedRow[ReplaceColumn];
            if (!string.IsNullOrEmpty(currentVal)) {
                if (UseRegex) {
                    try { SelectedRow[ReplaceColumn] = System.Text.RegularExpressions.Regex.Replace(currentVal, FindText, ReplaceText); } catch { }
                } else {
                    SelectedRow[ReplaceColumn] = currentVal.Replace(FindText, ReplaceText);
                }
            }
            
            ExecuteFindNext();
        }
        public void ExecuteReplaceAll() {
            if (SelectedCategory == null || string.IsNullOrEmpty(ReplaceColumn) || string.IsNullOrEmpty(FindText)) return;

            foreach (var row in SelectedCategory.Rows) {
                string currentVal = row[ReplaceColumn];
                if (string.IsNullOrEmpty(currentVal)) continue;

                if (UseRegex) {
                    try { row[ReplaceColumn] = System.Text.RegularExpressions.Regex.Replace(currentVal, FindText, ReplaceText); } catch { }
                } else {
                    row[ReplaceColumn] = currentVal.Replace(FindText, ReplaceText);
                }
            }
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
        public string GetSelectedFileFullPath() {
            if (string.IsNullOrEmpty(SelectedFile) || string.IsNullOrEmpty(_currentDirectory)) {
                return string.Empty;
            }
            if (_filePaths.TryGetValue(SelectedFile, out string? relativePath) && relativePath != null) {
                return Path.Combine(_currentDirectory, relativePath);
            }

            return string.Empty;
        }
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
            LoadYaml(filePath); 
            ToggleNewFilePanel();
            NewFileName = string.Empty;
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
                    RefreshIndices?.Invoke(); 
                    return;
                }
            }
            // Otherwise, just drop it at the bottom
            SelectedCategory.Rows.Add(newRow);
            SelectedRow = newRow;
            RefreshIndices?.Invoke(); 
        }

        public void DeleteSelectedRow(object? parameter) {
            if (SelectedCategory == null) return;
            if (parameter is System.Collections.IList selectedItems && selectedItems.Count > 0) {
                var itemsToDelete = selectedItems.Cast<DynamicYamlRow>().ToList();
                foreach (var item in itemsToDelete) {
                    SelectedCategory.Rows.Remove(item);
                }
            }
            else if (SelectedRow != null) {
                SelectedCategory.Rows.Remove(SelectedRow);
            }
            RefreshIndices?.Invoke();
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
        public void LoadPresamp(string filePath) {
            Categories.Clear();
            if (!File.Exists(filePath)) return;
            
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            byte[] rawBytes = File.ReadAllBytes(filePath);
            var strictUtf8 = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

            try {
                strictUtf8.GetString(rawBytes);
                _currentPresampEncoding = new System.Text.UTF8Encoding(true); 
            } 
            catch (System.Text.DecoderFallbackException) {
                _currentPresampEncoding = System.Text.Encoding.GetEncoding("shift_jis");
            }
            string[] lines = File.ReadAllLines(filePath, _currentPresampEncoding);
            YamlCategory? currentCategory = null;

            foreach (var rawLine in lines) {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                if (line.StartsWith("[") && line.EndsWith("]")) {
                    string sectionName = line.Substring(1, line.Length - 2);
                    currentCategory = new YamlCategory { Name = sectionName };
                    Categories.Add(currentCategory);

                    if (sectionName == "VOWEL") {
                        currentCategory.Columns = new List<string> { "ID", "Base", "Phonemes", "Vol" };
                    } else if (sectionName == "CONSONANT") {
                        currentCategory.Columns = new List<string> { "ID", "Phonemes", "Crossfade" };
                    } else if (sectionName == "REPLACE" || sectionName == "ALIAS") {
                        currentCategory.Columns = new List<string> { "Key", "Value" };
                    } else {
                        currentCategory.Columns = new List<string> { "Value" };
                    }
                    continue;
                }

                if (currentCategory == null) continue;
                var newRow = new DynamicYamlRow();
                
                if (currentCategory.Name == "VOWEL") {
                    var parts = line.Split('=');
                    newRow["ID"] = parts.Length > 0 ? parts[0] : "";
                    newRow["Base"] = parts.Length > 1 ? parts[1] : "";
                    newRow["Phonemes"] = parts.Length > 2 ? parts[2] : "";
                    newRow["Vol"] = parts.Length > 3 ? parts[3] : "";
                } 
                else if (currentCategory.Name == "CONSONANT") {
                    var parts = line.Split('=');
                    newRow["ID"] = parts.Length > 0 ? parts[0] : "";
                    newRow["Phonemes"] = parts.Length > 1 ? parts[1] : "";
                    newRow["Crossfade"] = parts.Length > 2 ? parts[2] : "";
                } 
                else if (currentCategory.Name == "REPLACE" || currentCategory.Name == "ALIAS") {
                    var parts = line.Split(new[] { '=' }, 2);
                    newRow["Key"] = parts.Length > 0 ? parts[0] : "";
                    newRow["Value"] = parts.Length > 1 ? parts[1] : "";
                } 
                else {
                    // Single value lists like [PRIORITY], [APPEND], [PITCH]
                    newRow["Value"] = line;
                }

                currentCategory.Rows.Add(newRow);
            }

            if (Categories.Count > 0) SelectedCategory = Categories[0];
            
            // FIX 2: Added the missing semicolon!
            ColumnsChanged?.Invoke(); 
        }
        public void SavePresamp(string filePath) {
            var lines = new List<string>();

            foreach (var cat in Categories) {
                lines.Add($"[{cat.Name}]");

                foreach (var row in cat.Rows) {
                    if (cat.Name == "VOWEL") {
                        lines.Add($"{row["ID"]}={row["Base"]}={row["Phonemes"]}={row["Vol"]}");
                    } 
                    else if (cat.Name == "CONSONANT") {
                        lines.Add($"{row["ID"]}={row["Phonemes"]}={row["Crossfade"]}");
                    } 
                    else if (cat.Name == "REPLACE" || cat.Name == "ALIAS") {
                        lines.Add($"{row["Key"]}={row["Value"]}");
                    } 
                    else {
                        string val = row["Value"] ?? "";
                        if (!string.IsNullOrEmpty(val)) {
                            lines.Add(val);
                        }
                    }
                }
            }

            File.WriteAllLines(filePath, lines, _currentPresampEncoding);
        }
        public void LoadSelectedFile() {
            if (string.IsNullOrEmpty(SelectedFile) || string.IsNullOrEmpty(_currentDirectory)) return;
            if (_filePaths.TryGetValue(SelectedFile, out string? relativePath) && relativePath != null) {
                string targetPath = Path.Combine(_currentDirectory, relativePath);
                if (SelectedFile.EndsWith(".ini", StringComparison.OrdinalIgnoreCase)) {
                    LoadPresamp(targetPath);
                } else {
                    LoadYaml(targetPath);
                }
            }
        }

        public void SaveCurrentFile() {
            if (string.IsNullOrEmpty(SelectedFile) || string.IsNullOrEmpty(_currentDirectory)) return;
            
            if (_filePaths.TryGetValue(SelectedFile, out string? relativePath) && relativePath != null) {
                string targetPath = Path.Combine(_currentDirectory, relativePath);
                if (SelectedFile.EndsWith(".ini", StringComparison.OrdinalIgnoreCase)) {
                    SavePresamp(targetPath);
                } else {
                    SaveYaml(); 
                }
            }
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