using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace OpenUtau.App.Controls {
    class PartControl : TemplatedControl {
        public static readonly DirectProperty<PartControl, Point> PositionProperty =
            AvaloniaProperty.RegisterDirect<PartControl, Point>(
                nameof(Position),
                o => o.Position,
                (o, v) => o.Position = v);
        public static readonly DirectProperty<PartControl, string> TextProperty =
            AvaloniaProperty.RegisterDirect<PartControl, string>(
                nameof(Text),
                o => o.Text,
                (o, v) => o.Text = v);

        public Point Position {
            get { return _position; }
            set { SetAndRaise(PositionProperty, ref _position, value); }
        }
        public string Text {
            get { return _text; }
            set { SetAndRaise(TextProperty, ref _text, value); }
        }

        private Point _position;
        private string _text = string.Empty;

        private FormattedText? formattedText;

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change) {
            base.OnPropertyChanged(change);
            if (!change.IsEffectiveValueChange) {
                return;
            }
            if (change.Property == PositionProperty) {
                Canvas.SetLeft(this, Position.X);
                Canvas.SetTop(this, Position.Y);
            }
        }

        public override void Render(DrawingContext context) {
            // Background
            context.DrawRectangle(Background, null, new Rect(1, 1, Width - 2, Height - 2), 4, 4);

            // Text
            if (formattedText == null || formattedText.Text != Text) {
                formattedText = new FormattedText(
                    Text,
                    new Typeface(TextBlock.GetFontFamily(this), FontStyle.Normal, FontWeight.Bold),
                    12,
                    TextAlignment.Left,
                    TextWrapping.NoWrap,
                    new Size(Width, Height));
            }
            context.DrawText(Foreground, new Point(3, 2), formattedText);

            // Notes
        }
    }
}
