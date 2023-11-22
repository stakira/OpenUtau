using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using OpenUtau.App.Views;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SharpCompress;

namespace OpenUtau.App.ViewModels {
    public class SingerSelectionViewModel : ViewModelBase {
        [Reactive] public ObservableCollection<string> Categories { get; set; } = new ObservableCollection<string>();
        [Reactive] public int SelectedCategory { get; set; } = 0;
        [Reactive] public USinger? SelectedSinger { get; set; }
        [Reactive] public string SelectedSingerPath { get; set; } = string.Empty;
        [Reactive] public string Search { get; set; } = string.Empty;
        public bool LoadDeepFolders { get => Preferences.Default.LoadDeepFolderSinger; }
        private int trackNo;
        private SingerSelectionDialog? dialog;

        public Dictionary<USingerType, List<USinger>> SortedSingers { get; private set; } = new Dictionary<USingerType, List<USinger>>();

        public SingerSelectionViewModel() { }
        public SingerSelectionViewModel(int trackNo, SingerSelectionDialog dialog) {
            this.WhenAnyValue(x => x.SelectedSinger)
                .Subscribe(value => {
                    if (value != null) {
                        SelectedSingerPath = value.Name + " [" + value.Location + "]";
                    } else {
                        SelectedSingerPath = "";
                    }
                });
            this.WhenAnyValue(x => x.Search)
                .Subscribe(value => SortSingers());
            this.WhenAnyValue(x => x.SelectedCategory)
                .Subscribe(value => SortSingers());

            this.trackNo = trackNo;
            this.dialog = dialog;

            SetCategories();
            SelectedSinger = DocManager.Inst.Project.tracks[trackNo].Singer;
        }

        private void SetCategories() {
            Categories.Clear();
            Categories.Add("Recents");
            Categories.Add("All");
            Categories.Add("Favs");
            foreach (string path in PathManager.Inst.SingersPaths) {
                Categories.Add(path);
            }
            SelectedCategory = 0;
        }

        public void SortSingers() {
            if (dialog == null || dialog.DataContext == null || SelectedCategory == -1) return;

            SortedSingers.Clear();
            switch (SelectedCategory) {
                case 0: //recents
                    SingerManager.Inst.SingerGroups.ForEach(type => SortedSingers.Add(type.Key,
                        Preferences.Default.RecentSingers
                        .Select(id => type.Value.FirstOrDefault(singer => singer.Id == id))
                        .OfType<USinger>()
                        .ToList()));
                    break;
                case 1: //all
                    SingerManager.Inst.SingerGroups.ForEach(type => SortedSingers.Add(type.Key, new List<USinger>(type.Value)));
                    break;
                case 2: //favs
                    SingerManager.Inst.SingerGroups.ForEach(type => SortedSingers.Add(type.Key,
                        type.Value.Where(singer => Preferences.Default.FavoriteSingers.Contains(singer.Id)).ToList()));
                    break;
                default: //directries
                    string dir = Categories[SelectedCategory];
                    SingerManager.Inst.SingerGroups.ForEach(type => SortedSingers.Add(type.Key,
                        type.Value.Where(singer => singer.BasePath == dir).ToList()));
                    break;
            }
            if (!string.IsNullOrWhiteSpace(Search)) {
                SortedSingers.ForEach(type => type.Value.RemoveAll(singer => !singer.Name.Contains(Search)));
            }
            dialog.RefreshSingers();
        }

        public async Task Reload() {
            await Task.Run(() => SingerManager.Inst.SearchAllSingers());
            SetCategories();
            SelectedSinger = DocManager.Inst.Project.tracks[trackNo].Singer;
        }

        public void ToggleLoadAllFolders() {
            Preferences.Default.LoadDeepFolderSinger = !Preferences.Default.LoadDeepFolderSinger;
            Preferences.Save();
            this.RaisePropertyChanged(nameof(LoadDeepFolders));
        }
    }
}
