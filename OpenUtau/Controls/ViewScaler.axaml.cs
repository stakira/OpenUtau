using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace OpenUtau.App.Controls {
    public partial class ViewScaler : UserControl {
        public static readonly DirectProperty<ViewScaler, double> MaxProperty =
            AvaloniaProperty.RegisterDirect<ViewScaler, double>(
                nameof(Max),
                o => o.Max,
                (o, v) => o.Max = v);
        public static readonly DirectProperty<ViewScaler, double> MinProperty =
            AvaloniaProperty.RegisterDirect<ViewScaler, double>(
                nameof(Min),
                o => o.Min,
                (o, v) => o.Min = v);
        public static readonly DirectProperty<ViewScaler, double> ValueProperty =
            AvaloniaProperty.RegisterDirect<ViewScaler, double>(
                nameof(Value),
                o => o.Value,
                (o, v) => o.Value = v);

        public double Max {
            get => max;
            set => SetAndRaise(MaxProperty, ref max, value);
        }
        public double Min {
            get => min;
            set => SetAndRaise(MinProperty, ref min, value);
        }
        public double Value {
            get => value_;
            set => SetAndRaise(ValueProperty, ref value_, value);
        }

        private double max;
        private double min;
        private double value_;

        public ViewScaler() {
            InitializeComponent();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);
            if (change.Property == MaxProperty || change.Property == MinProperty || change.Property == ValueProperty) {
                UpdatePath();
            }
        }

        private void UpdatePath() {
            // 1. ระบบป้องกันคณิตศาสตร์ล้มเหลว (Bulletproof Safeguard)
            // ป้องกัน Division by zero และ Log of zero/negative
            if (Min <= 0 || Max <= 0 || Value <= 0 || Max <= Min) {
                return;
            }

            // 2. คำนวณอัตราส่วนการซูมแบบเสถียร
            double offset = 7 * Math.Log(Max / Value, 2) / Math.Log(Max / Min, 2);
            
            // ล็อกขอบเขต offset ไม่ให้เกินช่วงที่ UI รองรับ (0 ถึง 7)
            offset = Math.Clamp(offset, 0, 7);
            
            double size = offset < 4 ? 4 : 8 - offset;
            
            if (double.IsNaN(offset) || double.IsNaN(size) ||
                double.IsInfinity(offset) || double.IsInfinity(size)) return;
            
            // 3. วาดเส้นด้วยความแม่นยำ 2 ตำแหน่ง (:F2) เพื่อลดภาระการทำงานของ UI Engine
            Path.Data = Geometry.Parse(FormattableString.Invariant(
                $"M {8 - size:F2} {offset + size:F2} L 8 {offset:F2} L {8 + size:F2} {offset + size:F2} M {8 - size:F2} {16 - size - offset:F2} L 8 {16 - offset:F2} L {8 + size:F2} {16 - size - offset:F2}"));
        }
    }
}
