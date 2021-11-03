using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace OpenUtau.Core.Ustx {
    public class UPhoneme {
        public int position;
        public string phoneme = "a";

        public string phonemeMapped { get; private set; }
        public UEnvelope envelope { get; private set; } = new UEnvelope();
        public UOto oto { get; private set; }
        public float preutter { get; private set; }
        public float overlap { get; private set; }
        public bool overlapped { get; private set; }
        public float tailIntrude { get; private set; }
        public float tailOverlap { get; private set; }
        [Obsolete] public float? preutterScale { get; set; } // TODO: cleanup
        [Obsolete] public float? overlapScale { get; set; } // TODO: cleanup
        public float? preutterDelta { get; set; }
        public float? overlapDelta { get; set; }

        public UNote Parent { get; set; }
        public int Index { get; set; }
        public int Duration { get; private set; }
        public int End { get { return position + Duration; } }
        public UPhoneme Prev { get; set; }
        public UPhoneme Next { get; set; }
        public bool Error { get; set; } = false;

        public bool HasPhonemeOverride { get; set; }
        public bool HasOffsetOverride { get; set; }

        public override string ToString() => $"\"{phoneme}\" pos:{position}";

        public UPhoneme Clone() {
            return new UPhoneme() {
                position = position,
                phoneme = phoneme,
            };
        }

        public void Validate(UProject project, UTrack track, UVoicePart part, UNote note) {
            Error = note.Error;
            ValidateDuration(note);
            ValidateOto(track, note);
            ValidateOverlap(project, note);
            ValidateEnvelope(project, note);
        }

        void ValidateDuration(UNote note) {
            if (Error) {
                return;
            }
            if (Parent.Extends != null) {
                Duration = Parent.Extends.ExtendedEnd - Parent.position - position;
            } else {
                Duration = Parent.ExtendedDuration - position;
            }
            if (Next != null) {
                Duration = Math.Min(Duration, Next.Parent.position + Next.position - (Parent.position + position));
            }
            Error = Duration <= 0;
        }

        void ValidateOto(UTrack track, UNote note) {
            phonemeMapped = string.Empty;
            if (Error) {
                return;
            }
            if (track.Singer == null || !track.Singer.Loaded) {
                Error = true;
                return;
            }
            // Load oto.
            if (track.Singer.TryGetMappedOto(phoneme, note.tone, out var oto)) {
                this.oto = oto;
                Error = false;
                phonemeMapped = oto.Alias;
            } else {
                this.oto = default;
                Error = true;
                phonemeMapped = string.Empty;
            }
        }

        void ValidateOverlap(UProject project, UNote note) {
            if (Error) {
                return;
            }
            float consonantStretch = (float)Math.Pow(2f, 1.0f - GetExpression(project, "vel").Item1 / 100f);
            if (overlapDelta != null) {
                overlap = (float)(oto.Overlap + overlapDelta) * consonantStretch;
            } else {
                overlap = (float)oto.Overlap * consonantStretch * (overlapScale ?? 1);
            }
            if (preutterDelta != null) {
                preutter = (float)(oto.Preutter + preutterDelta) * consonantStretch;
            } else {
                preutter = (float)oto.Preutter * consonantStretch * (preutterScale ?? 1);
            }
            overlapped = false;
            tailIntrude = 0;
            tailOverlap = 0;

            if (Prev == null) {
                return;
            }
            int gapTick = Parent.position + position - (Prev.Parent.position + Prev.End);
            float gapMs = (float)project.TickToMillisecond(gapTick);
            float maxPreutter = preutter;
            if (gapMs <= 0) {
                // Keep at least half of last phoneme, or 10% if preutterScale is set. 
                overlapped = true;
                maxPreutter = (float)project.TickToMillisecond(Prev.Duration) * (preutterDelta == null && preutterScale == null ? 0.5f : 0.9f);
            } else if (gapMs < preutter) {
                maxPreutter = gapMs;
            }
            if (preutter > maxPreutter) {
                float ratio = maxPreutter / preutter;
                preutter = maxPreutter;
                overlap *= ratio;
            }
            preutter = Math.Max(0, preutter);
            Prev.tailIntrude = overlapped ? Math.Max(preutter, preutter - overlap) : 0;
            Prev.tailOverlap = overlapped ? Math.Max(overlap, 0) : 0;
            Prev.ValidateEnvelope(project, Prev.Parent);
        }

        void ValidateEnvelope(UProject project, UNote note) {
            if (Error) {
                return;
            }
            var vol = GetExpression(project, "vol").Item1;
            var atk = GetExpression(project, "atk").Item1;
            var dec = GetExpression(project, "dec").Item1;

            Vector2 p0, p1, p2, p3, p4;
            p0.X = -preutter;
            p1.X = p0.X + Math.Max(overlap, 5f);
            p2.X = Math.Max(0f, p1.X);
            p3.X = (float)project.TickToMillisecond(Duration) - (float)tailIntrude;
            p4.X = p3.X + (float)tailOverlap;
            if (p3.X == p4.X) {
                p3.X = Math.Max(p2.X, p3.X - 25f);
            }

            p0.Y = 0f;
            p1.Y = vol;
            p1.Y = atk * vol / 100f;
            p2.Y = vol;
            p3.Y = vol * (1f - dec / 100f);
            p4.Y = 0f;

            envelope.data[0] = p0;
            envelope.data[1] = p1;
            envelope.data[2] = p2;
            envelope.data[3] = p3;
            envelope.data[4] = p4;
        }

        public Tuple<float, bool> GetExpression(UProject project, string abbr) {
            var descriptor = project.expressions[abbr];
            var note = Parent.Extends ?? Parent;
            int index = Parent.PhonemeOffset + Index;
            var expression = note.phonemeExpressions.FirstOrDefault(exp => exp.descriptor == descriptor && exp.index == index);
            if (expression != null) {
                return Tuple.Create(expression.value, true);
            } else {
                return Tuple.Create(descriptor.defaultValue, false);
            }
        }

        public void SetExpression(UProject project, string abbr, float value) {
            var descriptor = project.expressions[abbr];
            var note = Parent.Extends ?? Parent;
            int index = Parent.PhonemeOffset + Index;
            if (descriptor.defaultValue == value) {
                note.phonemeExpressions.RemoveAll(exp => exp.descriptor == descriptor && exp.index == index);
                return;
            }
            var expression = note.phonemeExpressions.FirstOrDefault(exp => exp.descriptor == descriptor && exp.index == index);
            if (expression != null) {
                expression.value = value;
            } else {
                note.phonemeExpressions.Add(new UExpression(descriptor) {
                    descriptor = descriptor,
                    index = index,
                    value = value,
                });
            }
        }

        public string GetResamplerFlags(UProject project) {
            StringBuilder builder = new StringBuilder();
            foreach (var descriptor in project.expressions.Values) {
                if (descriptor.type == UExpressionType.Numerical) {
                    if (!string.IsNullOrEmpty(descriptor.flag)) {
                        builder.Append(descriptor.flag);
                        int value = (int)GetExpression(project, descriptor.abbr).Item1;
                        builder.Append(value);
                    }
                }
                if (descriptor.type == UExpressionType.Options) {
                    if (descriptor.isFlag) {
                        int value = (int)GetExpression(project, descriptor.abbr).Item1;
                        builder.Append(descriptor.options[value]);
                    }
                }
            }
            return builder.ToString();
        }
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

    [JsonObject(MemberSerialization.OptIn)]
    public class UPhonemeOverride {
        [JsonProperty] public int index;
        [JsonProperty] public string phoneme;
        [JsonProperty] public int? offset;
        [JsonProperty] [Obsolete] public float? preutterScale; // TODO: cleanup
        [JsonProperty] [Obsolete] public float? overlapScale; // TODO: cleanup
        public float? preutterDelta;
        public float? overlapDelta;

        [YamlIgnore]
        public bool IsEmpty => string.IsNullOrEmpty(phoneme) && !offset.HasValue
            && !preutterScale.HasValue && !overlapScale.HasValue
            && !preutterDelta.HasValue && !overlapDelta.HasValue;

        public UPhonemeOverride Clone() {
            return new UPhonemeOverride() {
                index = index,
                phoneme = phoneme,
                offset = offset,
                preutterScale = preutterScale,
                overlapScale = overlapScale,
                preutterDelta = preutterDelta,
                overlapDelta = overlapDelta,
            };
        }
    }
}
