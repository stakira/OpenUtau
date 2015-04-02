using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public class UPart
    {
        public string Name = "New Part";
        public string Comment = "";

        public List<UNote> Notes = new List<UNote>();

        public UTrack Parent;

        public int bar = 4; // bar = number of beats
        public int beat = 4; // beat = number of quarter-notes
        public int bpm = 128000; // Beat per minute * 1000
        public int ppq = 96; // Pulse per quarter note

        public HashSet<UNote> selectedNotes = new HashSet<UNote>();
        public HashSet<UNote> tempSelectedNotes = new HashSet<UNote>();

        public OpenUtau.UI.Models.NotesCanvasModel ncModel;

        public UPart(UTrack parent)
        {
            Parent = parent;
        }

        public void UpdateGraphics()
        {
            foreach (UNote note in Notes)
            {
                note.updateGraphics(ncModel);
            }
        }

        public bool CheckOverlap()
        {
            bool pass = true;
            for (int i = 0; i < Notes.Count; i++)
                Notes[i].Error = false;
            for (int i = 0; i < Notes.Count; i++)
            {
                if (Notes.Count < 2 || i == Notes.Count - 1)
                    continue;
                else if (Notes[i].Channel != Notes[i + 1].Channel)
                    continue;
                else if (Notes[i].getEndOffset() > Notes[i + 1].offset)
                {
                    Notes[i].Error = true;
                    Notes[i + 1].Error = true;
                    pass = false;
                }
            }
            return pass;
        }

        # region Basic Note operations

        public void AddNote(UNote note)
        {
            Notes.Add(note);
            ncModel.notesCanvas.Children.Add(note.noteControl);
            note.updateGraphics(ncModel);
            Notes.Sort();
        }

        public bool RemoveNote(UNote note)
        {
            bool success = Notes.Remove(note);
            if (!success)
                throw new Exception("Note does not exist, cannot be removed");
            DeselectNote(note);
            ncModel.notesCanvas.Children.Remove(note.noteControl);
            note.noteControl.note = null; // Break reference loop
            Notes.Sort();
            return success;
        }

        public void RemoveSelectedNote()
        {
            foreach (UNote note in selectedNotes)
            {
                if (!Notes.Remove(note))
                    throw new Exception("Note does not exist, cannot be removed");
                ncModel.notesCanvas.Children.Remove(note.noteControl);
                note.noteControl.note = null; // Break reference loop
            }
            DeselectAll();
            Notes.Sort();
        }

        public void PrintNotes()
        {
            System.Diagnostics.Debug.WriteLine(Notes.Count.ToString() + " Notes in Total");
            foreach (UNote note in Notes)
            {
                System.Diagnostics.Debug.WriteLine("Note : " + note.offset.ToString() + " " + note.keyNo.ToString());
            }
            foreach (UNote note in selectedNotes)
            {
                System.Diagnostics.Debug.WriteLine("Selected Note : " + note.offset.ToString() + " " + note.keyNo.ToString());
            }
        }

        # endregion

        # region Selection related functions

        public void SelectNote(UNote note)
        {
            selectedNotes.Add(note);
            note.Selected = true;
        }

        public void SelectTempNote(UNote note)
        {
            if (!selectedNotes.Contains(note))
            {
                tempSelectedNotes.Add(note);
                note.Selected = true;
            }
        }

        public void DeselectTempAll()
        {
            foreach (UNote note in tempSelectedNotes)
            {
                note.Selected = false;
            }
            tempSelectedNotes.Clear();
        }

        public void FinishSelectTemp()
        {
            foreach (UNote note in tempSelectedNotes)
            {
                selectedNotes.Add(note);
            }
            tempSelectedNotes.Clear();
        }

        public void DeselectNote(UNote note)
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
            foreach (UNote note in Notes)
            {
                selectedNotes.Add(note);
                note.Selected = true;
            }
        }
        
        public void DeselectAll()
        {
            selectedNotes.Clear();
            foreach (UNote note in Notes)
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
            foreach (UNote note in Notes)
            {
                if (note.offset < X2 && note.getEndOffset() > X1 &&
                    note.keyNo <= Y2 && note.keyNo >= Y1) SelectTempNote(note);
            }
        }

        # endregion

    }
}
