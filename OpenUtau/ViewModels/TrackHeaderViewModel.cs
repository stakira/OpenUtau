using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Media.Imaging;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ScottPlot.MarkerShapes;
using Serilog;

namespace OpenUtau.App.ViewModels {
    public class TrackHeaderViewModel : ViewModelBase, IActivatableViewModel {
        public int TrackNo => track.TrackNo + 1;
        public USinger Singer => track.Singer;
        public Phonemizer Phonemizer => track.Phonemizer;
        public string PhonemizerTag => track.Phonemizer.Tag;
        public Core.Render.IRenderer Renderer => track.RendererSettings.Renderer;
        public IReadOnlyList<MenuItemViewModel>? SingerMenuItems { get; set; }
        public ReactiveCommand<USinger, Unit> SelectSingerCommand { get; }
        public IReadOnlyList<MenuItemViewModel>? PhonemizerMenuItems { get; set; }
        public ReactiveCommand<PhonemizerFactory, Unit> SelectPhonemizerCommand { get; }
        public IReadOnlyList<MenuItemViewModel>? RenderersMenuItems { get; set; }
        public ReactiveCommand<string, Unit> SelectRendererCommand { get; }
        [Reactive] public double Volume { get; set; }
        [Reactive] public bool Mute { get; set; }
        [Reactive] public bool Solo { get; set; }
        [Reactive] public Bitmap? Avatar { get; set; }

        public ViewModelActivator Activator { get; }

        private readonly UTrack track;

        public TrackHeaderViewModel() {
#if DEBUG
            SelectSingerCommand = ReactiveCommand.Create<USinger>(_ => { });
            SelectPhonemizerCommand = ReactiveCommand.Create<PhonemizerFactory>(_ => { });
            SelectRendererCommand = ReactiveCommand.Create<string>(_ => { });
            Activator = new ViewModelActivator();
            track = new UTrack();
#endif
        }

        public TrackHeaderViewModel(UTrack track) {
            this.track = track;
            SelectSingerCommand = ReactiveCommand.Create<USinger>(singer => {
                if (track.Singer != singer) {
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new TrackChangeSingerCommand(DocManager.Inst.Project, track, singer));
                    if (!string.IsNullOrEmpty(singer?.Id) &&
                        Preferences.Default.SingerPhonemizers.TryGetValue(Singer.Id, out var phonemizerName) &&
                        TryChangePhonemizer(phonemizerName)) {
                    } else if (!string.IsNullOrEmpty(singer?.DefaultPhonemizer)) {
                        TryChangePhonemizer(singer.DefaultPhonemizer);
                    }
                    if (singer == null || !singer.Found) {
                        var settings = new URenderSettings();
                        DocManager.Inst.ExecuteCmd(new TrackChangeRenderSettingCommand(DocManager.Inst.Project, track, settings));
                    } else if (singer.SingerType != track.RendererSettings.Renderer?.SingerType) {
                        var settings = new URenderSettings {
                            renderer = Core.Render.Renderers.GetDefaultRenderer(singer.SingerType),
                        };
                        DocManager.Inst.ExecuteCmd(new TrackChangeRenderSettingCommand(DocManager.Inst.Project, track, settings));
                    }
                    DocManager.Inst.EndUndoGroup();
                    if (!string.IsNullOrEmpty(singer?.Id) && singer.Found) {
                        Preferences.Default.RecentSingers.Remove(singer.Id);
                        Preferences.Default.RecentSingers.Insert(0, singer.Id);
                        if (Preferences.Default.RecentSingers.Count > 16) {
                            Preferences.Default.RecentSingers.RemoveRange(
                                16, Preferences.Default.RecentSingers.Count - 16);
                        }
                    }
                    Preferences.Save();
                }
                this.RaisePropertyChanged(nameof(Singer));
                this.RaisePropertyChanged(nameof(Renderer));
                RefreshAvatar();
            });
            SelectPhonemizerCommand = ReactiveCommand.Create<PhonemizerFactory>(factory => {
                if (track.Phonemizer.GetType() != factory.type) {
                    DocManager.Inst.StartUndoGroup();
                    var phonemizer = factory.Create();
                    DocManager.Inst.ExecuteCmd(new TrackChangePhonemizerCommand(DocManager.Inst.Project, track, phonemizer));
                    DocManager.Inst.EndUndoGroup();
                    var name = phonemizer!.GetType().FullName;
                    if (!string.IsNullOrEmpty(Singer?.Id) && phonemizer != null) {
                        Preferences.Default.SingerPhonemizers[Singer.Id] = name;
                    }
                    Preferences.Default.RecentPhonemizers.Remove(name);
                    Preferences.Default.RecentPhonemizers.Insert(0, name);
                    while (Preferences.Default.RecentPhonemizers.Count > 8) {
                        Preferences.Default.RecentPhonemizers.RemoveRange(
                            8, Preferences.Default.RecentPhonemizers.Count - 8);
                    }
                    Preferences.Save();
                }
                this.RaisePropertyChanged(nameof(Phonemizer));
                this.RaisePropertyChanged(nameof(PhonemizerTag));
            });
            SelectRendererCommand = ReactiveCommand.Create<string>(name => {
                var settings = new URenderSettings {
                    renderer = name,
                };
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new TrackChangeRenderSettingCommand(DocManager.Inst.Project, track, settings));
                DocManager.Inst.EndUndoGroup();
                this.RaisePropertyChanged(nameof(Renderer));
            });

