using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace OpenUtau.Core.USTx
{
    public class PartManager : ICmdSubscriber
    {
        class PartContainer
        {
            public UVoicePart Part = null;
        }

        Timer timer;

        UProject _project;
        PartContainer _partContainer;

        public PartManager()
        {
            _partContainer = new PartContainer();
            this.Subscribe(DocManager.Inst);
            timer = new Timer(Update, _partContainer, 0, 100);
        }

        private void Update(Object state)
        {
            var partContainer = state as PartContainer;
            if (partContainer.Part == null) return;
            ManagePart(partContainer.Part);
        }

        public void ManagePart(UVoicePart part)
        {
            lock (part)
            {
                CheckOverlappedNotes(part);
                UpdatePhonemeDurTick(part);
                UpdatePhonemeOto(part);
                UpdateOverlapAdjustment(part);
                UpdateEnvelope(part);
            }
        }

        private void UpdateEnvelope(UVoicePart part)
        {
            foreach (UNote note in part.Notes)
            {
                foreach (UPhoneme phoneme in note.Phonemes)
                {
                    phoneme.Envelope.Points[0].X = -phoneme.PreUtter;
                    phoneme.Envelope.Points[1].X = phoneme.Envelope.Points[0].X + (phoneme.Overlapped ? phoneme.Overlap : 5);
                    phoneme.Envelope.Points[2].X = phoneme.Envelope.Points[1].X;
                    phoneme.Envelope.Points[3].X = MusicMath.TickToMillisecond(phoneme.DurTick, DocManager.Inst.Project.Timing) - phoneme.TailIntrude;
                    phoneme.Envelope.Points[4].X = phoneme.Envelope.Points[3].X + phoneme.TailOverlap;
                }
            }
        }

        private void UpdateOverlapAdjustment(UVoicePart part)
        {
            UPhoneme lastPhoneme = null;
            UNote lastNote = null;
            foreach (UNote note in part.Notes)
            {
                foreach (UPhoneme phoneme in note.Phonemes)
                {
                    if (lastPhoneme != null)
                    {
                        int gapTick = phoneme.Parent.PosTick + phoneme.PosTick - lastPhoneme.Parent.PosTick - lastPhoneme.EndTick;
                        double gapMs = MusicMath.TickToMillisecond(gapTick, DocManager.Inst.Project.Timing);
                        if (gapMs < phoneme.PreUtter)
                        {
                            phoneme.Overlapped = true;
                            double lastDurMs = MusicMath.TickToMillisecond(lastPhoneme.DurTick, DocManager.Inst.Project.Timing);
                            if (phoneme.PreUtter - phoneme.Overlap > (gapMs + lastDurMs) / 2)
                            {
                                phoneme.OverlapCorrection = true;
                                double correctionRatio = (gapMs + lastDurMs) / 2 / (phoneme.PreUtter - phoneme.Overlap);
                                if (phoneme.PreUtter * correctionRatio > gapMs + lastDurMs)
                                    correctionRatio = (gapMs + lastDurMs) / phoneme.PreUtter;
                                phoneme.PreUtter *= correctionRatio;
                                phoneme.Overlap *= correctionRatio;

                                if (phoneme.PreUtter - phoneme.Overlap < gapMs) phoneme.Overlap = phoneme.PreUtter - gapMs;
                            }
                            else phoneme.OverlapCorrection = false;

                            lastPhoneme.TailIntrude = phoneme.PreUtter - gapMs;
                            lastPhoneme.TailOverlap = phoneme.Overlap;
                        }
                        else
                        {
                            phoneme.Overlapped = false;
                            lastPhoneme.TailIntrude = 0;
                            lastPhoneme.TailOverlap = 5;
                        }
                    }
                    else phoneme.Overlapped = false;
                    lastPhoneme = phoneme;
                }
                lastNote = note;
            }
        }

        private void UpdatePhonemeOto(UVoicePart part)
        {
            var singer = DocManager.Inst.Project.Tracks[part.TrackNo].Singer;
            foreach (UNote note in part.Notes)
            {
                foreach (UPhoneme phoneme in note.Phonemes)
                {
                    if (phoneme.AutoRemapped)
                    {
                        if (phoneme.Phoneme.StartsWith("?"))
                        {
                            phoneme.Phoneme = phoneme.Phoneme.Substring(1);
                            phoneme.AutoRemapped = false;
                        }
                        else
                        {
                            string noteString = MusicMath.GetNoteString(note.NoteNum);
                            if (singer.PitchMap.ContainsKey(noteString))
                                phoneme.RemappedBank = singer.PitchMap[noteString];
                        }
                    }

                    if (singer.AliasMap.ContainsKey(phoneme.PhonemeRemapped))
                    {
                        phoneme.Oto = singer.AliasMap[phoneme.PhonemeRemapped];
                        phoneme.PhonemeError = false;
                        phoneme.Overlap = phoneme.Oto.Overlap;
                        phoneme.PreUtter = phoneme.Oto.Preutter;
                        int vel = (int)phoneme.Parent.Expressions["velocity"].Data;
                        if (vel != 100)
                        {
                            double stretchRatio = Math.Pow(2, 1.0 - (double)vel / 100);
                            phoneme.Overlap *= stretchRatio;
                            phoneme.PreUtter *= stretchRatio;
                        }
                    }
                    else
                    {
                        phoneme.PhonemeError = true;
                        phoneme.Overlap = 0;
                        phoneme.PreUtter = 0;
                    }
                }
            }
        }

        private void UpdatePhonemeDurTick(UVoicePart part)
        {
            UNote lastNote = null;
            UPhoneme lastPhoneme = null;
            foreach (UNote note in part.Notes)
            {
                foreach(UPhoneme phoneme in note.Phonemes)
                {
                    phoneme.DurTick = phoneme.Parent.DurTick - phoneme.PosTick;
                    if (lastPhoneme != null)
                        if (lastPhoneme.Parent == phoneme.Parent)
                            lastPhoneme.DurTick = phoneme.PosTick - lastPhoneme.PosTick;
                    lastPhoneme = phoneme;
                }
                lastNote = note;
            }
        }

        private void CheckOverlappedNotes(UVoicePart part)
        {
            UNote lastNote = null;
            foreach (UNote note in part.Notes)
            {
                if (lastNote != null && lastNote.EndTick > note.PosTick)
                {
                    lastNote.Error = true;
                    note.Error = true;
                }
                else note.Error = false;
                lastNote = note;
            }
        }

        # region Cmd Handling

        # endregion

        # region ICmdSubscriber

        public void Subscribe(ICmdPublisher publisher) { if (publisher != null) publisher.Subscribe(this); }

        public void OnNext(UCommand cmd, bool isUndo)
        {
            if (cmd is PartCommand)
            {
                var _cmd = cmd as PartCommand;
                if (_cmd.part != _partContainer.Part) return;
                else if (_cmd is RemovePartCommand) _partContainer.Part = null;
            }
            else if (cmd is UNotification)
            {
                var _cmd = cmd as UNotification;
                if (_cmd is LoadPartNotification) { if (!(_cmd.part is UVoicePart)) return; _partContainer.Part = (UVoicePart)_cmd.part; _project = _cmd.project; }
                else if (_cmd is LoadProjectNotification) _partContainer.Part = null;
            }
        }

        # endregion

    }
}
