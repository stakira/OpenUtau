using System;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public class UPhoneme {
        [JsonProperty] public int position;
        [JsonProperty] public string phoneme = "a";
        [JsonProperty] public bool autoRemap = true;

        public UEnvelope envelope = new UEnvelope();
        public UOto oto;
        public float preutter;
        public float overlap;
        public bool overlapped = false;
        public float tailIntrude;
        public float tailOverlap;

        public UNote Parent { get; set; }
        public int Duration { get; set; }
        public int End { get { return position + Duration; } }
        public UPhoneme Prev { get; set; }
        public UPhoneme Next { get; set; }
        public bool Error { get; set; } = false;

        public UPhoneme Clone(UNote newParent) {
            return new UPhoneme() {
                Parent = newParent,
            };
        }

        public void Validate(UProject project, UTrack track, UVoicePart part, UNote note) {
            ValidateDuration(note);
            ValidateOto(track, note);
            ValidateOverlap(project, part);
            ValidateEnvelope(project, note);
        }

        void ValidateDuration(UNote note) {
            if (Next != null && Next.Parent == Parent) {
                Duration = Next.position - position;
            } else {
                Duration = note.duration - position;
            }
            Error = Duration <= 0;
        }

        void ValidateOto(UTrack track, UNote note) {
            if (Error) {
                return;
            }
            if (track.Singer == null || !track.Singer.Loaded) {
                Error = true;
                return;
            }
            // Select phoneme.
            string phonemeMapped = phoneme;
            if (autoRemap) {
                string noteString = MusicMath.GetNoteString(note.noteNum);
                if (track.Singer.PitchMap.ContainsKey(noteString)) {
                    phonemeMapped += track.Singer.PitchMap[noteString];
                }
            }
            // Load oto.
            if (track.Singer.TryGetOto(phonemeMapped, out var oto)) {
                this.oto = oto;
                Error = false;
                overlap = (float)oto.Overlap;
                preutter = (float)oto.Preutter;
                float vel = note.expressions["vel"].value;
                if (vel != 100) {
                    float stretchRatio = (float)Math.Pow(2f, 1.0f - vel / 100f);
                    overlap *= stretchRatio;
                    preutter *= stretchRatio;
                }
            } else {
                this.oto = default;
                Error = true;
                overlap = 0;
                preutter = 0;
            }
        }

        void ValidateOverlap(UProject project, UVoicePart part) {
            if (Error) {
                return;
            }
            if (Prev == null) {
                overlapped = false;
                return;
            }
            int gapTick = Parent.position + position - Prev.Parent.position - Prev.End;
            float gapMs = (float)project.TickToMillisecond(gapTick);
            if (gapMs < preutter) {
                overlapped = true;
                float lastDurMs = (float)project.TickToMillisecond(Prev.Duration);
                float correctionRatio = (lastDurMs + Math.Min(0, gapMs)) / 2 / (preutter - overlap);
                if (preutter - overlap > gapMs + lastDurMs / 2) {
                    preutter = gapMs + (preutter - gapMs) * correctionRatio;
                    overlap *= correctionRatio;
                } else if (preutter > gapMs + lastDurMs) {
                    overlap *= correctionRatio;
                    preutter = gapMs + lastDurMs;
                }
                Prev.tailIntrude = preutter - gapMs;
                Prev.tailOverlap = overlap;
            } else {
                overlapped = false;
                Prev.tailIntrude = 0;
                Prev.tailOverlap = 0;
            }
        }

        void ValidateEnvelope(UProject project, UNote note) {
            if (Error) {
                return;
            }
            var vol = note.expressions["vol"].value;
            var acc = note.expressions["acc"].value;
            var dec = note.expressions["dec"].value;

            Vector2 p0, p1, p2, p3, p4;
            p0.X = -preutter;
            p1.X = p0.X + (overlapped ? overlap : 5f);
            p2.X = Math.Max(0f, p1.X);
            p3.X = (float)project.TickToMillisecond(Duration) - (float)tailIntrude;
            p4.X = p3.X + (float)tailOverlap;

            p0.Y = 0f;
            p1.Y = vol;
            p1.X = p0.X + (overlapped ? overlap : 5f) * acc / 100f;
            p1.Y = acc * vol / 100f;
            p2.Y = vol;
            p3.Y = vol;
            p3.X -= (p3.X - p2.X) * dec / 500f;
            p3.Y *= 1f - dec / 100f;
            p4.Y = 0f;

            envelope.data[0] = p0;
            envelope.data[1] = p1;
            envelope.data[2] = p2;
            envelope.data[3] = p3;
            envelope.data[4] = p4;
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
}
