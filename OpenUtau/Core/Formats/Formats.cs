using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using OpenUtau.Core.USTx;

namespace OpenUtau.Core.Formats
{
    public enum ProjectFormats { Unknown, Vsq3, Vsq4, Ust, Ustx };

    static class Formats
    {
        const string ustMatch = "[#SETTING]";
        const string ustxMatch = USTx.ustxNameSpace;
        const string vsq3Match = VSQx.vsq3NameSpace;
        const string vsq4Match = VSQx.vsq4NameSpace;

        static public ProjectFormats DetectProjectFormat(string file)
        {
            string contents;
            try
            {
                contents = File.ReadAllText(file);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.GetType().ToString() + "\n" + e.Message);
                return ProjectFormats.Unknown;
            }

            if (contents.Contains(ustMatch)) return ProjectFormats.Ust;
            else if (contents.Contains(ustxMatch)) return ProjectFormats.Ustx;
            else if (contents.Contains(vsq3Match)) return ProjectFormats.Vsq3;
            else if (contents.Contains(vsq4Match)) return ProjectFormats.Vsq4;
            else return ProjectFormats.Unknown;
        }

        static public UProject LoadProject(string file)
        {
            ProjectFormats format = DetectProjectFormat(file);

            if (format == ProjectFormats.Ustx) { return USTx.Load(file); }
            else if (format == ProjectFormats.Vsq3 || format == ProjectFormats.Vsq4) { return VSQx.Load(file); }
            else if (format == ProjectFormats.Ust) { return Ust.Load(file); }
            else
            {
                System.Windows.MessageBox.Show("Unknown file format");
                return null;
            }
        }
    }
}
