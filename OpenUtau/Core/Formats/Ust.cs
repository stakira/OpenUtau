using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Core.USTx;
using OpenUtau.SimpleHelpers;

namespace OpenUtau.Core.Formats {

    public static class Ust {
        private const string versionTag = "[#VERSION]";
        private const string settingTag = "[#SETTING]";
        private const string endTag = "[#TRACKEND]";

        private enum UstVersion { Early, V1_0, V1_1, V1_2, Unknown };

        private enum UstBlock { Version, Setting, Note, Trackend, None };

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
            var currentNoteIndex = 0;
            var version = UstVersion.Early;
            var currentBlock = UstBlock.None;
            string[] lines;

            try {
                if (encoding == null) {
                    lines = File.ReadAllLines(file, FileEncoding.DetectFileEncoding(file));
                } else {
                    lines = File.ReadAllLines(file, encoding);
                }
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new UserMessageNotification(e.GetType().ToString() + "\n" + e.Message));
                return null;
            }

            var project = new UProject() { Resolution = 480, FilePath = file, Saved = false };
            project.RegisterExpression(new IntExpression(null, "velocity", "VEL") { Data = 100, Min = 0, Max = 200 });
            project.RegisterExpression(new IntExpression(null, "volume", "VOL") { Data = 100, Min = 0, Max = 200 });
            project.RegisterExpression(new IntExpression(null, "gender", "GEN") { Data = 0, Min = -100, Max = 100 });
            project.RegisterExpression(new IntExpression(null, "lowpass", "LPF") { Data = 0, Min = 0, Max = 100 });
            project.RegisterExpression(new IntExpression(null, "highpass", "HPF") { Data = 0, Min = 0, Max = 100 });
            project.RegisterExpression(new IntExpression(null, "accent", "ACC") { Data = 100, Min = 0, Max = 200 });
            project.RegisterExpression(new IntExpression(null, "decay", "DEC") { Data = 0, Min = 0, Max = 100 });

            var _track = new UTrack();
            project.Tracks.Add(_track);
            _track.TrackNo = 0;
            var part = new UVoicePart() { TrackNo = 0, PosTick = 0 };
            project.Parts.Add(part);

            var currentLines = new List<string>();
            var currentTick = 0;
            UNote currentNote = null;

            foreach (var line in lines) {
                if (line.Trim().StartsWith(@"[#") && line.Trim().EndsWith(@"]")) {
                    if (line.Equals(versionTag)) {
                        currentBlock = UstBlock.Version;
                    } else if (line.Equals(settingTag)) {
                        currentBlock = UstBlock.Setting;
                    } else {
                        if (line.Equals(endTag)) {
                            currentBlock = UstBlock.Trackend;
                        } else {
                            try { currentNoteIndex = int.Parse(line.Replace("[#", string.Empty).Replace("]", string.Empty)); } catch { DocManager.Inst.ExecuteCmd(new UserMessageNotification("Unknown ust format")); return null; }
                            currentBlock = UstBlock.Note;
                        }

                        if (currentLines.Count != 0) {
                            currentNote = NoteFromUst(project.CreateNote(), currentLines, version);
                            currentNote.PosTick = currentTick;
                            if (!currentNote.Lyric.Replace("R", string.Empty).Replace("r", string.Empty).Equals(string.Empty)) {
                                part.Notes.Add(currentNote);
                            }

                            currentTick += currentNote.DurTick;
                            currentLines.Clear();
                        }
                    }
                } else {
                    if (currentBlock == UstBlock.Version) {
                        if (line.StartsWith("UST Version")) {
                            var v = line.Trim().Replace("UST Version", string.Empty);
                            switch (v) {
                                case "1.0":
                                    version = UstVersion.V1_0;
                                    break;

                                case "1.1":
                                    version = UstVersion.V1_1;
                                    break;

                                case "1.2":
                                    version = UstVersion.V1_2;
                                    break;

                                default:
                                    version = UstVersion.Unknown;
                                    break;
                            }
                        }
                    }
                    if (currentBlock == UstBlock.Setting) {
                        if (line.StartsWith("Tempo=")) {
                            project.BPM = double.Parse(line.Trim().Replace("Tempo=", string.Empty));
                            if (project.BPM == 0) {
                                project.BPM = 120;
                            }
                        }
                        if (line.StartsWith("ProjectName=")) {
                            project.Name = line.Trim().Replace("ProjectName=", string.Empty);
                        }

                        if (line.StartsWith("VoiceDir=")) {
                            var singerpath = line.Trim().Replace("VoiceDir=", string.Empty);
                            var singer = DocManager.Inst.GetSinger(singerpath);
                            if (singer == null) {
                                singer = new USinger() { Name = "", Path = singerpath };
                            }

                            project.Singers.Add(singer);
                            project.Tracks[0].Singer = singer;
                        }
                    } else if (currentBlock == UstBlock.Note) {
                        currentLines.Add(line);
                    } else if (currentBlock == UstBlock.Trackend) {
                        break;
                    }
                }
            }

            if (currentBlock != UstBlock.Trackend) {
                DocManager.Inst.ExecuteCmd(new UserMessageNotification("Unexpected ust file end"));
            }

