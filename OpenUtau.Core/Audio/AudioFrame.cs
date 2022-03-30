namespace OpenUtau.Audio {
    public sealed class AudioFrame {
        public AudioFrame(double presentationTime, float[] data) {
            PresentationTime = presentationTime;
            Data = data;
        }
        public double PresentationTime { get; }
        public float[] Data { get; }
    }
}
