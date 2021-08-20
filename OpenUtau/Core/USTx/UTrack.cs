using Newtonsoft.Json;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public class UTrack {
        [JsonProperty] public string singer;

        public USinger Singer;

        public string SingerName => Singer != null ? Singer.DisplayName : "[No Singer]";
        public int TrackNo { set; get; }
        public int DisplayTrackNo => TrackNo + 1;
        public bool Mute { set; get; }
        public bool Solo { set; get; }
        public double Volume { set; get; }
        public double Pan { set; get; }

        public void BeforeSave() {
            singer = Singer == null ? null : Singer.VoicebankName;
        }

        public void AfterLoad(UProject project) {
            if (Singer == null && !string.IsNullOrEmpty(singer)) {
                Singer = DocManager.Inst.GetSinger(singer);
            }
            TrackNo = project.tracks.IndexOf(this);
        }
    }
}
