using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public class UPhoneme {
        [JsonProperty] public int position;
        [JsonProperty] public string phoneme = "a";
        [JsonProperty] public float preutter;
        [JsonProperty] public float overlap;
        [JsonProperty] public UEnvelope envelope = new UEnvelope();

        public UNote Parent { get; set; }
        public int Duration { get; set; }
        public int EndPosition { get { return position + Duration; } }
        public string PhonemeRemapped { get { return AutoRemapped ? phoneme + RemappedBank : phoneme; } }
        public string RemappedBank = string.Empty;
        public bool AutoEnvelope = true;
        public bool AutoRemapped = true;
        public double TailIntrude;
        public double TailOverlap;
        public UOto Oto;
        public bool Overlapped = false;
        public bool OverlapCorrection = true;

        public bool PhonemeError = false;

        public UPhoneme Clone(UNote newParent) {
            return new UPhoneme() {
                Parent = newParent,
            };
        }

        public void Validate() { }
    }

    public class UEnvelope {
        public List<Vector2> data = new List<Vector2>();

        public UEnvelope() {
            data.Add(new Vector2(0, 0));
            data.Add(new Vector2(0, 100));
            data.Add(new Vector2(0, 100));
            data.Add(new Vector2(0, 100));
            data.Add(new Vector2(0, 0));
        }
    }
}
