using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using Xunit;
using static OpenUtau.Classic.PluginRunner;

namespace OpenUtau.Classic {


    public class PluginRunnerTest {

        class ExecuteTestData : IEnumerable<object[]> {
            private readonly List<object[]> testData = new();

            public ExecuteTestData() {
                testData.Add(new object[] { BasicUProject(), IncludeNullResponse(), IncludeNullAssertion(), EmptyErrorMEthod() });
            }

            public IEnumerator<object[]> GetEnumerator() => testData.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public static ExecuteArgument BasicUProject() {
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

                return new ExecuteArgument(project, part, first, last);
            }

            private static Action<StreamWriter> IncludeNullResponse() {
                return (writer) => {
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
                };
            }

            private static Action<ReplaceNoteEventArgs> IncludeNullAssertion() {
                return (args) => {
                    Assert.Equal(4, args.ToRemove.Count);
                    Assert.Equal(3, args.ToAdd.Count);
                    Assert.Equal(480, args.ToAdd[0].duration);
                    Assert.Equal("A", args.ToAdd[0].lyric);
                    Assert.Equal(40, args.ToAdd[1].duration);
                    Assert.Equal("zo", args.ToAdd[1].lyric);
                    Assert.Equal(240, args.ToAdd[2].duration);
                    Assert.Equal("me", args.ToAdd[2].lyric);
                };
            }

            private static Action<PluginErrorEventArgs> EmptyErrorMEthod() {
                return (args) => {
                    // do nothing
                };
            }
        }

        [Theory]
        [ClassData(typeof(ExecuteTestData))]
        public void ExecuteTest(ExecuteArgument given, Action<StreamWriter> when, Action<ReplaceNoteEventArgs> then, Action<PluginErrorEventArgs> error) {
            // When
            var action = new Action<PluginRunner>((runner) => {
                runner.Execute(given.Project, given.Part, given.First, given.Last, new PluginStub(when));
            });

            // Then (Assert in ClassData)
            action(new PluginRunner(PathManager.Inst, then, error));
        }

        [Fact]
        public void ExecuteErrorTest() {
            // Given
            var given = ExecuteTestData.BasicUProject();

            // When
            var action = new Action<PluginRunner>((runner) => {
                runner.Execute(given.Project, given.Part, given.First, given.Last, new PluginStub((writer) => {
                    // return empty text (invoke error)
                }));
            });

            // Then
            var then = new Action<ReplaceNoteEventArgs>(( args) => {
                Assert.Fail("");
            });
            var error = new Action<PluginErrorEventArgs> ((args) => {
                Assert.True(true);
            });
            action(new PluginRunner(PathManager.Inst, then,error));
        }
    }

     class PluginStub : IPlugin {
        public PluginStub(Action<StreamWriter> action) {
            this.action = action;
        }
        private readonly Action<StreamWriter> action;

        public string Encoding => "shift_jis";

        public void Run(string tempFile) {
            File.Delete(tempFile);
            System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using (var writer = new StreamWriter(tempFile, false, System.Text.Encoding.GetEncoding(Encoding))) {
                action.Invoke(writer);
            }
        }
    }

    public class ExecuteArgument {
        public readonly UProject Project;
        public readonly UVoicePart Part;
        public readonly UNote First;
        public readonly UNote Last;

        public ExecuteArgument(UProject project, UVoicePart part, UNote first, UNote last) {
            Project = project;
            Part = part;
            First = first;
            Last = last;
        }
    }
}
