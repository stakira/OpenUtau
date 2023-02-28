using System.Collections.Generic;
using System.Linq;

using OpenUtau.Core.Format;

using static OpenUtau.Api.Phonemizer;

namespace OpenUtau.Core.Ustx {
    public static class PhonemeBaking {

        public static UNote GetLastNoteOfGroup(UNote note) {
            while (note != null) {
                UNote nextNote = note.Next;
                if (nextNote == null || nextNote.position > note.End || !nextNote.lyric.StartsWith("+")) {
                    return note;
                }
                note = nextNote;
            }
            return note;
        }

        //determine if phoneme2 is the next neighbor of phoneme 1
        public static bool IsNeighbor(UPhoneme phoneme1, UPhoneme phoneme2) {
            if (phoneme1.Next != phoneme2) {
                return false;
            }
            if (phoneme1.Parent == phoneme2.Parent) {
                return true;
            }
            UNote note = phoneme1.Parent;
            while (note != null) {
                UNote nextNote = note.Next;
                if (nextNote == null || nextNote.position > note.End) {
                    return false;
                }
                if (nextNote == phoneme2.Parent) {
                    return true;
                }
                if (!nextNote.lyric.StartsWith("+")) {
                    //phonemes can only be extended by slur notes.
                    return false;
                }
                note = nextNote;
            }
            return true;
        }

        //export phoneme as ustx notes
        public static List<UNote> BakePart(UProject project, UVoicePart part) {
            List<UNote> result = part.phonemes
                .Select(phoneme => project.CreateNote(
                    phoneme.Parent.tone,
                    phoneme.position,
                    60//Duration here is a placeholder. 
                    )).ToList();

            //Calculate duration for each phoneme
            int addedIndex = 0;
            for (int i = 0; i < result.Count - 1; i++) {
                result[i].lyric = part.phonemes[i - addedIndex].phoneme;
                var thisNote = part.phonemes[i - addedIndex].Parent;
                var nextNote = part.phonemes[i + 1 - addedIndex].Parent;

                //if this isn't the last phoneme of the sentence
                if (IsNeighbor(part.phonemes[i - addedIndex], part.phonemes[i + 1 - addedIndex])) {
                    result[i].duration = result[i + 1].position - result[i].position;
                    if (result[i].End > thisNote.End) { // Note duration over
                        if (result[i].position < thisNote.End) {
                            result.Insert(i + 1, result[i].Clone());
                            result[i].duration = thisNote.End - result[i].position;
                            result[i + 1].position = result[i].End;
                            result[i + 1].duration = part.phonemes[i + 1 - addedIndex].position - result[i + 1].position;
                            result[i + 1].tone = thisNote.Next.tone;
                            i++; // Skip
                            addedIndex++;
                        } else { // phoneme include in '+' note
                            result[i].tone = thisNote.Next.tone;
                        }
                    }
                } else {
                    result[i].duration = GetLastNoteOfGroup(thisNote).End - result[i].position;
                }
            }
            result[^1].lyric = part.phonemes[^1].phoneme;
            result[^1].duration = GetLastNoteOfGroup(part.phonemes[^1].Parent).End - result[^1].position;
            //TODO: pitch converting
            return result;
        }

        //TODO
        public static UProject BakeProject(UProject project) {
            //copy tracks
            UProject newProject = new UProject();
            Format.Ustx.AddDefaultExpressions(newProject);
            //Copy Tracks
            foreach (UTrack track in project.tracks) {
                //Phonemizer is DEFAULT
                var newTrack = new UTrack() {
                    TrackNo = track.TrackNo,
                    Singer = track.Singer,
                    RendererSettings = track.RendererSettings,
                    Mute = track.Mute,
                    Solo = track.Solo,
                    Volume = track.Volume,
                    Pan = track.Pan,
                };
                newProject.tracks.Add(newTrack);
            }
            //Convert Parts
            foreach (UPart part in project.parts) {
                if (part is UVoicePart voicePart) {
                    var phonemeNotes = new SortedSet<UNote>();
                    foreach (UNote note in BakePart(project, voicePart)) {
                        phonemeNotes.Add(note);
                    }
                    newProject.parts.Add(new UVoicePart {
                        name = voicePart.name,
                        notes = phonemeNotes,
                        trackNo = voicePart.trackNo,
                        position = voicePart.position,
                    });
                } else if (part is UWavePart wavePart) {
                    newProject.parts.Add(part);
                }
            }
            return newProject;
        }
    }
}
