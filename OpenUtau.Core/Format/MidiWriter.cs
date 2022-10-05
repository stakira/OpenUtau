using Melanchall.DryWetMidi.Common;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Format {
    public static class MidiWriter {
        static public void Save(string filePath, UProject project) {
            var midiFile = new MidiFile();
            var trackChunks = new List<TrackChunk> { };

            //Project resolution
            midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision((short)project.resolution);
            //Tempo
            midiFile.Chunks.Add(new TrackChunk());
            using (TempoMapManager tempoMapManager = midiFile.ManageTempoMap()) {
                foreach(UTempo uTempo in project.tempos){
                    tempoMapManager.SetTempo(uTempo.position, Tempo.FromBeatsPerMinute(uTempo.bpm));
                }
            }
            //Time Signature
            foreach (UTrack track in project.tracks) {
                trackChunks.Add(new TrackChunk());
            }
            //voice tracks
            foreach (UPart part in project.parts) {
                if (part is UVoicePart voicePart) {
                    var trackChunk = trackChunks[voicePart.trackNo];
                    var partOffset = part.position;
                    using (var objectsManager = new TimedObjectsManager<TimedEvent>(trackChunk.Events)) {
                        var events = objectsManager.Objects;
                        foreach (UNote note in voicePart.notes) {
                            string lyric = note.lyric;
                            if (lyric == "+") {
                                lyric = "-";
                            }
                            events.Add(new TimedEvent(new LyricEvent(lyric), note.position + partOffset));
                            events.Add(new TimedEvent(new NoteOnEvent((SevenBitNumber)(note.tone), (SevenBitNumber)45), note.position + partOffset));
                            events.Add(new TimedEvent(new NoteOffEvent((SevenBitNumber)(note.tone), (SevenBitNumber)45), note.position + partOffset + note.duration));
                        }
                    }
                }
            }
            
            foreach(TrackChunk trackChunk in trackChunks) {
                midiFile.Chunks.Add(trackChunk);
            }

            midiFile.Write(filePath,true,settings: new WritingSettings {
                TextEncoding = System.Text.Encoding.UTF8,
            }) ;
        }
    }
}
