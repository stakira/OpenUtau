using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace OpenUtau.Core.USTx
{
    public abstract class UExpression
    {
        public virtual UNote Parent { set; get; }
        public abstract string Name { get; }
        public abstract string Type { get; }
        public abstract object Data { set; get; }
        public abstract UExpression Clone(UNote newParent);
        public abstract UExpression Split(UNote newParent, int postick);
        //public abstract void Merge(int offset, UExpression exp);
        public abstract XElement ToXml(XNamespace ns);
        public abstract XElement ToTableXml(XNamespace ns);
    }

    public class FloatExpression : UExpression
    {
        public FloatExpression(UNote parent, string name) { this.Parent = parent; _name = name; }
        protected string _name;
        protected float _data;
        protected float _min = 0;
        protected float _max = 100;
        public override string Name { get { return _name; } }
        public override string Type { get { return "float"; } }
        public override object Data { set { _data = Math.Min(Max, Math.Max(Min, (float)value)); } get { return _data; } }
        public virtual float Min { set { _min = value; } get { return _min; } }
        public virtual float Max { set { _max = value; } get { return _max; } }
        public override UExpression Clone(UNote newParent) { return new FloatExpression(newParent, _name) { Min = this.Min, Max = this.Max, Data = Data }; }
        public override UExpression Split(UNote newParent, int postick) { var exp = Clone(newParent); return exp; }
        public override XElement ToXml(XNamespace ns)
        {
            return new XElement(ns + "exp", new XAttribute("t", Type), new XAttribute("id", Name), (float)Data);
        }
        public override XElement ToTableXml(XNamespace ns)
        {
            return new XElement(ns + "exp",
                new XAttribute("t", Type),
                new XAttribute("id", Name),
                new XAttribute("min", Min),
                new XAttribute("max", Max),
                (float)Data);
        }
        public static UExpression FromXml(XElement x, UNote parent, XNamespace ns)
        {
            return new FloatExpression(parent, x.Attribute("id").Value) { Data = float.Parse(x.Value) };
        }
    }

    public class CCExpression : FloatExpression
    {
        public CCExpression(UNote parent, string name) : base(parent, name) { }
        public override string Type { get { return "cc"; } }
        public override float Min { set { } get { return 0; } }
        public override float Max { set { } get { return 127; } }
        public override UExpression Clone(UNote newParent) { return new CCExpression(newParent, _name) { Data = Data }; }
        public override XElement ToXml(XNamespace ns)
        {
            return new XElement(ns + "exp",
                new XAttribute("t", Type),
                new XAttribute("id", Name),
                (float)Data);
        }
        public override XElement ToTableXml(XNamespace ns) { return ToXml(ns); }
        public static new UExpression FromXml(XElement x, UNote parent, XNamespace ns)
        {
            return new CCExpression(parent, x.Attribute("id").Value) { Data = float.Parse(x.Value) };
        }
    }

    public class ExpPoint : IComparable<ExpPoint>
    {
        public int X;
        public float Y;
        public ExpPoint(int x, float y) { X = x; Y = y; }
        public int CompareTo(ExpPoint other) { return this.X - ((ExpPoint)other).X; }
    }

    public class SerialExpression : UExpression
    {
        public SerialExpression(UNote parent, string name) { this.Parent = parent; _name = name; }
        protected string _name;
        protected List<ExpPoint> _data;
        protected float _min = 0;
        protected float _max = 127;
        public override string Name { get { return _name; } }
        public override string Type { get { return "serial"; } }
        public override object Data { set { _data = (List<ExpPoint>)value; } get { return _data; } }
        public virtual float Min { set { _min = value; } get { return _min; } }
        public virtual float Max { set { _max = value; } get { return _max; } }
        public void AddPoint(ExpPoint p) { _data.Add(p); _data.Sort(); }
        public void RemovePoint(ExpPoint p) { _data.Remove(p); }
        public override UExpression Clone(UNote newParent)
        {
            var data = new List<ExpPoint>();
            foreach (var p in this._data) data.Add(new ExpPoint(p.X, p.Y));
            return new SerialExpression(newParent, _name) { Min = this.Min, Max = this.Max, Data = data, };
        }
        public override UExpression Split(UNote newParent, int postick) {
            var newdata = new List<ExpPoint>();
            while (_data.Count > 0 && _data.Last().X >= postick) { newdata.Add(_data.Last()); _data.Remove(_data.Last()); }
            newdata.Reverse();
            return new SerialExpression(newParent, _name) { Min = this.Min, Max = this.Max, Data = newdata };
        }
        public override XElement ToXml(XNamespace ns) // FIXME
        {
            return new XElement(ns + "exp", new XAttribute("t", Type), new XAttribute("id", Name));
        }
        public override XElement ToTableXml(XNamespace ns)
        {
            return new XElement(ns + "exp",
                new XAttribute("t", Type),
                new XAttribute("id", Name),
                new XAttribute("min", Min),
                new XAttribute("max", Max));
        }
        public static UExpression FromXml(XElement x, UNote parent, XNamespace ns) // FIXME
        {
            return new SerialExpression(parent, x.Attribute("id").Value);
        }
    }

    //public class VibratoExpression : Expression
    //{
    //    public override string Name { get { return "Vibrato"; } }
    //    public int Amplitude;
    //    public float Start;
    //    public float End;
    //    public float Fade;
    //}

    //public class PitchExpression : Expression
    //{
    //    public override string Name { get { return "Pitch"; } }
    //    public List<Point> Points;
    //    public UPart Parent;
    //}

    //public class FineExpression : Expression
    //{
    //    public override string Name { get { return "Fine"; } }
    //    public List<Point> Points;
    //    public UPart Parent;
    //}

    //public class TimeExpression : Expression
    //{
    //    public float Value;
    //    public UNote Parent;
    //}
}
