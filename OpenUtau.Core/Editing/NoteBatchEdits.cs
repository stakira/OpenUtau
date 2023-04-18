using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;

namespace OpenUtau.Core.Editing {
    public class AddTailNote : BatchEdit {
        public string Name => name;

        private string lyric;
        private string name;

        public AddTailNote(string lyric, string name) {
            this.lyric = lyric;
            this.name = name;
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            List<UNote> toAdd = new List<UNote>();
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            foreach (var note in notes) {
                if (note.lyric != lyric && (note.Next == null || note.Next.position > note.End + 120)) {
                    toAdd.Add(project.CreateNote(note.tone, note.End, 120));
                }
            }
            if (toAdd.Count == 0) {
                return;
            }
            docManager.StartUndoGroup(true);
            foreach (var note in toAdd) {
                note.lyric = lyric;
                docManager.ExecuteCmd(new AddNoteCommand(part, note));
            }
            docManager.EndUndoGroup();
        }
    }

    public class Transpose : BatchEdit {
        public string Name => name;

        private int deltaNoteNum;
        private string name;

        public Transpose(int deltaNoteNum, string name) {
            this.deltaNoteNum = deltaNoteNum;
            this.name= name;
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            docManager.StartUndoGroup(true);
            foreach (var note in notes) {
                docManager.ExecuteCmd(new MoveNoteCommand(part, note, 0, deltaNoteNum));
            }
            docManager.EndUndoGroup();
        }
    }

    public class QuantizeNotes : BatchEdit {
        public virtual string Name => name;

        private int quantize;
        private string name;

        public QuantizeNotes(int quantize) {
            this.quantize = quantize;
            name = $"pianoroll.menu.notes.quantize{quantize}";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            docManager.StartUndoGroup(true);
            foreach (var note in notes) {
                int pos = note.position;
                int end = note.End;
                int newPos = (int)Math.Round(1.0 * pos / quantize) * quantize;
                int newEnd = (int)Math.Round(1.0 * end / quantize) * quantize;
                if (newPos != pos) {
                    docManager.ExecuteCmd(new MoveNoteCommand(part, note, newPos - pos, 0));
                    docManager.ExecuteCmd(new ResizeNoteCommand(part, note, newEnd - newPos - note.duration));
                } else if (newEnd != end) {
                    docManager.ExecuteCmd(new ResizeNoteCommand(part, note, newEnd - newPos - note.duration));
                }
            }
            docManager.EndUndoGroup();
        }
    }

    public class HanziToPinyin : BatchEdit {
        public virtual string Name => name;

        private string name;

