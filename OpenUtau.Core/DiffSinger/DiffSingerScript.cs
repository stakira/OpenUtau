using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.DiffSinger {
    public class DiffSingerScript {
        public string[] ph_seq;
        public int[] noteSeq;
        public double[] phDurMs;
        public double frameMs;
        public double[] f0_seq;
        public double offsetMs;
        public double[]? gender = null;
        public DiffSingerScript(RenderPhrase phrase) {
            const float headMs = DiffSingerUtils.headMs;
            const float tailMs = DiffSingerUtils.tailMs;
            
            ph_seq = phrase.phones
                .Select(p => p.phoneme)
                .Append("SP")
                .ToArray();
            phDurMs = phrase.phones
                .Select(p => p.durationMs)
                .Append(DiffSingerUtils.tailMs)
                .ToArray();
            noteSeq = phrase.phones
                .Select(p => (p.phoneme == "SP" || p.phoneme == "AP") ? 0 : p.tone)
                .Append(0)
                .ToArray();
            DiffSingerSinger singer = null;
            if (phrase.singer != null) { 
                singer = phrase.singer as DiffSingerSinger; 
            }
            if(singer != null) {
                frameMs = singer.getVocoder().frameMs();
            } else {
                frameMs = 10;
            }

            int headFrames = (int)(headMs / frameMs);
            int tailFrames = (int)(tailMs / frameMs);
            var totalFrames = (int)(phDurMs.Sum() / frameMs);

            f0_seq = DiffSingerUtils.SampleCurve(phrase, phrase.pitches, 0, frameMs, totalFrames, headFrames, tailFrames, 
                x => MusicMath.ToneToFreq(x * 0.01));

            //voicebank specific features
            if(singer != null) {
                if (singer.dsConfig.useKeyShiftEmbed) {
                    var range = singer.dsConfig.augmentationArgs.randomPitchShifting.range;
                    var positiveScale = (range[1] == 0) ? 0 : (12 / range[1] / 100);
                    var negativeScale = (range[0] == 0) ? 0 : (-12 / range[0] / 100);
                    gender = DiffSingerUtils.SampleCurve(phrase, phrase.gender, 0, frameMs, totalFrames, headFrames, tailFrames,
                        x => (x < 0) ? (-x * positiveScale) : (-x * negativeScale))
                        .ToArray();
                }
            }

            offsetMs = phrase.phones[0].positionMs;
        }
        
        public RawDiffSingerScript toRaw() {
            return new RawDiffSingerScript(this);
        }

        static public void SavePart(UProject project, UVoicePart part, string filePath) {
            var ScriptArray = RenderPhrase.FromPart(project, project.tracks[part.trackNo], part)
                .Select(x => new DiffSingerScript(x).toRaw()).ToArray();
            File.WriteAllText(filePath,
                JsonConvert.SerializeObject(ScriptArray, Formatting.Indented),
                new UTF8Encoding(false));
        }
    }

    public class RawDiffSingerScript {
        public string text = "AP";
        public string ph_seq;
        public string note_seq;
        public string note_dur_seq;
        public string is_slur_seq;
        public string ph_dur;
        public string f0_timestep;
        public string f0_seq;
        public string? gender_timestep = null;
        public string? gender = null;
        public string input_type = "phoneme";
        public double offset;

        public RawDiffSingerScript(DiffSingerScript script) {
            ph_seq = String.Join(" ", script.ph_seq);
            note_seq = String.Join(" ", 
                script.noteSeq
                .Select(x => x <= 0 ? "rest" : MusicMath.GetToneName(x)));
            ph_dur = String.Join(" ",script.phDurMs.Select(x => (x/1000).ToString("f6")));
            note_dur_seq = ph_dur;
            is_slur_seq = String.Join(" ", script.ph_seq.Select(x => "0"));
            f0_timestep = (script.frameMs / 1000).ToString();
            f0_seq = String.Join(" ", script.f0_seq.Select(x => x.ToString("f1")));
            offset = script.offsetMs / 1000;

            if(script.gender != null) {
                gender_timestep = f0_timestep;
                gender = String.Join(" ", script.gender.Select(x => x.ToString("f3")));
            }
        }
    }
}
