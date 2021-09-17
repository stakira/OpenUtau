using System;
using Newtonsoft.Json;
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

        [YamlIgnore] public Phonemizer Phonemizer { get; set; } = new DefaultPhonemizer();
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
            try {
                var type = Type.GetType(phonemizer);
                if (Phonemizer == null || type != Phonemizer.GetType()) {
                    Phonemizer = Activator.CreateInstance(type) as Phonemizer;
                }
            } catch (Exception e) {
                Log.Error(e, $"Failed to load phonemizer {phonemizer}");
            }
            if (Phonemizer == null) {
                Phonemizer = new DefaultPhonemizer();
            }
            if (Singer == null && !string.IsNullOrEmpty(singer)) {
                Singer = DocManager.Inst.GetSinger(singer);
                Phonemizer.SetSinger(Singer);
            }
            TrackNo = project.tracks.IndexOf(this);
        }
    }
}
