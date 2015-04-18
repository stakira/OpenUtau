using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OpenUtau.Core.USTx
{
    public abstract class Expression
    {
        public abstract string Name;
    }

    public class CCTypeExpression : Expression
    {
        public byte Value;
        public UNote Parent;
    }

    public class VibratoExpression : Expression
    {
        public override string Name { get { return "Vibrato"; } }
        public int Amplitude;
        public float Start;
        public float End;
        public float Fade;
        public UNote Parent;
    }

    public class PitchExpression : Expression
    {
        public override string Name { get { return "Pitch"; } }
        public List<Point> Points;
        public UPart Parent;
    }

    public class FineExpression : Expression
    {
        public override string Name { get { return "Pitch"; } }
        public List<Point> Points;
        public UPart Parent;
    }

    public class TimeExpression : Expression
    {
        public float Value;
        public UNote Parent;
    }
}
