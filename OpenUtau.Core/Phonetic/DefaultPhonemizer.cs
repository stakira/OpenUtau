namespace OpenUtau.Core {
    public class DefaultPhonemizer : Phonemizer {
        private Ustx.USinger singer;

        public override string Name => "Default Phonemizer";
        public override string Tag => "CV";
        public override void SetSinger(Ustx.USinger singer) => this.singer = singer;
        public override Phoneme[] Process(Note note, Note? prev, Note? next) {
            var phoneme = note.lyric;
            phoneme = TryMapPhoneme(phoneme, note.tone, singer);
            return new Phoneme[] {
                new Phoneme {
                    phoneme = phoneme,
                    duration = note.duration,
                }
            };
        }
    }
}
