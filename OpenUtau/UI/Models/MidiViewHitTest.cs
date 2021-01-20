using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using OpenUtau.Core;
using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Models
{
    public class PitchPointHitTestResult
    {
        public UNote Note;
        public int Index;
        public bool OnPoint;
        public double X;
        public double Y;
    }

    class MidiViewHitTest
    {
        MidiViewModel midiVM;
        UProject Project { get { return DocManager.Inst.Project; } }

        public MidiViewHitTest(MidiViewModel midiVM) { this.midiVM = midiVM; }

        public UNote HitTestNoteX(double x)
        {
            int tick = (int)(midiVM.CanvasToQuarter(x) * Project.Resolution);
            foreach (UNote note in midiVM.Part.Notes)
                if (note.PosTick <= tick && note.EndTick >= tick) return note;
            return null;
        }

        public UNote HitTestNote(Point mousePos)
        {
            int tick = (int)(midiVM.CanvasToQuarter(mousePos.X) * Project.Resolution);
            int noteNum = midiVM.CanvasToNoteNum(mousePos.Y);
            foreach (UNote note in midiVM.Part.Notes)
                if (note.PosTick <= tick && note.EndTick >= tick && note.NoteNum == noteNum) return note;
            return null;
        }

        public bool HitNoteResizeArea(UNote note, Point mousePos)
        {
            double x = midiVM.QuarterToCanvas((double)note.EndTick / Project.Resolution);
            return mousePos.X <= x && mousePos.X > x - UIConstants.ResizeMargin;
        }

        public UNote HitTestVibrato(Point mousePos)
        {
            int tick = (int)(midiVM.CanvasToQuarter(mousePos.X) * Project.Resolution);
            double pitch = midiVM.CanvasToPitch(mousePos.Y);
            foreach (UNote note in midiVM.Part.Notes)
                if (note.PosTick + note.DurTick * (1 - note.Vibrato.Length / 100) <= tick && note.EndTick >= tick &&
                    Math.Abs(note.NoteNum - pitch) < note.Vibrato.Depth / 100) return note;
            return null;
        }

        public PitchPointHitTestResult HitTestPitchPoint(Point mousePos)
        {
            foreach (var note in midiVM.Part.Notes)
            {
                if (midiVM.NoteIsInView(note)) // FIXME this is not enough
                {
                    if (note.Error) continue;
                    double lastX = 0, lastY = 0;
                    PitchPointShape lastShape = PitchPointShape.l;
                    for (int i = 0; i < note.PitchBend.Points.Count; i++)
                    {
                        var pit = note.PitchBend.Points[i];
                        int posTick = note.PosTick + Project.MillisecondToTick(pit.X);
                        double noteNum = note.NoteNum + pit.Y / 10;
                        double x = midiVM.TickToCanvas(posTick);
                        double y = midiVM.NoteNumToCanvas(noteNum) + midiVM.TrackHeight / 2;
                        if (Math.Abs(mousePos.X - x) < 4 && Math.Abs(mousePos.Y - y) < 4)
                            return new PitchPointHitTestResult() { Note = note, Index = i, OnPoint = true };
                        else if (mousePos.X < x && i > 0 && mousePos.X > lastX)
                        {
                            // Hit test curve
                            var lastPit = note.PitchBend.Points[i - 1];
                            double castY = MusicMath.InterpolateShape(lastX, x, lastY, y, mousePos.X, lastShape) - mousePos.Y;
                            if (y >= lastY)
                            {
                                if (mousePos.Y - y > 3 || lastY - mousePos.Y > 3) break;
                            }
                            else
                            {
                                if (y - mousePos.Y > 3 || mousePos.Y - lastY > 3) break;
                            }
                            double castX = MusicMath.InterpolateShapeX(lastX, x, lastY, y, mousePos.Y, lastShape) - mousePos.X;
                            double dis = double.IsNaN(castX) ? Math.Abs(castY) : Math.Cos(Math.Atan2(Math.Abs(castY), Math.Abs(castX))) * Math.Abs(castY);
                            if (dis < 3)
                            {
                                double msX = DocManager.Inst.Project.TickToMillisecond(midiVM.CanvasToQuarter(mousePos.X) * DocManager.Inst.Project.Resolution - note.PosTick);
                                double msY = (midiVM.CanvasToPitch(mousePos.Y) - note.NoteNum) * 10;
                                return (new PitchPointHitTestResult() { Note = note, Index = i - 1, OnPoint = false, X = msX, Y = msY });
                            }
                            else break;
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
