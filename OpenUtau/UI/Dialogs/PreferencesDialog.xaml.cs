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
    }
}
