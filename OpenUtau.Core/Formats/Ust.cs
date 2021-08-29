using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Formats {

    public static class Ust {
        static Encoding ShiftJIS = Encoding.GetEncoding("shift_jis");

        class UstLine {
            public string file;
            public int lineNumber;
            public string line;

            public override string ToString() {
                return $"\"{file}\"\nat line {lineNumber + 1}:\n\"{line}\"";
            }
        }

        public static UProject Load(string[] files) {
            foreach (var file in files) {
                if (Formats.DetectProjectFormat(file) != ProjectFormats.Ust) {
                    DocManager.Inst.ExecuteCmd(new UserMessageNotification("Multiple files must be all Ust files"));
                    return null;
                }
            }

            var projects = new List<UProject>();
            var encoding = ShiftJIS;
            foreach (var file in files) {
                using (var reader = new StreamReader(file, encoding)) {
                    projects.Add(Load(reader, file));
                }
            }

            var project = projects.First();
            project.name = "Merged Project";
            foreach (var p in projects) {
                if (p == project) {
                    continue;
                }
                var track = p.tracks[0];
                var part = p.parts[0];
                track.TrackNo = project.tracks.Count;
                part.trackNo = track.TrackNo;
                project.tracks.Add(track);
                project.parts.Add(part);
            }
            project.AfterLoad();
            project.Validate();
            return project;
        }

        public static UProject Load(StreamReader reader, string file) {
            var project = new UProject() { resolution = 480, FilePath = file, Saved = false };
            Ustx.AddDefaultExpressions(project);

            project.tracks.Add(new UTrack {
                TrackNo = 0,
            });
            var part = new UVoicePart() {
                trackNo = 0,
                position = 0,
                name = Path.GetFileNameWithoutExtension(file),
            };
            project.parts.Add(part);

            var blocks = ReadBlocks(reader, file);
            ParsePart(project, part, blocks);
            part.Duration = part.notes.Select(note => note.End).Max() + project.resolution;

            return project;
        }

        private static List<List<UstLine>> ReadBlocks(StreamReader reader, string file) {
            var result = new List<List<UstLine>>();
            int lineNumber = -1;
            while (!reader.EndOfStream) {
                string line = reader.ReadLine().Trim();
                lineNumber++;
                if (string.IsNullOrEmpty(line)) {
                    continue;
                }
                if (line.StartsWith(@"[#") && line.EndsWith(@"]")) {
                    result.Add(new List<UstLine>());
                }
                if (result.Count == 0) {
                    throw new FileFormatException("Unexpected beginning of ust file.");
                }
                result.Last().Add(new UstLine {
                    file = file,
                    line = line,
                    lineNumber = lineNumber
                });
            }
            return result;
        }

        private static void ParsePart(UProject project, UVoicePart part, List<List<UstLine>> blocks) {
            int tick = 0;
            foreach (var block in blocks) {
                string header = block[0].line;
                try {
                    switch (header) {
                        case "[#VERSION]":
                            break;
                        case "[#SETTING]":
                            ParseSetting(project, block);
                            break;
                        case "[#TRACKEND]":
                            break;
                        default:
                            if (int.TryParse(header.Substring(2, header.Length - 3), out int noteIndex)) {
                                UNote note = project.CreateNote();
                                ParseNote(note, block);
                                note.position = tick;
                                tick += note.duration;
                                if (note.lyric.ToLower() != "r") {
                                    part.notes.Add(note);
                                }
                            } else {
                                throw new FileFormatException($"Unexpected header\n{block[0]}");
                            }
                            break;
                    }
                } catch (Exception e) when (!(e is FileFormatException)) {
                    throw new FileFormatException($"Failed to parse block\n{block[0]}", e);
                }
            }
            SnapPitchPoints(part);
        }

        private static void SnapPitchPoints(UVoicePart part) {
            UNote lastNote = null;
            foreach (var note in part.notes) {
                if (lastNote != null && !note.pitch.snapFirst && note.position == lastNote.End) {
                    float dy = note.pitch.data[0].Y - lastNote.pitch.data[0].Y;
                    float dn = note.noteNum - lastNote.noteNum;
                    if (Math.Abs(dy + 10 * dn) < 1) {
                        note.pitch.snapFirst = true;
                    }
                }
                lastNote = note;
            }
        }

        private static void ParseSetting(UProject project, List<UstLine> ustBlock) {
            const string format = "<param>=<value>";
            for (int i = 1; i < ustBlock.Count; i++) {
                string line = ustBlock[i].line;
                var parts = line.Split('=');
                if (parts.Length != 2) {
                    throw new FileFormatException($"Line does not match format {format}.\n{ustBlock[i]}");
                }
                string param = parts[0].Trim();
                switch (param) {
                    case "Tempo":
                        if (ParseFloat(parts[1], out float temp)) {
                            project.bpm = temp;
                        }
                        break;
                    case "ProjectName":
                        project.name = parts[1].Trim();
                        break;
                    case "VoiceDir":
                        var singerpath = parts[1].Trim();
                        var singer = DocManager.Inst.GetSinger(singerpath);
                        if (singer == null) {
                            singer = new USinger("");
                        }
                        project.tracks[0].Singer = singer;
                        break;
                }
            }
        }

        private static void ParseNote(UNote note, List<UstLine> ustBlock) {
            const string format = "<param>=<value>";
            string pbs = null, pbw = null, pby = null, pbm = null;
            for (int i = 1; i < ustBlock.Count; i++) {
                string line = ustBlock[i].line;
                var parts = line.Split('=');
                if (parts.Length != 2) {
                    throw new FileFormatException($"Line does not match format {format}.\n{ustBlock[i]}");
                }
                string param = parts[0].Trim();
                bool error = false;
                bool isFloat = ParseFloat(parts[1], out float floatValue);
                switch (param) {
                    case "Length":
                        error |= !isFloat;
                        note.duration = (int)floatValue;
                        break;
                    case "Lyric":
                        ParseLyric(note, parts[1]);
                        break;
                    case "NoteNum":
                        error |= !isFloat;
                        note.noteNum = (int)floatValue;
                        break;
                    case "Velocity":
                        error |= !isFloat;
                        note.expressions["vel"].value = floatValue;
                        break;
                    case "Intensity":
                        error |= !isFloat;
                        note.expressions["vol"].value = floatValue;
                        break;
                    case "Moduration":
                        error |= !isFloat;
                        note.expressions["mod"].value = floatValue;
                        break;
                    case "VoiceOverlap":
                        error |= !isFloat;
                        //note.phonemes[0].overlap = floatValue;
                        break;
                    case "PreUtterance":
                        error |= !isFloat;
                        //note.phonemes[0].preutter = floatValue;
                        break;
                    case "Envelope":
                        ParseEnvelope(note, parts[1], ustBlock[i]);
                        break;
                    case "VBR":
                        ParseVibrato(note, parts[1], ustBlock[i]);
                        break;
                    case "PBS":
                        pbs = parts[1];
                        break;
                    case "PBW":
                        pbw = parts[1];
                        break;
                    case "PBY":
                        pby = parts[1];
                        break;
                    case "PBM":
                        pbm = parts[1];
                        break;
                    default:
                        break;
                }
                if (error) {
                    throw new FileFormatException($"Invalid {param}\n${ustBlock[i]}");
                }
            }
            ParsePitchBend(note, pbs, pbw, pby, pbm);
        }

        private static void ParseLyric(UNote note, string ust) {
            if (ust.StartsWith("?")) {
                ust = ust.Substring(1);
            }
            note.phonemes[0].phoneme = ust;
            note.lyric = ust;
        }

        private static void ParseEnvelope(UNote note, string ust, UstLine ustLine) {
            // p1,p2,p3,v1,v2,v3,v4,%,p4,p5,v5 (0,5,35,0,100,100,0,%,0,0,100)
            try {
                var parts = ust.Split(new[] { ',' }).Select(s => float.TryParse(s, out float v) ? v : -1).ToArray();
                if (parts.Length < 7) {
                    return;
                }
                float p1 = parts[0], p2 = parts[1], p3 = parts[2], v1 = parts[3], v2 = parts[4], v3 = parts[5], v4 = parts[6];
                if (parts.Length == 11) {
                    float p4 = parts[8], p5 = parts[9], v5 = parts[10];
                }
                note.expressions["dec"].value = 100f - v3;
            } catch (Exception e) {
                throw new FileFormatException($"Invalid Envelope\n{ustLine}", e);
            }
        }

        private static void ParseVibrato(UNote note, string ust, UstLine ustLine) {
            try {
                var args = ust.Split(',').Select(float.Parse).ToArray();
                if (args.Length < 7) {
                    throw new Exception();
                }
                note.vibrato.length = args[0];
                note.vibrato.period = args[1];
                note.vibrato.depth = args[2];
                note.vibrato.@in = args[3];
                note.vibrato.@out = args[4];
                note.vibrato.shift = args[5];
                note.vibrato.drift = args[6];
            } catch {
                throw new FileFormatException($"Invalid VBR\n{ustLine}");
            }
        }

        private static void ParsePitchBend(UNote note, string pbs, string pbw, string pby, string pbm) {
            if (!string.IsNullOrWhiteSpace(pbs)) {
                var points = note.pitch.data;
                points.Clear();
                // PBS
                var parts = pbs.Split(';');
                float pbsX = parts.Length >= 1 && ParseFloat(parts[0], out pbsX) ? pbsX : 0;
                float pbsY = parts.Length >= 2 && ParseFloat(parts[1], out pbsY) ? pbsY : 0;
                points.Add(new PitchPoint(pbsX, pbsY));
                note.pitch.snapFirst = parts.Length < 2;
                // PBW, PBY
                var x = points.First().X;
                if (!string.IsNullOrWhiteSpace(pbw)) {
                    var w = pbw.Split(',').Select(s => ParseFloat(s, out float v) ? v : 0).ToList();
                    var y = (pby ?? "").Split(',').Select(s => ParseFloat(s, out float v) ? v : 0).ToList();
                    while (w.Count > y.Count) {
                        y.Add(0);
                    }
                    for (var i = 0; i < w.Count(); i++) {
                        x += w[i];
                        points.Add(new PitchPoint(x, y[i]));
                    }
                }
                // PBM
                if (!string.IsNullOrWhiteSpace(pbm)) {
                    var m = pbm.Split(new[] { ',' });
                    for (var i = 0; i < m.Count() && i < points.Count; i++) {
                        switch (m[i]) {
                            case "r":
                                points[i].shape = PitchPointShape.o;
                                break;
                            case "s":
                                points[i].shape = PitchPointShape.l;
                                break;
                            case "j":
                                points[i].shape = PitchPointShape.i;
                                break;
                            default:
                                points[i].shape = PitchPointShape.io;
                                break;
                        }
                    }
                }
            }
        }

        static bool ParseFloat(string s, out float value) {
            if (string.IsNullOrEmpty(s)) {
                value = 0;
                return true;
            }
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public static void SavePart(UProject project, UVoicePart part, string filePath) {
            WriteNotes(project, part, part.notes, filePath);
        }

        public static List<UNote> WriteNotes(UProject project, UVoicePart part, IEnumerable<UNote> notes, string filePath) {
            List<UNote> sequence = new List<UNote>();
            using (var writer = new StreamWriter(filePath, false, ShiftJIS)) {
                WriteHeader(project, part, writer);
                int position = 0;
                foreach (var note in notes) {
                    if (note.position != position) {
                        writer.WriteLine($"[#{sequence.Count:D4}]");
                        var spacer = UNote.Create();
                        spacer.position = position;
                        spacer.duration = note.position - position;
                        spacer.lyric = "R";
                        spacer.noteNum = 60;
                        sequence.Add(spacer);
                        WriteNoteBody(spacer, writer);
                    }
                    writer.WriteLine($"[#{sequence.Count:D4}]");
                    WriteNoteBody(note, writer);
                    position = note.End;
                    sequence.Add(note);
                }
                WriteFooter(writer);
            }
            return sequence;
        }

        static void WriteHeader(UProject project, UVoicePart part, StreamWriter writer) {
            writer.WriteLine("[#SETTING]");
            writer.WriteLine($"Tempo={project.bpm}");
            writer.WriteLine("Tracks=1");
            var singer = project.tracks[part.trackNo].Singer;
            writer.WriteLine($"VoiceDir=%VOICE%{singer.Id}");
            writer.WriteLine($"CacheDir={project.cacheDir}");
            writer.WriteLine("Mode2=True");
        }

        static void WriteFooter(StreamWriter writer) {
            writer.WriteLine("[#TRACKEND]");
        }

        static void WriteNoteBody(UNote note, StreamWriter writer) {
            writer.WriteLine($"Length={note.duration}");
            writer.WriteLine($"Lyric={note.lyric}");
            writer.WriteLine($"NoteNum={note.noteNum}");
            writer.WriteLine("PreUtterance=");
            //writer.WriteLine("VoiceOverlap=");
            if (note.expressions.TryGetValue("vel", out var vel)) {
                writer.WriteLine($"Velocity={(int)vel.value}");
            }
            if (note.expressions.TryGetValue("vol", out var vol)) {
                writer.WriteLine($"Intensity={(int)vol.value}");
            }
            if (note.expressions.TryGetValue("mod", out var mod)) {
                writer.WriteLine($"Moduration={(int)mod.value}");
            }
            WriteEnvelope(note, writer);
            WritePitch(note, writer);
            WriteVibrato(note, writer);
        }

        static void WriteEnvelope(UNote note, StreamWriter writer) {

        }

        static void WritePitch(UNote note, StreamWriter writer) {
            if (note.pitch == null) {
                return;
            }
            var points = note.pitch.data;
            if (points.Count >= 2) {
                writer.WriteLine($"PBS={points[0].X};{points[0].Y}");
                List<string> pbw = new List<string>();
                List<string> pby = new List<string>();
                List<string> pbm = new List<string>();
                for (int i = 0; i < points.Count; ++i) {
                    switch (points[i].shape) {
                        case PitchPointShape.o:
                            pbm.Add("r");
                            break;
                        case PitchPointShape.l:
                            pbm.Add("s");
                            break;
                        case PitchPointShape.i:
                            pbm.Add("j");
                            break;
                        case PitchPointShape.io:
                            pbm.Add("");
                            break;
                    }
                }
                for (int i = 1; i < points.Count; ++i) {
                    var prev = points[i - 1];
                    var current = points[i];
                    pbw.Add((current.X - prev.X).ToString());
                    pby.Add(current.Y.ToString());
                }
                writer.WriteLine($"PBW={string.Join(",", pbw.ToArray())}");
                writer.WriteLine($"PBY={string.Join(",", pby.ToArray())}");
                writer.WriteLine($"PBM={string.Join(",", pbm.ToArray())}");
            }
        }

        static void WriteVibrato(UNote note, StreamWriter writer) {
            var vbr = note.vibrato;
            if (vbr != null && vbr.length > 0) {
                writer.WriteLine($"VBR={vbr.length},{vbr.period},{vbr.depth},{vbr.@in},{vbr.@out},{vbr.shift},{vbr.drift}");
            }
        }

        public static void ParseDiffs(UProject project, UVoicePart part, List<UNote> sequence, string diffFile) {
            using (var reader = new StreamReader(diffFile, ShiftJIS)) {
                var blocks = ReadBlocks(reader, diffFile);
                foreach (var block in blocks) {
                    string header = block[0].line;
                    switch (header) {
                        case "[#VERSION]":
                        case "[#SETTING]":
                        case "[#TRACKEND]":
                        case "[#PREV]":
                        case "[#NEXT]":
                        case "[#INSERT]":
                        case "[#DELETE]":
                            break;
                        default:
                            if (int.TryParse(header.Substring(2, header.Length - 3), out int noteIndex)) {
                                if (noteIndex < sequence.Count) {
                                    ParseNote(sequence[noteIndex], block);
                                }
                            }
                            break;
                    }
                }
            }
            int position = 0;
            foreach (var note in sequence) {
                note.position = position;
                position += note.duration;
            }
            SnapPitchPoints(part);
        }
    }
}
