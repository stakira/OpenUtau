using Melanchall.DryWetMidi.Common;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using System.Text;
using System.IO;
using UtfUnknown;
using System.Linq;

namespace OpenUtau.Core.Format {
    public class EncodingDetector {

        MemoryStream stream = new MemoryStream();

        public void ReadFile(string file) {
            var ReadingSettings = MidiWriter.BaseReadingSettings();
            
            ReadingSettings.DecodeTextCallback = new DecodeTextCallback(AddText);
            var midi = MidiFile.Read(file,ReadingSettings);
        }

        string AddText(byte[] bytes, ReadingSettings settings) {
            stream.Write(bytes);
            return "";
        }

        public Encoding Detect() {
            stream.Seek(0, SeekOrigin.Begin);
            var detectionResult = CharsetDetector.DetectFromStream(stream);
            if (detectionResult.Detected != null && detectionResult.Detected.Confidence > 0.5) {
                return detectionResult.Detected.Encoding;
            } else {
                return null;
            }
        }
    }

    public static class MidiWriter {
        static public UProject LoadProject(string file) {
            UProject uproject = new UProject();
            Ustx.AddDefaultExpressions(uproject);

            uproject.tracks = new List<UTrack>();

            var parts = Load(file, uproject);
            foreach (var part in parts) {
                var track = new UTrack();
                track.TrackNo = uproject.tracks.Count;
                part.trackNo = track.TrackNo;
                part.AfterLoad(uproject, track);
                uproject.tracks.Add(track);
                uproject.parts.Add(part);
            }
            return uproject;
        }

        public static ReadingSettings BaseReadingSettings() {
            return new ReadingSettings {
                InvalidChannelEventParameterValuePolicy = InvalidChannelEventParameterValuePolicy.ReadValid,
                InvalidChunkSizePolicy = InvalidChunkSizePolicy.Ignore,
                InvalidMetaEventParameterValuePolicy = InvalidMetaEventParameterValuePolicy.SnapToLimits,
                MissedEndOfTrackPolicy = MissedEndOfTrackPolicy.Ignore,
                NoHeaderChunkPolicy = NoHeaderChunkPolicy.Ignore,
                NotEnoughBytesPolicy = NotEnoughBytesPolicy.Ignore,
                UnexpectedTrackChunksCountPolicy = UnexpectedTrackChunksCountPolicy.Ignore,
                UnknownChannelEventPolicy = UnknownChannelEventPolicy.SkipStatusByteAndOneDataByte,
                UnknownChunkIdPolicy = UnknownChunkIdPolicy.ReadAsUnknownChunk,
                UnknownFileFormatPolicy = UnknownFileFormatPolicy.Ignore
            };
        }

        static public List<UVoicePart> Load(string file, UProject project) {
            List<UVoicePart> resultParts = new List<UVoicePart>();
            string defaultLyric = NotePresets.Default.DefaultLyric;
            // Detects lyric encoding
            Encoding lyricEncoding = Encoding.UTF8;
            var encodingDetector = new EncodingDetector();
            encodingDetector.ReadFile(file);
            var encodingResult = encodingDetector.Detect();
            if(encodingResult != null) {
                lyricEncoding = encodingResult;
            }
        //Get midifile resolution
            var ReadingSettings = BaseReadingSettings();
            ReadingSettings.TextEncoding = lyricEncoding;
            var midi = MidiFile.Read(file, ReadingSettings);
            TicksPerQuarterNoteTimeDivision timeDivision = midi.TimeDivision as TicksPerQuarterNoteTimeDivision;
            var PPQ = timeDivision.TicksPerQuarterNote;
            //Parse midi data
            foreach (TrackChunk trackChunk in midi.GetTrackChunks()) {
                var midiNoteList = trackChunk.GetNotes().ToList();
                if (midiNoteList.Count > 0) {
                    var part = new UVoicePart();
                    using (var objectsManager = new TimedObjectsManager<TimedEvent>(trackChunk.Events)) {
                        var events = objectsManager.Objects;
                        foreach (Melanchall.DryWetMidi.Interaction.Note midiNote in midiNoteList) {
                            var note = project.CreateNote(
                                midiNote.NoteNumber,
                                (int)(midiNote.Time * project.resolution / PPQ),
                                (int)(midiNote.Length * project.resolution / PPQ)
                            );
                            //handle lyric import
                            string lyric = events.Where(e => e.Event is LyricEvent && e.Time == midiNote.Time)
                                                     .Select(e => ((LyricEvent)e.Event).Text)
                                                     .FirstOrDefault();
                            if (lyric == null) {
                                lyric = defaultLyric;
                            }
                            if (lyric == "-") {
                                lyric = "+";
;                           }
                            note.lyric = lyric;
                            if (NotePresets.Default.AutoVibratoToggle && note.duration >= NotePresets.Default.AutoVibratoNoteDuration) { 
                                note.vibrato.length = NotePresets.Default.DefaultVibrato.VibratoLength; 
                            }
                            part.notes.Add(note);
                        }
                    }
                    resultParts.Add(part);
                }
            }
            return resultParts;
        }

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
