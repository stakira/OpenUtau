using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DynamicData.Binding;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

/*
 * Made And Checked By DELTA SYNTH & Gemini AI
 * Original Author: OpenUtau Team & Delta
 */

namespace OpenUtau.App.ViewModels {
    /// <summary>
    /// ตัวแทนข้อมูล Voice Color ที่ประกอบไปด้วยช่วงเสียงต่างๆ
    /// </summary>
    class VoiceColor : ReactiveObject {
        [Reactive] public string Name { get; set; }
        [Reactive] public ObservableCollectionExtended<VoiceColorRow> Rows { get; set; }

        public VoiceColor(string color, List<Subbank> subbanks) {
            Name = color;
            Rows = new ObservableCollectionExtended<VoiceColorRow>();
            
            // สร้างช่วงคีย์เปียโน (Midi Note 24 ถึง 107)
            for (int i = 107; i >= 24; --i) {
                Rows.Add(new VoiceColorRow(i, string.Empty, string.Empty));
            }

            foreach (var subbank in subbanks) {
                if (subbank.ToneRanges == null) continue;
                
                foreach (var range in subbank.ToneRanges) {
                    var parts = range.Split('-');
                    if (parts.Length == 1) {
                        int tone = MusicMath.NameToTone(parts[0]);
                        if (tone >= 24 && tone <= 107) {
                            Rows[107 - tone].Prefix = subbank.Prefix;
                            Rows[107 - tone].Suffix = subbank.Suffix;
                        }
                    } else if (parts.Length == 2) {
                        int start = MusicMath.NameToTone(parts[0]);
                        int end = MusicMath.NameToTone(parts[1]);
                        for (int tone = start; tone <= end; ++tone) {
                            if (tone >= 24 && tone <= 107) {
                                Rows[107 - tone].Prefix = subbank.Prefix;
                                Rows[107 - tone].Suffix = subbank.Suffix;
                            }
                        }
                    }
                }
            }
        }

        public override string ToString() =>
            string.IsNullOrEmpty(Name) ? "(เสียงหลัก)" : Name;
    }

    /// <summary>
    /// ข้อมูลแต่ละแถวใน Voice Color (แทนค่าหนึ่งตัวโน้ต)
    /// </summary>
    class VoiceColorRow : ReactiveObject {
        public readonly int index;
        [Reactive] public string Tone { get; private set; }
        [Reactive] public string Prefix { get; set; }
        [Reactive] public string Suffix { get; set; }

        public VoiceColorRow(int index, string prefix, string suffix) {
            this.index = index;
            Tone = MusicMath.GetToneName(index);
            Prefix = prefix;
            Suffix = suffix;
        }
    }

    public class EditSubbanksViewModel : ViewModelBase {
        [Reactive] public ObservableCollectionExtended<VoiceColor> Colors { get; set; }
        [Reactive] public VoiceColor? SelectedColor { get; set; }
        [Reactive] public bool IsEditableColor { get; set; }
        [Reactive] public ObservableCollectionExtended<VoiceColorRow>? Rows { get; set; }
        [Reactive] public ObservableCollectionExtended<VoiceColorRow>? SelectedRows { get; set; }
        [Reactive] public string Prefix { get; set; }
        [Reactive] public string Suffix { get; set; }

        public USinger? Singer { get; private set; }

        public EditSubbanksViewModel() {
            Colors = new ObservableCollectionExtended<VoiceColor>();
            
            // ตรวจสอบการเลือก Voice Color เพื่อเปิด/ปิดการแก้ไข
            this.WhenAnyValue(x => x.SelectedColor)
                .Subscribe(color => {
                    Rows = color?.Rows;
                    IsEditableColor = color != null && !string.IsNullOrEmpty(color.Name);
                });

            Prefix = string.Empty;
            Suffix = string.Empty;
        }

        public void SetSinger(USinger singer) {
            this.Singer = singer;
            LoadSubbanks();
        }

        /// <summary>
        /// โหลดข้อมูล Subbank จากตัวนักร้อง (Singer)
        /// </summary>
        public void LoadSubbanks() {
            if (Singer == null) return;

            try {
                Colors.Clear();
                var colorsMap = new Dictionary<string, List<Subbank>>();

                foreach (var subbank in Singer.Subbanks) {
                    string colorName = subbank.Color ?? string.Empty;
                    if (!colorsMap.TryGetValue(colorName, out var subbanks)) {
                        subbanks = new List<Subbank>();
                        colorsMap[colorName] = subbanks;
                    }
                    subbanks.Add(subbank.subbank);
                }

                foreach (var key in colorsMap.Keys.OrderBy(k => k)) {
                    Colors.Add(new VoiceColor(key, colorsMap[key]));
                }

                if (Colors.Count == 0) {
                    Colors.Add(new VoiceColor(string.Empty, new List<Subbank>()));
                }

                SelectedColor = Colors[0];
            } catch (Exception e) {
                NotifyError("ไม่สามารถโหลดข้อมูล Subbanks ได้", e);
            }
        }

