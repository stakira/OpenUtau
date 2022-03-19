using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        public float autoPreutter { get; private set; }
        public float autoOverlap { get; private set; }
        public bool overlapped { get; private set; }
        public float tailIntrude { get; private set; }
        public float tailOverlap { get; private set; }
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

        public void Validate(ValidateOptions options, UProject project, UTrack track, UVoicePart part, UNote note) {
            Error = note.Error;
            ValidateDuration(note);
            ValidateOto(track, note);
            ValidateOverlap(project, track, note);
            ValidateEnvelope(project, track, note);
        }

        void ValidateDuration(UNote note) {
            if (Error) {
                return;
            }
            var leadingNote = Parent.Extends ?? Parent;
            int pos = Parent.position + position;
            Duration = leadingNote.ExtendedEnd - pos;
            if (Next != null) {
                if (Next.Parent == Parent.Next && Parent.End == Next.Parent.position) {
                    Duration = int.MaxValue;
                }
                Duration = Math.Min(Duration, Next.Parent.position + Next.position - pos);
            }
            Error = Duration <= 0;
        }

        void ValidateOto(UTrack track, UNote note) {
            phonemeMapped = string.Empty;
            if (Error) {
                return;
            }
            if (track.Singer == null || !track.Singer.Found || !track.Singer.Loaded) {
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

        void ValidateOverlap(UProject project, UTrack track, UNote note) {
            if (Error) {
                return;
            }
            float consonantStretch = (float)Math.Pow(2f, 1.0f - GetExpression(project, track, Format.Ustx.VEL).Item1 / 100f);
            autoOverlap = (float)oto.Overlap * consonantStretch;
            autoPreutter = (float)oto.Preutter * consonantStretch;
            overlapped = false;
            tailIntrude = 0;
            tailOverlap = 0;

            if (Prev != null) {
                int gapTick = Parent.position + position - (Prev.Parent.position + Prev.End);
                float gapMs = (float)project.TickToMillisecond(gapTick);
                float prevDur = (float)project.TickToMillisecond(Prev.Duration);
                float maxPreutter = autoPreutter;
                if (gapMs <= 0) {
                    overlapped = true;
                    if (autoPreutter - autoOverlap > prevDur * 0.5f) {
                        maxPreutter = prevDur * 0.5f / (autoPreutter - autoOverlap) * autoPreutter;
                    }
                } else if (gapMs < autoPreutter) {
                    maxPreutter = gapMs;
                }
                if (autoPreutter > maxPreutter) {
                    float ratio = maxPreutter / autoPreutter;
                    autoPreutter = maxPreutter;
                    autoOverlap *= ratio;
                }
                if (autoPreutter > prevDur * 0.9f && overlapped) {
                    float delta = autoPreutter - prevDur * 0.9f;
                    autoPreutter -= delta;
                    autoOverlap -= delta;
                }
            }
            preutter = Math.Max(0, autoPreutter + (preutterDelta ?? 0));
            overlap = autoOverlap + (overlapDelta ?? 0);
            if (Prev != null) {
                Prev.tailIntrude = overlapped ? Math.Max(preutter, preutter - overlap) : 0;
                Prev.tailOverlap = overlapped ? Math.Max(overlap, 0) : 0;
                Prev.ValidateEnvelope(project, track, Prev.Parent);
            }
        }

        void ValidateEnvelope(UProject project, UTrack track, UNote note) {
            if (Error) {
                return;
            }
            var vol = GetExpression(project, track, Format.Ustx.VOL).Item1;
            var atk = GetExpression(project, track, Format.Ustx.ATK).Item1;
            var dec = GetExpression(project, track, Format.Ustx.DEC).Item1;

            Vector2 p0, p1, p2, p3, p4;
            p0.X = -preutter;
            p1.X = p0.X + (!overlapped && overlapDelta == null ? 5f : Math.Max(overlap, 5f));
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

        public Tuple<float, bool> GetExpression(UProject project, UTrack track, string abbr) {
            track.TryGetExpression(project, abbr, out var descriptor);
            var note = Parent.Extends ?? Parent;
            int index = Parent.PhonemeOffset + Index;
            var expression = note.phonemeExpressions.FirstOrDefault(
                exp => exp.descriptor.abbr == descriptor.abbr && exp.index == index);
            if (expression != null) {
                return Tuple.Create(expression.value, true);
            } else {
                return Tuple.Create(descriptor.defaultValue, false);
            }
        }

        public void SetExpression(UProject project, UTrack track, string abbr, float value) {
            track.TryGetExpression(project, abbr, out var descriptor);
            var note = Parent.Extends ?? Parent;
            int index = Parent.PhonemeOffset + Index;
            if (descriptor.defaultValue == value) {
                note.phonemeExpressions.RemoveAll(
                    exp => exp.descriptor.abbr == descriptor.abbr && exp.index == index);
                return;
            }
            var expression = note.phonemeExpressions.FirstOrDefault(
                exp => exp.descriptor.abbr == descriptor.abbr && exp.index == index);
            if (expression != null) {
                expression.descriptor = descriptor;
                expression.value = value;
            } else {
                note.phonemeExpressions.Add(new UExpression(descriptor) {
                    descriptor = descriptor,
                    index = index,
                    value = value,
                });
            }
        }

        public Tuple<string, int?>[] GetResamplerFlags(UProject project, UTrack track) {
            var flags = new List<Tuple<string, int?>>();
            foreach (var descriptor in project.expressions.Values) {
                if (descriptor.type == UExpressionType.Numerical) {
                    if (!string.IsNullOrEmpty(descriptor.flag)) {
                        int value = (int)GetExpression(project, track, descriptor.abbr).Item1;
                        flags.Add(Tuple.Create<string, int?>(descriptor.flag, value));
                    }
                }
                if (descriptor.type == UExpressionType.Options) {
                    if (descriptor.isFlag) {
                        int value = (int)GetExpression(project, track, descriptor.abbr).Item1;
                        flags.Add(Tuple.Create<string, int?>(descriptor.options[value], null));
                    }
                }
            }
            return flags.ToArray();
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

    public class UPhonemeOverride {
        public int index;
        public string phoneme;
        public int? offset;
        public float? preutterDelta;
        public float? overlapDelta;

        [YamlIgnore]
        public bool IsEmpty => string.IsNullOrEmpty(phoneme) && !offset.HasValue
            && !preutterDelta.HasValue && !overlapDelta.HasValue;

        public UPhonemeOverride Clone() {
            return new UPhonemeOverride() {
                index = index,
                phoneme = phoneme,
                offset = offset,
                preutterDelta = preutterDelta,
                overlapDelta = overlapDelta,
            };
        }
    }
}
