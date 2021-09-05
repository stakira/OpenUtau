using System.Linq;

namespace OpenUtau.Core {
    public class DefaultPhonemizer : Phonemizer {
        private Ustx.USinger singer;

        public override string Name => "Default Phonemizer";
        public override string Tag => "CV";
        public override void SetSinger(Ustx.USinger singer) => this.singer = singer;
        public override Phoneme[] Process(Note[] notes, Note? prevNeighbour, Note? nextNeighbour) {
            return new Phoneme[] {
                new Phoneme {
                    phoneme = notes[0].lyric,
                }
            };
        }
    }
}
