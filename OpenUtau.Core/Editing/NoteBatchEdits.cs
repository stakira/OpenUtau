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
}
