using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using OpenUtau.Api;
using Serilog;
using YamlDotNet.Serialization;

namespace OpenUtau.Core.Ustx {
    [JsonObject(MemberSerialization.OptIn)]
    public class UNote : IComparable {
        static readonly Regex phoneticHintPattern = new Regex(@"\[(.*)\]");

        [JsonProperty("pos")] public int position;
        [JsonProperty("dur")] public int duration;
        [JsonProperty("num")] public int tone;
        [JsonProperty("lrc")] public string lyric = "a";
        [JsonProperty("pit")] public UPitch pitch;
        [JsonProperty("vbr")] public UVibrato vibrato;
        [JsonProperty("exp")]
        [Obsolete("Only used for upgrading ustx v0.1")]
        public Dictionary<string, double?> expressions;
        [JsonProperty("nex")] public List<UExpression> noteExpressions = new List<UExpression>();
        [JsonProperty("pex")] public List<UExpression> phonemeExpressions = new List<UExpression>();
        [JsonProperty("phm")] public List<UPhonemeOverride> phonemeOverrides = new List<UPhonemeOverride>();

        [YamlIgnore] public List<UPhoneme> phonemes = new List<UPhoneme>();
        [YamlIgnore] public int End => position + duration;
        [YamlIgnore] public bool Selected { get; set; } = false;
        [YamlIgnore] public UNote Prev { get; set; }
        [YamlIgnore] public UNote Next { get; set; }
        [YamlIgnore] public UNote Extends { get; set; }
        [YamlIgnore] public int PhonemeOffset { get; set; }
        [YamlIgnore] public int ExtendedDuration { get; set; }
        [YamlIgnore] public int ExtendedEnd => position + ExtendedDuration;
        [YamlIgnore] public int LeftBound => position + Math.Min(0, phonemes.Count > 0 ? phonemes.First().position : 0);
        [YamlIgnore] public int RightBound => position + Math.Max(duration, phonemes.Count > 0 ? phonemes.Last().position + phonemes.Last().Duration : 0);
        [YamlIgnore] public bool Error { get; set; } = false;
        [YamlIgnore] public bool OverlapError { get; set; } = false;

        public static UNote Create() {
            var note = new UNote();
            note.pitch = new UPitch();
            note.vibrato = new UVibrato();
            return note;
        }

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
            return $"\"{lyric}\" Pos:{position} Dur:{duration} Tone:{tone}{(Error ? " Error" : string.Empty)}{(Selected ? " Selected" : string.Empty)}";
        }

        public void AfterLoad(UProject project, UTrack track, UVoicePart part) {
            foreach (var exp in noteExpressions) {
                exp.descriptor = project.expressions[exp.abbr];
            }
            foreach (var exp in phonemeExpressions) {
                exp.descriptor = project.expressions[exp.abbr];
            }
        }

        public void BeforeSave(UProject project, UTrack track, UVoicePart part) {
            noteExpressions.ForEach(exp => exp.index = null);
            noteExpressions = noteExpressions
                .OrderBy(exp => exp.abbr)
                .ToList();
            phonemeExpressions = phonemeExpressions
                .OrderBy(exp => exp.index)
                .ThenBy(exp => exp.abbr)
                .ToList();
        }

        public void Validate(UProject project, UTrack track, UVoicePart part) {
            duration = Math.Max(10, duration);
            if (Prev != null && Prev.End > position) {
                Error = true;
                OverlapError = true;
                return;
            }
            Error = false;
            OverlapError = false;
            if (track.Singer == null || !track.Singer.Loaded) {
                Error |= true;
            }
            if (pitch.snapFirst) {
                if (Prev != null && Prev.End == position) {
                    pitch.data[0].Y = (Prev.tone - tone) * 10;
                } else {
                    pitch.data[0].Y = 0;
                }
            }
            for (var i = 0; i < phonemes.Count; i++) {
                var phoneme = phonemes[i];
                phoneme.Parent = this;
                phoneme.Index = i;
            }
            // Update has override bits.
            foreach (var phoneme in phonemes) {
                phoneme.HasPhonemeOverride = false;
                phoneme.HasOffsetOverride = false;
                phoneme.preutterDelta = null;
                phoneme.overlapDelta = null;
            }
            foreach (var o in (Extends ?? this).phonemeOverrides) {
                int index = o.index - PhonemeOffset;
                if (index >= 0 && index < phonemes.Count) {
                    if (o.phoneme != null) {
                        phonemes[index].HasPhonemeOverride = true;
                    }
                    if (o.offset != null) {
                        phonemes[index].HasOffsetOverride = true;
                    }
                    phonemes[index].preutterDelta = o.preutterDelta;
                    phonemes[index].overlapDelta = o.overlapDelta;
                }
            }
            foreach (var phoneme in phonemes) {
                phoneme.Validate(project, track, part, this);
                Error |= phoneme.Error;
            }
        }

