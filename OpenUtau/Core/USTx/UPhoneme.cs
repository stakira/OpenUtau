using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    /// <summary>
    /// The basic unit of synthesis.
    /// </summary>
    public class UPhoneme
    {
        public UNote Parent;
        public int PosTick;
        public int DurTick;
        public int EndTick { get { return PosTick + DurTick; } }
        public string Phoneme;
        public string PhonemeRemapped;
        public bool AutoTiming = true;
        public bool AutoRemapped = true;

        public float PreUtter;
        public float Overlap;
        public UOto Oto;
        public bool OverlapCorrection = true;
        public EnvelopeExpression Envelope;

        public bool IsValid;

        public UPhoneme() { Envelope = new EnvelopeExpression(this.Parent) { ParentPhoneme = this }; }
        public UPhoneme Clone(UNote newParent) { var p = new UPhoneme() { Parent = newParent }; return p; }
    }
}
