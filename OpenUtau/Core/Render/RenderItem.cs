using System;
using System.Collections.Generic;
using System.Text;
using xxHashSharp;

using OpenUtau.Core.USTx;

namespace OpenUtau.Core.Render
{
    class RenderItem
    {
        // For resampler
        public string RawFile;
        public int NoteNum;
        public int Velocity;
        public int Volume;
        public string StrFlags;
        public List<int> PitchData;
        public int RequiredLength;
		public double Tempo;
        public UOto Oto;

        // For connector
        public double SkipOver;
        public double PosMs;
        public double DurMs;
        public List<ExpPoint> Envelope;

        // Sound data
        public CachedSound Sound = null;

        public uint HashParameters()
        {
            return xxHash.CalculateHash(Encoding.UTF8.GetBytes(RawFile + " " + GetResamplerExeArgs()));
        }

        public string GetResamplerExeArgs()
        {
            // fresamp.exe <infile> <outfile> <tone> <velocity> <flags> <offset> <length_req>
            // <fixed_length> <endblank> <volume> <modulation> <pitch>
            return $"{MusicMath.GetNoteString(NoteNum)} {Velocity:D} {StrFlags} {Oto.Offset} {RequiredLength:D} {Oto.Consonant} {Oto.Cutoff} {Volume:D} {0:D} {Tempo} {String.Join(",", PitchData)}";
        }
    }
}
