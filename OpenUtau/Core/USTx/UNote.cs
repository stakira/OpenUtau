using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public class UNote : IComparable
    {
        public const double minLength = 4.0 / 64;  // Actual minimal possible note length is 1/64 note

        public int PosTick;
        public int DurTick;
        public int NoteNum;
        public int Velocity;
        // public string Lyric;
        public string Phoneme;

        // TODO : remove old attributes
        public int keyNo;
        public double offset;
        public double length = 1;
        public string _lyric = "a";
        public OpenUtau.UI.Controls.NoteControl noteControl;

        int _channel = 0;
        bool _error = false;
        bool _selected = false;

        public int Channel
        {
            set { _channel = value; noteControl.Channel = value; }
            get { return _channel; }
        }

        public bool Error
        {
            set { _error = value; noteControl.Error = value; }
            get { return _error; }
        }

        public bool Selected
        {
            set { _selected = value; noteControl.Selected = value; }
            get { return _selected; }
        }

        public string Lyric
        {
            set { _lyric = value; noteControl.Lyric = value; }
            get { return _lyric; }
        }

        public UNote()
        {
            noteControl = new OpenUtau.UI.Controls.NoteControl();
            noteControl.note = this;
            noteControl.Channel = Channel;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            UNote other = obj as UNote;
            if (other == null)
                throw new ArgumentException("CompareTo object is not a Note");

            if (other.Channel < this.Channel)
                return 1;
            else if (other.Channel > this.Channel)
                return -1;
            else if (other.offset < this.offset)
                return 1;
            else if (other.offset > this.offset)
                return -1;
            else if (other.GetHashCode() < this.GetHashCode())
                return 1;
            else if (other.GetHashCode() > this.GetHashCode())
                return -1;
            else
                return 0;
        }

        public void updateGraphics(OpenUtau.UI.Models.NotesCanvasModel ncModel)
        {
            if (ncModel.noteInView(this)) {
                noteControl.Height = ncModel.noteHeight - 2;
                noteControl.Width = Math.Max(2, Math.Round(length * ncModel.noteWidth) - 1);
                System.Windows.Controls.Canvas.SetLeft(noteControl, Math.Round(ncModel.offsetToCanvas(offset)) + 1);
                System.Windows.Controls.Canvas.SetTop(noteControl, Math.Round(ncModel.keyToCanvas(keyNo)) + 1);
                this.noteControl.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                System.Windows.Controls.Canvas.SetLeft(noteControl, Math.Round(ncModel.offsetToCanvas(offset)) + 1);
                System.Windows.Controls.Canvas.SetTop(noteControl, Math.Round(ncModel.keyToCanvas(keyNo)) + 1);
                this.noteControl.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        public double getEndOffset() { return offset + length; }
    }
}
