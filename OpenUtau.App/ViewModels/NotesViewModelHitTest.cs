using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using Avalonia;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.ViewModels {
    public struct NoteHitInfo {
        public UNote note;
        public UPhoneme phoneme;
        public bool hitBody;
        public bool hitResizeArea;
        public bool hitX;
    }

    public struct PitchPointHitInfo {
        public UNote Note;
        public int Index;
        public bool OnPoint;
        public float X;
        public float Y;
        public bool IsFirst { get; set; }
        public bool CanDel { get; set; }
        public bool CanAdd { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> EaseInOutCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> LinearCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> EaseInCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> EaseOutCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> SnapCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> DelCommand { get; set; }
        public ReactiveCommand<PitchPointHitInfo, Unit> AddCommand { get; set; }
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

    class NotesViewModelHitTest {
        private readonly NotesViewModel viewModel;

        public NotesViewModelHitTest(NotesViewModel viewModel) {
            this.viewModel = viewModel;
        }

        public NoteHitInfo HitTestNote(Point point) {
            if (viewModel.Part == null) {
                return default;
            }
            NoteHitInfo result = default;
            int tick = viewModel.PointToTick(point);
            foreach (UNote note in viewModel.Part.notes) {
                if (note.position > tick || note.End < tick) {
                    continue;
                }
                result.note = note;
                result.hitX = true;
                var tone = viewModel.PointToTone(point);
                if (tone == note.tone) {
                    result.hitBody = true;
                    double x = viewModel.TickToneToPoint(note.End, tone).X;
                    result.hitResizeArea = point.X <= x && point.X > x - ViewConstants.ResizeMargin;
                    break;
                }
            }
            return result;
        }

        public NoteHitInfo HitTestExp(Point point) {
            if (viewModel.Part == null) {
                return default;
            }
            int tick = viewModel.PointToTick(point);
            foreach (UNote note in viewModel.Part.notes) {
                if (note.LeftBound > tick || note.RightBound < tick) {
                    continue;
                }
                foreach (var phoneme in note.phonemes) {
                    int left = note.position + phoneme.position;
                    int right = note.position + phoneme.position + phoneme.Duration;
                    if (left <= tick && tick <= right) {
                        return new NoteHitInfo {
                            note = note,
                            phoneme = phoneme,
                            hitX = true,
                        };
                    }
                }
            }
            return default;
        }

        public List<NoteHitInfo> HitTestExpRange(Point point1, Point point2) {
            var hits = new List<NoteHitInfo>();
            if (viewModel.Part == null) {
                return hits;
            }
            int tick1 = viewModel.PointToTick(point1);
            int tick2 = viewModel.PointToTick(point2);
            foreach (UNote note in viewModel.Part.notes) {
                if (note.LeftBound > tick2 || note.RightBound < tick1) {
                    continue;
                }
                foreach (var phoneme in note.phonemes) {
                    int left = note.position + phoneme.position;
                    int right = note.position + phoneme.position + phoneme.Duration;
                    if (left <= tick2 && tick1 <= right) {
                        hits.Add(new NoteHitInfo {
                            note = note,
                            phoneme = phoneme,
                            hitX = true,
                        });
                    }
                }
            }
            return hits;
        }

        public PitchPointHitInfo HitTestPitchPoint(Point point) {
            if (viewModel.Part == null || !viewModel.ShowPitch) {
                return default;
            }
            double leftTick = viewModel.TickOffset - 480;
            double rightTick = leftTick + viewModel.ViewportTicks + 480;
            foreach (var note in viewModel.Part.notes) {
                if (note.LeftBound >= rightTick || note.RightBound <= leftTick || note.Error) {
                    continue;
                }
                double lastX = 0, lastY = 0;
                PitchPointShape lastShape = PitchPointShape.l;
                for (int i = 0; i < note.pitch.data.Count; i++) {
                    var pit = note.pitch.data[i];
                    int posTick = note.position + viewModel.Project.MillisecondToTick(pit.X);
                    double tone = note.tone + pit.Y / 10;
                    var pitPoint = viewModel.TickToneToPoint(posTick, tone);
                    double x = pitPoint.X;
                    double y = pitPoint.Y + viewModel.TrackHeight / 2;
                    if (Math.Abs(point.X - x) < 4 && Math.Abs(point.Y - y) < 4)
                        return new PitchPointHitInfo() {
                            Note = note,
                            Index = i,
                            OnPoint = true,
                        };
                    else if (point.X < x && i > 0 && point.X > lastX) {
                        // Hit test curve
                        double castY = MusicMath.InterpolateShape(lastX, x, lastY, y, point.X, lastShape) - point.Y;
                        if (y >= lastY) {
                            if (point.Y - y > 3 || lastY - point.Y > 3) break;
                        } else {
                            if (y - point.Y > 3 || point.Y - lastY > 3) break;
                        }
                        double castX = MusicMath.InterpolateShapeX(lastX, x, lastY, y, point.Y, lastShape) - point.X;
                        double dis = double.IsNaN(castX) ? Math.Abs(castY) : Math.Cos(Math.Atan2(Math.Abs(castY), Math.Abs(castX))) * Math.Abs(castY);
                        if (dis < 3) {
                            double msX = viewModel.Project.TickToMillisecond(viewModel.PointToTick(point) - note.position);
                            double decCentY = (viewModel.PointToToneDouble(point) - note.tone) * 10;
                            return new PitchPointHitInfo() {
                                Note = note,
                                Index = i - 1,
                                OnPoint = false,
                                X = (float)msX,
                                Y = (float)decCentY,
                            };
                        } else break;
                    }
                    lastX = x;
                    lastY = y;
                    lastShape = pit.shape;
                }
            }
            return default;
        }

        public VibratoHitInfo HitTestVibrato(Point mousePos) {
            if (viewModel.Part == null || !viewModel.ShowVibrato) {
                return default;
            }
            VibratoHitInfo result = default;
            result.point = mousePos;
            foreach (var note in viewModel.Part.notes) {
                result.note = note;
                UVibrato vibrato = note.vibrato;
                Point toggle = viewModel.TickToneToPoint(vibrato.GetToggle(note));
                toggle = toggle.WithX(toggle.X - 10);
                if (WithIn(toggle, mousePos, 5)) {
                    result.hit = true;
                    result.hitToggle = true;
                    return result;
                }
                if (vibrato.length == 0) {
                    continue;
                }
                Point start = viewModel.TickToneToPoint(vibrato.GetEnvelopeStart(note));
                Point fadeIn = viewModel.TickToneToPoint(vibrato.GetEnvelopeFadeIn(note));
                Point fadeOut = viewModel.TickToneToPoint(vibrato.GetEnvelopeFadeOut(note));
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
                Point periodStart = viewModel.TickToneToPoint(periodStartPos);
                Point periodEnd = viewModel.TickToneToPoint(periodEndPos);
                if (Math.Abs(mousePos.Y - periodEnd.Y) < viewModel.TrackHeight / 6) {
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
            if (viewModel.Part == null || !viewModel.ShowPhoneme) {
                return default;
            }
            var project = viewModel.Project;
            PhonemeHitInfo result = default;
            result.point = mousePos;
            double leftTick = viewModel.TickOffset - 480;
            double rightTick = leftTick + viewModel.ViewportTicks + 480;
            foreach (var note in viewModel.Part.notes) {
                if (note.LeftBound >= rightTick || note.RightBound <= leftTick || note.Error) {
                    continue;
                }
                if (note.OverlapError) {
                    continue;
                }
                foreach (var phoneme in note.phonemes) {
                    if (phoneme.Error) {
                        continue;
                    }
                    int p0Tick = phoneme.Parent.position + phoneme.position + project.MillisecondToTick(phoneme.envelope.data[0].X);
                    double p0x = viewModel.TickToneToPoint(p0Tick, 0).X;
                    var point = new Point(p0x, 48 - phoneme.envelope.data[0].Y * 0.24 - 1);
                    if (WithIn(point, mousePos, 3)) {
                        result.phoneme = phoneme;
                        result.hit = true;
                        result.hitPreutter = true;
                        return result;
                    }
                    int p1Tick = phoneme.Parent.position + phoneme.position + viewModel.Project.MillisecondToTick(phoneme.envelope.data[1].X);
                    double p1x = viewModel.TickToneToPoint(p1Tick, 0).X;
                    point = new Point(p1x, 48 - phoneme.envelope.data[1].Y * 0.24);
                    if (WithIn(point, mousePos, 3)) {
                        result.phoneme = phoneme;
                        result.hit = true;
                        result.hitOverlap = true;
                        return result;
                    }
                    point = viewModel.TickToneToPoint(phoneme.Parent.position + phoneme.position, 0);
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

        private bool WithIn(Point p0, Point p1, double dist) {
            return Math.Abs(p0.X - p1.X) < dist && Math.Abs(p0.Y - p1.Y) < dist;
        }
    }
}
