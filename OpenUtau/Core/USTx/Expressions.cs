using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace OpenUtau.Core.USTx
{
    public abstract class Expression
    {
        public virtual UNote Parent { set; get; }
        public abstract string Name { get; }
        public abstract object Data { set; get; }
        public abstract Expression Clone(UNote newParent);
        public abstract Expression Split(UNote newParent, int postick);
        public abstract void Trim(int postick);
        public abstract XElement ToXml(XNamespace ns);
    }

    public class CCExpression : Expression
    {
        public const string Type = "cc";
        public CCExpression(UNote parent, string name) { this.Parent = parent; _name = name; }
        string _name;
        int _data;
        public override string Name { get { return _name; } }
        public override object Data { set { _data = Math.Min(127, Math.Max(0, (int)value)); } get { return _data; } }
        public override Expression Clone(UNote newParent) { return new CCExpression(newParent, _name) { Data = Data }; }
        public override Expression Split(UNote newParent, int postick) { var note = Clone(newParent); return note; }
        public override void Trim(int postick) { }
        public override XElement ToXml(XNamespace ns) { return new XElement(ns + "exp", new XAttribute("t", Type), new XAttribute("id", Name), (int)Data); }
        public static Expression FromXml(XElement x, UNote parent, XNamespace ns) { return new CCExpression(parent, x.Attribute("id").Value) { Data = int.Parse(x.Value) }; }
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
