using System;
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
        public bool hitBody;
        public bool hitResizeArea;
        public bool hitX;
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
                    if (noteNum == note.noteNum) {
                        result.hitBody = true;
                        double x = midiVM.QuarterToCanvas((double)note.End / Project.resolution);
                        result.hitResizeArea = mousePos.X <= x && mousePos.X > x - UIConstants.ResizeMargin;
                        break;
                    }
                }
            }
            return result;
        }

        public PitchPointHitInfo HitTestPitchPoint(Point mousePos) {
            foreach (var note in midiVM.Part.notes) {
                if (midiVM.NoteIsInView(note)) // FIXME this is not enough
                {
                    if (note.Error) continue;
                    double lastX = 0, lastY = 0;
                    PitchPointShape lastShape = PitchPointShape.l;
                    for (int i = 0; i < note.pitch.data.Count; i++) {
                        var pit = note.pitch.data[i];
                        int posTick = note.position + Project.MillisecondToTick(pit.X);
                        double noteNum = note.noteNum + pit.Y / 10;
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
                                double msY = (midiVM.CanvasToPitch(mousePos.Y) - note.noteNum) * 10;
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
    }
}