            part.DurTick = currentTick;
            return project;
        }

        private static UNote NoteFromUst(UNote note, List<string> lines, UstVersion version) {
            string pbs = "", pbw = "", pby = "", pbm = "";

            foreach (var line in lines) {
                if (line.StartsWith("Lyric=")) {
                    note.Phonemes[0].Phoneme = note.Lyric = line.Trim().Replace("Lyric=", string.Empty);
                    if (note.Phonemes[0].Phoneme.StartsWith("?")) {
                        note.Phonemes[0].Phoneme = note.Phonemes[0].Phoneme.Substring(1);
                        note.Phonemes[0].AutoRemapped = false;
                    }
                }
                if (line.StartsWith("Length=")) {
                    note.DurTick = int.Parse(line.Trim().Replace("Length=", string.Empty));
                }

                if (line.StartsWith("NoteNum=")) {
                    note.NoteNum = int.Parse(line.Trim().Replace("NoteNum=", string.Empty));
                }

                if (line.StartsWith("Velocity=")) {
                    note.Expressions["velocity"].Data = int.Parse(line.Trim().Replace("Velocity=", string.Empty));
                }

                if (line.StartsWith("Intensity=")) {
                    note.Expressions["volume"].Data = int.Parse(line.Trim().Replace("Intensity=", string.Empty));
                }

                if (line.StartsWith("PreUtterance=")) {
                    if (line.Trim() == "PreUtterance=") {
                        note.Phonemes[0].AutoEnvelope = true;
                    } else { note.Phonemes[0].AutoEnvelope = false; note.Phonemes[0].Preutter = double.Parse(line.Trim().Replace("PreUtterance=", "")); }
                }
                if (line.StartsWith("VoiceOverlap=")) {
                    note.Phonemes[0].Overlap = double.Parse(line.Trim().Replace("VoiceOverlap=", string.Empty));
                }

                if (line.StartsWith("Envelope=")) {
                    var pts = line.Trim().Replace("Envelope=", string.Empty).Split(new[] { ',' });
                    if (pts.Count() > 5) {
                        note.Expressions["decay"].Data = 100 - (int)double.Parse(pts[5]);
                    }
                }
                if (line.StartsWith("VBR=")) {
                    VibratoFromUst(note.Vibrato, line.Trim().Replace("VBR=", string.Empty));
                }

                if (line.StartsWith("PBS=")) {
                    pbs = line.Trim().Replace("PBS=", string.Empty);
                }

                if (line.StartsWith("PBW=")) {
                    pbw = line.Trim().Replace("PBW=", string.Empty);
                }

                if (line.StartsWith("PBY=")) {
                    pby = line.Trim().Replace("PBY=", string.Empty);
                }

                if (line.StartsWith("PBM=")) {
                    pbm = line.Trim().Replace("PBM=", string.Empty);
                }
            }

            if (pbs != string.Empty) {
                var pts = note.PitchBend.Data as List<PitchPoint>;
                pts.Clear();
                // PBS
                if (pbs.Contains(';')) {
                    pts.Add(new PitchPoint(double.Parse(pbs.Split(new[] { ';' })[0]), double.Parse(pbs.Split(new[] { ';' })[1])));
                    note.PitchBend.SnapFirst = false;
                } else {
                    pts.Add(new PitchPoint(double.Parse(pbs), 0));
                    note.PitchBend.SnapFirst = true;
                }
                var x = pts.First().X;
                if (pbw != string.Empty) {
                    var w = pbw.Split(new[] { ',' });
                    string[] y = null;
                    if (w.Count() > 1) {
                        y = pby.Split(new[] { ',' });
                    }

                    for (var i = 0; i < w.Count() - 1; i++) {
                        x += string.IsNullOrEmpty(w[i]) ? 0 : float.Parse(w[i]);
                        pts.Add(new PitchPoint(x, string.IsNullOrEmpty(y[i]) ? 0 : double.Parse(y[i])));
                    }
                    pts.Add(new PitchPoint(x + double.Parse(w[w.Count() - 1]), 0));
                }
                if (pbm != string.Empty) {
                    var m = pbw.Split(new[] { ',' });
                    for (var i = 0; i < m.Count() - 1; i++) {
                        pts[i].Shape = m[i] == "r" ? PitchPointShape.o :
                                       m[i] == "s" ? PitchPointShape.l :
                                       m[i] == "j" ? PitchPointShape.i : PitchPointShape.io;
                    }
                }
            }
            return note;
        }

        private static void VibratoFromUst(VibratoExpression vibrato, string ust) {
            var args = ust.Split(new[] { ',' }).Select(double.Parse).ToList();
            if (args.Count() >= 7) {
                vibrato.Length = args[0];
                vibrato.Period = args[1];
                vibrato.Depth = args[2];
                vibrato.In = args[3];
                vibrato.Out = args[4];
                vibrato.Shift = args[5];
                vibrato.Drift = args[6];
            }
        }

        private static string VibratoToUst(VibratoExpression vibrato) {
            var args = new List<double>()
            {
                vibrato.Length,
                vibrato.Period,
                vibrato.Depth,
                vibrato.In,
                vibrato.Out,
                vibrato.Shift,
                vibrato.Drift
            };
            return string.Join(",", args.ToArray());
        }
    }
}