            Activator = new ViewModelActivator();
            this.WhenActivated((CompositeDisposable disposables) => {
                Disposable.Create(() => {
                    MessageBus.Current.Listen<TracksRefreshEvent>()
                        .Subscribe(_ => {
                            ManuallyRaise();
                        }).DisposeWith(disposables);
                });
            });

            Volume = track.Volume;
            Mute = track.Mute;
            Solo = track.Solo;
            this.WhenAnyValue(x => x.Volume)
                .Subscribe(volume => {
                    track.Volume = volume;
                    DocManager.Inst.ExecuteCmd(new VolumeChangeNotification(track.TrackNo, Mute ? -24 : volume));
                });
            this.WhenAnyValue(x => x.Mute)
                .Subscribe(mute => {
                    track.Mute = mute;
                    DocManager.Inst.ExecuteCmd(new VolumeChangeNotification(track.TrackNo, mute ? -24 : Volume));
                });
            this.WhenAnyValue(x => x.Solo)
                .Subscribe(solo => {
                    track.Solo = solo;
                });

            RefreshAvatar();
        }

        public void ToggleSolo() {
            MessageBus.Current.SendMessage(new TracksSoloEvent(track.TrackNo, !track.Solo));
        }

        private bool TryChangePhonemizer(string phonemizerName) {
            try {
                var factory = DocManager.Inst.PhonemizerFactories.FirstOrDefault(factory => factory.type.FullName == phonemizerName);
                var phonemizer = factory?.Create();
                if (phonemizer != null) {
                    DocManager.Inst.ExecuteCmd(new TrackChangePhonemizerCommand(DocManager.Inst.Project, track, phonemizer));
                    return true;
                }
            } catch (Exception e) {
                Log.Error(e, $"Failed to load phonemizer {phonemizerName}");
            }
            return false;
        }

        public void RefreshSingers() {
            var items = new List<MenuItemViewModel>();
            items.AddRange(Preferences.Default.RecentSingers
                .Select(id => SingerManager.Inst.Singers.Values.FirstOrDefault(singer => singer.Id == id))
                .OfType<USinger>()
                .OrderBy(singer => singer.Name)
                .Select(singer => new MenuItemViewModel() {
                    Header = singer.Name,
                    Command = SelectSingerCommand,
                    CommandParameter = singer,
                }));
            var keys = SingerManager.Inst.SingerGroups.Keys.OrderBy(k => k);
            foreach (var key in keys) {
                items.Add(new MenuItemViewModel() {
                    Header = $"{key} ...",
                    Items = SingerManager.Inst.SingerGroups[key]
                        .Select(singer => new MenuItemViewModel() {
                            Header = singer.Name,
                            Command = SelectSingerCommand,
                            CommandParameter = singer,
                        }).ToArray(),
                });
            }
            SingerMenuItems = items;
            this.RaisePropertyChanged(nameof(SingerMenuItems));
        }

        public void RefreshPhonemizers() {
            var items = new List<MenuItemViewModel>();
            //Recently used phonemizers
            items.AddRange(Preferences.Default.RecentPhonemizers
                .Select(name => DocManager.Inst.PhonemizerFactories.FirstOrDefault(factory => factory.type.FullName == name))
                .OfType<PhonemizerFactory>()
                .OrderBy(factory => factory.tag)
                .Select(factory => new MenuItemViewModel() {
                    Header = factory.ToString(),
                    Command = SelectPhonemizerCommand,
                    CommandParameter = factory,
                }));
            //more phonemizers grouped by singing language
            items.Add(new MenuItemViewModel() {
                Header = $"{ThemeManager.GetString("tracks.more")} ...",
                Items = DocManager.Inst.PhonemizerFactories.GroupBy(factory => factory.language)
                .Select(group => new MenuItemViewModel() {
                    Header = (group.Key is null) ? "General" : group.Key,
                    Items = group.Select(factory => new MenuItemViewModel() {
                        Header = factory.ToString(),
                        Command = SelectPhonemizerCommand,
                        CommandParameter = factory,
                    }).ToArray(),
                }).ToArray()
            });
            PhonemizerMenuItems = items.ToArray();
            this.RaisePropertyChanged(nameof(PhonemizerMenuItems));
        }

        public void RefreshRenderers() {
            var items = new List<MenuItemViewModel>();
            if (track != null && track.Singer != null && track.Singer.Found) {
                items.AddRange(Core.Render.Renderers.GetSupportedRenderers(track.Singer.SingerType)
                    .Select(name => new MenuItemViewModel() {
                        Header = name,
                        Command = SelectRendererCommand,
                        CommandParameter = name,
                    }));
            }
            RenderersMenuItems = items.ToArray();
            this.RaisePropertyChanged(nameof(RenderersMenuItems));
        }

        public void RefreshAvatar() {
            var singer = track?.Singer;
            if (singer == null || singer.AvatarData == null) {
                Avatar = null;
                return;
            }
            try {
                using (var stream = new MemoryStream(singer.AvatarData)) {
                    Avatar = new Bitmap(stream);
                }
            } catch (Exception e) {
                Avatar = null;
                Log.Error(e, "Failed to decode avatar.");
            }
        }

        public void ManuallyRaise() {
            this.RaisePropertyChanged(nameof(Singer));
            this.RaisePropertyChanged(nameof(TrackNo));
            this.RaisePropertyChanged(nameof(Phonemizer));
            this.RaisePropertyChanged(nameof(PhonemizerTag));
            this.RaisePropertyChanged(nameof(Renderer));
            this.RaisePropertyChanged(nameof(Mute));
            this.RaisePropertyChanged(nameof(Solo));
            this.RaisePropertyChanged(nameof(Volume));
            RefreshAvatar();
        }

        public void Remove() {
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new RemoveTrackCommand(DocManager.Inst.Project, track));
            DocManager.Inst.EndUndoGroup();
        }

        public void MoveUp() {
            if (track == DocManager.Inst.Project.tracks.First()) {
                return;
            }
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new MoveTrackCommand(DocManager.Inst.Project, track, true));
            DocManager.Inst.EndUndoGroup();
        }

        public void MoveDown() {
            if (track == DocManager.Inst.Project.tracks.Last()) {
                return;
            }
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new MoveTrackCommand(DocManager.Inst.Project, track, false));
            DocManager.Inst.EndUndoGroup();
        }

        public void Duplicate() {
            DocManager.Inst.StartUndoGroup();
            //TODO
            var newTrack = new UTrack() {
                TrackNo = track.TrackNo+1,
                Singer = track.Singer,
                Phonemizer = track.Phonemizer,
                RendererSettings = track.RendererSettings,
                Mute = track.Mute,
                Solo = track.Solo,
                Volume = track.Volume,
                Pan = track.Pan,
            };
            DocManager.Inst.ExecuteCmd(new AddTrackCommand(DocManager.Inst.Project, newTrack));
            var parts = DocManager.Inst.Project.parts
                .Where(part => part.trackNo == track.TrackNo)
                .Select(part => part.Clone()).ToList();
            foreach(var part in parts) {
                part.trackNo = newTrack.TrackNo;
                DocManager.Inst.ExecuteCmd(new AddPartCommand(DocManager.Inst.Project, part));
            }
            DocManager.Inst.EndUndoGroup();
        }

        public void DuplicateSettings() {
            DocManager.Inst.StartUndoGroup();
            //TODO
            DocManager.Inst.ExecuteCmd(new AddTrackCommand(DocManager.Inst.Project, new UTrack() {
                TrackNo = track.TrackNo+1,
                Singer = track.Singer,
                Phonemizer = track.Phonemizer,
                RendererSettings = track.RendererSettings,
                Mute = track.Mute,
                Solo = track.Solo,
                Volume = track.Volume,
                Pan = track.Pan,
            }));
            DocManager.Inst.EndUndoGroup();
        }
    }
}
