using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core
{
    public class TrackPart
    {
        public int bar = 4; // bar = number of beats
        public int beat = 4; // beat = number of quarter-notes
        public int bpm = 128000; // Beat per minute * 1000
        public int ppq = 96; // Pulse per quarter note

        public List<Note> noteList = new List<Note>();
        public HashSet<Note> selectedNotes = new HashSet<Note>();
        public HashSet<Note> tempSelectedNotes = new HashSet<Note>();

        OpenUtau.UI.Models.NotesCanvasModel ncModel;

        public TrackPart(OpenUtau.UI.Models.NotesCanvasModel ncModel)
        {
            this.ncModel = ncModel;
        }

        public void UpdateGraphics()
        {
            foreach (Note note in noteList)
            {
                note.updateGraphics(ncModel);
            }
        }

        public bool CheckOverlap()
        {
            bool pass = true;
            for (int i = 0; i < noteList.Count; i++)
                noteList[i].Error = false;
            for (int i = 0; i < noteList.Count; i++)
            {
                if (noteList.Count < 2 || i == noteList.Count - 1)
                    continue;
                else if (noteList[i].Channel != noteList[i + 1].Channel)
                    continue;
                else if (noteList[i].getEndOffset() > noteList[i + 1].offset)
                {
                    noteList[i].Error = true;
                    noteList[i + 1].Error = true;
                    pass = false;
                }
            }
            return pass;
        }

        # region Basic Note operations

        public void AddNote(Note note)
        {
            noteList.Add(note);
            ncModel.notesCanvas.Children.Add(note.noteControl);
            note.updateGraphics(ncModel);
            noteList.Sort();
        }

        public bool RemoveNote(Note note)
        {
            bool success = noteList.Remove(note);
            if (!success)
                throw new Exception("Note does not exist, cannot be removed");
            DeselectNote(note);
            ncModel.notesCanvas.Children.Remove(note.noteControl);
            note.noteControl.note = null; // Break reference loop
            noteList.Sort();
            return success;
        }

        public void RemoveSelectedNote()
        {
            foreach (Note note in selectedNotes)
            {
                if (!noteList.Remove(note))
                    throw new Exception("Note does not exist, cannot be removed");
                ncModel.notesCanvas.Children.Remove(note.noteControl);
                note.noteControl.note = null; // Break reference loop
            }
            DeselectAll();
            noteList.Sort();
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

        # endregion

        # region Selection related functions

        public void SelectNote(Note note)
        {
            selectedNotes.Add(note);
            note.Selected = true;
        }

        public void SelectTempNote(Note note)
        {
            if (!selectedNotes.Contains(note))
            {
                tempSelectedNotes.Add(note);
                note.Selected = true;
            }
        }

        public void DeselectTempAll()
        {
            foreach (Note note in tempSelectedNotes)
            {
                note.Selected = false;
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
                note.Selected = false;
            }
        }

        public void SelectAll()
        {
            selectedNotes.Clear();
            foreach (Note note in noteList)
            {
                selectedNotes.Add(note);
                note.Selected = true;
            }
        }
        
        public void DeselectAll()
        {
            selectedNotes.Clear();
            foreach (Note note in noteList)
            {
                note.Selected = false;
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
