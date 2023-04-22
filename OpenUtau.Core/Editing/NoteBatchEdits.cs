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

        struct Point{
            public int X;
            public double Y;
            public PitchPointShape shape;
            public Point(int X, double Y, PitchPointShape shape = PitchPointShape.l) {
                this.X = X;
                this.Y = Y;
                this.shape = shape;
            }

            public Point ChangeShape(PitchPointShape shape) {
                return new Point(X, Y, shape);
            }
        }

        double deltaY(Point pt, Point lineStart, Point lineEnd, PitchPointShape shape){
            return pt.Y - MusicMath.InterpolateShape(lineStart.X, lineEnd.X, lineStart.Y, lineEnd.Y, pt.X, shape);
        }

        PitchPointShape DetermineShape(Point start, Point middle, Point end){
            if(start.Y==end.Y){
                return PitchPointShape.l;
            }
            var k = (middle.Y-start.Y)/(end.Y-start.Y);
            if(k > 0.67){
                return PitchPointShape.o;
            }
            if(k < 0.33){
                return PitchPointShape.i;
            }
            return PitchPointShape.l;
        }

        //reference: https://github.com/sdercolin/utaformatix3/blob/0f026f7024386ca8362972043c3471c6f2ac9859/src/main/kotlin/process/RdpSimplification.kt#L43
        /*
        * The Ramer–Douglas–Peucker algorithm is a line simplification algorithm
        * for reducing the number of points used to define its shape.
        *
        * Wikipedia: https://en.wikipedia.org/wiki/Ramer%E2%80%93Douglas%E2%80%93Peucker_algorithm
        * Implementation reference: https://rosettacode.org/wiki/Ramer-Douglas-Peucker_line_simplification
        * */
        List<Point> simplifyShape(List<Point> pointList, Double epsilon) {
            if (pointList.Count <= 2) {
                return pointList;
            }
            
            // Determine line shape
            var middlePoint = pointList[pointList.Count / 2];
            var startPoint = pointList[0];
            var endPoint = pointList[^1];
            var shape = DetermineShape(startPoint, middlePoint, endPoint);
            
            // Find the point with the maximum distance from line between start and end
            var dmax = 0.0;
            var index = 0;
            var end = pointList.Count - 1;
            for (var i = 1; i < end; i++) {
                var d = Math.Abs(deltaY(pointList[i], pointList[0], pointList[end], shape));
                if (d > dmax) {
                    index = i;
                    dmax = d;
                }
            }
            // If max distance is greater than epsilon, recursively simplify
            List<Point> results = new List<Point>();
            if (dmax > epsilon) {
                // Recursive call
                var recResults1 = simplifyShape(pointList.GetRange(0, index + 1), epsilon);
                var recResults2 = simplifyShape(pointList.GetRange(index, pointList.Count - index), epsilon);

                // Build the result list
                results.AddRange(recResults1.GetRange(0, recResults1.Count - 1));
                results.AddRange(recResults2);
                if (results.Count < 2) {
                    throw new Exception("Problem assembling output");
                }
            } else {
                //Just return start and end points
                results.Add(pointList[0].ChangeShape(shape));
                results.Add(pointList[end]);
            }
            return results;
        }

        public static int LastIndexOfMin<T>(IList<T> self, Func<T, double> selector, int startIndex, int endIndex)
        {
            if (self == null) {
                throw new ArgumentNullException("self");
            }

            if (self.Count == 0) {
                throw new ArgumentException("List is empty.", "self");
            }

            var min = selector(self[endIndex-1]);
            int minIndex = endIndex - 1;

            for (int i = endIndex - 1; i >= startIndex; --i) {
                var value = selector(self[i]);
                if (value < min) {
                    min = value;
                    minIndex = i;
                }
            }

            return minIndex;
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
                var points = Enumerable.Zip(
                    Enumerable.Range(0, pitches.Length),
                    pitches,
                    (i, pitch) => new Point(pitchStart + i * pitchInterval, pitch)
                ).ToList();

                //Reduce pitch point
                points = simplifyShape(points, 10);
                
                //determine where to distribute pitch point
                int idx = 0;
                //note_boundary[i] is the index of the first pitch point after the end of note i
                var note_boundaries = new int[phrase.notes.Length + 1];
                note_boundaries[0] = 2;
                foreach(int i in Enumerable.Range(0,phrase.notes.Length)) {
                    var note = phrase.notes[i];
                    while(idx<points.Count 
                        && points[idx].X<note.end){
                        idx++;
                    }
                    note_boundaries[i + 1] = idx;
                }
                //if there is zero point in the note, adjusted_boundaries is the index of the last zero point
                //otherwise, it is the index of the pitch point with minimal y-distance to the note
                var adjusted_boundaries = new int[phrase.notes.Length + 1];
                adjusted_boundaries[0] = 2;
                foreach(int i in Enumerable.Range(0,phrase.notes.Length - 1)){
                    var note = phrase.notes[i];
                    var notePitch = note.tone*100;
                    //var zero_point = points.FindIndex(note_boundaries[i], note_boundaries[i + 1] - note_boundaries[i], p => p.Y == 0);
                    var zero_point = Enumerable.Range(0,note_boundaries[i + 1] - note_boundaries[i])
                        .Select(j=>note_boundaries[i+1]-1-j)
                        .Where(j => (points[j].Y-notePitch) * (points[j-1].Y-notePitch) <= 0)
                        .DefaultIfEmpty(-1)
                        .First();
                    if(zero_point != -1){
                        adjusted_boundaries[i + 1] = zero_point + 1;
                    }else{
                        adjusted_boundaries[i + 1] = LastIndexOfMin(points, p => Math.Abs(p.Y - notePitch), note_boundaries[i], note_boundaries[i + 1]) + 2;
                    }
                }
                adjusted_boundaries[^1] = note_boundaries[^1];
                //distribute pitch point
                foreach(int i in Enumerable.Range(0,phrase.notes.Length)) {
                    var note = phrase.notes[i];
                    var pitch = points.GetRange(adjusted_boundaries[i]-2,adjusted_boundaries[i + 1]-(adjusted_boundaries[i]-2))
                        .Select(p => new PitchPoint(
                            (float)timeAxis.MsBetweenTickPos(note.position + part.position, p.X + part.position),
                            (float)(p.Y - note.tone * 100) / 10,
                            p.shape))
                        .ToList();
                    pitchPointsPerNote[note.position + phrase.position - part.position] 
                        = Tuple.Create(
                            points[adjusted_boundaries[i] - 2].X + phrase.position,
                            points[adjusted_boundaries[i + 1] - 1].X + phrase.position,
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
