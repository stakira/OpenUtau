using System;
using System.IO;
using System.Xml;

using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Formats {
    public static class VSQx {
        public const string vsq3NameSpace = @"http://www.yamaha.co.jp/vocaloid/schema/vsq3/";
        public const string vsq4NameSpace = @"http://www.yamaha.co.jp/vocaloid/schema/vsq4/";

        static public UProject Load(string file) {
            XmlDocument vsqx = new XmlDocument();
            vsqx.Load(file);

            XmlNamespaceManager nsmanager = new XmlNamespaceManager(vsqx.NameTable);
            nsmanager.AddNamespace("v3", vsq3NameSpace);
            nsmanager.AddNamespace("v4", vsq4NameSpace);

            XmlNode root;
            string nsPrefix;

            // Detect vsqx version
            if ((root = vsqx.SelectSingleNode("v3:vsq3", nsmanager)) != null) {
                nsPrefix = "v3:";
            } else if ((root = vsqx.SelectSingleNode("v4:vsq4", nsmanager)) != null) {
                nsPrefix = "v4:";
            } else {
                throw new FileFormatException("Unrecognizable VSQx file format.");
            }

            UProject uproject = new UProject();
            uproject.RegisterExpression(new UExpressionDescriptor("velocity", "vel", 0, 127, 64));
            uproject.RegisterExpression(new UExpressionDescriptor("volume", "vol", 0, 200, 100));
            uproject.RegisterExpression(new UExpressionDescriptor("opening", "ope", 0, 127, 127));
            uproject.RegisterExpression(new UExpressionDescriptor("accent", "acc", 0, 100, 50));
            uproject.RegisterExpression(new UExpressionDescriptor("decay", "dec", 0, 100, 50));

            string bpmPath = $"{nsPrefix}masterTrack/{nsPrefix}tempo/{nsPrefix}{(nsPrefix == "v3:" ? "bpm" : "v")}";
            string beatperbarPath = $"{nsPrefix}masterTrack/{nsPrefix}timeSig/{nsPrefix}{(nsPrefix == "v3:" ? "nume" : "nu")}";
            string beatunitPath = $"{nsPrefix}masterTrack/{nsPrefix}timeSig/{nsPrefix}{(nsPrefix == "v3:" ? "denomi" : "de")}";
            string premeasurePath = $"{nsPrefix}masterTrack/{nsPrefix}preMeasure";
            string resolutionPath = $"{nsPrefix}masterTrack/{nsPrefix}resolution";
            string projectnamePath = $"{nsPrefix}masterTrack/{nsPrefix}seqName";
            string projectcommentPath = $"{nsPrefix}masterTrack/{nsPrefix}comment";
            string trackPath = $"{nsPrefix}vsTrack";
            string tracknamePath = $"{nsPrefix}{(nsPrefix == "v3:" ? "trackName" : "name")}";
            string trackcommentPath = $"{nsPrefix}comment";
            string tracknoPath = $"{nsPrefix}{(nsPrefix == "v3:" ? "vsTrackNo" : "tNo")}";
            string partPath = $"{nsPrefix}{(nsPrefix == "v3:" ? "musicalPart" : "vsPart")}";
            string partnamePath = $"{nsPrefix}{(nsPrefix == "v3:" ? "partName" : "name")}";
            string partcommentPath = $"{nsPrefix}comment";
            string notePath = $"{nsPrefix}note";
            string postickPath = $"{nsPrefix}{(nsPrefix == "v3:" ? "posTick" : "t")}";
            string durtickPath = $"{nsPrefix}{(nsPrefix == "v3:" ? "durTick" : "dur")}";
            string notenumPath = $"{nsPrefix}{(nsPrefix == "v3:" ? "noteNum" : "n")}";
            string velocityPath = $"{nsPrefix}{(nsPrefix == "v3:" ? "velocity" : "v")}";
            string lyricPath = $"{nsPrefix}{(nsPrefix == "v3:" ? "lyric" : "y")}";
            string phonemePath = $"{nsPrefix}{(nsPrefix == "v3:" ? "phnms" : "p")}";
            string playtimePath = $"{nsPrefix}playTime";
            string partstyleattrPath = $"{nsPrefix}{(nsPrefix == "v3:" ? "partStyle" : "pStyle")}/{nsPrefix}{(nsPrefix == "v3:" ? "attr" : "v")}";
            string notestyleattrPath = $"{nsPrefix}{(nsPrefix == "v3:" ? "noteStyle" : "nStyle")}/{nsPrefix}{(nsPrefix == "v3:" ? "attr" : "v")}";

            uproject.bpm = Convert.ToDouble(root.SelectSingleNode(bpmPath, nsmanager).InnerText) / 100;
            uproject.beatPerBar = int.Parse(root.SelectSingleNode(beatperbarPath, nsmanager).InnerText);
            uproject.beatUnit = int.Parse(root.SelectSingleNode(beatunitPath, nsmanager).InnerText);
            uproject.resolution = int.Parse(root.SelectSingleNode(resolutionPath, nsmanager).InnerText);
            uproject.FilePath = file;
            uproject.name = root.SelectSingleNode(projectnamePath, nsmanager).InnerText;
            uproject.comment = root.SelectSingleNode(projectcommentPath, nsmanager).InnerText;

            int preMeasure = int.Parse(root.SelectSingleNode(premeasurePath, nsmanager).InnerText);
            int partPosTickShift = -preMeasure * uproject.resolution * uproject.beatPerBar * 4 / uproject.beatUnit;

            USinger usinger = new USinger("");

            foreach (XmlNode track in root.SelectNodes(trackPath, nsmanager)) // track
            {
                UTrack utrack = new UTrack() { Singer = usinger, TrackNo = uproject.tracks.Count };
                uproject.tracks.Add(utrack);

                //utrack.Name = track.SelectSingleNode(tracknamePath, nsmanager).InnerText;
                //utrack.Comment = track.SelectSingleNode(trackcommentPath, nsmanager).InnerText;
                utrack.TrackNo = int.Parse(track.SelectSingleNode(tracknoPath, nsmanager).InnerText);

                foreach (XmlNode part in track.SelectNodes(partPath, nsmanager)) // musical part
                {
                    UVoicePart upart = new UVoicePart();
                    uproject.parts.Add(upart);

                    upart.name = part.SelectSingleNode(partnamePath, nsmanager).InnerText;
                    upart.comment = part.SelectSingleNode(partcommentPath, nsmanager).InnerText;
                    upart.position = int.Parse(part.SelectSingleNode(postickPath, nsmanager).InnerText) + partPosTickShift;
                    upart.Duration = int.Parse(part.SelectSingleNode(playtimePath, nsmanager).InnerText);
                    upart.trackNo = utrack.TrackNo;

                    foreach (XmlNode note in part.SelectNodes(notePath, nsmanager)) {
                        UNote unote = uproject.CreateNote();

                        unote.position = int.Parse(note.SelectSingleNode(postickPath, nsmanager).InnerText);
                        unote.duration = int.Parse(note.SelectSingleNode(durtickPath, nsmanager).InnerText);
                        unote.noteNum = int.Parse(note.SelectSingleNode(notenumPath, nsmanager).InnerText);
                        unote.lyric = note.SelectSingleNode(lyricPath, nsmanager).InnerText;
                        unote.phonemes[0].phoneme = note.SelectSingleNode(phonemePath, nsmanager).InnerText;

                        unote.expressions["vel"].value = int.Parse(note.SelectSingleNode(velocityPath, nsmanager).InnerText);

                        foreach (XmlNode notestyle in note.SelectNodes(notestyleattrPath, nsmanager)) {
                            if (notestyle.Attributes["id"].Value == "opening")
                                unote.expressions["ope"].value = int.Parse(notestyle.InnerText);
                            else if (notestyle.Attributes["id"].Value == "accent")
                                unote.expressions["acc"].value = int.Parse(notestyle.InnerText);
                            else if (notestyle.Attributes["id"].Value == "decay")
                                unote.expressions["dec"].value = int.Parse(notestyle.InnerText);
                        }

                        unote.pitch.data[0].X = -(float)uproject.TickToMillisecond(Math.Min(15, unote.duration / 3));
                        unote.pitch.data[1].X = -unote.pitch.data[0].X;
                        upart.notes.Add(unote);
                    }
                }
            }

            uproject.AfterLoad();
            uproject.Validate();
            return uproject;
        }
    }
}
