using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.UI.Dialogs {
    /// <summary>
    /// Interaction logic for SingerViewDialog.xaml
    /// </summary>
    public partial class SingerViewDialog : Window, ICmdSubscriber {
        string location;

        public SingerViewDialog() {
            InitializeComponent();
            UpdateSingers();
            DocManager.Inst.AddSubscriber(this);
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            DocManager.Inst.RemoveSubscriber(this);
        }

        private void UpdateSingers() {
           var singerNames = DocManager.Inst.Singers.Values
                .Select(singer => singer.Name)
                .OrderBy(name => name)
                .ToList();
            if (singerNames.Count > 0) {
                name.SelectedIndex = 0;
                SetSinger(singerNames[0]);
            }
            name.ItemsSource = singerNames;
        }

        public void SetSinger(string singerName) {
            USinger singer = null;
            foreach (var pair in DocManager.Inst.Singers)
                if (pair.Value.Name == singerName) {
                    singer = pair.Value;
                }
            if (singer == null) {
                location = null;
                return;
            }
            avatar.Source = LoadAvatar(singer.Avatar);
            info.Inlines.Clear();
            info.Inlines.Add($"Author: {singer.Author}\n");
            info.Inlines.Add("Web: ");
            if (!string.IsNullOrWhiteSpace(singer.Web)) {
                var h = new Hyperlink() {
                    NavigateUri = new Uri(singer.Web),
                };
                h.RequestNavigate += new RequestNavigateEventHandler(hyperlink_RequestNavigate);
                h.Inlines.Add(singer.Web);
                info.Inlines.Add(h);
            }
            info.Inlines.Add("\n");
            info.Inlines.Add(singer.OtherInfo);
            location = singer.Location;
            otoview.Items.Clear();
            foreach (var set in singer.OtoSets) {
                foreach (var oto in set.Otos.Values) {
                    otoview.Items.Add(oto);
                }
            }

            singer.OtoSets
                .SelectMany(set => set.Errors)
                .ToList()
                .ForEach(e => {
                    info.Inlines.Add(new LineBreak());
                    info.Inlines.Add(new Run(e) { Foreground = Brushes.Red });
                });
        }

        private void name_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            SetSinger((string)name.SelectedItem);
        }

        private void locationButton_Click(object sender, RoutedEventArgs e) {
            OpenFolder(location);
        }

        private void hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void OpenFolder(string folderPath) {
            if (Directory.Exists(folderPath)) {
                Process.Start(new ProcessStartInfo {
                    Arguments = folderPath,
                    FileName = "explorer.exe",
                });
            }
        }

        private static BitmapImage LoadAvatar(string path) {
            if (string.IsNullOrEmpty(path)) {
                return null;
            }
            try {
                var avatar = new BitmapImage();
                avatar.BeginInit();
                avatar.CacheOption = BitmapCacheOption.OnLoad;
                avatar.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                avatar.EndInit();
                avatar.Freeze();
                return avatar;
            } catch (Exception e) {
                Log.Error(e, $"Failed to load avatar at {path}");
                return null;
            }
        }

        #region ICmdSubscriber

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is SingersChangedNotification) {
                UpdateSingers();
            }
        }

        #endregion
    }
}
