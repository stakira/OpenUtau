using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Format {
    // 1. เพิ่มนามสกุลใหม่เข้าไปใน Enum
    public enum ProjectFormats { Unknown, Vsq3, Vsq4, Ust, Ustx, Midi, Ufdata, Musicxml, Svp, Tssln, Acep, Vsq };

    public static class Formats {
        const string ustMatch = "[#SETTING]";
        const string ustxMatchJson = "\"ustxVersion\":";
        const string ustxMatchYaml = "ustx_version:";
        const string vsq3Match = VSQx.vsq3NameSpace;
        const string vsq4Match = VSQx.vsq4NameSpace;
        const string midiMatch = "MThd";
        const string ufdataMatch = "\"formatVersion\":";
        const string musicxmlMatch = "score-partwise";
        
        // [DELTA SYNTH] คำค้นหา (Match) สำหรับไฟล์ใหม่ๆ (อาจต้องปรับเปลี่ยนตาม Header จริงของไฟล์)
        const string svpMatch = "\"version\""; // ไฟล์ SVP ของ SynthV มักเป็น JSON ที่มีคำว่า version หรือ svkey
        const string acepMatch = "ACEStudio";  // ไฟล์ Acep มักจะมีลายเซ็นของ ACE Studio
        const string tsslnMatch = "vocaloid5"; // หรือโครงสร้างเฉพาะของ V5/V6
        // หมายเหตุ: ไฟล์ .vsq ของ Vocaloid 2 โครงสร้างเป็น MIDI ดังนั้นมันจะไปเข้าข่าย midiMatch ก่อน
        // เราอาจจะต้องเช็คนามสกุลไฟล์ (Extension) ช่วยด้วยในกรณีของ VSQ

        public static ProjectFormats DetectProjectFormat(string file) {
            var lines = new List<string>();
            using (var reader = new StreamReader(file)) {
                for (int i = 0; i < 10 && !reader.EndOfStream; ++i) {
                    lines.Add(reader.ReadLine());
                }
            }
            string contents = string.Join("\n", lines);
            string extension = Path.GetExtension(file).ToLower(); // ดึงนามสกุลไฟล์มาช่วยเช็ค

            if (contents.Contains(ustMatch)) {
                return ProjectFormats.Ust;
            } else if (contents.Contains(ustxMatchJson) || contents.Contains(ustxMatchYaml)) {
                return ProjectFormats.Ustx;
            } else if (contents.Contains(vsq3Match)) {
                return ProjectFormats.Vsq3;
            } else if (contents.Contains(vsq4Match)) {
                return ProjectFormats.Vsq4;
            } else if (contents.Contains(ufdataMatch)) {
                return ProjectFormats.Ufdata;
            } else if (contents.Contains(musicxmlMatch)) {
                return ProjectFormats.Musicxml;
            } else if (contents.Contains(svpMatch) && extension == ".svp") {
                return ProjectFormats.Svp;
            } else if (contents.Contains(acepMatch) || extension == ".acep") {
                return ProjectFormats.Acep;
            } else if (contents.Contains(tsslnMatch) || extension == ".tssln") {
                return ProjectFormats.Tssln;
            } else if (contents.Contains(midiMatch)) {
                // ถ้าเป็น MIDI แต่ลงท้ายด้วย .vsq ให้มองเป็น VSQ
                if (extension == ".vsq") return ProjectFormats.Vsq;
                return ProjectFormats.Midi;
            } else {
                return ProjectFormats.Unknown;
            }
        }

        public static UProject? ReadProject(string[] files){
            if (files.Length < 1) {
                return null;
            }
            ProjectFormats format = DetectProjectFormat(files[0]);
            UProject? project = null;
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
                case ProjectFormats.Ufdata:
                    project = Ufdata.Load(files[0]);
                    break;
                case ProjectFormats.Musicxml:
                    project = MusicXML.LoadProject(files[0]);
                    break;
                
                // [DELTA SYNTH] จุดเชื่อมต่อไปยัง Parser (ต้องมีคลาสเหล่านี้อยู่ในโปรเจกต์ด้วย)
                case ProjectFormats.Svp:
                    // project = Svp.Load(files[0]); 
                    break;
                case ProjectFormats.Tssln:
                    // project = Tssln.Load(files[0]); 
                    break;
                case ProjectFormats.Acep:
                    // project = Acep.Load(files[0]); 
                    break;
                case ProjectFormats.Vsq:
                    // project = Vsq.Load(files[0]); 
                    break;

                default:
                    throw new FileFormatException("Unknown file format");
            }
            return project;
        }

        public static void LoadProject(string[] files) {
            UProject project = ReadProject(files);
            if (project != null) {
                DocManager.Inst.ExecuteCmd(new LoadProjectNotification(project));
            }
        }

        public static UProject[] ReadProjects(string[] files){
            if (files == null || files.Length < 1) {
                return new UProject[0];
            }
            return files
                .Select(file => ReadProject(new string[] { file }))
                .Where(p => p != null)
                .Cast<UProject>()
                .ToArray();
        }

        public static void RecoveryProject(string[] files) {
            UProject project = ReadProject(files);
            if (project != null) {
                string originalPath = project.FilePath.Replace("-autosave.ustx", ".ustx").Replace("-backup.ustx", ".ustx");
                if (File.Exists(originalPath)) {
                    project.FilePath = originalPath;
                } else {
                    project.FilePath = string.Empty;
                    project.Saved = false;
                }
                
                DocManager.Inst.ExecuteCmd(new LoadProjectNotification(project));
            }
        }

        public static void ImportTracks(UProject project, UProject[] loadedProjects, bool importTempo = true) {
            if (loadedProjects == null || loadedProjects.Length < 1) {
                return;
            }
            int initialTracks = project.tracks.Count;
            int initialParts = project.parts.Count;
            foreach (UProject loaded in loadedProjects) {
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
            }
            if (importTempo) {
                var loaded = loadedProjects[0];
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

        public static void ImportTracks(UProject project, string[] files, bool importTempo = true) {
            if (files == null || files.Length < 1) {
                return;
            }
            UProject[] loadedProjects = files
                .Select(file => ReadProject(new string[] { file }))
                .Where(p => p != null)
                .Cast<UProject>()
                .ToArray();
            ImportTracks(project, loadedProjects, importTempo);
        }
    }
}
