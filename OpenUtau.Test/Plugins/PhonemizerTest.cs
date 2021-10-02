using System;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.Plugin.Builtin;
using Xunit;

namespace OpenUtau.Plugins {
    public abstract class PhonemizerTest<T> where T : Phonemizer {
        [Fact]
        public virtual void CreationTest() {
            var phonemizer = Activator.CreateInstance(typeof(T)) as Phonemizer;
            Assert.NotNull(phonemizer);
        }

        [Fact]
        public virtual void SetSingerTest() {
            var phonemizer = Activator.CreateInstance(typeof(T)) as Phonemizer;
            Assert.NotNull(phonemizer);
            phonemizer.SetSinger(new USinger("Dummy"));
            phonemizer.SetSinger(null);
        }
    }

    public class DefaultPhonemizerTest : PhonemizerTest<DefaultPhonemizer> { }
    public class ArpasingPhonemizerTest : PhonemizerTest<ArpasingPhonemizer> { }
    //public class JapaneseCVVCPhonemizerTest : PhonemizerTest<JapaneseCVVCPhonemizer> { }
    public class JapaneseVCVPhonemizerTest : PhonemizerTest<JapaneseVCVPhonemizer> { }
    public class KoreanCVCPhonemizerTest : PhonemizerTest<KoreanCVCPhonemizer> { }
    public class KoreanCVVCPhonemizerTest : PhonemizerTest<KoreanCVVCPhonemizer> { }
    public class KoreanCVVCStandardPronunciationPhonemizerTest : PhonemizerTest<KoreanCVVCStandardPronunciationPhonemizer> { }
}
