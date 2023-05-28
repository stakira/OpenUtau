using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class ExeSetupViewModel : ViewModelBase {
        public string filePath;
        [Reactive] public string message { get; set; }
        public ExeSetupViewModel(string filePath) {
            this.filePath = filePath;
            message = "installing " + filePath;
            if (OS.IsMacOS()) {
                message += "To use exe resamplers or wavtools on MacOS, please install wine32on64 using following commands:\n"
                    + "brew tap gcenx/wine\n"
                    + "brew install --cask --no-quarantine wine-crossover";
            }else if(OS.IsLinux()) {
                message += "To use exe resamplers or wavtools on Linux, please install wine from https://www.winehq.org/";
            }
        }
    }
}
