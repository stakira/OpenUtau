using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core
{
    public static class MusicMath
    {
        public static string[] noteStrings = new String[12] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        public static string GetKeyString(int keyNo) { return keyNo < 0 ? "" : noteStrings[keyNo % 12] + (keyNo / 12 - 2).ToString(); }

        public static double TickToNote(int tick, int resolution) { return tick / resolution; }

        public static int[] BlackNoteNums = { 1, 3, 6, 8, 10 };
        public static bool IsBlackKey(int noteNum) { return BlackNoteNums.Contains(noteNum % 12); }

        public static bool IsCenterKey(int noteNum) { return noteNum % 12 == 0; }

        public static double[] zoomRatios = { 1.0, 1.0 / 2, 1.0 / 4, 1.0 / 8, 1.0 / 16, 1.0 / 32, 1.0 / 64, 1.0 / 128, 1.0 / 256 };

        public static double getZoomRatio(double wholeNoteWidth, int beatPerBar, int beatUnit, double minWidth)
        {
            int i;

            switch (beatUnit)
            {
                case 2: i = 1; break;
                case 4: i = 2; break;
                case 8: i = 4; break;
                case 16: i = 4; break;
                default: throw new Exception("Invalid beat unit.");
            }

            if (beatPerBar % 4 == 0) i--; // level below bar is half bar, or 2 beatunit
            // else // otherwise level below bar is beat unit

            if (wholeNoteWidth * beatPerBar <= minWidth * beatUnit)
            {
                return beatPerBar / beatUnit;
            }
            else
            {
                while (i + 1 < zoomRatios.Length && wholeNoteWidth * zoomRatios[i + 1] > OpenUtau.UI.UIConstants.NoteMinDisplayWidth) i++;
                return zoomRatios[i];
            }
        }
    }
}
