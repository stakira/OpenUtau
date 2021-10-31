using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.Controls {
    public enum ExpDisMode { Hidden, Visible, Shadow };

    class ExpressionCanvas : Canvas {
        public static readonly DirectProperty<ExpressionCanvas, double> TickWidthProperty =
            AvaloniaProperty.RegisterDirect<ExpressionCanvas, double>(
                nameof(TickWidth),
                o => o.TickWidth,
                (o, v) => o.TickWidth = v);
        public static readonly DirectProperty<ExpressionCanvas, double> TickOffsetProperty =
            AvaloniaProperty.RegisterDirect<ExpressionCanvas, double>(
                nameof(TickOffset),
                o => o.TickOffset,
                (o, v) => o.TickOffset = v);
        public static readonly DirectProperty<ExpressionCanvas, UVoicePart?> PartProperty =
            AvaloniaProperty.RegisterDirect<ExpressionCanvas, UVoicePart?>(
                nameof(Part),
                o => o.Part,
                (o, v) => o.Part = v);
        public static readonly DirectProperty<ExpressionCanvas, string> KeyProperty =
            AvaloniaProperty.RegisterDirect<ExpressionCanvas, string>(
                nameof(Key),
                o => o.Key,
                (o, v) => o.Key = v);

        public double TickWidth {
            get => tickWidth;
            private set => SetAndRaise(TickWidthProperty, ref tickWidth, value);
        }
        public double TickOffset {
            get => tickOffset;
            private set => SetAndRaise(TickOffsetProperty, ref tickOffset, value);
        }
        public UVoicePart? Part {
            get => part;
            set => SetAndRaise(PartProperty, ref part, value);
        }
        public string Key {
            get => key;
            set => SetAndRaise(KeyProperty, ref key, value);
        }

        private double tickWidth;
        private double tickOffset;
        private UVoicePart? part;
        private string key = string.Empty;

        private HashSet<UNote> selectedNotes = new HashSet<UNote>();
        private Geometry pointGeometry;

        public ExpressionCanvas() {
            ClipToBounds = true;
            pointGeometry = new EllipseGeometry(new Rect(-2.5, -2.5, 5, 5));
            MessageBus.Current.Listen<NotesRefreshEvent>()
                .Subscribe(_ => InvalidateVisual());
            MessageBus.Current.Listen<NotesSelectionEvent>()
                .Subscribe(e => {
                    selectedNotes.Clear();
                    selectedNotes.UnionWith(e.selectedNotes);
                    selectedNotes.UnionWith(e.tempSelectedNotes);
                    InvalidateVisual();
                });
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change) {
            base.OnPropertyChanged(change);
            if (!change.IsEffectiveValueChange) {
                return;
            }
            InvalidateVisual();
        }

        public override void Render(DrawingContext context) {
            base.Render(context);
            if (Part == null) {
                return;
            }
            var viewModel = ((PianoRollViewModel?)DataContext)?.NotesViewModel;
            if (viewModel == null) {
                return;
            }
            var project = DocManager.Inst.Project;
            if (!project.expressions.TryGetValue(key, out var descriptor)) {
                return;
            }
            if (descriptor.max <= descriptor.min) {
                return;
            }
            double leftTick = TickOffset - 480;
            double rightTick = TickOffset + Bounds.Width / TickWidth + 480;
            foreach (UNote note in Part.notes) {
                if (note.LeftBound >= rightTick || note.RightBound <= leftTick) {
                    continue;
                }
                var hPen = selectedNotes.Contains(note) ? ThemeManager.AccentPen2Thickness2 : ThemeManager.AccentPen1Thickness2;
                var vPen = selectedNotes.Contains(note) ? ThemeManager.AccentPen2Thickness3 : ThemeManager.AccentPen1Thickness3;
                var brush = selectedNotes.Contains(note) ? ThemeManager.AccentBrush2 : ThemeManager.AccentBrush1;
                foreach (var phoneme in note.phonemes) {
                    if (phoneme.Error) {
                        continue;
                    }
                    var (value, overriden) = phoneme.GetExpression(project, Key);
                    double x1 = Math.Round(viewModel.TickToneToPoint(note.position + phoneme.position, 0).X);
                    double x2 = Math.Round(viewModel.TickToneToPoint(note.position + phoneme.End, 0).X);
                    double valueHeight = Math.Round(Bounds.Height - Bounds.Height * (value - descriptor.min) / (descriptor.max - descriptor.min));
                    double zeroHeight = Math.Round(Bounds.Height - Bounds.Height * (0f - descriptor.min) / (descriptor.max - descriptor.min));
                    context.DrawLine(vPen, new Point(x1 + 0.5, zeroHeight + 0.5), new Point(x1 + 0.5, valueHeight + 3));
                    context.DrawLine(hPen, new Point(x1 + 3, valueHeight), new Point(Math.Max(x1 + 3, x2 - 3), valueHeight));
                    using (var state = context.PushPreTransform(Matrix.CreateTranslation(x1 + 0.5, valueHeight))) {
                        context.DrawGeometry(overriden ? brush : ThemeManager.BackgroundBrush, hPen, pointGeometry);
                    }
                }
            }
        }
    }
}
