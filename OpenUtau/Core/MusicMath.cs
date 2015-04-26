using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core
{
    public struct MusicTiming
    {
        public double BPM;
        public int BeatPerBar;
        public int BeatUnit;
        public int Resolution;
        public MusicTiming(double bpm, int beatperbar, int beatunit, int resolution)
        {
            BPM = bpm;
            BeatPerBar = beatperbar;
            BeatUnit = beatunit;
            Resolution = resolution;
        }
    }

    public static class MusicMath
    {
        public static string[] noteStrings = new String[12] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        public static string GetNoteString(int noteNum) { return noteNum < 0 ? "" : noteStrings[noteNum % 12] + (noteNum / 12 - 1).ToString(); }

        public static double TickToNote(int tick, int resolution) { return tick / resolution; }

        public static int[] BlackNoteNums = { 1, 3, 6, 8, 10 };
        public static bool IsBlackKey(int noteNum) { return BlackNoteNums.Contains(noteNum % 12); }

        public static bool IsCenterKey(int noteNum) { return noteNum % 12 == 0; }

        public static double[] zoomRatios = { 4.0, 2.0, 1.0, 1.0 / 2, 1.0 / 4, 1.0 / 8, 1.0 / 16, 1.0 / 32, 1.0 / 64 };

        public static double getZoomRatio(double quarterWidth, int beatPerBar, int beatUnit, double minWidth)
        {
            int i;

            switch (beatUnit)
            {
                case 2: i = 0; break;
                case 4: i = 1; break;
                case 8: i = 2; break;
                case 16: i = 3; break;
                default: throw new Exception("Invalid beat unit.");
            }

            if (beatPerBar % 4 == 0) i--; // level below bar is half bar, or 2 beatunit
            // else // otherwise level below bar is beat unit

            if (quarterWidth * beatPerBar * 4 <= minWidth * beatUnit)
            {
                return beatPerBar / beatUnit * 4;
            }
            else
            {
                while (i + 1 < zoomRatios.Length && quarterWidth * zoomRatios[i + 1] > minWidth) i++;
                return zoomRatios[i];
            }
        }

        public static double TickToMillisecond(int tick, MusicTiming timing)
        {
            return tick * 60000.0 / timing.BPM * timing.BeatUnit / 4 / timing.Resolution;
        }

        public static int TimeToTick(double ms, MusicTiming timing)
        {
            return (int)Math.Ceiling(ms / 60000.0 * timing.BPM / timing.BeatUnit * 4 * timing.Resolution);
        }
        public static int TimeToTick(TimeSpan timespan, double BPM, int BeatUnit, int Resolution)
        {
            return (int)Math.Ceiling(timespan.TotalMinutes * BPM / BeatUnit * 4 * Resolution);
        }

        public static int TimeToTick(double ms, double BPM, int BeatUnit, int Resolution)
        {
            return (int)Math.Ceiling(ms / 60000.0 * BPM / BeatUnit * 4 * Resolution);
        }
    }
}
