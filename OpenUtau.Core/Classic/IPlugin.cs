namespace OpenUtau.Classic {
    public interface IPlugin {
        string Encoding { get; }
        void Run(string tempFile);
    }
}
