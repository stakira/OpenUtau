using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Formats {

    public static class Ust {
        class UstLine {
            public string file;
            public int lineNumber;
            public string line;

            public override string ToString() {
                return $"\"{file}\"\nat line {lineNumber + 1}:\n\"{line}\"";
            }
        }

        public static void Load(string[] files) {
            var ustTracks = true;
            foreach (var file in files) {
                if (Formats.DetectProjectFormat(file) != Core.Formats.ProjectFormats.Ust) { ustTracks = false; break; }
            }

            if (!ustTracks) {
                DocManager.Inst.ExecuteCmd(new UserMessageNotification("Multiple files must be all Ust files"));
                return;
            }

            var projects = new List<UProject>();
            foreach (var file in files) {
                projects.Add(Load(file));
            }

            var bpm = projects.First().bpm;
            var project = new UProject() { bpm = bpm, name = "Merged Project", Saved = false };
            foreach (var p in projects) {
                var _track = p.tracks[0];
                var _part = p.parts[0];
                _track.TrackNo = project.tracks.Count;
                _part.TrackNo = _track.TrackNo;
                project.tracks.Add(_track);
                project.parts.Add(_part);
            }

            if (project != null) {
                DocManager.Inst.ExecuteCmd(new LoadProjectNotification(project));
            }
        }

        public static UProject Load(string file, Encoding encoding = null) {
            var project = new UProject() { resolution = 480, filePath = file, Saved = false };
            project.RegisterExpression(new UExpressionDescriptor("velocity", "vel", 0, 200, 100));
            project.RegisterExpression(new UExpressionDescriptor("volume", "vol", 0, 200, 100));
            project.RegisterExpression(new UExpressionDescriptor("gender", "gen", -100, 100, 0));
            project.RegisterExpression(new UExpressionDescriptor("lowpass", "lpf", 0, 100, 0));
            project.RegisterExpression(new UExpressionDescriptor("highpass", "hpf", 0, 100, 0));
            project.RegisterExpression(new UExpressionDescriptor("accent", "acc", 0, 200, 100));
            project.RegisterExpression(new UExpressionDescriptor("decay", "dec", 0, 100, 0));

            project.tracks.Add(new UTrack { TrackNo = 0 });
            var part = new UVoicePart() { TrackNo = 0, PosTick = 0 };
            project.parts.Add(part);

            var blocks = ReadBlocks(file, encoding ?? Encoding.GetEncoding("shift_jis"));
            ParsePart(project, part, blocks);

            part.DurTick = part.notes.Select(note => note.End).Max() + project.resolution;

            project.AfterLoad();
            project.Validate();
            return project;
        }

        private static List<List<UstLine>> ReadBlocks(string file, Encoding encoding) {
            var reader = new StreamReader(file, encoding);
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
                        if (float.TryParse(parts[1], out float temp)) {
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
                bool isFloat = float.TryParse(parts[1], out float floatValue);
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
                    case "VoiceOverlap":
                        error |= !isFloat;
                        note.phonemes[0].overlap = floatValue;
                        break;
                    case "PreUtterance":
                        ParsePreUtterance(note, parts[1], ustBlock[i]);
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
                note.phonemes[0].autoRemap = false;
                ust = ust.Substring(1);
            }
            note.phonemes[0].phoneme = ust;
            note.lyric = ust;
        }

        private static void ParsePreUtterance(UNote note, string ust, UstLine ustLine) {
            if (string.IsNullOrWhiteSpace(ust)) {
                return;
            }
            if (!float.TryParse(ust, out float preutter)) {
                throw new FileFormatException($"Invalid PreUtterance\n${ustLine}");
            }
            note.phonemes[0].preutter = preutter; // Currently unused, overwritten by oto.
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
                    float p4 = parts[8], p5 = parts[9], v5 = parts[11];
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
                float pbsX = parts.Length >= 1 && float.TryParse(parts[0], out pbsX) ? pbsX : 0;
                float pbsY = parts.Length >= 2 && float.TryParse(parts[1], out pbsY) ? pbsY : 0;
                points.Add(new PitchPoint(pbsX, pbsY));
                note.pitch.snapFirst = parts.Length < 2;
                // PBW, PBY
                var x = points.First().X;
                if (!string.IsNullOrWhiteSpace(pbw)) {
                    var w = pbw.Split(',').Select(s => string.IsNullOrEmpty(s) ? 0 : float.Parse(s)).ToList();
                    var y = (pby ?? "").Split(',').Select(s => string.IsNullOrEmpty(s) ? 0 : float.Parse(s)).ToList();
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
                    for (var i = 0; i < m.Count() - 1; i++) {
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
    }
}
