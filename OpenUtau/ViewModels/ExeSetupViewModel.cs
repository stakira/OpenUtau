using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class ExeSetupViewModel : ViewModelBase {
        public string filePath;
        [Reactive] public string message { get; set; }
        public ExeSetupViewModel(string filePath) {
            this.filePath = filePath;
            message = string.Format(ThemeManager.GetString("exesetup.installing"), filePath);
            if (OS.IsMacOS()) {
                message += "\n\n" + ThemeManager.GetString("exesetup.mac");
            } else if(OS.IsLinux()) {
                message += "\n\n" + string.Format(ThemeManager.GetString("exesetup.linux"), "https://www.winehq.org/");
            }
        }
    }
}
