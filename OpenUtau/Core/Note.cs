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

        private int _channel = 0;
        private bool _selected = false;

        public int Channel
        {
            set
            {
                _channel = value;
                noteControl.Channel = _channel;
            }
            get
            {
                return _channel;
            }
        }

        public bool Selected
        {
            set
            {
                if (value && !_selected)
                {
                    noteControl.SetSelected();
                }
                else if (!value && _selected)
                {
                    noteControl.SetUnselected();
                }

                _selected = value;
            }

            get
            {
                return _selected;
            }
        }

        public Note()
        {
            noteControl = new OpenUtau.UI.Controls.NoteControl();
            noteControl.note = this;
            noteControl.Channel = Channel;
        }

        public void updateGraphics(OpenUtau.UI.Models.NotesCanvasModel ncModel)
        {
            noteControl.Height = ncModel.noteHeight - 2;
            noteControl.Width = Math.Max(2, Math.Round(length * ncModel.noteWidth) - 3);
            System.Windows.Controls.Canvas.SetLeft(noteControl, Math.Round(ncModel.offsetToCanvas(offset)) + 2);
            System.Windows.Controls.Canvas.SetTop(noteControl, Math.Round(ncModel.keyToCanvas(keyNo)) + 1);
        }

        public double getEndOffset() { return offset + length; }

        public string ToUstx() { return ""; }
        public string ToUst() { return ""; }
        public string ToVsqx() { return ""; }
        public string ToVsq() { return ""; }
    }
}
