using System.Threading.Tasks;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using OpenUtau.Core.Ustx;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class SingerPublishViewModel : ViewModelBase {
        USinger singer;
        [Reactive] public bool UseIgnore { get; set; }
        [Reactive] public string IgnoreTypes { get; set; }

        public SingerPublishViewModel(USinger singer) {
            this.singer = singer;
            UseIgnore = Preferences.Default.VoicebankPublishUseIgnore;
            IgnoreTypes = Preferences.Default.VoicebankPublishIgnores;
        }

        public Task Publish(string outputFile){
            return Task.Run(() => {
                try {
                    Preferences.Default.VoicebankPublishUseIgnore = UseIgnore;
                    if(UseIgnore){
                        Preferences.Default.VoicebankPublishIgnores = IgnoreTypes;
                    }
                    Preferences.Save();
                    var basePath = PathManager.Inst.SingersInstallPath;
                    var publisher = new VoicebankPublisher((progress, info) => {
                        DocManager.Inst.ExecuteCmd(new ProgressBarNotification(progress, info));
                    }, UseIgnore ? IgnoreTypes : null);
                    publisher.Publish(singer, outputFile);
                } finally {
                    new Task(() => {
                        DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, ""));
                    }).Start(DocManager.Inst.MainScheduler);
                }
            });
        }
    }
}