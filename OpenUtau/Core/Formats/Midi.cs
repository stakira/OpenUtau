using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Midi;

using OpenUtau.Core.USTx;

namespace OpenUtau.Core.Formats
{
    public static class Midi
    {
        static public List<UVoicePart> Load(string file, UProject project)
        {
            List<UVoicePart> resultParts = new List<UVoicePart>();
            MidiFile midi = new MidiFile(file);
            for (int i = 0; i < midi.Tracks; i++)
            {
                Dictionary<int, UVoicePart> parts = new Dictionary<int, UVoicePart>();
                foreach (var e in midi.Events.GetTrackEvents(i))
                    if (e is NoteOnEvent)
                    {
                        var _e = e as NoteOnEvent;
                        if (!parts.ContainsKey(_e.Channel)) parts.Add(_e.Channel, new UVoicePart());
                        var note = project.CreateNote();
                        note.PosTick = (int)_e.AbsoluteTime * project.Resolution / midi.DeltaTicksPerQuarterNote;
                        note.DurTick = _e.NoteLength * project.Resolution / midi.DeltaTicksPerQuarterNote;
                        note.NoteNum = _e.NoteNumber;
                        parts[e.Channel].Notes.Add(note);
                    }
                foreach (var pair in parts)
                {
                    pair.Value.DurTick = pair.Value.GetMinDurTick(project);
                    resultParts.Add(pair.Value);
                }
            }
            return resultParts;
        }
    }
}
