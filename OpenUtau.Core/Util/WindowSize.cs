namespace OpenUtau.Core.Util {
    public class WindowSize {
        public double Width { get; set; } = 1200;
        public double Height { get; set; } = 650;
        public int? PositionX { get; set; }
        public int? PositionY { get; set; }
        public int State { get; set; }

        public bool TryGetPosition(out int x, out int y) {
            x = PositionX ?? 0;
            y = PositionY ?? 0;
            return PositionX != null && PositionY != null;
        }

        public void Set(double width, double height, int posX, int posY, int state) {
            Width = width;
            Height = height;
            PositionX = posX;
            PositionY = posY;
            State = state == 1 ? 0 : state; // Ignore minimized state
        }
    }
}
