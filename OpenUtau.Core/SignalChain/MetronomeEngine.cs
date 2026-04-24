using System;
using OpenUtau.Core.Util;

namespace OpenUtau.Core.SignalChain {
    class MetronomeEngine : ISignalSource {
        private const int MetronomeAttackMs = 5;
        private const int MetronomeBeatReleaseMs = 10;
        private const int MetronomeBarReleaseMs = 10;
        private const double AccentFrequencyOffset = 880;

        private readonly ToneGenerator toneGenerator = new ToneGenerator(0.6f);
        private readonly MetronomeScheduler scheduler = new MetronomeScheduler();

        private static double MetronomeBarFreq => Preferences.Default.MetronomeHighFrequency;
        private static double MetronomeBeatFreq => Preferences.Default.MetronomeLowFrequency;
        private static double MetronomeBarAccentFreq => Preferences.Default.MetronomeHighFrequency + AccentFrequencyOffset;
        private static double MetronomeBeatAccentFreq => Preferences.Default.MetronomeLowFrequency + AccentFrequencyOffset;

        public bool Enabled { get; private set; }

        public bool IsReady(int position, int count) {
            return toneGenerator.IsReady(position, count);
        }

        public int Mix(int position, float[] buffer, int offset, int count) {
            return toneGenerator.Mix(position, buffer, offset, count);
        }

        public void SetEnabled(bool enabled, TimeAxis? timeAxis = null, int tick = -1) {
            Enabled = enabled;
            if (!enabled) {
                Stop();
                return;
            }
            if (timeAxis != null && tick >= 0) {
                scheduler.Reset(timeAxis, tick);
            }
        }

        public void Stop() {
            toneGenerator.EndAllTones();
            scheduler.Clear();
        }

        public void StartPlayback(TimeAxis timeAxis, int tick) {
            ArgumentNullException.ThrowIfNull(timeAxis);
            toneGenerator.EndAllTones();
            if (Enabled) {
                scheduler.Reset(timeAxis, tick);
            } else {
                scheduler.Clear();
            }
        }

        public void UpdateSchedule(TimeAxis timeAxis, int tick) {
            ArgumentNullException.ThrowIfNull(timeAxis);
            if (!Enabled) {
                scheduler.Clear();
                return;
            }
            scheduler.Reset(timeAxis, tick);
        }

        public void TryPlay(int tick, double currentMs, TimeAxis timeAxis) {
            ArgumentNullException.ThrowIfNull(timeAxis);
            if (!Enabled || !scheduler.IsScheduled || currentMs < scheduler.NextMs) {
                return;
            }
            PlayClick(scheduler.NextBeat == 0);
            scheduler.Advance(timeAxis);
            scheduler.SkipPast(timeAxis, currentMs);
            if (scheduler.IsScheduled && scheduler.NextTick <= tick) {
                scheduler.Reset(timeAxis, tick);
            }
        }

        private void PlayClick(bool accent) {
            if (accent) {
                toneGenerator.StartTones(
                    (MetronomeBarFreq, MetronomeAttackMs, MetronomeBarReleaseMs),
                    (MetronomeBarAccentFreq, MetronomeAttackMs, MetronomeBeatReleaseMs));
                toneGenerator.EndTones(MetronomeBarFreq, MetronomeBarAccentFreq);
            } else {
                toneGenerator.StartTones(
                    (MetronomeBeatFreq, MetronomeAttackMs, MetronomeBeatReleaseMs),
                    (MetronomeBeatAccentFreq, MetronomeAttackMs, MetronomeBeatReleaseMs / 2));
                toneGenerator.EndTones(MetronomeBeatFreq, MetronomeBeatAccentFreq);
            }
        }
    }
}
