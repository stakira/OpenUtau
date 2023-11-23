using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using OpenUtau.Core.Ustx;

namespace OpenUtau.App.Views {
    public partial class SingerSelectionDialog : Window {
        private SingerSelectionViewModel viewModel;

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

            foreach (var type in viewModel.SortedSingers) {
                var singersListBox = new ListBox { ItemsSource = type.Value };
                singersListBox.Classes.Add("singers");
                singersListBox.SelectionChanged += SingerSelectionChanged;
                var expander = new Expander {
                    Header = type.Key.ToString() + ": " + type.Value.Count + " singers",
                    Content = singersListBox
                };
                SingersPanel.Children.Add(expander);
            }
        }

        private void SingerSelectionChanged(object? sender, SelectionChangedEventArgs e) {
            if(e.AddedItems.Count > 0 && e.AddedItems[0] is USinger singer && singer.Found) {
                OnFinish(singer);
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

        void OnFinish(USinger singer) {
            if (onFinish != null) {
                onFinish.Invoke(singer);
            }
            Close();
        }
    }
}
