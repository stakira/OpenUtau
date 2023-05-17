using System.Collections.Generic;
using System.IO;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Format {
    public enum ProjectFormats { Unknown, Vsq3, Vsq4, Ust, Ustx, Midi };

    public static class Formats {
        const string ustMatch = "[#SETTING]";
        const string ustxMatchJson = "\"ustxVersion\":";
        const string ustxMatchYaml = "ustx_version:";
        const string vsq3Match = VSQx.vsq3NameSpace;
        const string vsq4Match = VSQx.vsq4NameSpace;
        const string midiMatch = "MThd";

        public static ProjectFormats DetectProjectFormat(string file) {
            var lines = new List<string>();
            using (var reader = new StreamReader(file)) {
                for (int i = 0; i < 10 && !reader.EndOfStream; ++i) {
                    lines.Add(reader.ReadLine());
                }
            }
            string contents = string.Join("\n", lines);
            if (contents.Contains(ustMatch)) {
                return ProjectFormats.Ust;
            } else if (contents.Contains(ustxMatchJson) || contents.Contains(ustxMatchYaml)) {
                return ProjectFormats.Ustx;
            } else if (contents.Contains(vsq3Match)) {
                return ProjectFormats.Vsq3;
            } else if (contents.Contains(vsq4Match)) {
                return ProjectFormats.Vsq4;
            } else if (contents.Contains(midiMatch)) {
                return ProjectFormats.Midi;
            } else {
                return ProjectFormats.Unknown;
            }
        }

        public static void LoadProject(string[] files) {
            if (files.Length < 1) {
                return;
            }
            ProjectFormats format = DetectProjectFormat(files[0]);
            UProject project;
            switch (format) {
                case ProjectFormats.Ustx:
                    project = Ustx.Load(files[0]);
                    break;
                case ProjectFormats.Vsq3:
                case ProjectFormats.Vsq4:
                    project = VSQx.Load(files[0]);
                    break;
                case ProjectFormats.Ust:
                    project = Ust.Load(files);
                    break;
                case ProjectFormats.Midi:
                    project = MidiWriter.LoadProject(files[0]);
                    break;
                default:
                    throw new FileFormatException("Unknown file format");
            }
            if (project != null) {
                DocManager.Inst.ExecuteCmd(new LoadProjectNotification(project));
            }
        }

        public static void ImportTracks(UProject project, string[] files) {
            if (files.Length < 1) {
                return;
            }
            int initialTracks = project.tracks.Count;
            int initialParts = project.parts.Count;
            foreach (string file in files) {
                ProjectFormats format = DetectProjectFormat(file);
                UProject loaded;
                switch (format) {
                    case ProjectFormats.Ustx:
                        loaded = Ustx.Load(file);
                        break;
                    case ProjectFormats.Vsq3:
                    case ProjectFormats.Vsq4:
                        loaded = VSQx.Load(file);
                        break;
                    case ProjectFormats.Ust:
                        loaded = Ust.Load(new[] { file });
                        break;
                    default:
                        throw new FileFormatException("Unknown file format");
                }
                int trackCount = project.tracks.Count;
                foreach (var (abbr, descriptor) in loaded.expressions) {
                    if (!project.expressions.ContainsKey(abbr)) {
                        project.expressions.Add(abbr, descriptor);
                    }
                }
                foreach (var track in loaded.tracks) {
                    track.TrackNo = project.tracks.Count;
                    project.tracks.Add(track);
                }
                foreach (var part in loaded.parts) {
                    project.parts.Add(part);
                    part.trackNo += trackCount;
                }
                project.timeSignatures.Clear();
                project.timeSignatures.AddRange(loaded.timeSignatures);
                project.tempos.Clear();
                project.tempos.AddRange(loaded.tempos);
            }
            for (int i = initialTracks; i < project.tracks.Count; i++) {
                project.tracks[i].AfterLoad(project);
            }
            for (int i = initialParts; i < project.parts.Count; i++) {
                var part = project.parts[i];
                part.AfterLoad(project, project.tracks[part.trackNo]);
            }
            project.ValidateFull();
            DocManager.Inst.ExecuteCmd(new LoadProjectNotification(project));
        }
    }
}
