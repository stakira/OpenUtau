using System;
using System.Collections.ObjectModel;
using System.Linq;
using DynamicData;

namespace OpenUtau.App.ViewModels {
    public class VoiceColorMappingViewModel : ViewModelBase {

        public string TrackName { get; set; }
        public ObservableCollection<ColorMapping> ColorMappings { get; set; } = new ObservableCollection<ColorMapping>();

        public VoiceColorMappingViewModel(string[] oldColors, string[] newColors, string trackName) {
            var NewColors = new ObservableCollection<string>(newColors);
            NewColors[0] = "(Default)";
            TrackName = trackName;

            for (int i = 0; i < oldColors.Length; i++) {
                if (i == 0) {
                    ColorMappings.Add(new ColorMapping("(Default)", 0, 0, NewColors));
                } else if (newColors.Contains(oldColors[i])) {
                    ColorMappings.Add(new ColorMapping(oldColors[i], i, newColors.IndexOf(oldColors[i]), NewColors));
                } else if (i < newColors.Length) {
                    ColorMappings.Add(new ColorMapping(oldColors[i], i, i, NewColors));
                } else {
                    ColorMappings.Add(new ColorMapping(oldColors[i], i, 0, NewColors));
                }
            }
        }
    }

    public class ColorMapping {
        public string Name { get; set; }
        public int OldIndex { get; set; }
        public int SelectedIndex { get; set; }
        public ObservableCollection<string> NewColors { get; set; }

        public ColorMapping(string name,int oldIndex, int selectedIndex, ObservableCollection<string> newColors) {
            Name = name;
            OldIndex = oldIndex;
            SelectedIndex = selectedIndex;
            NewColors = newColors;
        }
    }
}
