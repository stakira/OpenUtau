using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NWaves.Signals;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Analysis.Some {
    public static class AudioSlicer{
        static int sample_rate = 44100;
        static float threshold = 0.02f;
        static int hop_size = 441;
        static int win_size = 1764;
        static int min_length = 500;
        static int min_interval = 30;
        static int max_sil_kept = 50;

        public struct Chunk{
            public double offsetMs;//position of this slice in the audio file in milliseconds
            public float[] samples;

            public Chunk(double offsetMs, float[] samples){
                this.offsetMs = offsetMs;
                this.samples = samples;
            }

            public Chunk(float[] originalSamples, int startIndex, int endIndex){
                samples = originalSamples[startIndex..endIndex];
                offsetMs = (double)startIndex * (1000.0 / sample_rate);
            }
        }

        static double[] get_rms(
            float[] samples, 
            int frame_length = 2048, 
            int hop_length = 512
            ){
            //reference: https://github.com/openvpi/audio-slicer/blob/main/slicer2.py#L5
            //y = np.pad(samples, padding, mode="constant")
            float[] y = new float[samples.Length + frame_length];
            Array.Copy(samples, 0, y, frame_length / 2, samples.Length);
            for(int i=0; i<y.Length; i++){
                y[i] = y[i] * y[i];
            }
            int output_size = samples.Length / hop_length;
            return Enumerable.Range(0, output_size)
                .Select(i => Math.Sqrt(y[(i*hop_length)..(i*hop_length+frame_length)].Average()))
                .ToArray();
        }

        static int argmin(this double[] array){
            //numpy's argmin function
            return Array.IndexOf(array, array.Min());
        }

        public static List<Chunk> Slice(float[] samples){
            //reference: https://github.com/openvpi/audio-slicer/blob/main/slicer2.py#L68
            if((samples.Length + hop_size - 1) / hop_size <= min_length){
                return new List<Chunk>{new Chunk(0, samples)};
            }
            var rms_list = get_rms(
                samples,
                frame_length: win_size,
                hop_length: hop_size
            );
            var sil_tags = new List<Tuple<int,int>>();
            int silence_start = -1;//here -1 means none
            int clip_start = 0;
            foreach(int i in Enumerable.Range(0, rms_list.Length)){
                var rms = rms_list[i];
                //Keep looping while frame is silent.
                if(rms < threshold){
                    //Record start of silent frames.
                    if(silence_start < 0){
                        silence_start = i;
                    }
                    continue;
                }
                //Keep looping while frame is not silent and silence start has not been recorded.
                if(silence_start < 0){
                    continue;
                }
                //Clear recorded silence start if interval is not enough or clip is too short
                var is_leading_silence = silence_start == 0 && i > max_sil_kept;
                var need_slice_middle = i - silence_start >= min_interval && i - clip_start >= min_length;
                if(!is_leading_silence && !need_slice_middle){
                    silence_start = -1;
                    continue;
                }
                //Need slicing. Record the range of silent frames to be removed.
                if(i - silence_start <= max_sil_kept){
                    var pos = rms_list[silence_start..(i+1)].argmin() + silence_start;
                    if(silence_start == 0){
                        sil_tags.Add(Tuple.Create(0,pos));
                    } else {
                        sil_tags.Add(Tuple.Create(pos, pos));
                    }
                    clip_start = pos;
                } else if(i - silence_start <= max_sil_kept * 2){
                    var pos = rms_list[(i - max_sil_kept)..(silence_start + max_sil_kept + 1)].argmin();
                    pos += i - max_sil_kept;
                    var pos_l = rms_list[silence_start..(silence_start + max_sil_kept + 1)].argmin() + silence_start;
                    var pos_r = rms_list[(i - max_sil_kept)..(i+1)].argmin() + i - max_sil_kept;
                    if(silence_start == 0){
                        sil_tags.Add(Tuple.Create(0, pos_r));
                        clip_start = pos_r;
                    } else {
                        sil_tags.Add(Tuple.Create(Math.Min(pos_l, pos), Math.Max(pos_r, pos)));
                        clip_start = Math.Max(pos_r, pos);
                    }
                } else {
                    var pos_l = rms_list[silence_start..(silence_start + max_sil_kept + 1)].argmin() + silence_start;
                    var pos_r = rms_list[(i - max_sil_kept)..(i+1)].argmin() + i - max_sil_kept;
                    if(silence_start == 0){
                        sil_tags.Add(Tuple.Create(0, pos_r));
                    } else {
                        sil_tags.Add(Tuple.Create(pos_l, pos_r));
                    }
                    clip_start = pos_r;
                }
                silence_start = -1;
            }
            //Deal with trailing silence.
            var total_frames = rms_list.Length;
            if(silence_start >= 0 && total_frames - silence_start >= min_interval){
                var silence_end = Math.Min(total_frames, silence_start + max_sil_kept);
                var pos = rms_list[silence_start..(silence_end + 1)].argmin() + silence_start;
                sil_tags.Add(Tuple.Create(pos, total_frames + 1));
            }
            //Apply and return slices.
            if(sil_tags.Count == 0){
                return new List<Chunk>{new Chunk(0, samples)};
            } else {
                var chunks = new List<Chunk>();
                if(sil_tags[0].Item1 > 0){
                    chunks.Add(new Chunk(
                        samples, 
                        0, 
                        sil_tags[0].Item1 * hop_size
                    ));
                }
                foreach(var i in Enumerable.Range(0, sil_tags.Count - 1)){
                    chunks.Add(new Chunk(
                        samples, 
                        sil_tags[i].Item2 * hop_size, 
                        sil_tags[i+1].Item1 * hop_size
                    ));
                }
                if(sil_tags[^1].Item2 < total_frames){
                    chunks.Add(new Chunk(
                        samples,
                        sil_tags[^1].Item2 * hop_size,
                        total_frames * hop_size
                    ));
                }
                return chunks;
            }
        }
    }

    class SomeConfig{
        public string model = "model.onnx";
        public int sample_rate = 44100;
    }

    public class Some: IDisposable {
        InferenceSession session;
        string Location;
        private bool disposedValue;

        struct SomeResult{
            //midi number of each note
            public float[] note_midi;
            //whether each note is a rest
            public bool[] note_rest;
            //duration of each note in seconds
            public float[] note_dur;
        }        

        public Some() {
            Location = Path.Combine(PathManager.Inst.DependencyPath, "some");
            string yamlpath = Path.Combine(Location, "some.yaml");
            if (!File.Exists(yamlpath)) {
                //TODO: onnx download site
                throw new FileNotFoundException($"Error loading SOME. Please download SOME from\nhttps://github.com/xunmengshe/OpenUtau/releases/0.0.0.0");
            }

            var config = Yaml.DefaultDeserializer.Deserialize<SomeConfig>(
                File.ReadAllText(yamlpath, System.Text.Encoding.UTF8));
            session = Onnx.getInferenceSession(Path.Combine(Location, config.model));
        }

        SomeResult Analyze(float[] samples) {
            //Analyze a slice of audio samples and return the result
            var min = samples.Min();
            var max = samples.Max();
            var inputs = new List<NamedOnnxValue>();
            inputs.Add(NamedOnnxValue.CreateFromTensor("waveform",
                new DenseTensor<float>(samples, new int[] { samples.Length }, false)
                .Reshape(new int[] { 1, samples.Length })));
            var outputs = session.Run(inputs);
            float[] note_midi = outputs
                .Where(o => o.Name == "note_midi")
                .First()
                .AsTensor<float>()
                .ToArray();
            bool[] note_rest = outputs
                .Where(o => o.Name == "note_rest")
                .First()
                .AsTensor<bool>()
                .ToArray();
            float[] note_dur = outputs
                .Where(o => o.Name == "note_dur")
                .First()
                .AsTensor<float>()
                .ToArray();
            return new SomeResult{
                note_midi = note_midi,
                note_rest = note_rest,
                note_dur = note_dur
            };
        }

        private float[] ToMono(float[] stereoSamples, int channels){
            if(channels == 1){
                return stereoSamples;
            }
            float[] monoSamples = new float[stereoSamples.Length / channels];
            for(int i = 0; i < monoSamples.Length; i++){
                monoSamples[i] = stereoSamples[(i*channels)..((i+1)*channels-1)].Average();
            }
            return monoSamples;
        }

        public UVoicePart Transcribe(UProject project, UWavePart wavePart, Action<int> progress){
            //Run SOME model with the audio part user selected to extract note information
            //convert samples to mono and slice
            
            var monoSamples = ToMono(wavePart.Samples, wavePart.channels);
            var chunks = AudioSlicer.Slice(monoSamples);
            var part = new UVoicePart();
            part.position = wavePart.position;
            part.Duration = wavePart.Duration;
            var timeAxis = project.timeAxis;
            double partOffsetMs = timeAxis.TickPosToMsPos(wavePart.position);
            double currMs = partOffsetMs;

            int wavPosS = 0;//position of current slice in seconds
            foreach(var chunk in chunks){
                wavPosS = (int)(chunk.offsetMs / 1000);
                progress.Invoke(wavPosS);
                var someResult = Analyze(chunk.samples);
                var note_midi = someResult.note_midi;
                var note_rest = someResult.note_rest;
                var note_dur = someResult.note_dur;
                //Put the notes into a new voice part
                double chunkOffsetMs = chunk.offsetMs + partOffsetMs;
                currMs = chunkOffsetMs;
                foreach(int index in Enumerable.Range(0, note_midi.Length)){
                    var noteDurMs = note_dur[index] * 1000;
                    if(!note_rest[index]){
                        var posTick = timeAxis.MsPosToTickPos(currMs);
                        var durTick = timeAxis.MsPosToTickPos(currMs + noteDurMs) - posTick;
                        var note = project.CreateNote(
                            (int)Math.Round(note_midi[index]),
                            posTick - wavePart.position,
                            durTick
                        );
                        part.notes.Add(note);
                    }
                    currMs += noteDurMs;
                }
            }
            var endTick = timeAxis.MsPosToTickPos(currMs);
            if(endTick > part.End){
                part.Duration = endTick - part.position;
            }
            return part;
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    session.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
