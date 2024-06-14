using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using OpenUtau.Api;
using OpenUtau.App.Views;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
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
        [Reactive] public string TrackName { get; set; } = string.Empty;
        [Reactive] public SolidColorBrush TrackAccentColor { get; set; } = ThemeManager.GetTrackColor("Blue").AccentColor;
        [Reactive] public TrackColor TrackColor { get; set; } = ThemeManager.GetTrackColor("Blue");
        [Reactive] public double Volume { get; set; }
        [Reactive] public double Pan { get; set; }
        [Reactive] public bool Mute { get; set; }
        [Reactive] public bool Muted { get; set; }
        [Reactive] public bool Solo { get; set; }
        [Reactive] public Bitmap? Avatar { get; set; }

        public ViewModelActivator Activator { get; }

        private readonly UTrack track;

        // Parameterless constructor for Avalonia preview only.
        public TrackHeaderViewModel() {
            SelectSingerCommand = ReactiveCommand.Create<USinger>(_ => { });
            SelectPhonemizerCommand = ReactiveCommand.Create<PhonemizerFactory>(_ => { });
            SelectRendererCommand = ReactiveCommand.Create<string>(_ => { });
            Activator = new ViewModelActivator();
            track = new UTrack(DocManager.Inst.Project);
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
                    DocManager.Inst.ExecuteCmd(new VoiceColorRemappingNotification(track.TrackNo, true));
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
                    MessageBus.Current.SendMessage(new PianorollRefreshEvent("Part"));
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
                    var name = phonemizer.GetType().FullName!;
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

            TrackName = track.TrackName;
            TrackAccentColor = ThemeManager.GetTrackColor(track.TrackColor).AccentColor;
            TrackColor = Preferences.Default.UseTrackColor
                ? ThemeManager.GetTrackColor(track.TrackColor)
                : ThemeManager.GetTrackColor("Blue");
            Volume = track.Volume;
            Pan = track.Pan;
            Mute = track.Mute;
            Muted = track.Muted;
            Solo = track.Solo;
            this.WhenAnyValue(x => x.Volume)
                .Subscribe(volume => {
                    track.Volume = volume;
                    DocManager.Inst.ExecuteCmd(new VolumeChangeNotification(track.TrackNo, Muted ? -24 : volume));
                });
            this.WhenAnyValue(x => x.Pan)
                .Subscribe(pan => {
                    track.Pan = pan;
                    DocManager.Inst.ExecuteCmd(new PanChangeNotification(track.TrackNo, pan));
                });
            this.WhenAnyValue(x => x.Mute)
                .Subscribe(mute => {
                    track.Mute = mute;
                });
            this.WhenAnyValue(x => x.Muted)
                .Subscribe(muted => {
                    track.Muted = muted;
                    DocManager.Inst.ExecuteCmd(new VolumeChangeNotification(track.TrackNo, muted ? -24 : Volume));
                });
            this.WhenAnyValue(x => x.Solo)
                .Subscribe(solo => {
                    track.Solo = solo;
                });

            RefreshAvatar();
        }

        public void ToggleSolo() {
            MessageBus.Current.SendMessage(new TracksSoloEvent(track.TrackNo, !track.Solo, false));
        }

        public void SoloAdditionally() {
            MessageBus.Current.SendMessage(new TracksSoloEvent(track.TrackNo, !track.Solo, true));
        }

        public void UnsoloAll() {
            MessageBus.Current.SendMessage(new TracksSoloEvent(-1, false, false));
        }

        public void ToggleMute() {
            if (!Mute) {
                Mute = true;
            } else {
                Mute = false;
            }
            this.RaisePropertyChanged(nameof(Mute));
            JudgeMuted();
        }
        public void ToggleMute(bool mute) {
            if (mute) {
                Mute = true;
            } else {
                Mute = false;
            }
            this.RaisePropertyChanged(nameof(Mute));
            JudgeMuted();
        }

        public void MuteOnly() {
            MessageBus.Current.SendMessage(new TracksMuteEvent(-1, false));
            ToggleMute();
        }

        public void MuteAllOthers() {
            MessageBus.Current.SendMessage(new TracksMuteEvent(-1, true));
            ToggleMute();
        }

        public void UnmuteAll() {
            MessageBus.Current.SendMessage(new TracksMuteEvent(-1, false));
        }

        public void JudgeMuted() {
            if (Solo) {
                Muted = false;
            } else if (Mute) {
                Muted = true;
            } else if (DocManager.Inst.Project.SoloTrackExist) {
                Muted = true;
            } else {
                Muted = false;
            }
            this.RaisePropertyChanged(nameof(Muted));
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
                .Select(singer => new SingerMenuItemViewModel() {
                    Header = singer.LocalizedName,
                    Command = SelectSingerCommand,
                    CommandParameter = singer,
                }));
            items.Add(new SingerMenuItemViewModel() {
                Header = "Favourites ...",
                Items = Preferences.Default.FavoriteSingers
                    .Select(id => SingerManager.Inst.Singers.Values.FirstOrDefault(singer => singer.Id == id))
                    .OfType<USinger>()
                    .LocalizedOrderBy(singer => singer.LocalizedName)
                    .Select(singer => new SingerMenuItemViewModel() {
                        Header = singer.LocalizedName,
                        Command = SelectSingerCommand,
                        CommandParameter = singer,
                    }).ToArray(),
            });

            var keys = SingerManager.Inst.SingerGroups.Keys.OrderBy(k => k);
            foreach (var key in keys) {
                items.Add(new SingerMenuItemViewModel() {
                    Header = $"{key} ...",
                    Items = SingerManager.Inst.SingerGroups[key]
                        .Select(singer => new SingerMenuItemViewModel() {
                            Header = singer.LocalizedName,
                            Command = SelectSingerCommand,
                            CommandParameter = singer,
                        }).ToArray(),
                });
            }

            items.Add(new MenuItemViewModel() { // Separator
                Header = "-",
                Height = 1
            });
            items.Add(new MenuItemViewModel() {
                Header = ThemeManager.GetString("tracks.installsinger"),
                Command = ReactiveCommand.Create(async () => {
                    var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                        ?.MainWindow as MainWindow;
                    if(mainWindow == null){
                        return;
                    }
                    var file = await FilePicker.OpenFileAboutSinger(
                        mainWindow, "menu.tools.singer.install", FilePicker.ArchiveFiles);
                    if (file == null) {
                        return;
                    }
                    try {
                        if (file.EndsWith(Core.Vogen.VogenSingerInstaller.FileExt)) {
                            Core.Vogen.VogenSingerInstaller.Install(file);
                            return;
                        }
                        if (file.EndsWith(DependencyInstaller.FileExt)) {
                            DependencyInstaller.Install(file);
                            return;
                        }

                        var setup = new SingerSetupDialog() {
                            DataContext = new SingerSetupViewModel() {
                                ArchiveFilePath = file,
                            },
                        };
                        _ = setup.ShowDialog(mainWindow);
                        if (setup.Position.Y < 0) {
                            setup.Position = setup.Position.WithY(0);
                        }
                    } catch (Exception e) {
                        Log.Error(e, $"Failed to install singer {file}");
                        MessageCustomizableException mce;
                        if(e is MessageCustomizableException){
                            mce = (MessageCustomizableException)e;
                        } else {
                            mce = new MessageCustomizableException($"Failed to install singer {file}", $"<translate:errors.failed.installsinger>: {file}", e);
                        }
                        _ = await MessageBox.ShowError(mainWindow, mce);
                    }
                })
            });
            items.Add(new MenuItemViewModel() {
                Header = ThemeManager.GetString("tracks.opensingers"),
                Command = ReactiveCommand.Create(() => {
                    try {
                        OS.OpenFolder(PathManager.Inst.SingersPath);
                    } catch (Exception e) {
                        DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(e));
                    }
                })
            });
            if (!string.IsNullOrWhiteSpace(PathManager.Inst.AdditionalSingersPath) && Directory.Exists(PathManager.Inst.AdditionalSingersPath)) {
                items.Add(new MenuItemViewModel() {
                    Header = ThemeManager.GetString("tracks.openaddsingers"),
                    Command = ReactiveCommand.Create(() => {
                        try {
                            OS.OpenFolder(PathManager.Inst.AdditionalSingersPath);
                        } catch (Exception e) {
                            DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(e));
                        }
                    })
                });
            }
            items.Add(new MenuItemViewModel() {
                Header = ThemeManager.GetString("singers.refresh"),
                Command = ReactiveCommand.Create(() => {
                    DocManager.Inst.ExecuteCmd(new LoadingNotification(typeof(MainWindow), true, "singer"));
                    SingerManager.Inst.SearchAllSingers();
                    DocManager.Inst.ExecuteCmd(new SingersRefreshedNotification());
                    DocManager.Inst.ExecuteCmd(new LoadingNotification(typeof(MainWindow), false, "singer"));
                })
            });

            SingerMenuItems = items;
            this.RaisePropertyChanged(nameof(SingerMenuItems));
        }

        public string GetPhonemizerGroupHeader(string key){
            if(key is null){
                return "General";
            }
            if(ThemeManager.TryGetString($"languages.{key.ToLowerInvariant()}", out var value)){
                return $"{key}: {value}";
            }
            return key;
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
                .OrderBy(group => group.Key)
                .Select(group => new MenuItemViewModel() {
                    Header = GetPhonemizerGroupHeader(group.Key),
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
            this.RaisePropertyChanged(nameof(Muted));
            this.RaisePropertyChanged(nameof(Solo));
            this.RaisePropertyChanged(nameof(Volume));
            this.RaisePropertyChanged(nameof(Pan));
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

        public void Rename() {
            var dialog = new TypeInDialog();
            dialog.Title = ThemeManager.GetString("tracks.rename");
            dialog.SetText(track.TrackName);
            dialog.onFinish = name => {
                if (!string.IsNullOrWhiteSpace(name) && name != track.TrackName) {
                    DocManager.Inst.StartUndoGroup();
                    this.TrackName = name;
                    DocManager.Inst.ExecuteCmd(new RenameTrackCommand(DocManager.Inst.Project, track, name));
                    DocManager.Inst.EndUndoGroup();
                }
            };
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null) {
                dialog.ShowDialog(desktop.MainWindow);
            }
        }

        public async void SelectTrackColor() {
            var dialog = new TrackColorDialog();
            dialog.DataContext = new TrackColorViewModel(track);
            
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null) {
                await dialog.ShowDialog(desktop.MainWindow);
                TrackAccentColor = ThemeManager.GetTrackColor(track.TrackColor).AccentColor;
                TrackColor = Preferences.Default.UseTrackColor
                ? ThemeManager.GetTrackColor(track.TrackColor)
                : ThemeManager.GetTrackColor("Blue");
            }
        }

        public void Duplicate() {
            DocManager.Inst.StartUndoGroup();
            //TODO
            var newTrack = new UTrack(track.TrackName + "_copy") {
                TrackNo = track.TrackNo + 1,
                Singer = track.Singer,
                Phonemizer = track.Phonemizer,
                RendererSettings = track.RendererSettings,
                Mute = track.Mute,
                Muted = track.Muted,
                Solo = false,
                Volume = track.Volume,
                Pan = track.Pan,
                TrackColor = track.TrackColor
            };
            DocManager.Inst.ExecuteCmd(new AddTrackCommand(DocManager.Inst.Project, newTrack));
            var parts = DocManager.Inst.Project.parts
                .Where(part => part.trackNo == track.TrackNo)
                .Select(part => part.Clone()).ToList();
            foreach (var part in parts) {
                part.trackNo = newTrack.TrackNo;
                DocManager.Inst.ExecuteCmd(new AddPartCommand(DocManager.Inst.Project, part));
            }
            DocManager.Inst.EndUndoGroup();
        }

        public void DuplicateSettings() {
            DocManager.Inst.StartUndoGroup();
            //TODO
            DocManager.Inst.ExecuteCmd(new AddTrackCommand(DocManager.Inst.Project, new UTrack(track.TrackName + "_copy") {
                TrackNo = track.TrackNo + 1,
                Singer = track.Singer,
                Phonemizer = track.Phonemizer,
                RendererSettings = track.RendererSettings,
                Mute = track.Mute,
                Muted = track.Muted,
                Solo = false,
                Volume = track.Volume,
                Pan = track.Pan,
                TrackColor = track.TrackColor
            }));
            DocManager.Inst.EndUndoGroup();
        }

        public void VoiceColorRemapping() {
            if (track.Singer != null && track.Singer.Found && track.VoiceColorExp != null) {
                DocManager.Inst.ExecuteCmd(new VoiceColorRemappingNotification(track.TrackNo, false));
            }
        }
    }
}
