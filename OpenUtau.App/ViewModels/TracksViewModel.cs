using System;
using Avalonia;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.ViewModels {
    public class TracksViewModel : ViewModelBase {
        UProject Project => DocManager.Inst.Project;

        public double OffsetX {
            get => offsetX;
            set => this.RaiseAndSetIfChanged(ref offsetX, value);
        }
        public double OffsetY {
            get => offsetY;
            set => this.RaiseAndSetIfChanged(ref offsetY, value);
        }
        public double TickWidth {
            get => tickWidth;
            set => this.RaiseAndSetIfChanged(ref tickWidth, Math.Clamp(value, ViewConstants.TickWidthMin, ViewConstants.TickWidthMax));
        }
        public double TrackHeight {
            get => trackHeight;
            set => this.RaiseAndSetIfChanged(ref trackHeight, Math.Clamp(value, ViewConstants.TrackHeightMin, ViewConstants.TrackHeightMax));
        }
        public Point Offset => new(OffsetX, OffsetY);
        public double ViewWidth {
            get => viewWidth;
            set => this.RaiseAndSetIfChanged(ref viewWidth, value);
        }
        public double ViewHeight {
            get => viewHeight;
            set => this.RaiseAndSetIfChanged(ref viewHeight, value);
        }

        private double offsetX;
        private double offsetY;
        private double tickWidth = ViewConstants.TickWidthDefault;
        private double trackHeight = ViewConstants.TrackHeightDefault;
        private double viewWidth;
        private double viewHeight;
    }
}
