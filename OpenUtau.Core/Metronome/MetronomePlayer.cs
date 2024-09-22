using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Timers;


namespace OpenUtau.Core.Metronome {
    public class MetronomePlayer {
        public static MetronomePlayer Instance {
            get {
                if (instance == null) {
                    instance = new MetronomePlayer();
                    return instance;
                } else {
                    return instance;
                }
            }
        }

        // Output device and mixer
        private readonly WaveOut outputDevice;
        private MixingSampleProvider mixer;
        // Beat pattern
        public string AccentedBeatPath { get; set; } = "Metronome/SnareHi.wav";
        public string NormalBeatPath { get; set; } = "Metronome/SnareLo.wav";
        private PatternEngine patternEngine = new PatternEngine();
        private SampleSource AccentedPattern { get; set; }
        private SampleSource NormalPattern { get; set; }
        public VolumeSampleProvider accentedVolumeProvider { get; set; }
        public VolumeSampleProvider normalVolumeProvider { get; set; }
        // Metronome settings
        public bool IsPlaying => outputDevice.PlaybackState == PlaybackState.Playing;
        public int MinBPM { get; set; } = 20;
        public int MaxBPM { get; set; } = 500;
        private int bpm = 120;
        public int BPM { 
            get {
                return bpm;
            }
            set {
                if (value < MinBPM)
                    bpm = MinBPM;
                else if (value > MaxBPM)
                    bpm = MaxBPM;
                else
                    bpm = value;
            }
        }
        public int Beats { get; set; } = 4;
        public int NoteLength { get; set; } = 4;
        public bool OpenMetronome { get; set; } = false;

        private Timer? timer;
        public float Volume {
            get => outputDevice.Volume;
            set {
                outputDevice.Volume = Math.Clamp(value, 0, 1);
                if (timer == null) {
                    timer = new Timer(3000);
                    timer.Elapsed += saveSetting;
                    timer.AutoReset = false;
                    timer.Enabled = true;
                } else {
                    timer.Stop();
                    timer.Interval = 3000;
                    timer.Start();
                }
            }
        }

        private void saveSetting(Object source, ElapsedEventArgs e) {
            if (settingsData == null) {
                settingsData = new MetronomeSetting();
            }
            if ((settingsData.Volume != Volume)) {
                settingsData.Volume = Volume;
                string jsonString = JsonConvert.SerializeObject(settingsData, Formatting.Indented);
                File.WriteAllText("Metronome/MetronomeSetting.json", jsonString);
            }

            timer?.Dispose();
            timer = null;
        }

        private static MetronomePlayer? instance;

        public class MetronomeSetting {
            public string AccentedBeatPath { get; set; } = "SnareHi.wav";
            public string NormalBeatPath { get; set; } = "SnareLo.wav";
            public float Volume { get; set; } = 1.0f;
        }

        private MetronomeSetting? settingsData = null;

        private MetronomePlayer()
        {
            try {
                string jsonString = File.ReadAllText("Metronome/MetronomeSetting.json");
                settingsData = JsonConvert.DeserializeObject<MetronomeSetting>(jsonString);
                if (settingsData != null) {
                    AccentedBeatPath = "Metronome/" + settingsData.AccentedBeatPath;
                    NormalBeatPath = "Metronome/" + settingsData.NormalBeatPath;
                }
            } catch {
                //Console.WriteLine("Read MetronomeSetting Error!!!");
            }

            // Create beat pattern
            patternEngine.AccentedBeat = new SampleSource(AccentedBeatPath);
            patternEngine.NormalBeat = new SampleSource(NormalBeatPath);
            AccentedPattern = patternEngine.CreateAccentedBeatPattern(BPM, Beats, NoteLength);
            NormalPattern = patternEngine.CreateNormalBeatPattern(BPM, Beats, NoteLength);

            // Create Volume Providers
            accentedVolumeProvider = new VolumeSampleProvider(new SampleSourceProvider(AccentedPattern));
            normalVolumeProvider = new VolumeSampleProvider(new SampleSourceProvider(NormalPattern));

            // Create output device and mixer
            outputDevice = new WaveOut();
            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, patternEngine.AccentedBeat.WaveFormat.Channels));
            mixer.ReadFully = true;
            outputDevice.Init(mixer);

            if(settingsData != null) {
                Volume = settingsData.Volume;
            }
        }

        public void UpdateParmas(int bpm, int beats, int noteLength)
        {
            BPM = bpm;
            Beats = beats;
            NoteLength = noteLength;
            Update();
        }

        public void Play()
        {
            if (!IsPlaying && OpenMetronome) {
                accentedVolumeProvider = new VolumeSampleProvider(new SampleSourceProvider(AccentedPattern));
                normalVolumeProvider = new VolumeSampleProvider(new SampleSourceProvider(NormalPattern));
                accentedVolumeProvider.Volume = 1.0f;
                normalVolumeProvider.Volume = 1.0f;
                mixer.AddMixerInput(accentedVolumeProvider);
                mixer.AddMixerInput(normalVolumeProvider);
                outputDevice.Play();
            }
        }
        public void Stop()
        {
            if (IsPlaying) {
                mixer.RemoveAllMixerInputs();
                outputDevice.Stop();
            }
        }

        public void Update()
        {
            if (!IsPlaying) {
                AccentedPattern = patternEngine.CreateAccentedBeatPattern(BPM, Beats, NoteLength);
                NormalPattern = patternEngine.CreateNormalBeatPattern(BPM, Beats, NoteLength);
            }
        }

        public void ApplyBeatSound(string accentedBeatPath, string normalBeatPath)
        {
            AccentedBeatPath = accentedBeatPath;
            NormalBeatPath = normalBeatPath;
            patternEngine.AccentedBeat = new SampleSource(AccentedBeatPath);
            patternEngine.NormalBeat = new SampleSource(NormalBeatPath);

            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, patternEngine.AccentedBeat.WaveFormat.Channels));
            mixer.ReadFully = true;
            outputDevice.Init(mixer);

            Update();
        }
    }
}