        public void AddSubbank(string name) {
            if (string.IsNullOrWhiteSpace(name) || Colors.Any(c => c.Name == name)) return;

            var color = new VoiceColor(name, new List<Subbank>());
            Colors.Add(color);
            SelectedColor = color;
        }

        public void RemoveSubbank() {
            if (SelectedColor == null || string.IsNullOrEmpty(SelectedColor.Name)) return;

            Colors.Remove(SelectedColor);
            SelectedColor = Colors.FirstOrDefault();
        }

        public void RenameSubbank(string name) {
            if (string.IsNullOrWhiteSpace(name) || SelectedColor == null || SelectedColor.Name == name) return;
            SelectedColor.Name = name;
        }

        /// <summary>
        /// ตั้งค่า Prefix และ Suffix ให้กับแถวที่เลือก
        /// </summary>
        public void Set(IList items) {
            foreach (var item in items) {
                if (item is VoiceColorRow row) {
                    row.Prefix = Prefix;
                    row.Suffix = Suffix;
                }
            }
        }

        /// <summary>
        /// ล้างค่า Prefix และ Suffix ของแถวที่เลือก
        /// </summary>
        public void Clear(IList items) {
            foreach (var item in items) {
                if (item is VoiceColorRow row) {
                    row.Prefix = string.Empty;
                    row.Suffix = string.Empty;
                }
            }
        }

        /// <summary>
        /// บันทึกการตั้งค่า Subbanks ลงในไฟล์ character.yaml
        /// </summary>
        public void SaveSubbanks() {
            if (Singer == null) return;

            var yamlPath = Path.Combine(Singer.Location, "character.yaml");
            VoicebankConfig? bankConfig = null;

            try {
                if (File.Exists(yamlPath)) {
                    using var stream = File.OpenRead(yamlPath);
                    bankConfig = VoicebankConfig.Load(stream);
                }
            } catch { /* ปล่อยผ่านหากโหลดไฟล์เก่าไม่ได้เพื่อสร้างใหม่ */ }

            bankConfig ??= new VoicebankConfig();
            bankConfig.Subbanks = ColorsToSubbanks();

            try {
                using var stream = File.Open(yamlPath, FileMode.Create);
                bankConfig.Save(stream);
                LoadSubbanks(); // รีโหลดข้อมูลเพื่อให้ UI อัปเดตล่าสุด
            } catch (Exception e) {
                NotifyError("ไม่สามารถบันทึกข้อมูล Subbanks ได้", e);
            }
        }

        private Subbank[] ColorsToSubbanks() {
            var result = new List<Subbank>();
            foreach (var color in Colors) {
                var subbankGroups = new Dictionary<Tuple<string, string>, SortedSet<int>>();

                foreach (var row in color.Rows) {
                    var key = Tuple.Create(row.Prefix, row.Suffix);
                    if (!subbankGroups.TryGetValue(key, out var toneSet)) {
                        toneSet = new SortedSet<int>();
                        subbankGroups[key] = toneSet;
                    }
                    toneSet.Add(row.index);
                }

                foreach (var group in subbankGroups) {
                    result.Add(new Subbank {
                        Color = color.Name,
                        Prefix = group.Key.Item1,
                        Suffix = group.Key.Item2,
                        ToneRanges = ToneSetToRanges(group.Value)
                    });
                }
            }
            return result.ToArray();
        }

        private string[] ToneSetToRanges(SortedSet<int> toneSet) {
            var ranges = new List<string>();
            int? start = null;
            int last = -1;

            foreach (var tone in toneSet) {
                if (start == null) {
                    start = tone;
                } else if (tone != last + 1) {
                    ranges.Add(FormatRange(start.Value, last));
                    start = tone;
                }
                last = tone;
            }

            if (start != null) {
                ranges.Add(FormatRange(start.Value, last));
            }

            return ranges.ToArray();
        }

        private string FormatRange(int start, int end) =>
            start == end ? MusicMath.GetToneName(start) : $"{MusicMath.GetToneName(start)}-{MusicMath.GetToneName(end)}";

        private void NotifyError(string message, Exception e) {
            var customEx = new MessageCustomizableException(message, $"<translate:errors.failed.generic>: {message}", e);
            DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(customEx));
        }
    }
}
