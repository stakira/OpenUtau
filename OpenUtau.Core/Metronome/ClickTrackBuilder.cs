using System;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OpenUtau.Core.Metronome
{
    public static class ClickTrackBuilder
    {
        public const int SoundDurationMs = 20;
        public const byte MinPlaybackSpeedPercent = 30;

        public static ISampleProvider BuildSinglePulse(in BarInfo info, ClickSettings settings)
        {
            var track = BuildBar(info, settings, 100);
            return new LoopingSampleProvider(CachedSound.FromSampleProvider(track));
        }

        public static ISampleProvider BuildClickTrack(
            BarInfo[] infos,
            ClickSettings settings,
            float playbackSpeedPercent,
            bool precount,
            bool loop)
        {
            var providers = new ISampleProvider[infos.Length];

            Parallel.For(0, infos.Length, (i) => 
            {
                providers[i] = CachedSound.FromSampleProvider(BuildBar(infos[i], settings, playbackSpeedPercent));
            });              

            var track = new ConcatenatingSampleProvider(providers);

            ISampleProvider precountMeasure = null;

            if (precount)
            {
                precountMeasure = BuildBar(
                    new BarInfo(infos[0].Tempo,
                                settings.PrecountBarBeats,
                                settings.PrecountBarNoteLength),
                    settings,
                    playbackSpeedPercent);
            }

            if (loop)
            {
                if (precountMeasure is null)
                    return new LoopingSampleProvider(CachedSound.FromSampleProvider(track));
                else
                    return precountMeasure.FollowedBy(new LoopingSampleProvider(CachedSound.FromSampleProvider(track)));
            }
            else
            {
                if (precountMeasure is null)
                    return track;
                else
                    return precountMeasure.FollowedBy(track);
            }
        }

        private static ISampleProvider BuildBar(in BarInfo info, ClickSettings settings, float playbackSpeedPercent)
        {
            var providers = new ISampleProvider[info.Beats];

            var interval = info.GetInterval(playbackSpeedPercent);

            var silenceInterval = TimeSpan.FromMilliseconds(interval - SoundDurationMs);
            
            for (var i = 0; i < info.Beats; i++)
            {
                var click = new SignalGenerator()
                {
                    Gain = 1,
                    Frequency = i > 0 ? settings.ClickFreq : settings.AccentClickFreq,
                    Type = settings.WaveType
                }.Take(TimeSpan.FromMilliseconds(SoundDurationMs));

                providers[i] = new OffsetSampleProvider(click)
                {
                    LeadOut = silenceInterval
                };
            }

            return new ConcatenatingSampleProvider(providers);
        }
    }
}