        public void Phonemize(UProject project, UTrack track) {
            if (track.Singer == null || !track.Singer.Loaded) {
                return;
            }
            if (Extends != null) {
                return;
            }

            List<UNote> notes = new List<UNote> { this };
            while (notes.Last().Next != null && notes.Last().Next.Extends == this) {
                notes.Add(notes.Last().Next);
            }

            int endOffset = 0;
            if (notes.Last().Next != null && notes.Last().Next.phonemes.Count > 0) {
                endOffset = Math.Min(0, notes.Last().Next.position - notes.Last().End + notes.Last().Next.phonemes[0].position);
            }

            var prev = Prev?.ToPhonemizerNote(track);
            var next = notes.Last().Next?.ToPhonemizerNote(track);
            bool prevIsNeighbour = Prev?.End >= position;
            if (Prev?.Extends != null) {
                prev = Prev.Extends.ToPhonemizerNote(track);
                var phoneme = prev.Value;
                phoneme.duration = Prev.ExtendedDuration;
                prev = phoneme;
            }
            var prevs = new List<UNote>();
            if (prevIsNeighbour) {
                prevs.Add(Prev);
                while (prevs.Last().Prev != null && prevs.Last().Extends != null && prevs.Last().Prev.End >= prevs.Last().position) {
                    prevs.Add(prevs.Last().Prev);
                }
                prevs.Reverse();
            }
            var phonemizerPrevs = prevs.Select(n => n.ToPhonemizerNote(track)).ToArray();
            bool nextIsNeighbour = notes.Last().End >= notes.Last().Next?.position;
            track.Phonemizer.SetTiming(project.bpm, project.beatUnit, project.resolution);
            var phonemizerNotes = notes.Select(note => note.ToPhonemizerNote(track)).ToArray();
            phonemizerNotes[phonemizerNotes.Length - 1].duration += endOffset;
            if (string.IsNullOrEmpty(phonemizerNotes[0].lyric) &&
                string.IsNullOrEmpty(phonemizerNotes[0].phoneticHint)) {
                phonemes.Clear();
                return;
            }
            Phonemizer.Result phonemizerResult;
            try {
                phonemizerResult = track.Phonemizer.Process(
                    phonemizerNotes,
                    prev,
                    next,
                    prevIsNeighbour ? prev : null,
                    nextIsNeighbour ? next : null,
                    phonemizerPrevs);
            } catch (Exception e) {
                Log.Error(e, "phonemizer error");
                phonemizerResult = new Phonemizer.Result() {
                    phonemes = new Phonemizer.Phoneme[] {
                        new Phonemizer.Phoneme {
                            phoneme = "error"
                        }
                    }
                };
            }
            var newPhonemes = phonemizerResult.phonemes;
            // Apply overrides.
            for (int i = phonemeOverrides.Count - 1; i >= 0; --i) {
                if (phonemeOverrides[i].IsEmpty) {
                    phonemeOverrides.RemoveAt(i);
                }
            }
            foreach (var o in phonemeOverrides) {
                if (o.index >= 0 && o.index < newPhonemes.Length) {
                    var p = newPhonemes[o.index];
                    if (o.phoneme != null) {
                        p.phoneme = o.phoneme;
                    }
                    if (o.offset != null) {
                        p.position += o.offset.Value;
                    }
                    newPhonemes[o.index] = p;
                }
            }
            // Safety treatment after phonemizer output and phoneme overrides.
            int maxPostion = notes.Last().End - notes.First().position + endOffset - 10;
            for (int i = newPhonemes.Length - 1; i >= 0; --i) {
                var p = newPhonemes[i];
                p.position = Math.Min(p.position, maxPostion);
                newPhonemes[i] = p;
                maxPostion = p.position - 10;
            }
            DistributePhonemes(notes, newPhonemes);
        }

