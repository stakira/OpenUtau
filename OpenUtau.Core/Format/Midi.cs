using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Midi;
using NChardet;

using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;

namespace OpenUtau.Core.Format
{
    public class MyCharsetDetectionObserver: NChardet.ICharsetDetectionObserver
    {
        public string Charset = null;

        public void Notify(string charset) {
            Charset = charset;
        }
    }
    public static class Midi
    {
        static public List<UVoicePart> Load(string file, UProject project)
        {
            List<UVoicePart> resultParts = new List<UVoicePart>();
            MidiFile midi = new MidiFile(file);
            string lyric = NotePresets.Default.DefaultLyric;
            for (int i = 0; i < midi.Tracks; i++)
            {
                //detect lyric encoding
                Detector det = new Detector(6);
                MyCharsetDetectionObserver cdo = new MyCharsetDetectionObserver();
                det.Init(cdo);
                bool done = false;
                foreach (var e in midi.Events.GetTrackEvents(i)) {
                    if (e is TextEvent) { //Lyric event
                        var _e = e as TextEvent;
                        if (_e.MetaEventType == MetaEventType.Lyric) {
                            done = det.DoIt(_e.Data, _e.Data.Length, false);
                            if (done) {
                                break;
                            }
                        }
                    }
                }
                det.DataEnd();
                string Charset = cdo.Charset;
                Encoding lyricEncoding = Encoding.UTF8;
                if (Charset!=null){
                    lyricEncoding = Encoding.GetEncoding(Charset);
                }
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
                            lyric = lyricEncoding.GetString(_e.Data);
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
