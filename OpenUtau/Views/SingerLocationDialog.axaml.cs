using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.Views {
    public partial class SingerLocationDialog : Window {
        [Reactive] public ObservableCollection<string> locations { get; set; }

        public SingerLocationDialog() {
            InitializeComponent();

            var list = new List<string>();
            list.AddRange(Preferences.Default.AdditionalSingerPaths);
            list.RemoveAll(path => !Directory.Exists(path));
            locations = new ObservableCollection<string>(list);
            DataContext = locations;

            OriginalPath.Text = PathManager.Inst.SingersPath;
            if (Preferences.Default.InstallToAdditionalSingersPath) {
                UseAdditionalPath.IsChecked = true;
            } else {
                UseOriginalPath.IsChecked = true;
            }
            SetAdditionalPath();
        }

        private void SetAdditionalPath() {
            if (locations.Count > 0) {
                AdditionalPath.Text = locations.First();
                UseAdditionalPath.IsEnabled = true;
            } else {
                UseOriginalPath.IsChecked = true;
                AdditionalPath.Text = string.Empty;
                UseAdditionalPath.IsEnabled = false;
            }
        }

        private async void AddLocation(object sender, RoutedEventArgs args) {
            var path = await FilePicker.OpenFolder(this, "prefs.paths.addlsinger");
            if (string.IsNullOrEmpty(path)) {
                return;
            }
            if (Directory.Exists(path)) {
                locations.Add(path);
                locations.Distinct();
                SetAdditionalPath();
            }
        }

        private void RemoveLocation(object sender, RoutedEventArgs args) {
            if (PathListBox.SelectedItems != null && PathListBox.SelectedItems.Count > 0) {
                var list = PathListBox.SelectedItems.Cast<string>().ToList();
                foreach (string path in list) {
                    locations.Remove(path);
                }
                SetAdditionalPath();
            }
        }

        private void Apply(object sender, RoutedEventArgs args) {
            if (locations.Count > 0) {
                Preferences.Default.InstallToAdditionalSingersPath = (UseAdditionalPath.IsChecked == true);
            } else {
                Preferences.Default.InstallToAdditionalSingersPath = false;
            }
            Preferences.SetSingerSearchPaths(locations.ToList()); // set and save
            Close();
        }
        private void Close(object sender, RoutedEventArgs args) {
            Close();
        }
    }
}
