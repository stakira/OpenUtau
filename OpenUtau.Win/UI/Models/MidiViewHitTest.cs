using System;
using System.Numerics;
using System.Windows;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.UI.Models {
    public class PitchPointHitInfo {
        public UNote Note;
        public int Index;
        public bool OnPoint;
        public float X;
        public float Y;
    }

    public struct NoteHitInfo {
        public UNote note;
        public UPhoneme phoneme;
        public bool hitBody;
        public bool hitResizeArea;
        public bool hitX;
    }

    public struct VibratoHitInfo {
        public UNote note;
        public bool hit;
        public bool hitToggle;
        public bool hitStart;
        public bool hitIn;
        public bool hitOut;
        public bool hitDepth;
        public bool hitShift;
        public bool hitPeriod;
        public Point point;
        public float initialShift;
    }

    public struct PhonemeHitInfo {
        public UPhoneme phoneme;
        public bool hit;
        public bool hitPosition;
        public bool hitPreutter;
        public bool hitOverlap;
        public Point point;
    }

    class MidiViewHitTest {
        readonly MidiViewModel midiVM;
        UProject Project => DocManager.Inst.Project;

        public MidiViewHitTest(MidiViewModel midiVM) {
            this.midiVM = midiVM;
        }

        public NoteHitInfo HitTestNote(Point mousePos) {
            NoteHitInfo result = default;
            int tick = (int)(midiVM.CanvasToQuarter(mousePos.X) * Project.resolution);
            foreach (UNote note in midiVM.Part.notes) {
                if (note.position <= tick && note.End >= tick) {
                    result.note = note;
                    result.hitX = true;
                    var noteNum = midiVM.CanvasToNoteNum(mousePos.Y);
                    if (noteNum == note.tone) {
                        result.hitBody = true;
                        double x = midiVM.QuarterToCanvas((double)note.End / Project.resolution);
                        result.hitResizeArea = mousePos.X <= x && mousePos.X > x - UIConstants.ResizeMargin;
                        break;
                    }
                }
            }
            return result;
        }

        public NoteHitInfo HitTestExp(Point mousePos) {
            NoteHitInfo result = default;
            int tick = (int)(midiVM.CanvasToQuarter(mousePos.X) * Project.resolution);
            foreach (UNote note in midiVM.Part.notes) {
                foreach (var phoneme in note.phonemes) {
                    int left = note.position + phoneme.position;
                    int right = note.position + phoneme.position + phoneme.Duration;
                    if (left <= tick && tick <= right) {
                        result.note = note;
                        result.phoneme = phoneme;
                        result.hitX = true;
                        return result;
                    }
                }
            }
            return result;
        }

        public PitchPointHitInfo HitTestPitchPoint(Point mousePos) {
            if (!midiVM.ShowPitch) {
                return null;
            }
            foreach (var note in midiVM.Part.notes) {
                // FIXME pitch point maybe in view while note is not.
                if (midiVM.NoteIsInView(note) && !note.Error) {
                    double lastX = 0, lastY = 0;
                    PitchPointShape lastShape = PitchPointShape.l;
                    for (int i = 0; i < note.pitch.data.Count; i++) {
                        var pit = note.pitch.data[i];
                        int posTick = note.position + Project.MillisecondToTick(pit.X);
                        double noteNum = note.tone + pit.Y / 10;
                        double x = midiVM.TickToCanvas(posTick);
                        double y = midiVM.NoteNumToCanvas(noteNum) + midiVM.TrackHeight / 2;
                        if (Math.Abs(mousePos.X - x) < 4 && Math.Abs(mousePos.Y - y) < 4)
                            return new PitchPointHitInfo() { Note = note, Index = i, OnPoint = true };
                        else if (mousePos.X < x && i > 0 && mousePos.X > lastX) {
                            // Hit test curve
                            double castY = MusicMath.InterpolateShape(lastX, x, lastY, y, mousePos.X, lastShape) - mousePos.Y;
                            if (y >= lastY) {
                                if (mousePos.Y - y > 3 || lastY - mousePos.Y > 3) break;
                            } else {
                                if (y - mousePos.Y > 3 || mousePos.Y - lastY > 3) break;
                            }
                            double castX = MusicMath.InterpolateShapeX(lastX, x, lastY, y, mousePos.Y, lastShape) - mousePos.X;
                            double dis = double.IsNaN(castX) ? Math.Abs(castY) : Math.Cos(Math.Atan2(Math.Abs(castY), Math.Abs(castX))) * Math.Abs(castY);
                            if (dis < 3) {
                                double msX = DocManager.Inst.Project.TickToMillisecond(midiVM.CanvasToQuarter(mousePos.X) * DocManager.Inst.Project.resolution - note.position);
                                double msY = (midiVM.CanvasToPitch(mousePos.Y) - note.tone) * 10;
                                return new PitchPointHitInfo() { Note = note, Index = i - 1, OnPoint = false, X = (float)msX, Y = (float)msY };
                            } else break;
                        }
                        lastX = x;
                        lastY = y;
                        lastShape = pit.shape;
                    }
                }
            }
            return null;
        }

        public VibratoHitInfo HitTestVibrato(Point mousePos) {
            VibratoHitInfo result = default;
            result.point = mousePos;
            foreach (var note in midiVM.Part.notes) {
                result.note = note;
                UVibrato vibrato = note.vibrato;
                Point toggle = midiVM.TickToneToCanvas(vibrato.GetToggle(note));
                toggle.X -= 10;
                if (WithIn(toggle, mousePos, 5)) {
                    result.hit = true;
                    result.hitToggle = true;
                    return result;
                }
                if (vibrato.length == 0) {
                    continue;
                }
                Point start = midiVM.TickToneToCanvas(vibrato.GetEnvelopeStart(note));
                Point fadeIn = midiVM.TickToneToCanvas(vibrato.GetEnvelopeFadeIn(note));
                Point fadeOut = midiVM.TickToneToCanvas(vibrato.GetEnvelopeFadeOut(note));
                if (WithIn(start, mousePos, 3)) {
                    result.hit = true;
                    result.hitStart = true;
                } else if (WithIn(fadeIn, mousePos, 3)) {
                    result.hit = true;
                    result.hitIn = true;
                } else if (WithIn(fadeOut, mousePos, 3)) {
                    result.hit = true;
                    result.hitOut = true;
                } else if (Math.Abs(fadeIn.Y - mousePos.Y) < 3 && fadeIn.X < mousePos.X && mousePos.X < fadeOut.X) {
                    result.hit = true;
                    result.hitDepth = true;
                }

                vibrato.GetPeriodStartEnd(note, DocManager.Inst.Project, out var periodStartPos, out var periodEndPos);
                Point periodStart = midiVM.TickToneToCanvas(periodStartPos);
                Point periodEnd = midiVM.TickToneToCanvas(periodEndPos);
                if (Math.Abs(mousePos.Y - periodEnd.Y) < midiVM.TrackHeight / 6) {
                    if (Math.Abs(mousePos.X - periodEnd.X) < 3) {
                        result.hit = true;
                        result.hitPeriod = true;
                    } else if (mousePos.X > periodStart.X && mousePos.X < periodEnd.X) {
                        result.hit = true;
                        result.hitShift = true;
                        result.initialShift = vibrato.shift;
                    }
                }
                if (result.hit) {
                    return result;
                }
            }
            return default;
        }

        public PhonemeHitInfo HitTestPhoneme(Point mousePos) {
            PhonemeHitInfo result = default;
            result.point = mousePos;
            foreach (var note in midiVM.Part.notes) {
                if (note.OverlapError) {
                    continue;
                }
                foreach (var phoneme in note.phonemes) {
                    if (phoneme.Error) {
                        continue;
                    }
                    int p0x = midiVM.Project.MillisecondToTick(phoneme.envelope.data[0].X);
                    var point = new Point(midiVM.TickToCanvas(phoneme.Parent.position + phoneme.position + p0x), 48 - phoneme.envelope.data[0].Y * 0.24 - 1);
                    if (WithIn(point, mousePos, 3)) {
                        result.phoneme = phoneme;
                        result.hit = true;
                        result.hitPreutter = true;
                        return result;
                    }
                    int p1x = midiVM.Project.MillisecondToTick(phoneme.envelope.data[1].X);
                    point = new Point(midiVM.TickToCanvas(phoneme.Parent.position + phoneme.position + p1x), 48 - phoneme.envelope.data[1].Y * 0.24);
                    if (WithIn(point, mousePos, 3)) {
                        result.phoneme = phoneme;
                        result.hit = true;
                        result.hitOverlap = true;
                        return result;
                    }
                    point = new Point(midiVM.TickToCanvas(phoneme.Parent.position + phoneme.position), 0);
                    if (Math.Abs(point.X - mousePos.X) < 3) {
                        result.phoneme = phoneme;
                        result.hit = true;
                        result.hitPosition = true;
                        return result;
                    }
                }
            }
            return result;
        }

        bool WithIn(Point p0, Point p1, double dist) {
            return Math.Abs(p0.X - p1.X) < dist && Math.Abs(p0.Y - p1.Y) < dist;
        }
    }
}