        public HanziToPinyin() {
            name = "pianoroll.menu.notes.hanzitopinyin";
        }
        
        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var pinyinNotes = selectedNotes
                .Where(note => BaseChinesePhonemizer.IsHanzi(note.lyric))
                .ToArray();
            var pinyinResult = BaseChinesePhonemizer.Romanize(pinyinNotes.Select(note=>note.lyric));
            docManager.StartUndoGroup(true);
            foreach(var t in Enumerable.Zip(pinyinNotes, pinyinResult,
                (note, pinyin) => Tuple.Create(note, pinyin))) {
                docManager.ExecuteCmd(new ChangeNoteLyricCommand(part, t.Item1, t.Item2));
            }
            docManager.EndUndoGroup();
        }
    }

    public class ResetPitchBends : BatchEdit {
        public virtual string Name => name;

        private string name;

        public ResetPitchBends() {
            name = "pianoroll.menu.notes.reset.pitchbends";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            docManager.StartUndoGroup(true);
            foreach (var note in notes) {
                docManager.ExecuteCmd(new ResetPitchPointsCommand(part, note));
            }
            docManager.EndUndoGroup();
        }
    }

    public class ResetAllExpressions : BatchEdit {
        public virtual string Name => name;

        private string name;

        public ResetAllExpressions() {
            name = "pianoroll.menu.notes.reset.exps";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            docManager.StartUndoGroup(true);
            foreach (var note in notes) {
                if (note.phonemeExpressions.Count > 0) {
                    docManager.ExecuteCmd(new ResetExpressionsCommand(part, note));
                }
            }
            var curveAbbrs = part.curves.Select(c => c.abbr).ToArray();
            foreach (var abbr in curveAbbrs) {
                docManager.ExecuteCmd(new ClearCurveCommand(part, abbr));
            }
            docManager.EndUndoGroup();
        }
    }

    public class ClearVibratos : BatchEdit {
        public virtual string Name => name;

        private string name;

        public ClearVibratos() {
            name = "pianoroll.menu.notes.clear.vibratos";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            docManager.StartUndoGroup(true);
            foreach (var note in notes) {
                if (note.vibrato.length > 0) {
                    docManager.ExecuteCmd(new VibratoLengthCommand(part, note, 0));
                }
            }
            docManager.EndUndoGroup();
        }
    }

    public class ResetVibratos : BatchEdit {
        public virtual string Name => name;

        private string name;

        public ResetVibratos() {
            name = "pianoroll.menu.notes.reset.vibratos";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            docManager.StartUndoGroup(true);
            foreach (var note in notes) {
                docManager.ExecuteCmd(new VibratoPeriodCommand(part, note, NotePresets.Default.DefaultVibrato.VibratoPeriod));
                docManager.ExecuteCmd(new VibratoDepthCommand(part, note, NotePresets.Default.DefaultVibrato.VibratoDepth));
                docManager.ExecuteCmd(new VibratoFadeInCommand(part, note, NotePresets.Default.DefaultVibrato.VibratoIn));
                docManager.ExecuteCmd(new VibratoFadeOutCommand(part, note, NotePresets.Default.DefaultVibrato.VibratoOut));
                docManager.ExecuteCmd(new VibratoShiftCommand(part, note, NotePresets.Default.DefaultVibrato.VibratoShift));
                if (NotePresets.Default.AutoVibratoToggle && note.duration >= NotePresets.Default.AutoVibratoNoteDuration) {
                    docManager.ExecuteCmd(new VibratoLengthCommand(part, note, NotePresets.Default.DefaultVibrato.VibratoLength));
                } else {
                    docManager.ExecuteCmd(new VibratoLengthCommand(part, note, 0));
                }
            }
            docManager.EndUndoGroup();
        }
    }

    public class ClearTimings : BatchEdit {
        public virtual string Name => name;

        private string name;

        public ClearTimings() {
            name = "pianoroll.menu.notes.reset.phonemetimings";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            docManager.StartUndoGroup(true);
            foreach (var note in notes) {
                bool shouldClear = false;
                foreach (var o in note.phonemeOverrides) {
                    if (o.offset != null || o.preutterDelta != null || o.overlapDelta != null) {
                        shouldClear = true;
                        break;
                    }
                }
                if (shouldClear) {
                    docManager.ExecuteCmd(new ClearPhonemeTimingCommand(part, note));
                }
            }
            docManager.EndUndoGroup();
        }
    }

    public class LoadRenderedPitch : BatchEdit {
        public virtual string Name => name;

        private string name;

        public LoadRenderedPitch() {
            name = "pianoroll.menu.notes.loadrenderedpitch";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var renderer = project.tracks[part.trackNo].RendererSettings.Renderer;
            if (renderer == null || !renderer.SupportsRenderPitch) {
                docManager.ExecuteCmd(new ErrorMessageNotification("Not supported"));
                return;
            }
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            var positions = notes.Select(n => n.position + part.position).ToHashSet();
            var phrases = part.renderPhrases.Where(phrase => phrase.notes.Any(n => positions.Contains(phrase.position + n.position)));
            docManager.StartUndoGroup(true);
            float minPitD = -1200;
            if (project.expressions.TryGetValue(Format.Ustx.PITD, out var descriptor)) {
                minPitD = descriptor.min;
            }
            foreach (var phrase in phrases) {
                var result = renderer.LoadRenderedPitch(phrase);
                if (result == null) {
                    continue;
                }
                int? lastX = null;
                int? lastY = null;
                // TODO: Optimize interpolation and command.
                for (int i = 0; i < result.tones.Length; i++) {
                    if (result.tones[i] < 0) {
                        continue;
                    }
                    int x = phrase.position - part.position + (int)result.ticks[i];
                    int pitchIndex = Math.Clamp((x - (phrase.position - part.position - phrase.leading)) / 5, 0, phrase.pitches.Length - 1);
                    float basePitch = phrase.pitchesBeforeDeviation[pitchIndex];
                    int y = (int)(result.tones[i] * 100 - basePitch);
                    lastX ??= x;
                    lastY ??= y;
                    if (y > minPitD) {
                        docManager.ExecuteCmd(new SetCurveCommand(
                            project, part, Format.Ustx.PITD, x, y, lastX.Value, lastY.Value));
                    }
                    lastX = x;
                    lastY = y;
                }
            }
            docManager.EndUndoGroup();
        }
    }

    public class BakePitch: BatchEdit {
        public virtual string Name => name;
        private string name;
        public BakePitch() {
            name = "pianoroll.menu.notes.bakepitch";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            TimeAxis timeAxis = project.timeAxis;
            const int pitchInterval = 5;
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            var positions = notes.Select(n => n.position + part.position).ToHashSet();
            var phrases = part.renderPhrases.Where(phrase => phrase.notes.Any(n => positions.Contains(phrase.position + n.position)));
            float minPitD = -1200;
            if (project.expressions.TryGetValue(Format.Ustx.PITD, out var descriptor)) {
                minPitD = descriptor.min;
            }
            //Dictionary from note start tick to pitch point
            //value is a tuple of (starttick, endtick, pitch points)
            //Here starttick and endtick are project absolute tick, and pitch points are ms relative to the starttick
            var pitchPointsPerNote = new Dictionary<int, Tuple<int,int,List<PitchPoint>>>();
            foreach (var phrase in phrases) {
                var pitchStart = -phrase.leading;
                //var ticks = Enumerable.Range(0, phrase.duration).Select(i => i * 5).ToArray();
                var pitches = phrase.pitches;
                //Reduce pitch point
                //reference: https://github.com/oatsu-gh/ENUNU/blob/2c96053cc651b600cf15668991f8131c1d85eb6c/synthesis/extensions/f0_feedbacker.py#L64
                //Currently only extreme points are preserved
                var delta_pitch = pitches.Zip(pitches.Skip(1), (a, b) => b - a).Prepend(0).Append(0).ToList();
                var reduced_pitch_indices = Enumerable.Range(1,pitches.Length-2)
                    .Where(i => delta_pitch[i - 1] * delta_pitch[i]<=0 && delta_pitch[i - 1] != delta_pitch[i])
                    .Prepend(0).Append(pitches.Length-1)
                    .ToArray();
                //distribute pitch point
                //reference:https://github.com/oatsu-gh/ENUNU/blob/2c96053cc651b600cf15668991f8131c1d85eb6c/synthesis/extensions/f0_feedbacker.py#L26
                int idx = 0;
                //note_boundary[i] is the index of the pitch point that is the first pitch point after the end of note i
                var note_boundary = new int[phrase.notes.Length + 1];
                note_boundary[0] = 2;
                foreach(int i in Enumerable.Range(0,phrase.notes.Length)) {
                    var note = phrase.notes[i];
                    while(idx<reduced_pitch_indices.Length 
                        && pitchStart+reduced_pitch_indices[idx]*pitchInterval<note.end){
                        idx++;
                    }
                    note_boundary[i + 1] = idx;
                }
                foreach(int i in Enumerable.Range(0,phrase.notes.Length)) {
                    var note = phrase.notes[i];
                    var pitch = reduced_pitch_indices[(note_boundary[i]-2)..note_boundary[i + 1]]
                        .Select(j => new PitchPoint(
                            (float)timeAxis.MsBetweenTickPos(note.position + part.position, pitchStart + j * pitchInterval + part.position),
                            (pitches[j] - note.tone * 100) / 10))
                        .ToList();
                    pitchPointsPerNote[note.position + phrase.position - part.position] 
                        = Tuple.Create(
                            pitchStart + reduced_pitch_indices[note_boundary[i] - 2] * pitchInterval + phrase.position,
                            pitchStart + reduced_pitch_indices[note_boundary[i + 1] - 1] * pitchInterval + phrase.position,
                            pitch);
                }
            }
            docManager.StartUndoGroup(true);
            foreach(var note in selectedNotes) {
                if (pitchPointsPerNote.TryGetValue(note.position, out var tickRangeAndPitch)) {
                    var pitch = tickRangeAndPitch.Item3;
                    docManager.ExecuteCmd(new ResetPitchPointsCommand(part, note));
                    int index = 0;
                    foreach(var point in pitch) {
                        docManager.ExecuteCmd(new AddPitchPointCommand(part, note, point, index));
                        index++;
                    }
                    docManager.ExecuteCmd(new DeletePitchPointCommand(part, note, index));
                    docManager.ExecuteCmd(new DeletePitchPointCommand(part, note, index));
                    var lastPitch = note.pitch.data[^1]; 
                    docManager.ExecuteCmd(new MovePitchPointCommand(part, lastPitch ,0, -lastPitch.Y));
                    
                }
            }
            docManager.EndUndoGroup();
            docManager.StartUndoGroup(true);
            foreach(var note in selectedNotes) {
                if (pitchPointsPerNote.TryGetValue(note.position, out var tickRangeAndPitch)) {
                    docManager.ExecuteCmd(new SetCurveCommand(project, part, Format.Ustx.PITD, 
                        tickRangeAndPitch.Item1, 0, 
                        tickRangeAndPitch.Item1, 0));
                    docManager.ExecuteCmd(new SetCurveCommand(project, part, Format.Ustx.PITD, 
                        tickRangeAndPitch.Item2, 0, 
                        tickRangeAndPitch.Item2, 0));
                    docManager.ExecuteCmd(new SetCurveCommand(project, part, Format.Ustx.PITD, 
                        tickRangeAndPitch.Item1, 0, 
                        tickRangeAndPitch.Item2, 0));
                }
            }
            docManager.EndUndoGroup();
            
        }
    }
}
