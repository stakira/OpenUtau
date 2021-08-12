using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public class UNote : IComparable {
        [JsonProperty("pos")] public int position;
        [JsonProperty("dur")] public int duration;
        [JsonProperty("num")] public int noteNum;
        [JsonProperty("lrc")] public string lyric = "a";
        [JsonProperty("pho")] public List<UPhoneme> phonemes = new List<UPhoneme>();
        [JsonProperty("pit")] public UPitch pitch;
        [JsonProperty("vbr")] public UVibrato vibrato;
        [JsonProperty("exp")] public Dictionary<string, UExpression> expressions = new Dictionary<string, UExpression>();

        public int End { get { return position + duration; } }
        public bool Selected { get; set; } = false;
        public bool Error { get; set; } = false;

        public static UNote Create() {
            var note = new UNote();
            note.phonemes.Add(new UPhoneme() {
                Parent = note,
                position = 0,
            });
            note.pitch = new UPitch();
            note.vibrato = new UVibrato();
            return note;
        }

        public string GetResamplerFlags() { return "Y0H0F0"; }

        public int CompareTo(object obj) {
            if (obj == null) return 1;

            if (!(obj is UNote other))
                throw new ArgumentException("CompareTo object is not a Note");

            if (position != other.position) {
                return position.CompareTo(other.position);
            }
            return GetHashCode().CompareTo(other.GetHashCode());
        }

        public override string ToString() {
            return $"\"{lyric}\" Pos:{position} Dur:{duration} Note:{noteNum}{(Error ? " Error" : string.Empty)}{(Selected ? " Selected" : string.Empty)}";
        }

        public void Validate(UProject project) {
            int lastPosition = duration;
            for (var i = phonemes.Count - 1; i >= 0; --i) {
                var phoneme = phonemes[i];
                phoneme.Parent = this;
                phoneme.Duration = lastPosition - phoneme.position;
                phoneme.Validate();
            }
            foreach (var pair in expressions) {
                pair.Value.descriptor = project.expressions[pair.Key];
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class UVibrato {
        float _length;
        float _period;
        float _depth;
        float _in;
        float _out;
        float _shift;
        float _drift;

        [JsonProperty] public float length { get => _length; set => _length = Math.Max(0, Math.Min(100, value)); }
        [JsonProperty] public float period { get => _period; set => _period = Math.Max(64, Math.Min(512, value)); }
        [JsonProperty] public float depth { get => _depth; set => _depth = Math.Max(5, Math.Min(200, value)); }
        [JsonProperty]
        public float @in {
            get => _in;
            set {
                _in = Math.Max(0, Math.Min(100, value));
                _out = Math.Min(_out, 100 - value);
            }
        }
        [JsonProperty]
        public float @out {
            get => _out;
            set {
                _out = Math.Max(0, Math.Min(100, value));
                _in = Math.Min(_in, 100 - value);
            }
        }
        [JsonProperty] public float shift { get => _shift; set => _shift = Math.Max(0, Math.Min(100, value)); }
        [JsonProperty] public float drift { get => _drift; set => _drift = Math.Max(-100, Math.Min(100, value)); }

        public UVibrato Clone() {
            var result = new UVibrato {
                length = length,
                period = period,
                depth = depth,
                @in = @in,
                @out = @out,
                shift = shift,
                drift = drift
            };
            return result;
        }

        public UVibrato Split(int offset) {
            // TODO
            return Clone();
        }
    }

    public enum PitchPointShape {
        /// <summary>
        /// SineInOut
        /// </summary>
        io,
        /// <summary>
        /// Linear
        /// </summary>
        l,
        /// <summary>
        /// SineIn
        /// </summary>
        i,
        /// <summary>
        /// SineOut
        /// </summary>
        o
    };

    [JsonObject(MemberSerialization.OptIn)]
    public class PitchPoint : IComparable<PitchPoint> {
        [JsonProperty] public float X;
        [JsonProperty] public float Y;
        [JsonProperty] public PitchPointShape shape;

        public PitchPoint(float x, float y, PitchPointShape shape = PitchPointShape.io) {
            X = x;
            Y = y;
            this.shape = shape;
        }

        public PitchPoint Clone() {
            return new PitchPoint(X, Y, shape);
        }

        public int CompareTo(PitchPoint other) { return X.CompareTo(other.X); }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class UPitch {
        [JsonProperty] public List<PitchPoint> data = new List<PitchPoint>();
        [JsonProperty] public bool snapFirst = true;

        public void AddPoint(PitchPoint p) {
            data.Add(p);
            data.Sort();
        }

        public void RemovePoint(PitchPoint p) {
            data.Remove(p);
        }

        public UPitch Clone() {
            var result = new UPitch();
            foreach (var p in data) {
                result.data.Add(p.Clone());
            }
            return result;
        }

        public UPitch Split(int offset) {
            var result = new UPitch() {
                snapFirst = true,
            };
            while (data.Count > 0 && data.Last().X >= offset) {
                result.data.Add(data.Last());
                data.Remove(data.Last());
            }
            result.data.Reverse();
            return result;
        }
    }
}
