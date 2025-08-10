using System.IO;
using System.Threading.Tasks;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class SingerPublishViewModel : ViewModelBase {
        public USinger singer;
        [Reactive] public bool UseIgnore { get; set; }
        [Reactive] public string IgnoreTypes { get; set; }

        public SingerPublishViewModel(USinger singer) {
            this.singer = singer;
            UseIgnore = Preferences.Default.VoicebankPublishUseIgnore;
            IgnoreTypes = Preferences.Default.VoicebankPublishIgnores;
        }

        public Task Publish(string outputFile) {
            return Task.Run(() => {
                try {
                    Preferences.Default.VoicebankPublishUseIgnore = UseIgnore;
                    if (UseIgnore) {
                        Preferences.Default.VoicebankPublishIgnores = IgnoreTypes;
                    }
                    Preferences.Save();
                    if (Directory.Exists(singer.Location)) {
                        var publisher = new VoicebankPublisher((progress, info) => {
                            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(progress, info));
                        }, UseIgnore ? IgnoreTypes : null);
                        publisher.Publish(singer, outputFile);
                    } else if (File.Exists(singer.Location)) {
                        File.Copy(singer.Location, outputFile);
                    }
                } finally {
                    new Task(() => {
                        DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, ""));
                    }).Start(DocManager.Inst.MainScheduler);
                }
            });
        }
    }
}
