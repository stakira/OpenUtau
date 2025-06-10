using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class ExeSetupViewModel : ViewModelBase {
        public string filePath;
        [Reactive] public string message { get; set; }
        public ExeSetupViewModel(string filePath) {
            this.filePath = filePath;
            message = $"Installing {filePath}...\n\n";
            if (OS.IsMacOS()) {
                message += "To use exe resamplers or wavtools on MacOS:\n"
                    + "1. Install wine using following commands:\n"
                    + "      % brew tap gcenx/wine\n"
                    + "      % brew install --cask --no-quarantine wine-crossover\n"
                    + "2. Set wine path in Preferences > Advanced > Wine Path";
            } else if(OS.IsLinux()) {
                message += "To use exe resamplers or wavtools on Linux:\n"
                    + "1. Install wine from https://www.winehq.org/\n" 
                    + "2. Set wine path in Preferences > Advanced > Wine Path";
            }
        }
    }
}
