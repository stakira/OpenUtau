using System;
using System.Linq;
using Newtonsoft.Json;
using OpenUtau.Api;
using Serilog;
using YamlDotNet.Serialization;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public class UTrack {
        [JsonProperty] public string singer;
        [JsonProperty] public string phonemizer;

        private USinger singer_;

        [YamlIgnore]
        public USinger Singer {
            get => singer_;
            set {
                if (singer_ != value) {
                    singer_ = value;
                    Phonemizer.SetSinger(value);
                }
            }
        }

        [YamlIgnore] public Phonemizer Phonemizer { get; set; } = PhonemizerFactory.Get(typeof(DefaultPhonemizer)).Create();
        [YamlIgnore] public string PhonemizerTag => Phonemizer.Tag;

        [YamlIgnore] public string SingerName => Singer != null ? Singer.DisplayName : "[No Singer]";
        [YamlIgnore] public int TrackNo { set; get; }
        [YamlIgnore] public int DisplayTrackNo => TrackNo + 1;
        [YamlIgnore] public bool Mute { set; get; }
        [YamlIgnore] public bool Solo { set; get; }
        [YamlIgnore] public double Volume { set; get; }
        [YamlIgnore] public double Pan { set; get; }

        public void BeforeSave() {
            singer = Singer?.Id;
            phonemizer = Phonemizer.GetType().FullName;
        }

        public void AfterLoad(UProject project) {
            if (Phonemizer == null) {
                try {
                    var factory = DocManager.Inst.PhonemizerFactories.FirstOrDefault(factory => factory.type.FullName == phonemizer);
                    Phonemizer = factory?.Create();
                } catch (Exception e) {
                    Log.Error(e, $"Failed to load phonemizer {phonemizer}");
                }
            }
            if (Phonemizer == null) {
                Phonemizer = PhonemizerFactory.Get(typeof(DefaultPhonemizer)).Create();
            }
            if (Singer == null && !string.IsNullOrEmpty(singer)) {
                Singer = DocManager.Inst.GetSinger(singer);
                if (Singer == null) {
                    Singer = new USinger(singer);
                }
                Phonemizer.SetSinger(Singer);
            }
            TrackNo = project.tracks.IndexOf(this);
        }
    }
}
