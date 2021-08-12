using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Formats
{
    public enum ProjectFormats { Unknown, Vsq3, Vsq4, Ust, Ustx };

    static class Formats
    {
        const string ustMatch = "[#SETTING]";
        const string vsq3Match = VSQx.vsq3NameSpace;
        const string vsq4Match = VSQx.vsq4NameSpace;

        public static ProjectFormats DetectProjectFormat(string file)
        {
            if (!IsTextFile(file)) return ProjectFormats.Unknown;
            string contents = "";
            StreamReader streamReader = null;
            try
            {
                streamReader = File.OpenText(file);
                for (int i = 0; i < 10; i++) {
                    if (streamReader.Peek() < 0) break;
                    contents += streamReader.ReadLine();
                }
            }
            catch (Exception e)
            {
                if (streamReader != null) streamReader.Dispose();
                System.Windows.MessageBox.Show(e.GetType().ToString() + "\n" + e.Message);
                return ProjectFormats.Unknown;
            }

            if (contents.Contains(ustMatch)) return ProjectFormats.Ust;
            else if (contents.Length > 0 && contents[0] == '{') return ProjectFormats.Ustx;
            else if (contents.Contains(vsq3Match)) return ProjectFormats.Vsq3;
            else if (contents.Contains(vsq4Match)) return ProjectFormats.Vsq4;
            else return ProjectFormats.Unknown;
        }

        public static void LoadProject(string file)
        {
            ProjectFormats format = DetectProjectFormat(file);
            UProject project = null;

            if (format == ProjectFormats.Ustx) { project = Ustx.Load(file); }
            else if (format == ProjectFormats.Vsq3 || format == ProjectFormats.Vsq4) { project = VSQx.Load(file); }
            else if (format == ProjectFormats.Ust) { project = Ust.Load(file); }
            else
            {
                System.Windows.MessageBox.Show("Unknown file format");
            }
            if (project != null) { DocManager.Inst.ExecuteCmd(new LoadProjectNotification(project)); }
        }

        public static bool IsTextFile(string file)
        {
            FileStream stream = null;
            try
            {
                FileInfo info = new FileInfo(file);
                if (info.Length > 8 * 1024 * 1024) return false;
                stream = info.OpenRead();
                byte[] data = new byte[1024];
                stream.Read(data, 0, 1024);
                int i = 1;
                while (i < 1024 && i < info.Length)
                {
                    if (data[i - 1] == 0 && data[i] == 0) return false;
                    i++;
                }
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (stream != null) stream.Dispose();
            }
        }
    }
}
