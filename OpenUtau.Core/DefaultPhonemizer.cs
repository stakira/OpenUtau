using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core {
    [Phonemizer("Default Phonemizer", "CV")]
    public class DefaultPhonemizer : Phonemizer {
        public override void SetSinger(USinger singer) { }
        public override Phoneme[] Process(Note[] notes, Note? prevNeighbour, Note? nextNeighbour) {
            return new Phoneme[] {
                new Phoneme {
                    phoneme = notes[0].lyric,
                }
            };
        }
    }
}
