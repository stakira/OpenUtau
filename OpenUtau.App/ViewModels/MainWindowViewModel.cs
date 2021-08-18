using System;
using System.Collections.Generic;
using System.Text;

namespace OpenUtau.App.ViewModels {
    public class MainWindowViewModel : ViewModelBase {
        public string Greeting => "Welcome to Avalonia!";

        public string AppVersion => $"OpenUtau v{System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version}";

        public void SeekStart() { System.Diagnostics.Debug.WriteLine("SeekStart"); }
        public void SeekEnd() { System.Diagnostics.Debug.WriteLine("SeekEnd"); }
        public void PlayOrPause() { System.Diagnostics.Debug.WriteLine("PlayOrPause"); }
        public void Stop() { System.Diagnostics.Debug.WriteLine("Stop"); }
    }
}
