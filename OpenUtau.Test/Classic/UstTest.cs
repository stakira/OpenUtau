using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenUtau.Core.Ustx;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Classic {
    public class UstTest {
        readonly ITestOutputHelper output;

        public UstTest(ITestOutputHelper output) {
            this.output = output;
        }

        [Fact]
        public void UstLoadingTest() {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            dir = Path.Join(dir, "Usts");
            var encoding = Encoding.GetEncoding("shift_jis");
            var readerOptions = new ReaderOptions {
                ArchiveEncoding = new ArchiveEncoding(encoding, encoding)
            };
            foreach (var file in Directory.GetFiles(dir, "*.zip")) {
                using (var archive = ArchiveFactory.Open(file, readerOptions)) {
                    foreach (var entry in archive.Entries) {
                        if (Path.GetExtension(entry.Key) != ".ust") {
                            continue;
                        }
                        output.WriteLine(Path.GetFileName(file) + ":" + entry.Key);
                        using (var reader = new StreamReader(entry.OpenEntryStream(), encoding)) {
                            var project = Ust.Load(reader, entry.Key);
                            project.AfterLoad();
                            project.ValidateFull();
                            Assert.Single(project.parts);
                            var part = project.parts[0] as UVoicePart;
                            Assert.True(part.notes.Count > 0);
                        }
                    }
                }
            }
        }


        [Fact]
        public void EqualInLyric() {
            string ust = @"
[#SETTING]
Tempo=128.00
Tool1=
[#0000]
Length=15
Lyric=A==B[C=D],EFG
NoteNum=60
PreUtterance=";
            using (var stream = new MemoryStream()) {
                using (var writer = new StreamWriter(stream, leaveOpen: true)) {
                    writer.Write(ust);
                }
                stream.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(stream)) {
                    var project = Ust.Load(reader, "test.ust");
                    project.AfterLoad();
                    project.ValidateFull();
                    Assert.Single(project.parts);
                    var part = project.parts[0] as UVoicePart;
                    Assert.Single(part.notes);
                    Assert.Equal("A==B[C=D],EFG", part.notes.First().lyric);
                    Assert.Equal(60, part.notes.First().tone);
                }
            }
        }

        [Fact]
        public void ParsePluginParseNoteTest() {
            // This method is tested only when the plugin returns only some blocks

            // Given
            var project = new UProject();
            project.tracks.Add(new UTrack {
                TrackNo = 0,
            });
            var part = new UVoicePart() {
                trackNo = 0,
                position = 0,
            };
            project.parts.Add(part);

            var before = UNote.Create();
            before.lyric = "a";
            before.duration = 10;
            
            var first = UNote.Create();
            first.lyric = "ka";
            first.duration = 20;
            
            var second = UNote.Create();
            second.lyric = "r";
            second.duration = 30;
            
            var third = UNote.Create();
            third.lyric = "ta";
            third.duration = 40;

            var last = UNote.Create();
            last.lyric = "na";
            last.duration = 50;
            
            var after = UNote.Create();
            after.lyric = "ha";
            after.duration = 60;
            
            part.notes.Add(before);
            part.notes.Add(first);
            part.notes.Add(second);
            part.notes.Add(third);
            part.notes.Add(last);
            part.notes.Add(after);

            var sequence = new List<UNote> {
                first,
                second,
                third,
                last
            };

            var encoding = "shift_jis";

            // Create plugin edited tmp file
            string diffFile = Path.GetTempFileName();
            try {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                using (var writer = new StreamWriter(diffFile, false, Encoding.GetEncoding(encoding))) {
                    // duration and lyric
                    writer.WriteLine("[#0000]");
                    writer.WriteLine("Length=480");
                    writer.WriteLine("Lyric=A");
                    writer.WriteLine("[#0001]");
                    writer.WriteLine("Length=480");
                    writer.WriteLine("Lyric=R");
                    // duration is null (change)
                    writer.WriteLine("[#0002]");
                    writer.WriteLine("Lyric=zo");
                    // duration is zero (delete)
                    writer.WriteLine("[#0003]");
                    writer.WriteLine("Length=");
                    // insert
                    writer.WriteLine("[#INSERT]");
                    writer.WriteLine("Length=240");
                    writer.WriteLine("Lyric=me");
                }

                // When
                var (toRemove, toAdd) = Ust.ParsePlugin(project, part, first, last, sequence, diffFile, encoding);

                // Then
                Assert.Equal(4, toRemove.Count);
                Assert.Equal(3, toAdd.Count);
                Assert.Equal(480, toAdd[0].duration);
                Assert.Equal("A", toAdd[0].lyric);
                Assert.Equal(40, toAdd[1].duration);
                Assert.Equal("zo", toAdd[1].lyric);
                Assert.Equal(240, toAdd[2].duration);
                Assert.Equal("me", toAdd[2].lyric);
            } finally {
                File.Delete(diffFile);
            }
        }
    }
}
