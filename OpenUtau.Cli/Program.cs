using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using NAudio.Wave;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Api;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.SignalChain;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using OpenUtau.Core.DiffSinger;

namespace OpenUtau.Cli {
    class Program {
        static int Main(string[] args) {
            var root = new RootCommand("OpenUtau CLI: phonemize and DiffSinger render");

            var cmd = new Command("render", "Phonemize and render a .ustx using DiffSinger voicebank");
            cmd.AddOption(new Option<FileInfo>(new[] { "--project", "-p" }, "Input USTX project file") { IsRequired = true });
            cmd.AddOption(new Option<int>(new[] { "--track", "-t" }, () => 0, "Track index to render"));
            cmd.AddOption(new Option<string>(new[] { "--lyrics", "-l" }, "Space-separated lyrics (override .ustx lyrics)"));
            cmd.AddOption(new Option<FileInfo>(new[] { "--phonemizer-model", "-m" }, "(Optional) Enunu phonemizer ONNX model file"));
            cmd.AddOption(new Option<DirectoryInfo>(new[] { "--voicebank", "-v" }, "DiffSinger voicebank folder") { IsRequired = true });
            cmd.AddOption(new Option<FileInfo>(new[] { "--out-timings", "-o" }, "Output phoneme timings JSON file") { IsRequired = true });
            cmd.AddOption(new Option<FileInfo>(new[] { "--out-wav", "-w" }, "Output WAV file") { IsRequired = true });
            cmd.Handler = CommandHandler.Create<FileInfo, int, string, FileInfo, DirectoryInfo, FileInfo, FileInfo>(RunRender);
            root.AddCommand(cmd);

            return root.InvokeAsync(args).Result;
        }

        static void RunRender(FileInfo project, int track, string lyrics, FileInfo phonemizerModel, DirectoryInfo voicebank, FileInfo outTimings, FileInfo outWav) {
            // 1) Load project
            Console.WriteLine($"Loading project '{project}'...");
            var docs = Formats.LoadProject(new[] { project.FullName });
            if (!docs.Any()) {
                Console.Error.WriteLine("Failed to load project.");
                Environment.Exit(1);
            }
            var proj = docs.First();

            // 2) Locate track and part
            if (track < 0 || track >= proj.Tracks.Count) {
                Console.Error.WriteLine($"Track index {track} is out of range.");
                Environment.Exit(1);
            }
            var utrack = proj.Tracks[track];
            var part = utrack.VoiceParts.FirstOrDefault();
            if (part == null) {
                Console.Error.WriteLine($"No voice part found on track {track}.");
                Environment.Exit(1);
            }

            // 3) Override lyrics if specified
            if (!string.IsNullOrEmpty(lyrics)) {
                var tokens = lyrics.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var notes = part.Notes;
                if (tokens.Length != notes.Count) {
                    Console.Error.WriteLine($"Lyrics count ({tokens.Length}) does not match note count ({notes.Count}).");
                    Environment.Exit(1);
                }
                for (int i = 0; i < notes.Count; i++) {
                    notes[i].lyric = tokens[i];
                }
                part.Notes = notes;
            }

            // 4) Load DiffSinger voicebank
            Preferences.Default.LoadDeepFolderSinger = true;
            var loader = new VoicebankLoader(voicebank.FullName);
            var vbs = loader.SearchAll().ToList();
            if (!vbs.Any()) {
                Console.Error.WriteLine($"No voicebank found under '{voicebank}'.");
                Environment.Exit(1);
            }
            var vb = vbs.First();
            VoicebankLoader.LoadVoicebank(vb);
            var singer = new DiffSingerSinger(vb);
            utrack.Singer = singer;

            // 5) Setup phonemizer from track
            var phonemizer = utrack.Phonemizer;
            phonemizer.SetSinger(utrack.Singer);
            phonemizer.SetTiming(proj.timeAxis);

            // 6) Phonemize note groups
            Console.WriteLine("Phonemizing...");
            var noteGroups = part.GetNoteClusters().Select(c => c.Notes).ToList();
            var phonemes = noteGroups.SelectMany(group => {
                var result = phonemizer.Process(group.ToArray(), null, null, null, null, Array.Empty<Phonemizer.Note>());
                return result.phonemes;
            }).ToArray();

            // 7) Write timings JSON
            File.WriteAllText(outTimings.FullName, JsonSerializer.Serialize(phonemes, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Timings written to '{outTimings}'.");

            // 8) Build RenderPhrase
            Console.WriteLine("Building render phrase...");
            var renderPhrase = new RenderPhrase(proj, utrack, part, part.Phonemes.ToList());

            // 9) Render with DiffSinger
            Console.WriteLine("Rendering with DiffSinger...");
            var renderer = new DiffSingerRenderer();
            var result = renderer.Render(renderPhrase, new Progress(1), 0, new CancellationTokenSource(), false).Result;
            var samples = result.samples;
            if (samples == null || samples.Length == 0) {
                Console.Error.WriteLine("Render failed or returned no samples.");
                Environment.Exit(1);
            }

            // 10) Write WAV (16-bit mono)
            Console.WriteLine($"Writing WAV to '{outWav}'...");
            Directory.CreateDirectory(Path.GetDirectoryName(outWav.FullName));
            using var writer = new WaveFileWriter(outWav.FullName, new WaveFormat((int)singer.dsConfig.sample_rate, 1));
            foreach (var sample in samples) {
                writer.WriteSample(sample);
            }
            Console.WriteLine("Done.");
        }
    }
}