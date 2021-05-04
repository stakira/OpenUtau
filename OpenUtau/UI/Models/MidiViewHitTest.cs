using System;
using System.Windows;

using OpenUtau.Core;
using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Models {
    public class PitchPointHitInfo {
        public UNote Note;
        public int Index;
        public bool OnPoint;
        public double X;
        public double Y;
    }

    public struct NoteHitInfo {
        public UNote note;
        public bool hitBody;
        public bool hitResizeArea;
        public bool hitVibrato;
        public bool hitX;
    }

    class MidiViewHitTest {
        MidiViewModel midiVM;
        UProject Project => DocManager.Inst.Project;

        public MidiViewHitTest(MidiViewModel midiVM) {
            this.midiVM = midiVM;
        }

        public NoteHitInfo HitTestNote(Point mousePos) {
            NoteHitInfo result = default;
            int tick = (int)(midiVM.CanvasToQuarter(mousePos.X) * Project.Resolution);
            foreach (UNote note in midiVM.Part.Notes) {
                if (note.PosTick <= tick && note.EndTick >= tick) {
                    result.note = note;
                    result.hitX = true;
                    var noteNum = midiVM.CanvasToNoteNum(mousePos.Y);
                    if (noteNum == note.NoteNum) {
                        result.hitVibrato = false;
                        result.hitBody = true;
                        double x = midiVM.QuarterToCanvas((double)note.EndTick / Project.Resolution);
                        result.hitResizeArea = mousePos.X <= x && mousePos.X > x - UIConstants.ResizeMargin;
                        break;
                    } else if (noteNum == note.NoteNum - 1) {
                        result.hitVibrato = true;
                    }
                }
            }
            return result;
        }

        public PitchPointHitInfo HitTestPitchPoint(Point mousePos) {
            foreach (var note in midiVM.Part.Notes) {
                if (midiVM.NoteIsInView(note)) // FIXME this is not enough
                {
                    if (note.Error) continue;
                    double lastX = 0, lastY = 0;
                    PitchPointShape lastShape = PitchPointShape.l;
                    for (int i = 0; i < note.PitchBend.Points.Count; i++) {
                        var pit = note.PitchBend.Points[i];
                        int posTick = note.PosTick + Project.MillisecondToTick(pit.X);
                        double noteNum = note.NoteNum + pit.Y / 10;
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
                                double msX = DocManager.Inst.Project.TickToMillisecond(midiVM.CanvasToQuarter(mousePos.X) * DocManager.Inst.Project.Resolution - note.PosTick);
                                double msY = (midiVM.CanvasToPitch(mousePos.Y) - note.NoteNum) * 10;
                                return (new PitchPointHitInfo() { Note = note, Index = i - 1, OnPoint = false, X = msX, Y = msY });
                            } else break;
                        }
                        lastX = x;
                        lastY = y;
                        lastShape = pit.Shape;
                    }
                }
            }
            return null;
        }
    }
}
