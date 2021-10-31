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
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace OpenUtau.App.ViewModels {
    public class TrackHeaderViewModel : ViewModelBase, IActivatableViewModel {
        public int TrackNo => track.TrackNo + 1;
        public USinger Singer => track.Singer;
        public Phonemizer Phonemizer => track.Phonemizer;
        public string PhonemizerTag => track.Phonemizer.Tag;
        public IReadOnlyList<MenuItemViewModel>? SingerMenuItems { get; set; }
        public ReactiveCommand<USinger, Unit> SelectSingerCommand { get; }
        public IReadOnlyList<MenuItemViewModel>? PhonemizerMenuItems { get; set; }
        public ReactiveCommand<PhonemizerFactory, Unit> SelectPhonemizerCommand { get; }
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
                    DocManager.Inst.EndUndoGroup();
                }
                this.RaisePropertyChanged(nameof(Singer));
                RefreshAvatar();
            });
            SelectPhonemizerCommand = ReactiveCommand.Create<PhonemizerFactory>(factory => {
                if (track.Phonemizer.GetType() != factory.type) {
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new TrackChangePhonemizerCommand(DocManager.Inst.Project, track, factory.Create()));
                    DocManager.Inst.EndUndoGroup();
                }
                this.RaisePropertyChanged(nameof(Phonemizer));
                this.RaisePropertyChanged(nameof(PhonemizerTag));
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

        public void RefreshSingers() {
            SingerMenuItems = DocManager.Inst.Singers.Values.Select(singer => new MenuItemViewModel() {
                Header = singer.Name,
                Command = SelectSingerCommand,
                CommandParameter = singer,
            }).OrderBy(item => item.Header).ToArray();
            this.RaisePropertyChanged(nameof(SingerMenuItems));
        }

        public void RefreshPhonemizers() {
            PhonemizerMenuItems = DocManager.Inst.PhonemizerFactories.Select(factory => new MenuItemViewModel() {
                Header = factory.ToString(),
                Command = SelectPhonemizerCommand,
                CommandParameter = factory,
            }).ToArray();
            this.RaisePropertyChanged(nameof(PhonemizerMenuItems));
        }

        public void RefreshAvatar() {
            var singer = track?.Singer;
            if (singer == null || singer.AvatarData == null) {
                Avatar = null;
                return;
            }
            try {
                using (var memoryStream = new MemoryStream(singer.AvatarData)) {
                    Avatar = Bitmap.DecodeToWidth(memoryStream, 80);
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
    }
}
