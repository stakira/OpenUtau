using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.Controls {
    class PartControl : TemplatedControl, IDisposable {
        public static readonly DirectProperty<PartControl, double> TickWidthProperty =
            AvaloniaProperty.RegisterDirect<PartControl, double>(
                nameof(TickWidth),
                o => o.TickWidth,
                (o, v) => o.TickWidth = v);
        public static readonly DirectProperty<PartControl, double> TrackHeightProperty =
            AvaloniaProperty.RegisterDirect<PartControl, double>(
                nameof(TrackHeight),
                o => o.TrackHeight,
                (o, v) => o.TrackHeight = v);
        public static readonly DirectProperty<PartControl, Point> OffsetProperty =
            AvaloniaProperty.RegisterDirect<PartControl, Point>(
                nameof(Offset),
                o => o.Offset,
                (o, v) => o.Offset = v);
        public static readonly DirectProperty<PartControl, string> TextProperty =
            AvaloniaProperty.RegisterDirect<PartControl, string>(
                nameof(Text),
                o => o.Text,
                (o, v) => o.Text = v);
        public static readonly DirectProperty<PartControl, bool> SelectedProperty =
            AvaloniaProperty.RegisterDirect<PartControl, bool>(
                nameof(Selected),
                o => o.Selected,
                (o, v) => o.Selected = v);

        // Tick width in pixel.
        public double TickWidth {
            get => tickWidth;
            set => SetAndRaise(TickWidthProperty, ref tickWidth, value);
        }
        public double TrackHeight {
            get => trackHeight;
            set => SetAndRaise(TrackHeightProperty, ref trackHeight, value);
        }
        public Point Offset {
            get { return offset; }
            set { SetAndRaise(OffsetProperty, ref offset, value); }
        }
        public string Text {
            get { return text; }
            set { SetAndRaise(TextProperty, ref text, value); }
        }
        public bool Selected {
            get { return selected; }
            set { SetAndRaise(SelectedProperty, ref selected, value); }
        }

        private double tickWidth;
        private double trackHeight;
        private Point offset;
        private string text = string.Empty;
        private bool selected;

        public readonly UPart part;
        private readonly Pen notePen = new Pen(Brushes.White, 3);
        private List<IDisposable> unbinds = new List<IDisposable>();

        public PartControl(UPart part, PartsCanvas canvas) {
            this.part = part;
            Foreground = Brushes.White;
            Text = part.DisplayName;

            unbinds.Add(this.Bind(TickWidthProperty, canvas.GetObservable(PartsCanvas.TickWidthProperty)));
            unbinds.Add(this.Bind(TrackHeightProperty, canvas.GetObservable(PartsCanvas.TrackHeightProperty)));
            unbinds.Add(this.Bind(WidthProperty, canvas.GetObservable(PartsCanvas.TickWidthProperty).Select(tickWidth => tickWidth * part.Duration)));
            unbinds.Add(this.Bind(HeightProperty, canvas.GetObservable(PartsCanvas.TrackHeightProperty)));
            unbinds.Add(this.Bind(OffsetProperty, canvas.WhenAnyValue(x => x.TickOffset, x => x.TrackOffset,
                (tick, track) => new Point(-tick * TickWidth, -track * TrackHeight))));

            SetPosition();
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change) {
            base.OnPropertyChanged(change);
            if (!change.IsEffectiveValueChange) {
                return;
            }
            if (change.Property == OffsetProperty ||
                change.Property == TrackHeightProperty ||
                change.Property == TickWidthProperty) {
                SetPosition();
            }
            if (change.Property == SelectedProperty ||
                change.Property == TextProperty) {
                InvalidateVisual();
            }
        }

        public void SetPosition() {
            Canvas.SetLeft(this, Offset.X + part.position * tickWidth);
            Canvas.SetTop(this, Offset.Y + part.trackNo * trackHeight);
        }

        public void SetSize() {
            Width = TickWidth * part.Duration;
            Height = trackHeight;
        }

        public void Refersh() {
            Text = part.name;
        }

        public override void Render(DrawingContext context) {
            // Background
            context.DrawRectangle(
                Selected ? ThemeManager.AccentBrush2 : ThemeManager.AccentBrush1,
                null, new Rect(1, 0, Width - 1, Height - 1), 4, 4);

            // Text
            var textLayout = TextLayoutCache.Get(Text, Foreground!, 12);
            using (var state = context.PushPreTransform(Matrix.CreateTranslation(3, 2))) {
                textLayout.Draw(context);
            }

            // Notes
            if (part != null && part is UVoicePart voicePart && voicePart.notes.Count > 0) {
                int maxTone = voicePart.notes.Max(note => note.tone);
                int minTone = voicePart.notes.Min(note => note.tone);
                if (maxTone - minTone < 36) {
                    int additional = (36 - (maxTone - minTone)) / 2;
                    minTone -= additional;
                    maxTone += additional;
                }
                using var pushedState = context.PushPreTransform(Matrix.CreateScale(1, trackHeight / (maxTone - minTone)));
                foreach (var note in voicePart.notes) {
                    var start = new Point((int)(note.position * tickWidth), maxTone - note.tone);
                    var end = new Point((int)(note.End * tickWidth), maxTone - note.tone);
                    context.DrawLine(notePen, start, end);
                }
            }
        }

        public void Dispose() {
            unbinds.ForEach(u => u.Dispose());
            unbinds.Clear();
        }
    }
}
