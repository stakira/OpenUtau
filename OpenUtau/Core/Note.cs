using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core
{
    public class Note
    {
        public const double minLength = 4.0 / 64;  // Actual minimal possible note length is 1/64 note

        public int keyNo;
        public double offset;
        public double length = 1;
        public string lyric = "a";
        public OpenUtau.UI.Controls.NoteControl noteControl;

        public bool selected = false;
        private bool selected_last = false;

        public Note()
        {
            noteControl = new OpenUtau.UI.Controls.NoteControl();
            noteControl.note = this;
        }

        public void updateGraphics(OpenUtau.UI.Models.NotesCanvasModel ncModel)
        {
            noteControl.Height = ncModel.noteHeight - 2;
            noteControl.Width = Math.Max(2, Math.Round(length * ncModel.noteWidth) - 3);
            System.Windows.Controls.Canvas.SetLeft(noteControl, Math.Round(ncModel.offsetToCanvas(offset)) + 2);
            System.Windows.Controls.Canvas.SetTop(noteControl, Math.Round(ncModel.keyToCanvas(keyNo)) + 1);
            if (selected && !selected_last)
                noteControl.SetSelected();
            else if (!selected && selected_last)
                noteControl.SetUnselected();
            selected_last = selected;
        }

        public double getEndOffset() { return offset + length; }
    }
}
