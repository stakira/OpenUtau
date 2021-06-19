using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Core.USTx;

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

            var bpm = projects.First().BPM;
            var project = new UProject() { BPM = bpm, Name = "Merged Project", Saved = false };
            foreach (var p in projects) {
                var _track = p.Tracks[0];
                var _part = p.Parts[0];
                _track.TrackNo = project.Tracks.Count;
                _part.TrackNo = _track.TrackNo;
                project.Tracks.Add(_track);
                project.Parts.Add(_part);
            }

            if (project != null) {
                DocManager.Inst.ExecuteCmd(new LoadProjectNotification(project));
            }
        }

        public static UProject Load(string file, Encoding encoding = null) {
            var project = new UProject() { Resolution = 480, FilePath = file, Saved = false };
            project.RegisterExpression(new IntExpression(null, "velocity", "VEL") { Data = 100, Min = 0, Max = 200 });
            project.RegisterExpression(new IntExpression(null, "volume", "VOL") { Data = 100, Min = 0, Max = 200 });
            project.RegisterExpression(new IntExpression(null, "gender", "GEN") { Data = 0, Min = -100, Max = 100 });
            project.RegisterExpression(new IntExpression(null, "lowpass", "LPF") { Data = 0, Min = 0, Max = 100 });
            project.RegisterExpression(new IntExpression(null, "highpass", "HPF") { Data = 0, Min = 0, Max = 100 });
            project.RegisterExpression(new IntExpression(null, "accent", "ACC") { Data = 100, Min = 0, Max = 200 });
            project.RegisterExpression(new IntExpression(null, "decay", "DEC") { Data = 0, Min = 0, Max = 100 });

            project.Tracks.Add(new UTrack { TrackNo = 0 });
            var part = new UVoicePart() { TrackNo = 0, PosTick = 0 };
            project.Parts.Add(part);

            var blocks = ReadBlocks(file, encoding ?? Encoding.GetEncoding("shift_jis"));
            ParsePart(project, part, blocks);

            part.DurTick = part.Notes.Select(note => note.EndTick).Max() + project.Resolution;
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
                                note.PosTick = tick;
                                tick += note.DurTick;
                                if (note.Lyric != "R") {
                                    part.Notes.Add(note);
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
                        if (double.TryParse(parts[1], out double temp)) {
                            project.BPM = temp;
                        }
                        break;
                    case "ProjectName":
                        project.Name = parts[1].Trim();
                        break;
                    case "VoiceDir":
                        var singerpath = parts[1].Trim();
                        var singer = DocManager.Inst.GetSinger(singerpath);
                        if (singer == null) {
                            singer = new USinger("");
                        }
                        project.Singers.Add(singer);
                        project.Tracks[0].Singer = singer;
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
                bool isDouble = double.TryParse(parts[1], out double doubleValue);
                switch (param) {
                    case "Length":
                        error |= !isDouble;
                        note.DurTick = (int)doubleValue;
                        break;
                    case "Lyric":
                        ParseLyric(note, parts[1]);
                        break;
                    case "NoteNum":
                        error |= !isDouble;
                        note.NoteNum = (int)doubleValue;
                        break;
                    case "Velocity":
                        error |= !isDouble;
                        note.Expressions["velocity"].Data = (int)doubleValue;
                        break;
                    case "Intensity":
                        error |= !isDouble;
                        note.Expressions["volume"].Data = (int)doubleValue;
                        break;
                    case "VoiceOverlap":
                        error |= !isDouble;
                        note.Phonemes[0].Overlap = doubleValue;
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
                note.Phonemes[0].AutoRemapped = false;
                ust = ust.Substring(1);
            }
            note.Phonemes[0].Phoneme = ust;
            note.Lyric = ust;
        }

        private static void ParsePreUtterance(UNote note, string ust, UstLine ustLine) {
            if (string.IsNullOrWhiteSpace(ust)) {
                note.Phonemes[0].AutoEnvelope = true;
                return;
            }
            note.Phonemes[0].AutoEnvelope = false;
            if (!double.TryParse(ust, out double preutter)) {
                throw new FileFormatException($"Invalid PreUtterance\n${ustLine}");
            }
            note.Phonemes[0].Preutter = preutter;
        }

        private static void ParseEnvelope(UNote note, string ust, UstLine ustLine) {
            // p1,p2,p3,v1,v2,v3,v4,%,p4,p5,v5 (0,5,35,0,100,100,0,%,0,0,100)
            try {
                var parts = ust.Split(new[] { ',' }).Select(s => double.TryParse(s, out double v) ? v : -1).ToArray();
                if (parts.Length < 7) {
                    return;
                }
                double p1 = parts[0], p2 = parts[1], p3 = parts[2], v1 = parts[3], v2 = parts[4], v3 = parts[5], v4 = parts[6];
                if (parts.Length == 11) {
                    double p4 = parts[8], p5 = parts[9], v5 = parts[11];
                }
                note.Expressions["decay"].Data = 100 - (int)v3;
            } catch (Exception e) {
                throw new FileFormatException($"Invalid Envelope\n{ustLine}", e);
            }
        }

        private static void ParseVibrato(UNote note, string ust, UstLine ustLine) {
            try {
                var args = ust.Split(',').Select(double.Parse).ToArray();
                if (args.Length < 7) {
                    throw new Exception();
                }
                note.Vibrato.Length = args[0];
                note.Vibrato.Period = args[1];
                note.Vibrato.Depth = args[2];
                note.Vibrato.In = args[3];
                note.Vibrato.Out = args[4];
                note.Vibrato.Shift = args[5];
                note.Vibrato.Drift = args[6];
            } catch {
                throw new FileFormatException($"Invalid VBR\n{ustLine}");
            }
        }

        private static void ParsePitchBend(UNote note, string pbs, string pbw, string pby, string pbm) {
            if (!string.IsNullOrWhiteSpace(pbs)) {
                var points = note.PitchBend.Data as List<PitchPoint>;
                points.Clear();
                if (pbs.Contains(';')) {
                    points.Add(new PitchPoint(double.Parse(pbs.Split(new[] { ';' })[0]), double.Parse(pbs.Split(new[] { ';' })[1])));
                    note.PitchBend.SnapFirst = false;
                } else {
                    points.Add(new PitchPoint(double.Parse(pbs), 0));
                    note.PitchBend.SnapFirst = true;
                }
                var x = points.First().X;
                if (!string.IsNullOrWhiteSpace(pbw)) {
                    var w = pbw.Split(',').Select(s => string.IsNullOrEmpty(s) ? 0 : double.Parse(s)).ToList();
                    var y = (pby ?? "").Split(',').Select(s => string.IsNullOrEmpty(s) ? 0 : double.Parse(s)).ToList();
                    while (w.Count > y.Count) {
                        y.Add(0);
                    }
                    for (var i = 0; i < w.Count(); i++) {
                        x += w[i];
                        points.Add(new PitchPoint(x, y[i]));
                    }
                }
                if (!string.IsNullOrWhiteSpace(pbm)) {
                    var m = pbw.Split(new[] { ',' });
                    for (var i = 0; i < m.Count() - 1; i++) {
                        switch (m[i]) {
                            case "r":
                                points[i].Shape = PitchPointShape.o;
                                break;
                            case "s":
                                points[i].Shape = PitchPointShape.l;
                                break;
                            case "j":
                                points[i].Shape = PitchPointShape.i;
                                break;
                            default:
                                points[i].Shape = PitchPointShape.io;
                                break;
                        }
                    }
                }
            }
        }
    }
}
