using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Classic;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public abstract class PhonemizerTestBase {
        protected readonly ITestOutputHelper output;

        protected abstract Phonemizer CreatePhonemizer();

        public PhonemizerTestBase(ITestOutputHelper output) {
            this.output = output;
        }

        public void RunPhonemizeTest(string singerName, string[] lyrics, string[] tones, string[] colors, string[] aliases) {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var basePath = Path.Join(dir, "Files");
            var file = Path.Join(basePath, singerName, "character.txt");

            var voicebank = new Voicebank() { File = file, BasePath = dir };
            VoicebankLoader.LoadVoicebank(voicebank);
            var singer = new ClassicSinger(voicebank);
            singer.EnsureLoaded();

            var timeAxis = new Core.TimeAxis();
            timeAxis.BuildSegments(new Core.Ustx.UProject());

            var phonemizer = CreatePhonemizer();
            phonemizer.SetSinger(singer);
            phonemizer.SetTiming(timeAxis);

            var results = new List<Phonemizer.Result>();
            var groups = GetNotes(lyrics, tones, colors);
            for (var i = 0; i < groups.Count; i++) {
                results.Add(phonemizer.Process(
                    groups[i],
                    i > 0 ? groups[i - 1].First() : null,
                    i < groups.Count - 1 ? groups[i + 1].First() : null,
                    i > 0 ? groups[i - 1].First() : null,
                    i < groups.Count - 1 ? groups[i + 1].First() : null,
                    i > 0 ? groups[i - 1] : null));
            }
            var resultAliases = results.SelectMany(r => r.phonemes).Select(p => p.phoneme).ToArray();

            var builder = new StringBuilder();
            foreach (var resultAlias in resultAliases) {
                builder.Append("\"");
                builder.Append(resultAlias);
                builder.Append("\", ");
            }
            output.WriteLine(builder.ToString());

            Assert.Equal(aliases, resultAliases);
        }

        List<Phonemizer.Note[]> GetNotes(string[] lyrics, string[] tones, string[] colors) {
            Assert.Equal(lyrics.Length, tones.Length);
            Assert.Equal(lyrics.Length, colors.Length);
            var result = new List<Phonemizer.Note[]>();
            var group = new List<Phonemizer.Note>();
            int position = 240;
            for (var i = 0; i < lyrics.Length; i++) {
                var lyric = lyrics[i];
                if (!lyric.StartsWith("+") && group.Count > 0) {
                    result.Add(group.ToArray());
                    group.Clear();
                }
                group.Add(new Phonemizer.Note {
                    lyric = lyric,
                    duration = 240,
                    position = position,
                    tone = Core.MusicMath.NameToTone(tones[i]),
                    phonemeAttributes = new Phonemizer.PhonemeAttributes[] {
                        new Phonemizer.PhonemeAttributes{
                            index = 0,
                            consonantStretchRatio = 1,
                            voiceColor = colors[i],
                        }
                    },
                });
                position += 240;
            }
            if (group.Count > 0) {
                result.Add(group.ToArray());
                group.Clear();
            }
            return result;
        }
    }
}
