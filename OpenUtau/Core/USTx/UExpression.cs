using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using JSON = System.Web.Script.Serialization.JavaScriptSerializer;

namespace OpenUtau.Core.USTx
{
    public abstract class UExpression
    {
        public UExpression(UNote parent, string name, string abbr) { _parent = parent; _name = name; _abbr = abbr; }

        protected UNote _parent;
        protected string _name;
        protected string _abbr;

        public UNote Parent { get { return _parent; } }
        public virtual string Name { get { return _name; } }
        public virtual string Abbr { get { return _abbr; } }

        public abstract string Type { get; }
        public abstract object Data { set; get; }

        public abstract UExpression Clone(UNote newParent);
        public abstract UExpression Split(UNote newParent, int offset);
    }

    public class IntExpression : UExpression
    {
        public IntExpression(UNote parent, string name, string abbr) : base(parent, name, abbr) { }
        protected int _data;
        protected int _min = 0;
        protected int _max = 100;
        public virtual int Min { set { _min = value; } get { return _min; } }
        public virtual int Max { set { _max = value; } get { return _max; } }
        public override string Type { get { return "int"; } }
        public override object Data { set { _data = Math.Min(Max, Math.Max(Min, (int)value)); } get { return _data; } }
        public override UExpression Clone(UNote newParent) { return new IntExpression(newParent, Name, Abbr) { Min = Min, Max = Max, Data = Data }; }
        public override UExpression Split(UNote newParent, int postick) { var exp = Clone(newParent); return exp; }
    }

    public class ExpPoint : IComparable<ExpPoint>
    {
        public double X;
        public double Y;
        public int CompareTo(ExpPoint other)
        {
            if (this.X > other.X) return 1;
            else if (this.X == other.X) return 0;
            else return -1;
        }
        public ExpPoint(double x, double y) { X = x; Y = y; }
        public ExpPoint Clone() { return new ExpPoint(X, Y); }
    }

    public enum PitchPointShape { SineInOut, Linear, SineIn, SineOut };
    public class PitchPoint : ExpPoint
    {
        public PitchPointShape Shape;
        public PitchPoint(double x, double y, PitchPointShape shape = PitchPointShape.SineIn) : base(x, y) { Shape = shape; }
        public new PitchPoint Clone() { return new PitchPoint(X, Y, Shape); }
    }

    public class PitchBendExpression : UExpression
    {
        public PitchBendExpression(UNote parent) : base(parent, "pitch", "PIT") {
            _data.Add(new PitchPoint(0, 0));
            _data.Add(new PitchPoint(0, 0));
        }
        protected List<PitchPoint> _data = new List<PitchPoint>();
        protected bool _snapFirst = true;
        public override string Type { get { return "pitch"; } }
        public override object Data { set { _data = (List<PitchPoint>)value; } get { return _data; } }
        public List<PitchPoint> Points { get { return _data; } }
        public bool SnapFirst { set { _snapFirst = value; } get { return _snapFirst; } }
        public void AddPoint(PitchPoint p) { _data.Add(p); _data.Sort(); }
        public void RemovePoint(PitchPoint p) { _data.Remove(p); }
        public override UExpression Clone(UNote newParent)
        {
            var data = new List<PitchPoint>();
            foreach (var p in this._data) data.Add(p.Clone());
            return new PitchBendExpression(newParent) { Data = data };
        }
        public override UExpression Split(UNote newParent, int offset)
        {
            var newdata = new List<PitchPoint>();
            while (_data.Count > 0 && _data.Last().X >= offset) { newdata.Add(_data.Last()); _data.Remove(_data.Last()); }
            newdata.Reverse();
            return new PitchBendExpression(newParent) { Data = newdata, SnapFirst = true };
        }
    }

    public class EnvelopeExpression : UExpression
    {
        public EnvelopeExpression(UNote parent) : base(parent, "envelope", "env")
        {
            _data.Add(new ExpPoint(0, 0));
            _data.Add(new ExpPoint(0, 100));
            _data.Add(new ExpPoint(0, 100));
            _data.Add(new ExpPoint(0, 100));
            _data.Add(new ExpPoint(0, 0));
        }
        protected List<ExpPoint> _data = new List<ExpPoint>();
        public override string Type { get { return "envelope"; } }
        public override object Data { set { _data = (List<ExpPoint>)value; } get { return _data; } }
        public List<ExpPoint> Points { get { return _data; } }
        public UPhoneme ParentPhoneme;
        public override UExpression Clone(UNote newParent)
        {
            var data = new List<ExpPoint>();
            foreach (var p in this._data) data.Add(p.Clone());
            return new EnvelopeExpression(newParent) { Data = data };
        }
        public override UExpression Split(UNote newParent, int offset)
        {
            var newdata = new List<ExpPoint>();
            // TODO
            return new EnvelopeExpression(newParent) { Data = newdata };
        }
    }

    public class VibratoExpression : UExpression
    {
        public VibratoExpression(UNote parent) : base(parent, "vibrato", "VBR") { }
        double _length;
        double _period;
        double _depth;
        double _in;
        double _out;
        double _shift;
        double _drift;
        public double Length { set { _length = Math.Max(0, Math.Min(100, value)); } get { return _length; } }
        public double Period { set { _period = Math.Max(64, Math.Min(512, value)); } get { return _period; } }
        public double Depth { set { _depth = Math.Max(5, Math.Min(200, value)); } get { return _depth; } }
        public double In { set { _in = Math.Max(0, Math.Min(100, value)); _out = Math.Min(_out, 100 - value); } get { return _in; } }
        public double Out { set { _out = Math.Max(0, Math.Min(100, value)); _in = Math.Min(_in, 100 - value); } get { return _out; } }
        public double Shift { set { _shift = Math.Max(0, Math.Min(100, value)); } get { return _shift; } }
        public double Drift { set { _drift = Math.Max(-100, Math.Min(100, value)); } get { return _drift; } }
        public override string Type { get { return "pitch"; } }
        public override object Data { set; get; }
        public override UExpression Clone(UNote newParent)
        {
            return new VibratoExpression(newParent)
            {
                _length = _length,
                _period = _period,
                _depth = _depth,
                _in = _in,
                _out = _out,
                _shift = _shift,
                _drift = _drift
            };
        }
        public override UExpression Split(UNote newParent, int postick) { var exp = Clone(newParent); return exp; }
    }
}
