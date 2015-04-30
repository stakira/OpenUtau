using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

using OpenUtau.Core.USTx;

namespace OpenUtau.Core.Formats
{
    static class VSQx
    {
        public const string vsq3NameSpace = @"http://www.yamaha.co.jp/vocaloid/schema/vsq3/";
        public const string vsq4NameSpace = @"http://www.yamaha.co.jp/vocaloid/schema/vsq4/";

        static public UProject Load(string file)
        {
            XmlDocument vsqx = new XmlDocument();
            
            try
            {
                vsqx.Load(file);
            }
            catch(Exception e)
            {
                System.Windows.MessageBox.Show(e.GetType().ToString() + "\n" + e.Message);
                return null;
            }

            XmlNamespaceManager nsmanager = new XmlNamespaceManager(vsqx.NameTable);
            nsmanager.AddNamespace("v3", vsq3NameSpace);
            nsmanager.AddNamespace("v4", vsq4NameSpace);

            XmlNode root;
            string nsPrefix;

            // Detect vsqx version
            root = vsqx.SelectSingleNode("v3:vsq3", nsmanager);
            
            if (root != null) nsPrefix = "v3:";
            else
            {
                root = vsqx.SelectSingleNode("v4:vsq4", nsmanager);

                if (root != null) nsPrefix = "v4:";
                else
                {
                    System.Windows.MessageBox.Show("Unrecognizable VSQx file format.");
                    return null;
                }
            }

            UProject uproject = new UProject();
            uproject.RegisterExpression(new IntExpression(null, "velocity", "VEL") { Data = 64, Min = 0, Max = 127 });
            uproject.RegisterExpression(new IntExpression(null, "opening", "OPE") { Data = 127, Min = 0, Max = 127 });
            uproject.RegisterExpression(new IntExpression(null, "accent", "ACC") { Data = 50, Min = 0, Max = 100 });
            uproject.RegisterExpression(new IntExpression(null, "decay", "DEC") { Data = 50, Min = 0, Max = 100 });

            string bpmPath = string.Format("{0}masterTrack/{0}tempo/{0}{1}", nsPrefix, nsPrefix == "v3:" ? "bpm" : "v");
            string beatperbarPath = string.Format("{0}masterTrack/{0}timeSig/{0}{1}", nsPrefix, nsPrefix == "v3:" ? "nume" : "nu");
            string beatunitPath = string.Format("{0}masterTrack/{0}timeSig/{0}{1}", nsPrefix, nsPrefix == "v3:" ? "denomi" : "de");
            string premeasurePath = string.Format("{0}masterTrack/{0}preMeasure", nsPrefix);
            string resolutionPath = string.Format("{0}masterTrack/{0}resolution", nsPrefix);
            string projectnamePath = string.Format("{0}masterTrack/{0}seqName", nsPrefix);
            string projectcommentPath = string.Format("{0}masterTrack/{0}comment", nsPrefix);
            string tracknamePath = string.Format("{0}{1}", nsPrefix, nsPrefix == "v3:" ? "trackName" : "name");
            string trackcommentPath = string.Format("{0}comment", nsPrefix);
            string tracknoPath = string.Format("{0}{1}", nsPrefix, nsPrefix == "v3:" ? "vsTrackNo" : "tNo");
            string partPath = string.Format("{0}{1}", nsPrefix, nsPrefix == "v3:" ? "musicalPart" : "vsPart");
            string partnamePath = string.Format("{0}{1}", nsPrefix, nsPrefix == "v3:" ? "partName" : "name");
            string partcommentPath = string.Format("{0}comment", nsPrefix);
            string notePath = string.Format("{0}note", nsPrefix);
            string postickPath = string.Format("{0}{1}", nsPrefix, nsPrefix == "v3:" ? "posTick" : "t");
            string durtickPath = string.Format("{0}{1}", nsPrefix, nsPrefix == "v3:" ? "durTick" : "dur");
            string notenumPath = string.Format("{0}{1}", nsPrefix, nsPrefix == "v3:" ? "noteNum" : "n");
            string velocityPath = string.Format("{0}{1}", nsPrefix, nsPrefix == "v3:" ? "velocity" : "v");
            string lyricPath = string.Format("{0}{1}", nsPrefix, nsPrefix == "v3:" ? "lyric" : "y");
            string phonemePath = string.Format("{0}{1}", nsPrefix, nsPrefix == "v3:" ? "phnms" : "p");
            string playtimePath = string.Format("{0}playTime", nsPrefix);
            string partstyleattrPath = string.Format("{0}{1}/{0}{2}", nsPrefix, nsPrefix == "v3:" ? "partStyle" : "pStyle", nsPrefix == "v3:" ? "attr" : "v");
            string notestyleattrPath = string.Format("{0}{1}/{0}{2}", nsPrefix, nsPrefix == "v3:" ? "noteStyle" : "nStyle", nsPrefix == "v3:" ? "attr" : "v");

            uproject.BPM = Convert.ToDouble(root.SelectSingleNode(bpmPath, nsmanager).InnerText) / 100;
            uproject.BeatPerBar = int.Parse(root.SelectSingleNode(beatperbarPath, nsmanager).InnerText);
            uproject.BeatUnit = int.Parse(root.SelectSingleNode(beatunitPath, nsmanager).InnerText);
            uproject.Resolution = int.Parse(root.SelectSingleNode(resolutionPath, nsmanager).InnerText);
            uproject.FilePath = file;
            uproject.Name = root.SelectSingleNode(projectnamePath, nsmanager).InnerText;
            uproject.Comment = root.SelectSingleNode(projectcommentPath, nsmanager).InnerText;

            int preMeasure = int.Parse(root.SelectSingleNode(premeasurePath, nsmanager).InnerText);
            int partPosTickShift = -preMeasure * uproject.Resolution * uproject.BeatPerBar * 4 / uproject.BeatUnit;

            foreach (XmlNode track in root.SelectNodes(nsPrefix + "vsTrack", nsmanager)) // track
            {
                UTrack utrack = new UTrack();
                uproject.Tracks.Add(utrack);

                utrack.Name = track.SelectSingleNode(tracknamePath, nsmanager).InnerText;
                utrack.Comment = track.SelectSingleNode(trackcommentPath, nsmanager).InnerText;
                utrack.TrackNo = int.Parse(track.SelectSingleNode(tracknoPath, nsmanager).InnerText);

                foreach (XmlNode part in track.SelectNodes(partPath, nsmanager)) // musical part
                {
                    UVoicePart upart = new UVoicePart();
                    uproject.Parts.Add(upart);

                    upart.Name = part.SelectSingleNode(partnamePath, nsmanager).InnerText;
                    upart.Comment = part.SelectSingleNode(partcommentPath, nsmanager).InnerText;
                    upart.PosTick = int.Parse(part.SelectSingleNode(postickPath, nsmanager).InnerText) + partPosTickShift;
                    upart.DurTick = int.Parse(part.SelectSingleNode(playtimePath, nsmanager).InnerText);
                    upart.TrackNo = utrack.TrackNo;

                    foreach (XmlNode note in part.SelectNodes(notePath, nsmanager))
                    {
                        UNote unote = uproject.CreateNote();
                        upart.Notes.Add(unote);

                        unote.PosTick = int.Parse(note.SelectSingleNode(postickPath, nsmanager).InnerText);
                        unote.DurTick = int.Parse(note.SelectSingleNode(durtickPath, nsmanager).InnerText);
                        unote.NoteNum = int.Parse(note.SelectSingleNode(notenumPath, nsmanager).InnerText);
                        unote.Lyric = note.SelectSingleNode(lyricPath, nsmanager).InnerText;
                        unote.Phonemes.Add(new UPhoneme() { Phoneme = note.SelectSingleNode(phonemePath, nsmanager).InnerText });

                        unote.Expressions["velocity"].Data = float.Parse(note.SelectSingleNode(velocityPath, nsmanager).InnerText);

                        foreach (XmlNode notestyle in note.SelectNodes(notestyleattrPath, nsmanager))
                        {
                            if (notestyle.Attributes["id"].Value == "opening")
                                unote.Expressions["opening"].Data = float.Parse(notestyle.InnerText);
                            else if (notestyle.Attributes["id"].Value == "accent")
                                unote.Expressions["accent"].Data = float.Parse(notestyle.InnerText);
                            else if (notestyle.Attributes["id"].Value == "decay")
                                unote.Expressions["decay"].Data = float.Parse(notestyle.InnerText);
                        }

                        unote.Channel = 0; // FIXME
                    }
                }
            }

            return uproject;
        }
    }
}
