using System;
using System.Collections.Generic;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.Plugin.Builtin;
using Xunit;

namespace OpenUtau.Plugins {
    public abstract class PhonemizerTest<T> where T : Phonemizer {
        USinger GetDummySinger() {
            var voicebank = new Voicebank {
                BasePath = "null",
                File = "null",
                Name = "Dummy",
            };
            var otoSet = new OtoSet {
                File = "null",
                Name = "",
            };
            otoSet.Otos.Add(new Oto {
                Alias = "a",
                Wav = "a.wav",
                Phonetic = "a",
            });
            voicebank.OtoSets.Add(otoSet);
            return new ClassicSinger(voicebank);
        }

        [Fact]
        public virtual void CreationTest() {
            var phonemizer = Activator.CreateInstance(typeof(T)) as Phonemizer;
            Assert.NotNull(phonemizer);
        }

        [Fact]
        public virtual void SetSingerTest() {
            var phonemizer = Activator.CreateInstance(typeof(T)) as Phonemizer;
            Assert.NotNull(phonemizer);
            phonemizer.SetSinger(USinger.CreateMissing("Unloaded"));
            phonemizer.SetSinger(null);
        }

        [Fact]
        public virtual void DummySingerPhonemizeTest() {
            var phonemizer = Activator.CreateInstance(typeof(T)) as Phonemizer;
            Assert.NotNull(phonemizer);
            phonemizer.SetSinger(GetDummySinger());
            phonemizer.Process(new Phonemizer.Note[] {
                new Phonemizer.Note {
                    lyric = "a",
                    duration = 480,
                    position = 240,
                    tone = 60
                }
            }, null, null, null, null, new Phonemizer.Note[0]);
        }
    }

    public class DefaultPhonemizerTest : PhonemizerTest<DefaultPhonemizer> { }
    public class ArpasingPhonemizerTest : PhonemizerTest<ArpasingPhonemizer> { }
    public class JapaneseCVVCPhonemizerTest : PhonemizerTest<JapaneseCVVCPhonemizer> { }
    public class JapaneseVCVPhonemizerTest : PhonemizerTest<JapaneseVCVPhonemizer> { }
    public class KoreanCVCPhonemizerTest : PhonemizerTest<KoreanCVCPhonemizer> { }
    public class KoreanCVVCPhonemizerTest : PhonemizerTest<KoreanCVVCPhonemizer> { }
}