        static List<Phonemizer.PhonemeAttributes> attributesBuffer = new List<Phonemizer.PhonemeAttributes>();
        private Phonemizer.Note ToPhonemizerNote(UTrack track) {
            string lrc = lyric;
            string phoneticHint = null;
            lrc = phoneticHintPattern.Replace(lrc, match => {
                phoneticHint = match.Groups[1].Value;
                return "";
            });
            attributesBuffer.Clear();
            foreach (var exp in phonemeExpressions) {
                if (exp.abbr != "vel" && exp.abbr != "alt" && exp.abbr != "clr" && exp.abbr != "shft") {
                    continue;
                }
                var posInBuffer = attributesBuffer.FindIndex(attr => attr.index == exp.index);
                if (posInBuffer < 0) {
                    posInBuffer = attributesBuffer.Count;
                    attributesBuffer.Add(new Phonemizer.PhonemeAttributes());
                }
                Phonemizer.PhonemeAttributes attr = attributesBuffer[posInBuffer];
                attr.index = exp.index.Value;
                if (exp.abbr == "vel") {
                    attr.consonantStretchRatio = Math.Pow(2, 1.0 - exp.value / 100.0);
                } else if (exp.abbr == "alt") {
                    attr.alternate = (int)exp.value;
                } else if (exp.abbr == "clr" && track.VoiceColorExp != null) {
                    int optionIdx = (int)exp.value;
                    if (optionIdx < track.VoiceColorExp.options.Length && optionIdx >= 0) {
                        attr.voiceColor = track.VoiceColorExp.options[optionIdx];
                    }
                } else if (exp.abbr == "shft") {
                    attr.toneShift = (int)exp.value;
                }
                attributesBuffer[posInBuffer] = attr;
            }
            var attributes = attributesBuffer.ToArray();
            attributesBuffer.Clear();
            return new Phonemizer.Note() {
                lyric = lrc.Trim(),
                phoneticHint = phoneticHint?.Trim(),
                tone = tone,
                position = position,
                duration = duration,
                phonemeAttributes = attributes,
            };
        }

        private void DistributePhonemes(List<UNote> notes, Phonemizer.Phoneme[] phonemes) {
            int endPosition = 0;
            int index = 0;
            foreach (var note in notes) {
                note.PhonemeOffset = index;
                endPosition += note.duration;
                int indexInNote = 0;
                while (index < phonemes.Length && phonemes[index].position < endPosition) {
                    while (note.phonemes.Count - 1 < indexInNote) {
                        note.phonemes.Add(new UPhoneme());
                    }
                    note.phonemes[indexInNote].phoneme = phonemes[index].phoneme;
                    note.phonemes[indexInNote].position = phonemes[index].position - (note.position - notes[0].position);
                    index++;
                    indexInNote++;
                }
                while (note.phonemes.Count > indexInNote) {
                    note.phonemes.RemoveAt(note.phonemes.Count - 1);
                }
            }
        }

        public UPhonemeOverride GetPhonemeOverride(int index) {
            var result = phonemeOverrides.Find(o => o.index == index);
            if (result == null) {
                result = new UPhonemeOverride { index = index };
                phonemeOverrides.Add(result);
            }
            return result;
        }

        /*
        public float GetExpression(UProject project, string abbr) {
            var descriptor = project.expressions[abbr];
            Trace.Assert(descriptor.isNoteExpression);
            var note = Extends ?? this;
            var expression = note.noteExpressions.FirstOrDefault(exp => exp.descriptor == descriptor);
            return expression == null ? expression.value : descriptor.defaultValue;
        }

        public void SetExpression(UProject project, string abbr, float value) {
            var descriptor = project.expressions[abbr];
            Trace.Assert(descriptor.isNoteExpression);
            var note = Extends ?? this;
            if (value == descriptor.defaultValue) {
                note.noteExpressions.RemoveAll(exp => exp.descriptor == descriptor);
                return;
            }
            var expression = note.noteExpressions.FirstOrDefault(exp => exp.descriptor == descriptor);
            if (expression != null) {
                expression.value = value;
            } else {
                note.noteExpressions.Add(new UExpression(descriptor) {
                    descriptor = descriptor,
                    value = value,
                });
            }
        }
        */

