using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using OpenUtau.Core.USTx;

namespace OpenUtau.Core.Formats
{
    class USTx
    {
        public const string ustxVersion = @"alpha";
        public const string ustxNameSpace = @"http://openutau.github.io/schema/ustx/";
        static XNamespace u = ustxNameSpace;

        static public UProject Load(string file)
        {
            UProject project = new UProject();

            XDocument xdoc = XDocument.Load(file);
            XElement xroot = xdoc.Descendants(u + "ustx").First();
            foreach (XElement xpart in xroot.Descendants(u + "voicepart"))
            {
                UVoicePart part = new UVoicePart()
                {
                    TrackNo = int.Parse(xpart.Element(u + "track").Value),
                    PosTick = int.Parse(xpart.Element(u + "pos").Value),
                    DurTick = int.Parse(xpart.Element(u + "dur").Value),
                    Name = xpart.Element(u + "name").Value,
                    Comment = xpart.Element(u + "comment").Value
                };
                project.Parts.Add(part);
                foreach (XElement xnote in xpart.Descendants(u + "note"))
                {
                    UNote note = new UNote()
                    {
                        PosTick = int.Parse(xnote.Element(u + "pos").Value),
                        DurTick = int.Parse(xnote.Element(u + "dur").Value),
                        NoteNum = int.Parse(xnote.Element(u + "n").Value),
                        Lyric = xnote.Element(u + "y").Value,
                        Phoneme = xnote.Element(u + "p").Value
                    };
                    part.Notes.Add(note);
                }
            }

            return project;
        }

        static public void Save(string file, UProject project)
        {
            XElement xroot = new XElement(u + "ustx",
                new XAttribute("xmlns", ustxNameSpace),
                new XElement(u + "version", new XCData(ustxVersion)));
            XDocument xdoc = new XDocument(xroot);
            foreach (UPart part in project.Parts)
            {
                if (part is UVoicePart){
                    XElement xpart = new XElement(u + "voicepart",
                        new XElement(u + "track", part.TrackNo),
                        new XElement(u + "pos", part.PosTick),
                        new XElement(u + "dur", part.DurTick),
                        new XElement(u + "name", new XCData(part.Name)),
                        new XElement(u + "comment", new XCData(part.Comment))
                        );
                    foreach (UNote note in ((UVoicePart)part).Notes)
                    {
                        XElement xexpressions = new XElement(u + "expressions");
                        foreach (var pair in note.Expressions) xexpressions.Add(pair.Value.ToXml(u));

                        XElement xnote = new XElement(u + "note",
                            new XElement(u + "pos", note.PosTick),
                            new XElement(u + "dur", note.DurTick),
                            new XElement(u + "n", note.NoteNum),
                            new XElement(u + "y", new XCData(note.Lyric)),
                            new XElement(u + "p", new XCData(note.Phoneme)),
                            xexpressions);
                        xpart.Add(xnote);
                    }
                    xroot.Add(xpart);
                }
            }
            xdoc.Save(file);
        }
    }
}
