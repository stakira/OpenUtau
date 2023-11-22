using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using OpenUtau.Core.Ustx;

namespace OpenUtau.App.Views {
    public partial class SingerSelectionDialog : Window {
        private SingerSelectionViewModel viewModel;
        private Dictionary<string, ListBox> listBoxes = new Dictionary<string, ListBox>();

        public Action<USinger>? onFinish;

        public SingerSelectionDialog() {
            InitializeComponent();
            viewModel = new SingerSelectionViewModel();
        }
        public SingerSelectionDialog(int trackNo) {
            InitializeComponent();
            DataContext = viewModel = new SingerSelectionViewModel(trackNo, this);
            viewModel.SortSingers();
        }

        public void RefreshSingers() {
            SingersPanel.Children.Clear();
            listBoxes.Clear();

            foreach (var type in viewModel.SortedSingers) {
                var singersListBox = new ListBox { ItemsSource = type.Value };
                singersListBox.Classes.Add("singers");
                singersListBox.SelectionChanged += SingerSelectionChanged;
                listBoxes.Add(type.Key.ToString(), singersListBox);
                var expander = new Expander {
                    Header = type.Key.ToString() + ": " + type.Value.Count + " singers",
                    Content = singersListBox
                };
                SingersPanel.Children.Add(expander);
            }
            SelectionRefresh();
        }

        private void SingerSelectionChanged(object? sender, SelectionChangedEventArgs e) {
            if(e.AddedItems.Count > 0) {
                viewModel.SelectedSinger = e.AddedItems[0] as USinger;
            }
            SelectionRefresh();
        }

        public void SelectionRefresh() {
            foreach (ListBox list in listBoxes.Values) {
                if (list.Items.Contains(viewModel.SelectedSinger)) {
                    if (list.SelectedItem != viewModel.SelectedSinger) {
                        list.SelectedItem = viewModel.SelectedSinger;
                    }
                } else {
                    list.UnselectAll();
                }
            }
        }

        private async void Reload(object? sender, RoutedEventArgs e) {
            grid.IsEnabled = false;
            SingersPanel.Children.Clear();
            SingersPanel.Children.Add(new TextBlock {
                Text = ThemeManager.GetString("progress.loadingsingers"),
                Margin = Avalonia.Thickness.Parse("30")
            });
            await viewModel.Reload();
            grid.IsEnabled = true;
        }

        private async void FolderSettings(object? sender, RoutedEventArgs e) {
            var dialog = new SingerLocationDialog();
            await dialog.ShowDialog(this);
            Reload(sender, e);
        }

        private void ToggleLoadAllFolders(object? sender, RoutedEventArgs e) {
            viewModel.ToggleLoadAllFolders();
            Reload(sender, e);
        }

        void OnFinish(object? sender, RoutedEventArgs args) {
            if (onFinish != null && viewModel.SelectedSinger != null && viewModel.SelectedSinger.Found) {
                onFinish.Invoke(viewModel.SelectedSinger);
            }
            Close();
        }
    }
}
