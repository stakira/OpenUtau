using System.Linq;

namespace OpenUtau.Core {
    public class DefaultPhonemizer : Phonemizer {
        private Ustx.USinger singer;

        public override string Name => "Default Phonemizer";
        public override string Tag => "CV";
        public override void SetSinger(Ustx.USinger singer) => this.singer = singer;
        public override Phoneme[] Process(Note[] notes, Note? prev, Note? next) {
            var note = notes[0];
            var phoneme = note.lyric;
            phoneme = TryMapPhoneme(phoneme, note.tone, singer);
            return new Phoneme[] {
                new Phoneme {
                    phoneme = phoneme,
                    duration = notes.Sum(n => n.duration),
                }
            };
        }
    }
}
