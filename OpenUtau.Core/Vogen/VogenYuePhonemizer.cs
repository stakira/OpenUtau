using Microsoft.ML.OnnxRuntime;
using OpenUtau.Api;
using TinyPinyin;

namespace OpenUtau.Core.Vogen {
    [Phonemizer("Vogen Chinese Yue Phonemizer", "VOGEN ZH-YUE")]
    public class VogenYuePhonemizer : VogenBasePhonemizer {
        private static InferenceSession g2p;
        private static InferenceSession prosody;

        public VogenYuePhonemizer() {
            g2p ??= new InferenceSession(Data.VogenRes.g2p_yue);
            G2p = g2p;
            prosody ??= new InferenceSession(Data.VogenRes.po_yue);
            Prosody = prosody;
        }

        protected override string LangPrefix => "yue:";

        protected override string Romanize(string lyric) {
            if (lyric.Length > 0 && PinyinHelper.IsChinese(lyric[0])) {
                return PinyinHelper.GetPinyin(lyric).ToLowerInvariant();
            }
            return lyric;
        }
    }
}
