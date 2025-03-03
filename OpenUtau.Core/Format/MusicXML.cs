using System;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Collections.Generic;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using Serilog;
using UtfUnknown;

namespace OpenUtau.Core.Format
{
    public static class MusicXML
    {
        static public UProject LoadProject(string file)
        {
            UProject uproject = new UProject();
            Ustx.AddDefaultExpressions(uproject);

            uproject.tracks.Clear();
            uproject.parts.Clear();
            uproject.tempos.Clear();
            uproject.timeSignatures.Clear();

            var score = ReadXMLScore(file);

            foreach (var part in score.Part)
            {
                var utrack = new UTrack(uproject);
                utrack.TrackNo = uproject.tracks.Count;
                uproject.tracks.Add(utrack);

                var upart = new UVoicePart();
                upart.trackNo = utrack.TrackNo;
                uproject.parts.Add(upart);

                int divisions = (int)part.Measure[0].Attributes[0].Divisions;
                int currPosTick = 0;

                foreach (var measure in part.Measure)
                {
                    // BPM
                    double? bpm;
                    if ((bpm = MeasureBPM(measure)).HasValue) {
                        uproject.tempos.Add(new UTempo(currPosTick, bpm.Value));
                        Log.Information($"Measure {measure.Number} BPM: {bpm.ToString()}");
                    }

                    // Time Signature
                    foreach (var attributes in measure.Attributes) {
                        foreach (var time in attributes.Time) {
                            if (time.Beats.Count > 0 && time.BeatType.Count > 0) {
                                uproject.timeSignatures.Add(new UTimeSignature {
                                    barPosition = currPosTick,
                                    beatPerBar = Int32.Parse(time.Beats[0]),
                                    beatUnit = Int32.Parse(time.BeatType[0])
                                });
                                Log.Information($"Measure {measure.Number} Time Signature: {time.Beats[0]}/{time.BeatType[0]}");
                            }
                        }
                    }

                    // Note
                    foreach(var note in measure.Note) {
                        int durTick = (int)note.Duration * uproject.resolution / divisions;

                        if (note.Rest != null) {
                            // pass
                        }
                        else {
                            var pitch = note.Pitch.Step.ToString() + note.Pitch.Octave.ToString();
                            int tone = MusicMath.NameToTone(pitch) + (int)note.Pitch.Alter;
                            UNote unote = uproject.CreateNote(tone, currPosTick, durTick);
                            if (note.Lyric.Count > 0) {
                                unote.lyric = note.Lyric[0].Text[0].Value;
                            }
                            upart.notes.Add(unote);
                        }

                        currPosTick += durTick;
                    }
                }
                upart.Duration = upart.GetMinDurTick(uproject);
            }
            if(uproject.tempos.Count == 0){
                uproject.tempos.Add(new UTempo(0, 120));
            }
            if(uproject.tempos[0].position > 0){
                uproject.tempos[0].position = 0;
            }
            uproject.AfterLoad();
            uproject.ValidateFull();
            return uproject;
        }

        static public Encoding DetectXMLEncoding(string file)
        {
            Encoding xmlEncoding = Encoding.UTF8;
            var detectionResult = CharsetDetector.DetectFromFile(file);

            if (detectionResult.Detected != null && detectionResult.Detected.Confidence > 0.5)
            {
                xmlEncoding = detectionResult.Detected.Encoding;
            }
            return xmlEncoding;
        }

        static public double? MeasureBPM(MusicXMLSchema.ScorePartwisePartMeasure measure)
        {
            foreach (var direction in measure.Direction) {
                if (direction.Sound != null) { return (double)direction.Sound.Tempo; }
            }
            return null;
        }

        static public MusicXMLSchema.ScorePartwise ReadXMLScore(string xmlFile)
        {
            Log.Information($"MusicXML Character Encoding: {DetectXMLEncoding(xmlFile)}");

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;
            settings.MaxCharactersFromEntities = 1024;

            using (var fs = new FileStream(xmlFile, FileMode.Open))
            using (var xmlReader = XmlReader.Create(fs, settings))
            {
                XmlSerializer s = new XmlSerializer(typeof(MusicXMLSchema.ScorePartwise));

                var score = s.Deserialize(xmlReader) as MusicXMLSchema.ScorePartwise;
                return score;
            }
        }
    }
}
