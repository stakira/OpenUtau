using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace OpenUtau.Core
{
    class VSQx
    {
        static public void Parse(string file, OpenUtau.Core.TrackPart trackpart)
        {
            if (!File.Exists(file))
            {
                System.Diagnostics.Debug.Print("File not exist");
            }
            else
            {
                System.Diagnostics.Debug.Print("File exist");
            }

            XmlDocument doc = new XmlDocument();
            
            try
            {
                doc.Load(file);
            }
            catch
            {
                System.Diagnostics.Debug.Print("File not load");
            }
            System.Diagnostics.Debug.Print(doc.NamespaceURI);

            XmlNamespaceManager nsmanager = new XmlNamespaceManager(doc.NameTable);
            nsmanager.AddNamespace("v3", "http://www.yamaha.co.jp/vocaloid/schema/vsq3/");

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            watch.Start();
            XmlNode root = doc.SelectSingleNode("v3:vsq3", nsmanager);

            double tempo = Convert.ToDouble(root.SelectSingleNode("v3:masterTrack/v3:tempo/v3:bpm", nsmanager).InnerText) / 100;
            int beatPerBar = Convert.ToInt32(root.SelectSingleNode("v3:masterTrack/v3:timeSig/v3:nume", nsmanager).InnerText);
            int beatUnit = Convert.ToInt32(root.SelectSingleNode("v3:masterTrack/v3:timeSig/v3:denomi", nsmanager).InnerText);
            /*
            foreach (XmlNode track in root.SelectNodes("v3:vsTrack", nsmanager))
            {
                int trackNo = Convert.ToInt32(track.SelectSingleNode("v3:vsTrackNo", nsmanager).InnerText);
                string trackName = track.SelectSingleNode("v3:trackName", nsmanager).InnerText;

                foreach (XmlNode part in track.SelectNodes("v3:musicalPart", nsmanager))
                {
                    string partName = part.SelectSingleNode("v3:partName", nsmanager).InnerText;
                    foreach (XmlNode note in part.SelectNodes("v3:note", nsmanager))
                    {
                        int posTick = Convert.ToInt32(note.SelectSingleNode("v3:posTick", nsmanager).InnerText);
                        double offset = ((double)posTick) / 480;
                        int durTick = Convert.ToInt32(note.SelectSingleNode("v3:durTick", nsmanager).InnerText);
                        double length = ((double)durTick) / 480;
                        int noteNum = Convert.ToInt32(note.SelectSingleNode("v3:noteNum", nsmanager).InnerText);
                        int velocity = Convert.ToInt32(note.SelectSingleNode("v3:velocity", nsmanager).InnerText);
                        string lyric = note.SelectSingleNode("v3:lyric", nsmanager).InnerText;
                        string phnms = note.SelectSingleNode("v3:phnms", nsmanager).InnerText;
                    }
                }
            }
            */

            XmlNode part = root.SelectNodes("v3:vsTrack", nsmanager)[0].SelectNodes("v3:musicalPart", nsmanager)[0];
            foreach (XmlNode note in part.SelectNodes("v3:note", nsmanager))
            {
                int posTick = Convert.ToInt32(note.SelectSingleNode("v3:posTick", nsmanager).InnerText);
                double offset = ((double)posTick) / 240;
                int durTick = Convert.ToInt32(note.SelectSingleNode("v3:durTick", nsmanager).InnerText);
                double length = ((double)durTick) / 240;
                int noteNum = Convert.ToInt32(note.SelectSingleNode("v3:noteNum", nsmanager).InnerText);
                int velocity = Convert.ToInt32(note.SelectSingleNode("v3:velocity", nsmanager).InnerText);
                string lyric = note.SelectSingleNode("v3:lyric", nsmanager).InnerText;
                string phnms = note.SelectSingleNode("v3:phnms", nsmanager).InnerText;

                trackpart.AddNote(new Note() { offset = offset, length = length, keyNo = noteNum, Lyric = lyric });
            }

            watch.Stop();
            System.Diagnostics.Debug.Print(watch.Elapsed.TotalMilliseconds.ToString() + " ms");
        }
    }
}
