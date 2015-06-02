using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using OpenUtau.Core;

namespace OpenUtau.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class PreferencesDialog : Window
    {
        private Grid _selectedGrid = null;
        private Grid SelectedGrid
        {
            set
            {
                if (_selectedGrid == value) return;
                if (_selectedGrid != null) _selectedGrid.Visibility = System.Windows.Visibility.Hidden;
                _selectedGrid = value;
                if (_selectedGrid != null) _selectedGrid.Visibility = System.Windows.Visibility.Visible;
            }
            get
            {
                return _selectedGrid;
            }
        }

        List<string> singerPaths;
        public PreferencesDialog()
        {
            InitializeComponent();

            pathsItem.IsSelected = true;
            UpdateSingerPaths();
            UpdateEngines();
        }

        # region Paths

        private void UpdateSingerPaths()
        {
            singerPaths = PathManager.Inst.GetSingerSearchPaths().ToList();
            singerPathsList.ItemsSource = singerPaths;
        }

        private void singerPathAddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                PathManager.Inst.AddSingerSearchPath(dialog.SelectedPath);
                UpdateSingerPaths();
                DocManager.Inst.SearchAllSingers();
            }
        }

        private void singerPathRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            PathManager.Inst.RemoveSingerSearchPath((string)singerPathsList.SelectedItem);
            UpdateSingerPaths();
            singerPathRemoveButton.IsEnabled = false;
            DocManager.Inst.SearchAllSingers();
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (treeView.SelectedItem == pathsItem) SelectedGrid = pathsGrid;
            else if (treeView.SelectedItem == themesItem) SelectedGrid = themesGrid;
            else if (treeView.SelectedItem == playbackItem) SelectedGrid = playbackGrid;
            else if (treeView.SelectedItem == renderingItem) SelectedGrid = renderingGrid;
            else SelectedGrid = null;
        }

        private void singerPathsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            singerPathRemoveButton.IsEnabled = (string)singerPathsList.SelectedItem != PathManager.DefaultSingerPath;
        }

        # endregion

        # region Engine select

        List<string> engines;

        private void UpdateEngines()
        {
            if (Core.Util.Preferences.Default.InternalEnginePreview) this.previewRatioInternal.IsChecked = true;
            else this.previewRatioExternal.IsChecked = true;
            if (Core.Util.Preferences.Default.InternalEngineExport) this.exportRatioInternal.IsChecked = true;
            else this.exportRatioExternal.IsChecked = true;

            var enginesInfo = Core.ResamplerDriver.ResamplerDriver.SearchEngines(PathManager.Inst.GetEngineSearchPath());
            engines = enginesInfo.Select(x => x.Name).ToList();
            if (engines.Count == 0)
            {
                this.previewRatioInternal.IsChecked = true;
                this.exportRatioInternal.IsChecked = true;
                this.previewRatioExternal.IsEnabled = false;
                this.exportRatioExternal.IsEnabled = false;
                this.previewEngineCombo.IsEnabled = false;
                this.exportEngineCombo.IsEnabled = false;
            }
            else
            {
                this.previewEngineCombo.ItemsSource = engines;
                this.exportEngineCombo.ItemsSource = engines;
                previewEngineCombo.SelectedIndex = Math.Max(0, engines.IndexOf(Core.Util.Preferences.Default.ExternalPreviewEngine));
                exportEngineCombo.SelectedIndex = Math.Max(0, engines.IndexOf(Core.Util.Preferences.Default.ExternalExportEngine));
            }
        }

        private void previewEngine_Checked(object sender, RoutedEventArgs e)
        {
            Core.Util.Preferences.Default.InternalEnginePreview = sender == this.previewRatioInternal;
            Core.Util.Preferences.Save();
        }

        private void exportEngine_Checked(object sender, RoutedEventArgs e)
        {
            Core.Util.Preferences.Default.InternalEngineExport = sender == this.exportRatioInternal;
            Core.Util.Preferences.Save();
        }

        private void previewEngineCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Core.Util.Preferences.Default.ExternalPreviewEngine = engines[this.previewEngineCombo.SelectedIndex];
            Core.Util.Preferences.Save();
        }

        private void exportEngineCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Core.Util.Preferences.Default.ExternalExportEngine = engines[this.exportEngineCombo.SelectedIndex];
            Core.Util.Preferences.Save();
        }

        # endregion

    }
}
