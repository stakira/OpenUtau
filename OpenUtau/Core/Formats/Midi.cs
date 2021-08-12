using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Midi;

using OpenUtau.Core.Ustx;

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
                        var note = project.CreateNote(
                            _e.NoteNumber,
                            (int)_e.AbsoluteTime * project.resolution / midi.DeltaTicksPerQuarterNote,
                            _e.NoteLength * project.resolution / midi.DeltaTicksPerQuarterNote);
                        parts[e.Channel].notes.Add(note);
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
