using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OpenUtau.Core.Metronome
{
    public class AudioPlayer : IDisposable
    {
        public static AudioPlayer Instance => instance;

        public event EventHandler PlaybackStopped;

        public bool IsPlaying => outputDevice.PlaybackState == PlaybackState.Playing;

        public float Volume
        {
            get => outputDevice.Volume;
            set => outputDevice.Volume = Math.Clamp(value, 0, 1);
        }

        private static readonly AudioPlayer instance;

        private readonly IWavePlayer outputDevice;
        private readonly MixingSampleProvider mixer;

        private AudioPlayer()
        {
            const int sampleRate = 44100;
            const int channelCount = 2;

            outputDevice = new WaveOutEvent();

            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount));
            mixer.MixerInputEnded += OnMixerInputEnded;

            outputDevice.Init(mixer);
        }

        static AudioPlayer()
        {
            instance = new AudioPlayer();
        }

        public void PlaySound(ISampleProvider input)
        {
            mixer.RemoveAllMixerInputs();
            AddMixerInput(input);
            outputDevice.Play();
        }

        public void Stop()
        {
            outputDevice.Stop();
        }

        public void Dispose()
        {
            outputDevice.Dispose();
        }

        private void AddMixerInput(ISampleProvider input)
        {
            mixer.AddMixerInput(ConvertChannelCount(input));
        }

        private ISampleProvider ConvertChannelCount(ISampleProvider input)
        {
            if (input.WaveFormat.Channels == mixer.WaveFormat.Channels)
                return input;

            if (input.WaveFormat.Channels == 1 && mixer.WaveFormat.Channels == 2)
                return new MonoToStereoSampleProvider(input);

            throw new NotImplementedException("Channel count is more that 2");
        }

        private void OnMixerInputEnded(object sender, SampleProviderEventArgs e)
        {
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }
    }
}
