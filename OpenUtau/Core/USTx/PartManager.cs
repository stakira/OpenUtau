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
            lock (partContainer.Part)
            {
                var part = partContainer.Part;
                var singer = DocManager.Inst.Project.Tracks[part.TrackNo].Singer;
                UPhoneme lastPhoneme = null;
                foreach (UNote note in part.Notes)
                {
                    foreach (UPhoneme phoneme in note.Phonemes)
                    {
                        if (lastPhoneme != null)
                        {
                            if (lastPhoneme.Parent != note) lastPhoneme.DurTick = lastPhoneme.Parent.DurTick - lastPhoneme.PosTick;
                            else lastPhoneme.DurTick = phoneme.PosTick - lastPhoneme.PosTick;
                        }
                        lastPhoneme = phoneme;
                    }
                }
                if (lastPhoneme != null) { lastPhoneme.DurTick = lastPhoneme.Parent.DurTick - lastPhoneme.PosTick; }
                UNote lastNote = null;
                lastPhoneme = null;
                foreach (UNote note in part.Notes)
                {
                    foreach (UPhoneme phoneme in note.Phonemes)
                    {
                        string pho = phoneme.Phoneme;
                        if (pho.StartsWith("?") && phoneme.AutoRemapped)
                        {
                            phoneme.Phoneme = pho = pho.Substring(1);
                            phoneme.AutoRemapped = false;
                        }
                        else if (phoneme.AutoRemapped)
                        {
                            if (singer.PitchMap.ContainsKey(MusicMath.GetNoteString(note.NoteNum)))
                                pho += singer.PitchMap[MusicMath.GetNoteString(note.NoteNum)];
                        }
                        if (singer.AliasMap.ContainsKey(pho))
                        {
                            var oto = singer.AliasMap[pho];
                            phoneme.IsValid = true;
                            phoneme.Overlap = oto.Overlap;
                            phoneme.PreUtter = oto.Preutter;
                            bool continuous = false;
                            if (lastNote != null && lastPhoneme.Parent != note && lastNote.EndTick == note.PosTick ||
                                lastPhoneme != null && lastPhoneme.Parent == note)
                            {
                                continuous = true;
                                float lastDurMs = (float)MusicMath.TickToMillisecond(lastPhoneme.DurTick, DocManager.Inst.Project.Timing);
                                // Overlap correction
                                if (phoneme.PreUtter - phoneme.Overlap > lastDurMs / 2)
                                {
                                    float correctionRatio = lastDurMs / 2 / (phoneme.PreUtter - phoneme.Overlap);
                                    phoneme.Overlap *= correctionRatio;
                                    phoneme.PreUtter *= correctionRatio;
                                }
                            }
                            phoneme.Envelope.Points[0].X = 0;
                            phoneme.Envelope.Points[1].X = phoneme.Overlap;
                            phoneme.Envelope.Points[2].X = phoneme.Overlap;
                            phoneme.Envelope.Points[4].X = phoneme.PreUtter + (float)MusicMath.TickToMillisecond(phoneme.DurTick, DocManager.Inst.Project.Timing);
                            phoneme.Envelope.Points[3].X = phoneme.Envelope.Points[4].X - 5;

                            if (continuous)
                            {
                                lastPhoneme.Envelope.Points[3].X = lastPhoneme.Envelope.Points[4].X - phoneme.PreUtter;
                                lastPhoneme.Envelope.Points[4].X = lastPhoneme.Envelope.Points[3].X + phoneme.Overlap;
                            }
                        }
                        else
                        {
                            phoneme.IsValid = false;
                            phoneme.Overlap = 0;
                            phoneme.PreUtter = 0;
                        }
                        lastPhoneme = phoneme;
                    }
                    if (lastNote != null && note.PitchBend.SnapFirst)
                    {
                        note.PitchBend.Points[0].Y = (lastNote.NoteNum - note.NoteNum) * 10;
                    }
                    lastNote = note;
                }
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
