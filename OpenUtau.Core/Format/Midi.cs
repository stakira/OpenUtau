using System.Collections.Generic;
using System.Text;
using NAudio.Midi;
using UtfUnknown;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using System.IO;

namespace OpenUtau.Core.Format {
    public static class Midi {
        static public UProject LoadProject(string file) {
            UProject uproject = new UProject();
            Ustx.AddDefaultExpressions(uproject);

            uproject.tracks = new List<UTrack>();

            var parts = Load(file, uproject);
            foreach (var part in parts) {
                var track = new UTrack(uproject);
                track.TrackNo = uproject.tracks.Count;
                part.trackNo = track.TrackNo;
                part.AfterLoad(uproject, track);
                uproject.tracks.Add(track);
                uproject.parts.Add(part);
            }
            return uproject;
        }
        static public List<UVoicePart> Load(string file, UProject project) {
            List<UVoicePart> resultParts = new List<UVoicePart>();
            MidiFile midi = new MidiFile(file, false);
            string lyric = NotePresets.Default.DefaultLyric;
            // Detects lyric encoding
            Encoding lyricEncoding = Encoding.UTF8;
            using (var stream = new MemoryStream()) {
                for (int i = 0; i < midi.Tracks; i++) {
                    foreach (var e in midi.Events.GetTrackEvents(i)) {
                        if (e is TextEvent te && te.MetaEventType == MetaEventType.Lyric) {
                            stream.Write(te.Data);
                        }
                    }
                }
                stream.Seek(0, SeekOrigin.Begin);
                var detectionResult = CharsetDetector.DetectFromStream(stream);
                if (detectionResult.Detected != null && detectionResult.Detected.Confidence > 0.5) {
                    lyricEncoding = detectionResult.Detected.Encoding;
                }
            }
            for (int i = 0; i < midi.Tracks; i++) {
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
                        if (lyric == "-") {
                            lyric = "+";
                        }
                        note.lyric = lyric;
                        if (NotePresets.Default.AutoVibratoToggle && note.duration >= NotePresets.Default.AutoVibratoNoteDuration) note.vibrato.length = NotePresets.Default.DefaultVibrato.VibratoLength;
                        parts[e.Channel].notes.Add(note);
                        lyric = NotePresets.Default.DefaultLyric;
                    } else if (e is TextEvent te && te.MetaEventType == MetaEventType.Lyric) {
                        lyric = lyricEncoding.GetString(te.Data);
                    }
                }
                foreach (var pair in parts) {
                    pair.Value.Duration = pair.Value.GetMinDurTick(project);
                    resultParts.Add(pair.Value);
                }
            }
            return resultParts;
        }
    }
}
