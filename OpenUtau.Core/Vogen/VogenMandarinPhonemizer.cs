using Microsoft.ML.OnnxRuntime;
using OpenUtau.Api;
using TinyPinyin;

namespace OpenUtau.Core.Vogen {
    [Phonemizer("Vogen Chinese Mandarin Phonemizer", "VOGEN ZH", language: "ZH")]
    public class VogenMandarinPhonemizer : VogenBasePhonemizer {
        private static InferenceSession? g2p;
        private static InferenceSession? prosody;

        public VogenMandarinPhonemizer() {
            g2p ??= new InferenceSession(Data.VogenRes.g2p_man);
            G2p = g2p;
            prosody ??= new InferenceSession(Data.VogenRes.po_man);
            Prosody = prosody;
        }
        protected override string LangPrefix => "man:";

        protected override string[] Romanize(string[] lyrics) {
            var result = new string[lyrics.Length];
            for (int i = 0; i < lyrics.Length; ++i) {
                string lyric = lyrics[i];
                if (lyric.Length > 0 && PinyinHelper.IsChinese(lyric[0])) {
                    result[i] = PinyinHelper.GetPinyin(lyric).ToLowerInvariant();
                } else {
                    result[i] = lyric;
                }
            }
            return result;
        }
    }
}
