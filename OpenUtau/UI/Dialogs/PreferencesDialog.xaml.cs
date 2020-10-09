using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using OpenUtau.Core;

namespace OpenUtau.UI.Dialogs {

    /// <summary>
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class PreferencesDialog : Window {
        private Grid _selectedGrid = null;
        private List<string> singerPaths;

        private List<string> engines;

        public PreferencesDialog() {
            InitializeComponent();

            pathsItem.IsSelected = true;
            UpdateSingerPaths();
            UpdateEngines();
        }

        private Grid SelectedGrid {
            set {
                if (_selectedGrid == value) {
                    return;
                }

                if (_selectedGrid != null) {
                    _selectedGrid.Visibility = Visibility.Collapsed;
                }

                _selectedGrid = value;
                if (_selectedGrid != null) {
                    _selectedGrid.Visibility = Visibility.Visible;
                }
            }
            get => _selectedGrid;
        }

        # region Paths

        private void UpdateSingerPaths() {
            singerPaths = Core.Util.Preferences.GetSingerSearchPaths();
            singerPathsList.ItemsSource = singerPaths;
        }

        private void singerPathAddButton_Click(object sender, RoutedEventArgs e) {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                PathManager.Inst.AddSingerSearchPath(dialog.SelectedPath);
                UpdateSingerPaths();
                DocManager.Inst.SearchAllSingers();
            }
        }

        private void singerPathRemoveButton_Click(object sender, RoutedEventArgs e) {
            PathManager.Inst.RemoveSingerSearchPath((string)singerPathsList.SelectedItem);
            UpdateSingerPaths();
            singerPathRemoveButton.IsEnabled = false;
            DocManager.Inst.SearchAllSingers();
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            if (treeView.SelectedItem == pathsItem) {
                SelectedGrid = pathsGrid;
            } else if (treeView.SelectedItem == themesItem) {
                SelectedGrid = themesGrid;
            } else if (treeView.SelectedItem == playbackItem) {
                SelectedGrid = playbackGrid;
            } else if (treeView.SelectedItem == renderingItem) {
                SelectedGrid = renderingGrid;
            } else {
                SelectedGrid = null;
            }
        }

        private void singerPathsList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            singerPathRemoveButton.IsEnabled = (string)singerPathsList.SelectedItem != PathManager.DefaultSingerPath;
        }

        # endregion

        # region Engine selection

        private void UpdateEngines() {
            var enginesInfo = Core.ResamplerDriver.ResamplerDriver.Search(PathManager.Inst.GetEngineSearchPath());
            engines = enginesInfo.Select(x => x.Name).ToList();
            if (engines.Count == 0) {
                previewEngineCombo.IsEnabled = false;
                exportEngineCombo.IsEnabled = false;
            } else {
                previewEngineCombo.ItemsSource = engines;
                exportEngineCombo.ItemsSource = engines;
                previewEngineCombo.SelectedIndex = Math.Max(0, engines.IndexOf(Core.Util.Preferences.Default.ExternalPreviewEngine));
                exportEngineCombo.SelectedIndex = Math.Max(0, engines.IndexOf(Core.Util.Preferences.Default.ExternalExportEngine));
            }
        }

        private void previewEngineCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Core.Util.Preferences.Default.ExternalPreviewEngine = engines[previewEngineCombo.SelectedIndex];
            Core.Util.Preferences.Save();
        }

        private void exportEngineCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Core.Util.Preferences.Default.ExternalExportEngine = engines[exportEngineCombo.SelectedIndex];
            Core.Util.Preferences.Save();
        }

        # endregion
    }
}
