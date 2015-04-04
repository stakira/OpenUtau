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
            nsmanager.AddNamespace("v3", "http://www.yamaha.co.jp/vocaloid/schema/vsq3/");
            nsmanager.AddNamespace("v4", "http://www.yamaha.co.jp/vocaloid/schema/vsq4/");

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

            string bpmPath = string.Format("{0}masterTrack/{0}tempo/{0}{1}", nsPrefix, nsPrefix == "v3:" ? "bpm" : "v");
            string beatperbarPath = string.Format("{0}masterTrack/{0}timeSig/{0}{1}", nsPrefix, nsPrefix == "v3:" ? "nume" : "nu");
            string beatunitPath = string.Format("{0}masterTrack/{0}timeSig/{0}{1}", nsPrefix, nsPrefix == "v3:" ? "denomi" : "de");
            string prebarPath = string.Format("{0}masterTrack/{0}preMeasure", nsPrefix);
            string resolutionPath = string.Format("{0}masterTrack/{0}resolution", nsPrefix);
            string projectnamePath = string.Format("{0}masterTrack/{0}seqName", nsPrefix);
            string projectcommentPath = string.Format("{0}masterTrack/{0}comment", nsPrefix);
            string tracknamePath = string.Format("{0}{1}", nsPrefix, nsPrefix == "v3:" ? "trackName" : "name");
            string trackcommentPath = string.Format("{0}comment", nsPrefix);
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

            uproject.BPM = Convert.ToDouble(root.SelectSingleNode(bpmPath, nsmanager).InnerText) / 100;
            uproject.BeatPerBar = Convert.ToInt32(root.SelectSingleNode(beatperbarPath, nsmanager).InnerText);
            uproject.BeatUnit = Convert.ToInt32(root.SelectSingleNode(beatunitPath, nsmanager).InnerText);
            uproject.PreBar = Convert.ToInt32(root.SelectSingleNode(prebarPath, nsmanager).InnerText);
            uproject.Resolution = Convert.ToInt32(root.SelectSingleNode(resolutionPath, nsmanager).InnerText);
            uproject.FilePath = file;
            uproject.Name = root.SelectSingleNode(projectnamePath, nsmanager).InnerText;
            uproject.Comment = root.SelectSingleNode(projectcommentPath, nsmanager).InnerText;

            foreach (XmlNode track in root.SelectNodes(nsPrefix + "vsTrack", nsmanager)) // track
            {
                UTrack utrack = new UTrack(uproject);
                uproject.Tracks.Add(utrack);

                utrack.Name = track.SelectSingleNode(tracknamePath, nsmanager).InnerText;
                utrack.Comment = track.SelectSingleNode(trackcommentPath, nsmanager).InnerText;

                foreach (XmlNode part in track.SelectNodes(partPath, nsmanager)) // musical part
                {
                    UPart upart = new UPart(utrack);
                    utrack.Parts.Add(upart);

                    upart.Name = part.SelectSingleNode(partnamePath, nsmanager).InnerText;
                    upart.Comment = part.SelectSingleNode(partcommentPath, nsmanager).InnerText;
                    upart.PosTick = Convert.ToInt32(part.SelectSingleNode(postickPath, nsmanager).InnerText);
                    upart.DurTick = Convert.ToInt32(part.SelectSingleNode(playtimePath, nsmanager).InnerText);

                    foreach (XmlNode note in part.SelectNodes(notePath, nsmanager))
                    {
                        UNote unote = new UNote(upart);
                        upart.Notes.Add(unote);

                        unote.PosTick = Convert.ToInt32(note.SelectSingleNode(postickPath, nsmanager).InnerText);
                        unote.DurTick = Convert.ToInt32(note.SelectSingleNode(durtickPath, nsmanager).InnerText);
                        unote.NoteNum = Convert.ToInt32(note.SelectSingleNode(notenumPath, nsmanager).InnerText);
                        unote.Velocity = Convert.ToInt32(note.SelectSingleNode(velocityPath, nsmanager).InnerText);
                        unote.Lyric = note.SelectSingleNode(lyricPath, nsmanager).InnerText;
                        unote.Phoneme = note.SelectSingleNode(phonemePath, nsmanager).InnerText;

                        // Old
                        unote.offset = ((double)unote.PosTick) / 240;
                        unote.length = ((double)unote.DurTick) / 240;
                        unote.keyNo = unote.NoteNum;
                        unote.Channel = 0;
                    }
                }
            }
            return uproject;
        }
    }
}
