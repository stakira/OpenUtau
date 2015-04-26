using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using OpenUtau.Core;
using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Controls
{
    public class PhonemeElement : ExpElement
    {
        public OpenUtau.UI.Models.MidiViewModel midiVM;
        
        Pen pen;

        public PhonemeElement() { pen = new Pen(Brushes.MediumPurple, 1); pen.Freeze(); }

        public override void RedrawIfUpdated()
        {
            //if (!_updated) return;
            DrawingContext cxt = visual.RenderOpen();
            if (Part != null)
            {
                foreach (var note in Part.Notes)
                {
                    if (!midiVM.NoteIsInView(note)) continue;
                    double y = midiVM.TrackHeight * ((double)UIConstants.MaxNoteNum - 1 - note.NoteNum) - midiVM.OffsetY + 1.5;
                    for (int i = 0; i < note.Phonemes.Count; i++)
                    {
                        var phoneme = note.Phonemes[i];
                        double trackHeight = midiVM.TrackHeight - 2;
                        double x0 = (note.PosTick + MusicMath.TimeToTick(phoneme.Envelope.Points[0].X - phoneme.PreUtter, DocManager.Inst.Project.Timing))
                             * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
                        double y0 = (1 - phoneme.Envelope.Points[0].Y / 100) * trackHeight;
                        double x1 = (note.PosTick + MusicMath.TimeToTick(phoneme.Envelope.Points[1].X - phoneme.PreUtter, DocManager.Inst.Project.Timing))
                             * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
                        double y1 = (1 - phoneme.Envelope.Points[1].Y / 100) * trackHeight;
                        double x2 = (note.PosTick + MusicMath.TimeToTick(phoneme.Envelope.Points[2].X - phoneme.PreUtter, DocManager.Inst.Project.Timing))
                             * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
                        double y2 = (1 - phoneme.Envelope.Points[2].Y / 100) * trackHeight;
                        double x3 = (note.PosTick + MusicMath.TimeToTick(phoneme.Envelope.Points[3].X - phoneme.PreUtter, DocManager.Inst.Project.Timing))
                             * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
                        double y3 = (1 - phoneme.Envelope.Points[3].Y / 100) * trackHeight;
                        double x4 = (note.PosTick + MusicMath.TimeToTick(phoneme.Envelope.Points[4].X - phoneme.PreUtter, DocManager.Inst.Project.Timing))
                             * midiVM.QuarterWidth / DocManager.Inst.Project.Resolution;
                        double y4 = (1 - phoneme.Envelope.Points[4].Y / 100) * trackHeight;
                        cxt.DrawLine(pen, new Point(x0, y + y0), new Point(x1, y + y1));
                        cxt.DrawLine(pen, new Point(x1, y + y1), new Point(x2, y + y2));
                        cxt.DrawLine(pen, new Point(x2, y + y2), new Point(x3, y + y3));
                        cxt.DrawLine(pen, new Point(x3, y + y3), new Point(x4, y + y4));
                    }
                }
            }
            else
            {
                cxt.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(0, 0), new Point(100, 100)));
            }
            cxt.Close();
            _updated = false;
        }
    }
}
