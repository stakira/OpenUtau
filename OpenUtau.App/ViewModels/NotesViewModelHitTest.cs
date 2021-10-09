using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

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

    }
}
