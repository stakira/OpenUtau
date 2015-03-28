using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core
{
    public class TrackPart
    {
        public List<Note> noteList = new List<Note>();
        public HashSet<Note> selectedNotes = new HashSet<Note>();
        public HashSet<Note> tempSelectedNotes = new HashSet<Note>();
        OpenUtau.UI.Models.NotesCanvasModel ncModel;

        public TrackPart(OpenUtau.UI.Models.NotesCanvasModel ncModel)
        {
            this.ncModel = ncModel;
        }

        public class NoteComparer : IComparer<Note>
        {
            public int Compare(Note lhs, Note rhs)
            {
                if (lhs.offset < rhs.offset) return -1;
                else if (lhs.offset > rhs.offset) return 1;
                else if (lhs.keyNo < rhs.keyNo) return -1;
                else if (lhs.keyNo > rhs.keyNo) return 1;
                else return 0;
            }
        }

        static NoteComparer noteComparer;

        public void AddNote(Note note)
        {
            noteList.Add(note);
            ncModel.notesCanvas.Children.Add(note.noteControl);
            note.updateGraphics(ncModel);
        }

        public bool RemoveNote(Note note)
        {
            bool success = noteList.Remove(note);
            if (!success)
                throw new Exception("Note does not exist, cannot be removed");
            DeselectNote(note);
            ncModel.notesCanvas.Children.Remove(note.noteControl);
            note.noteControl.note = null; // Break reference loop
            return success;
        }

        public void SortNote()
        {
            if (noteComparer == null) noteComparer = new NoteComparer();
            noteList.Sort(noteComparer);
        }

        public void PrintNotes()
        {
            System.Diagnostics.Debug.WriteLine(noteList.Count.ToString() + " Notes in Total");
            foreach (Note note in noteList)
            {
                System.Diagnostics.Debug.WriteLine("Note : " + note.offset.ToString() + " " + note.keyNo.ToString());
            }
            foreach (Note note in selectedNotes)
            {
                System.Diagnostics.Debug.WriteLine("Selected Note : " + note.offset.ToString() + " " + note.keyNo.ToString());
            }
        }

        public void UpdateGraphics()
        {
            foreach (Note note in noteList)
            {
                note.updateGraphics(ncModel);
            }
        }

        # region Selection related functions

        public void SelectNote(Note note)
        {
            selectedNotes.Add(note);
            note.selected = true;
            note.updateGraphics(ncModel);
        }

        public void SelectTempNote(Note note)
        {
            if (!selectedNotes.Contains(note))
            {
                tempSelectedNotes.Add(note);
                note.selected = true;
                note.updateGraphics(ncModel);
            }
        }

        public void DeselectTempAll()
        {
            foreach (Note note in tempSelectedNotes)
            {
                note.selected = false;
                note.updateGraphics(ncModel);
            }
            tempSelectedNotes.Clear();
        }

        public void FinishSelectTemp()
        {
            foreach (Note note in tempSelectedNotes)
            {
                selectedNotes.Add(note);
            }
            tempSelectedNotes.Clear();
        }

        public void DeselectNote(Note note)
        {
            if (selectedNotes.Contains(note))
            {
                selectedNotes.Remove(note);
                note.selected = false;
                note.updateGraphics(ncModel);
            }
        }

        public void SelectAll()
        {
            selectedNotes.Clear();
            foreach (Note note in noteList)
            {
                selectedNotes.Add(note);
                note.selected = true;
                note.updateGraphics(ncModel);
            }
        }
        
        public void DeselectAll()
        {
            selectedNotes.Clear();
            foreach (Note note in noteList)
            {
                note.selected = false;
                note.updateGraphics(ncModel);
            }
        }

        public void SelectTempInBox(double X1, double X2, double Y1, double Y2)
        {
            if (X1 > X2)
            {
                double temp = X1;
                X1 = X2;
                X2 = temp;
            }
            
            if (Y1 > Y2)
            {
                double temp = Y1;
                Y1 = Y2;
                Y2 = temp;
            }

            DeselectTempAll();
            foreach (Note note in noteList)
            {
                if (note.offset < X2 && note.getEndOffset() > X1 &&
                    note.keyNo <= Y2 && note.keyNo >= Y1) SelectTempNote(note);
            }
        }

        # endregion

    }
}
