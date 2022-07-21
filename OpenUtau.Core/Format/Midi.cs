using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Midi;

using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;

namespace OpenUtau.Core.Format
{
    public static class Midi
    {
        static public List<UVoicePart> Load(string file, UProject project)
        {
            List<UVoicePart> resultParts = new List<UVoicePart>();
            MidiFile midi = new MidiFile(file);
            string lyric = NotePresets.Default.DefaultLyric;
            for (int i = 0; i < midi.Tracks; i++)
            {
                Dictionary<int, UVoicePart> parts = new Dictionary<int, UVoicePart>();
                foreach (var e in midi.Events.GetTrackEvents(i)) {
                    if (e is NoteOnEvent) {
                        var _e = e as NoteOnEvent;
                        if (_e.OffEvent == null) {
                            continue;
                        }
                        if (!parts.ContainsKey(_e.Channel)) parts.Add(_e.Channel, new UVoicePart());
                        var note = project.CreateNote(
                            _e.NoteNumber,
                            (int)_e.AbsoluteTime * project.resolution / midi.DeltaTicksPerQuarterNote,
                            _e.NoteLength * project.resolution / midi.DeltaTicksPerQuarterNote);
                        if(lyric=="-") {
                            lyric = "+";
                        }
                        note.lyric = lyric;
                        if (NotePresets.Default.AutoVibratoToggle && note.duration >= NotePresets.Default.AutoVibratoNoteDuration) note.vibrato.length = NotePresets.Default.DefaultVibrato.VibratoLength;
                        parts[e.Channel].notes.Add(note);
                        lyric = NotePresets.Default.DefaultLyric;
                    }
                    else if (e is TextEvent) { //Lyric event
                        var _e = e as TextEvent;
                        if(_e.MetaEventType == MetaEventType.Lyric) {
                            lyric = _e.Text;
                        }
                    }
                }
                foreach (var pair in parts)
                {
                    pair.Value.Duration = pair.Value.GetMinDurTick(project);
                    resultParts.Add(pair.Value);
                }
            }
            return resultParts;
        }
    }
}