        public UNote Clone() {
            return new UNote() {
                position = position,
                duration = duration,
                tone = tone,
                lyric = lyric,
                pitch = pitch.Clone(),
                vibrato = vibrato.Clone(),
                noteExpressions = noteExpressions.Select(exp => exp.Clone()).ToList(),
                phonemeExpressions = phonemeExpressions.Select(exp => exp.Clone()).ToList(),
                phonemeOverrides = phonemeOverrides.Select(o => o.Clone()).ToList(),
            };
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class UVibrato {
        // Vibrato percentage of note length.
        float _length;
        // Period in milliseconds.
        float _period = 175f;
        // Depth in cents (1 semitone = 100 cents).
        float _depth = 25f;
        // Fade-in percentage of vibrato length.
        float _in = 10f;
        // Fade-out percentage of vibrato length.
        float _out = 10f;
        // Shift percentage of period length.
        float _shift;
        float _drift;

        [JsonProperty] public float length { get => _length; set => _length = Math.Max(0, Math.Min(100, value)); }
        [JsonProperty] public float period { get => _period; set => _period = Math.Max(5, Math.Min(500, value)); }
        [JsonProperty] public float depth { get => _depth; set => _depth = Math.Max(5, Math.Min(200, value)); }
        [JsonProperty]
        public float @in {
            get => _in;
            set {
                _in = Math.Max(0, Math.Min(100, value));
                _out = Math.Min(_out, 100 - _in);
            }
        }
        [JsonProperty]
        public float @out {
            get => _out;
            set {
                _out = Math.Max(0, Math.Min(100, value));
                _in = Math.Min(_in, 100 - _out);
            }
        }
        [JsonProperty] public float shift { get => _shift; set => _shift = Math.Max(0, Math.Min(100, value)); }
        [JsonProperty] public float drift { get => _drift; set => _drift = Math.Max(-100, Math.Min(100, value)); }

        [YamlIgnore] public float NormalizedStart => 1f - length / 100f;

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

        /// <summary>
        /// Evaluate a position on the vibrato curve.
        /// </summary>
        /// <param name="nPos">Normalized position in note length.</param>
        /// <returns>Vector2(tick, noteNum)</returns>
        public Vector2 Evaluate(float nPos, float nPeriod, UNote note) {
            float nStart = NormalizedStart;
            float nIn = length / 100f * @in / 100f;
            float nInPos = nStart + nIn;
            float nOut = length / 100f * @out / 100f;
            float nOutPos = 1f - nOut;
            float t = (nPos - nStart) / nPeriod + shift / 100f;
            float y = (float)Math.Sin(2 * Math.PI * t) * depth;
            if (nPos < nStart) {
                y = 0;
            } else if (nPos < nInPos) {
                y *= (nPos - nStart) / nIn;
            } else if (nPos > nOutPos) {
                y *= (1f - nPos) / nOut;
            }
            return new Vector2(note.position + note.duration * nPos, note.tone - 0.5f + y / 100f);
        }

        public Vector2 GetEnvelopeStart(UNote note) {
            return new Vector2(
                note.position + note.duration * NormalizedStart,
                note.tone - 3f);
        }

        public Vector2 GetEnvelopeFadeIn(UNote note) {
            return new Vector2(
                note.position + note.duration * (NormalizedStart + length / 100f * @in / 100f),
                note.tone - 3f + depth / 50f);
        }

        public Vector2 GetEnvelopeFadeOut(UNote note) {
            return new Vector2(
                note.position + note.duration * (1f - length / 100f * @out / 100f),
                note.tone - 3f + depth / 50f);
        }

        public Vector2 GetEnvelopeEnd(UNote note) {
            return new Vector2(
                note.position + note.duration,
                note.tone - 3f);
        }

        public Vector2 GetToggle(UNote note) {
            return new Vector2(note.position + note.duration, note.tone - 1.5f);
        }

        public void GetPeriodStartEnd(UNote note, UProject project, out Vector2 start, out Vector2 end) {
            float periodTick = project.MillisecondToTick(period);
            float shiftTick = periodTick * shift / 100f;
            start = new Vector2(
                note.position + note.duration * NormalizedStart + shiftTick,
                note.tone - 3.5f);
            end = new Vector2(
                note.position + note.duration * NormalizedStart + shiftTick + periodTick,
                note.tone - 3.5f);
        }

        public float ToneToDepth(UNote note, float tone) {
            return (tone - (note.tone - 3f)) * 50f;
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

        public PitchPoint() { }

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
            var result = new UPitch() {
                data = data.Select(p => p.Clone()).ToList(),
                snapFirst = snapFirst,
            };
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
