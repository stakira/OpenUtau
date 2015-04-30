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
            else if (contents.Length > 0 && contents[0] == '{') return ProjectFormats.Ustx;
            else if (contents.Contains(vsq3Match)) return ProjectFormats.Vsq3;
            else if (contents.Contains(vsq4Match)) return ProjectFormats.Vsq4;
            else return ProjectFormats.Unknown;
        }

        static public void LoadProject(string file)
        {
            ProjectFormats format = DetectProjectFormat(file);
            UProject project = null;

            if (format == ProjectFormats.Ustx) { project = USTx.Load(file); }
            else if (format == ProjectFormats.Vsq3 || format == ProjectFormats.Vsq4) { project = VSQx.Load(file); }
            else if (format == ProjectFormats.Ust) { project = Ust.Load(file); }
            else
            {
                System.Windows.MessageBox.Show("Unknown file format");
            }
            if (project != null) { DocManager.Inst.ExecuteCmd(new LoadProjectNotification(project)); }
        }
    }
}
