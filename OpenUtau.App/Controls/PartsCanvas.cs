using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.Controls {
    class PartsCanvas : Canvas, ICmdSubscriber {
        public static readonly DirectProperty<PartsCanvas, double> TickWidthProperty =
            AvaloniaProperty.RegisterDirect<PartsCanvas, double>(
                nameof(TickWidth),
                o => o.TickWidth,
                (o, v) => o.TickWidth = v);
        public static readonly DirectProperty<PartsCanvas, double> TrackHeightProperty =
            AvaloniaProperty.RegisterDirect<PartsCanvas, double>(
                nameof(TrackHeight),
                o => o.TrackHeight,
                (o, v) => o.TrackHeight = v);
        public static readonly DirectProperty<PartsCanvas, double> TickProperty =
            AvaloniaProperty.RegisterDirect<PartsCanvas, double>(
                nameof(Tick),
                o => o.Tick,
                (o, v) => o.Tick = v);
        public static readonly DirectProperty<PartsCanvas, double> TrackProperty =
            AvaloniaProperty.RegisterDirect<PartsCanvas, double>(
                nameof(Track),
                o => o.Track,
                (o, v) => o.Track = v);

        public double TickWidth {
            get => _tickWidth;
            private set => SetAndRaise(TickWidthProperty, ref _tickWidth, value);
        }
        public double TrackHeight {
            get => _trackHeight;
            private set => SetAndRaise(TrackHeightProperty, ref _trackHeight, value);
        }
        public double Tick {
            get => _tick;
            private set => SetAndRaise(TickProperty, ref _tick, value);
        }
        public double Track {
            get => _track;
            private set => SetAndRaise(TrackProperty, ref _track, value);
        }

        private double _tickWidth;
        private double _trackHeight;
        private double _tick;
        private double _track;

        Dictionary<UPart, PartControl> partControls = new();

        public PartsCanvas() {
            this.WhenAnyValue(x => x.TickWidth, x => x.TrackHeight, x => x.Tick, x => x.Track)
                .Subscribe(_ => Refresh());
            DocManager.Inst.AddSubscriber(this);
        }

        void Add(UPart part) {
            var control = new PartControl {
                DataContext = part,
                Foreground = Brushes.White,
                Background = Brushes.Gray,
                Text = part.name,
            };
            control.Width = 100;
            Children.Add(control);
            partControls.Add(part, control);
        }

        void Remove(UPart part) {
            var control = partControls[part];
            partControls.Remove(part);
            Children.Remove(control);
        }

        public void Refresh() {
            var offset = new Point(-Tick * TickWidth, -Track * TrackHeight);
            foreach (var pair in partControls) {
                var part = pair.Key;
                var control = pair.Value;
                var position = offset + new Point(part.position * TickWidth, part.trackNo * TrackHeight);
                control.Position = position;
                control.Text = part.name;
                control.Width = TickWidth * part.Duration;
                control.Height = TrackHeight;
            }
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is PartCommand partCmd) {
                if (partCmd is AddPartCommand && !isUndo || partCmd is RemovePartCommand && isUndo) {
                    Add(partCmd.part);
                } else if (partCmd is AddPartCommand && isUndo || partCmd is RemovePartCommand && !isUndo) {
                    Remove(partCmd.part);
                } else if (partCmd is ReplacePartCommand replacePartCmd) {
                    if (!isUndo) {
                        Remove(replacePartCmd.part);
                        Add(replacePartCmd.newPart);
                    } else {
                        Remove(replacePartCmd.newPart);
                        Add(replacePartCmd.part);
                    }
                }
            } else if (cmd is UNotification) {
                if (cmd is LoadProjectNotification loadProjectNotif) {
                    foreach (var part in partControls.Keys) {
                        Remove(part);
                    }
                    foreach (var part in loadProjectNotif.project.parts) {
                        Add(part);
                    }
                }
            }
        }
    }
}
