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
                    VoiceColorExp = null;
                }
            }
        }

        [YamlIgnore] public Phonemizer Phonemizer { get; set; } = PhonemizerFactory.Get(typeof(DefaultPhonemizer)).Create();
        [YamlIgnore] public string PhonemizerTag => Phonemizer.Tag;

        [YamlIgnore] public string SingerName => Singer != null ? Singer.DisplayName : "[No Singer]";
        [YamlIgnore] public int TrackNo { set; get; }
        [YamlIgnore] public int DisplayTrackNo => TrackNo + 1;
        public bool Mute { set; get; }
        public bool Solo { set; get; }
        public double Volume { set; get; }
        [YamlIgnore] public double Pan { set; get; }
        [YamlIgnore] public UExpressionDescriptor VoiceColorExp { set; get; }

        public bool TryGetExpression(UProject project, string key, out UExpressionDescriptor descriptor) {
            if (!project.expressions.TryGetValue(key, out descriptor)) {
                return false;
            }
            if (key == "clr" && VoiceColorExp != null) {
                descriptor = VoiceColorExp;
            }
            return true;
        }

        public void OnSingerRefreshed() {
            if (Singer != null && Singer.Loaded && !DocManager.Inst.Singers.ContainsKey(Singer.Id)) {
                Singer.Found = false;
                Singer.Loaded = false;
            }
            VoiceColorExp = null;
        }

        public void Validate(UProject project) {
            if (Singer != null && Singer.Found) {
                Singer.EnsureLoaded();
            }
            if (project.expressions.TryGetValue("clr", out var descriptor)) {
                if (VoiceColorExp == null && Singer != null && Singer.Found && Singer.Loaded) {
                    VoiceColorExp = descriptor.Clone();
                    var colors = Singer.Subbanks.Select(subbank => subbank.Color).ToHashSet();
                    VoiceColorExp.options = colors.OrderBy(c => c).ToArray();
                    VoiceColorExp.max = VoiceColorExp.options.Length - 1;
                }
            }
        }

        public void BeforeSave() {
            singer = Singer?.Id;
            phonemizer = Phonemizer.GetType().FullName;
        }

        public void AfterLoad(UProject project) {
            if (Phonemizer == null || !string.IsNullOrEmpty(phonemizer)) {
                try {
                    var factory = DocManager.Inst.PhonemizerFactories.FirstOrDefault(factory => factory.type.FullName == phonemizer);
                    Phonemizer = factory?.Create();
                    phonemizer = null;
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
            }
            Phonemizer.SetSinger(Singer);
            TrackNo = project.tracks.IndexOf(this);
        }
    }
}
