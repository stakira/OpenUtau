using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using OpenUtau.App.ViewModels;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.Controls {
    class TrackHeaderCanvas : Canvas {
        public static readonly DirectProperty<TrackHeaderCanvas, double> TrackHeightProperty =
            AvaloniaProperty.RegisterDirect<TrackHeaderCanvas, double>(
                nameof(TrackHeight),
                o => o.TrackHeight,
                (o, v) => o.TrackHeight = v);
        public static readonly DirectProperty<TrackHeaderCanvas, double> TrackOffsetProperty =
            AvaloniaProperty.RegisterDirect<TrackHeaderCanvas, double>(
                nameof(TrackOffset),
                o => o.TrackOffset,
                (o, v) => o.TrackOffset = v);
        public static readonly DirectProperty<TrackHeaderCanvas, ObservableCollection<UTrack>> ItemsProperty =
            AvaloniaProperty.RegisterDirect<TrackHeaderCanvas, ObservableCollection<UTrack>>(
                nameof(Items),
                o => o.Items,
                (o, v) => o.Items = v);

        public double TrackHeight {
            get => trackHeight;
            private set => SetAndRaise(TrackHeightProperty, ref trackHeight, value);
        }
        public double TrackOffset {
            get => trackOffset;
            private set => SetAndRaise(TrackOffsetProperty, ref trackOffset, value);
        }
        public ObservableCollection<UTrack> Items {
            get => _items;
            set => SetAndRaise(ItemsProperty, ref _items, value);
        }

        private double trackHeight;
        private double trackOffset;
        private ObservableCollection<UTrack> _items;

        private Dictionary<UTrack, TrackHeader> trackHeaders = new Dictionary<UTrack, TrackHeader>();
        private TrackAdder? trackAdder;

        public TrackHeaderCanvas() {
            MessageBus.Current.Listen<TracksRefreshEvent>()
                .Subscribe(_ => {
                    foreach (var (track, header) in trackHeaders) {
                        header.ViewModel?.ManuallyRaise();
                    }
                    if (trackAdder != null) {
                        trackAdder.TrackNo = trackHeaders.Count;
                    }
                });
            MessageBus.Current.Listen<TracksSoloEvent>()
                .Subscribe(e => {
                    foreach (var (track, header) in trackHeaders) {
                        if (header.ViewModel != null) {
                            if (track.TrackNo == e.trackNo) {
                                header.ViewModel.Solo = e.solo;
                                if (e.solo) {
                                    header.ViewModel.Mute = false;
                                }
                            } else {
                                header.ViewModel.Solo = false;
                                header.ViewModel.Mute = e.solo;
                            }
                            header.ViewModel.ManuallyRaise();
                        }
                    }
                });
        }

        protected override void OnInitialized() {
            base.OnInitialized();
            trackAdder = new TrackAdder();
            trackAdder.Bind(this);
            Children.Add(trackAdder);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change) {
            base.OnPropertyChanged(change);
            if (!change.IsEffectiveValueChange) {
                return;
            }
            if (change.Property == ItemsProperty) {
                if (change.OldValue != null && change.OldValue.Value is ObservableCollection<UTrack> oldCol) {
                    oldCol.CollectionChanged -= Items_CollectionChanged;
                }
                if (change.NewValue.HasValue && change.NewValue.Value is ObservableCollection<UTrack> newCol) {
                    newCol.CollectionChanged += Items_CollectionChanged;
                }
            } else if (change.Property == DataContextProperty) {
                if (trackAdder != null) {
                    trackAdder.DataContext = DataContext;
                }
            }
        }

        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null) {
                        foreach (var item in e.OldItems) {
                            if (item is UTrack track) {
                                Remove(track);
                            }
                        }
                    }
                    if (e.NewItems != null) {
                        foreach (var item in e.NewItems) {
                            if (item is UTrack track) {
                                Add(track);
                            }
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var (track, _) in trackHeaders) {
                        Remove(track);
                    }
                    break;
            }
        }

        void Add(UTrack track) {
            var vm = new TrackHeaderViewModel(track);
            var header = new TrackHeader() {
                DataContext = vm,
                ViewModel = vm,
            };
            header.Bind(track, this);
            Children.Add(header);
            trackHeaders.Add(track, header);
            if (trackAdder != null) {
                trackAdder.TrackNo = trackHeaders.Count;
            }
        }

        void Remove(UTrack track) {
            var header = trackHeaders[track];
            header.Dispose();
            trackHeaders.Remove(track);
            Children.Remove(header);
            if (trackAdder != null) {
                trackAdder.TrackNo = trackHeaders.Count;
            }
        }
    }
}
