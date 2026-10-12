using System;
using System.IO;
using OpenUtau.Core.Neutrino;
using Xunit;

namespace OpenUtau.Test.Core.Neutrino {
    public class NeutrinoOnnxEngineTest {
        [Fact]
        public void PitchOffsetsPreserveUnvoicedFramesAndApplyCents() {
            float[] f0 = { 440, 0, 440, 440 };
            NeutrinoOnnxEngine.ApplyPitchOffsets(f0, 1, new float[] { 0, 1200, 1200, -1200, 0 });
            Assert.Equal(new float[] { 880, 0, 220, 440 }, f0);
        }

        [Fact]
        public void VocoderReceivesMelFirstAndF0InLastChannelOfEachFrame() {
            float[] packed = new float[4 * 101];
            float[] mel = new float[200];
            for (int i = 0; i < mel.Length; i++) { mel[i] = -7 + i * .01f; }
            NeutrinoOnnxEngine.CopyVocoderFeatures(packed, 1, mel, new[] { 261.6256f, 293.6648f });
            Assert.Equal(mel[0], packed[101]);
            Assert.Equal(mel[99], packed[200]);
            Assert.Equal(261.6256f, packed[201]);
            Assert.Equal(mel[100], packed[202]);
            Assert.Equal(mel[199], packed[301]);
            Assert.Equal(293.6648f, packed[302]);
            Assert.Equal(0, packed[0]);
            Assert.Equal(0, packed[303]);
        }

        [Fact]
        public void LabelMapsPhonemeAndScorePitchToModelInputs() {
            var phone = NeutrinoOnnxEngine.ParseLabel("5000000 10000000 v@xx^pau-k+a=pau_xx~00-1!2[xx/E:C4]xx");
            Assert.Equal("k", phone.Symbol);
            Assert.Equal(17, phone.Id);
            Assert.Equal(0, phone.InNote);
            Assert.Equal(.5, phone.Start);
            Assert.Equal(1, phone.End);
            Assert.Equal(.5f, phone.NoteDuration);
            Assert.InRange(phone.Hz, 261.62f, 261.63f);
            var vowel = NeutrinoOnnxEngine.ParseLabel("5000000 10000000 v@xx^k-a+sil=pau_xx~00-2!1[xx/E:F#4]xx");
            Assert.Equal(1, vowel.Id);
            Assert.Equal(1, vowel.InNote);
            Assert.InRange(vowel.Hz, 369.99f, 370.00f);
        }

        [Fact]
        public void RestHasNoPitchAndUnknownPhonemesFailClearly() {
            var rest = NeutrinoOnnxEngine.ParseLabel("0 5000000 p@xx^xx-pau+a=xx_xx~00-1!1[xx/E:xx]xx");
            Assert.Equal(26, rest.Id);
            Assert.Equal(0, rest.Hz);
            Assert.Throws<InvalidDataException>(() => NeutrinoOnnxEngine.ParseLabel("0 5000000 v@xx^xx-unknown+a=xx_xx~00-1!1[xx/E:C4]xx"));
            Assert.Throws<InvalidDataException>(() => NeutrinoOnnxEngine.ParseLabel("bad label"));
        }
    }
}
