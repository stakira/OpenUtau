using System;
using Newtonsoft.Json;
using Serilog;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public class UTrack {
        [JsonProperty] public string singer;
        [JsonProperty] public string phonemizer;

        private USinger singer_;
        public USinger Singer {
            get => singer_;
            set {
                if (singer_ != value) {
                    singer_ = value;
                    Phonemizer.SetSinger(value);
                }
            }
        }

        public Phonemizer Phonemizer = new DefaultPhonemizer();

        public string SingerName => Singer != null ? Singer.DisplayName : "[No Singer]";
        public int TrackNo { set; get; }
        public int DisplayTrackNo => TrackNo + 1;
        public bool Mute { set; get; }
        public bool Solo { set; get; }
        public double Volume { set; get; }
        public double Pan { set; get; }

        public void BeforeSave() {
            singer = Singer?.Id;
            phonemizer = Phonemizer.GetType().FullName;
        }

        public void AfterLoad(UProject project) {
            try {
                var type = Type.GetType(phonemizer);
                Phonemizer = Activator.CreateInstance(type) as Phonemizer;
            } catch (Exception e) {
                Log.Error(e, $"Failed to load phonemizer {phonemizer}");
            }
            if (Phonemizer == null) {
                Phonemizer = new DefaultPhonemizer();
            }
            if (Singer == null && !string.IsNullOrEmpty(singer)) {
                Singer = DocManager.Inst.GetSinger(singer);
            }
            TrackNo = project.tracks.IndexOf(this);
        }
    }
}
