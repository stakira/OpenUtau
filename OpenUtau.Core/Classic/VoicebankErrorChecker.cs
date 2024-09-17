using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace OpenUtau.Classic {
    public class VoicebankError {
        public string message;
        public FileTrace trace;
        public string soundFile;
        public Exception e;

        public override string ToString() {
            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(message)) {
                builder.AppendLine(message);
            }
            if (trace != null) {
                builder.AppendLine(trace.ToString());
            }
            if (!string.IsNullOrEmpty(soundFile)) {
                builder.AppendLine(soundFile);
            }
            if (e != null) {
                builder.AppendLine(e.ToString());
            }
            return builder.ToString();
        }
    }

    public class VoicebankErrorChecker {
        public List<VoicebankError> Errors = new List<VoicebankError>();
        public List<VoicebankError> Infos = new List<VoicebankError>();

        readonly string path;
        readonly Voicebank voicebank;
        readonly Dictionary<string, double> fileDurations = new Dictionary<string, double>();

        public VoicebankErrorChecker(string path, string basePath) {
            this.path = path;
            string charTxt = Path.Combine(path, VoicebankLoader.kCharTxt);
            voicebank = new Voicebank() {
                File = charTxt,
                BasePath = basePath,
            };
        }

        public void Check() {
            if (!File.Exists(voicebank.File)) {
                Errors.Add(new VoicebankError() {
                    message = "character.txt not found",
                });
                return;
            }
            string charYaml = Path.Combine(path, VoicebankLoader.kCharYaml);
            if (!File.Exists(charYaml)) {
                Infos.Add(new VoicebankError() {
                    message = "character.yaml not found",
                });
            }
            try {
                VoicebankLoader.LoadVoicebank(voicebank);
            } catch (Exception e) {
                Errors.Add(new VoicebankError() {
                    message = "Failed to load voicebank",
                    e = e,
                });
                return;
            }
            foreach (var otoSet in voicebank.OtoSets) {
                string dir = Path.Combine(path, Path.GetDirectoryName(otoSet.File));
                foreach (var oto in otoSet.Otos) {
                    if (!oto.IsValid) {
                        Errors.Add(new VoicebankError() {
                            trace = oto.FileTrace,
                            message = $"Invalid oto format.",
                        });
                        continue;
                    }
                    string filePath = Path.Combine(dir, oto.Wav);
                    if (!TryGetFileDuration(filePath, oto, out double fileDuration)) {
                        continue;
                    }
                    if (fileDuration <= 0) {
                        Errors.Add(new VoicebankError() {
                            trace = oto.FileTrace,
                            soundFile = filePath,
                            message = $"Invalid duration {fileDuration}.",
                        });
                        continue;
                    }
                    CheckOto(oto, fileDuration);
                }
            }
            if (FindDuplication(out List<Oto> duplicates)) {
                string message = "";
                duplicates.ForEach(oto => message += "\n" + oto.FileTrace.file + " : " + oto.Alias);
                Errors.Add(new VoicebankError() {
                    message = $"There are duplicate aliases.{message}"
                });
            }
            foreach(var otoSet in voicebank.OtoSets) {
                CheckCaseMatchForFileReference(otoSet);
                CheckDuplicatedNameIgnoringCase(otoSet);
            }
            CheckCaseMatchForFileReference(voicebank.BasePath, new string[]{
                "chatacter.txt", 
                "character.yaml", 
                "prefix.map",
                });
        }

        bool TryGetFileDuration(string filePath, Oto oto, out double fileDuration) {
            if (fileDurations.TryGetValue(filePath, out fileDuration)) {
                return true;
            }
            if (!File.Exists(filePath)) {
                Errors.Add(new VoicebankError() {
                    trace = oto.FileTrace,
                    soundFile = filePath,
                    message = $"Sound file missing",
                });
                return false;
            }
            try {
                using (var wav = Core.Format.Wave.OpenFile(filePath)) {
                    fileDuration = wav.TotalTime.TotalMilliseconds;
                    var waveFormat = wav.ToSampleProvider().WaveFormat;
                    if (waveFormat.SampleRate != 44100) {
                        Errors.Add(new VoicebankError() {
                            trace = oto.FileTrace,
                            soundFile = filePath,
                            message = $"Sample rate of the sound file is not 44100Hz."
                        });
                    }
                    if (waveFormat.Channels != 1) {
                        Infos.Add(new VoicebankError() {
                            trace = oto.FileTrace,
                            soundFile = filePath,
                            message = $"Sound file is not mono channel."
                        });
                    }
                    /* If sound is not 16bit, it cannot be opened.
                    if (waveFormat.BitsPerSample != 16) {
                        Errors.Add(new VoicebankError() {
                            trace = oto.FileTrace,
                            soundFile = filePath,
                            message = $"Bit rate of the sound file is not 16bit."
                        });
                    }*/
                }
            } catch (Exception e) {
                Errors.Add(new VoicebankError() {
                    trace = oto.FileTrace,
                    soundFile = filePath,
                    message = $"Cannot open sound file.",
                    e = e,
                });
                return false;
            }
            fileDurations.Add(filePath, fileDuration);
            return true;
        }

        bool CheckOto(Oto oto, double fileDuration) {
            bool valid = true;
            if (oto.Offset < 0) {
                Errors.Add(new VoicebankError() {
                    trace = oto.FileTrace,
                    message = "Offset must be >= 0.",
                });
                valid = false;
            }
            if (oto.Offset > fileDuration) {
                Errors.Add(new VoicebankError() {
                    trace = oto.FileTrace,
                    message = $"Offset out of sound file duration {fileDuration}.",
                });
                valid = false;
            }
            if (oto.Preutter < 0) {
                Errors.Add(new VoicebankError() {
                    trace = oto.FileTrace,
                    message = "Preutter must be >= 0.",
                });
                valid = false;
            }
            if (oto.Preutter + oto.Offset > fileDuration) {
                Errors.Add(new VoicebankError() {
                    trace = oto.FileTrace,
                    message = $"Preutter out of sound file duration {fileDuration}.",
                });
                valid = false;
            }
            if (oto.Consonant < 0) {
                Errors.Add(new VoicebankError() {
                    trace = oto.FileTrace,
                    message = "Consonant must be >= 0.",
                });
                valid = false;
            }
            if (oto.Consonant + oto.Offset > fileDuration) {
                Errors.Add(new VoicebankError() {
                    trace = oto.FileTrace,
                    message = $"Consonant out of sound file duration {fileDuration}.",
                });
                valid = false;
            }
            if (oto.Overlap + oto.Offset > fileDuration) {
                Errors.Add(new VoicebankError() {
                    trace = oto.FileTrace,
                    message = $"Overlap out of sound file duration {fileDuration}.",
                });
                valid = false;
            }
            double cutoff = oto.Cutoff < 0 ? oto.Offset - oto.Cutoff : fileDuration - oto.Cutoff;
            if (cutoff < oto.Offset + oto.Preutter) {
                Errors.Add(new VoicebankError() {
                    trace = oto.FileTrace,
                    message = $"Cutoff must be to the right of preutter.",
                });
                valid = false;
            }
            if (cutoff < oto.Offset + oto.Overlap) {
                Errors.Add(new VoicebankError() {
                    trace = oto.FileTrace,
                    message = $"Cutoff must be to the right of overlap.",
                });
                valid = false;
            }
            if (cutoff <= oto.Offset + oto.Consonant) {
                Errors.Add(new VoicebankError() {
                    trace = oto.FileTrace,
                    message = $"Cutoff must be to the right of consonant.",
                });
                valid = false;
            }
            return valid;
        }

        bool FindDuplication(out List<Oto> duplicates) {
            duplicates = voicebank.OtoSets
                .SelectMany(set => set.Otos)
                .GroupBy(oto => oto.Alias)
                .Where(alias => alias.Count() > 1)
                .SelectMany(group => group).ToList();

            return duplicates.Count > 0;
        }
    
        /// <summary>
        /// Check if the file names in the oto.ini are the same as the file names in the file system.
        /// </summary>
        /// <param name="otoSet">otoSet to be checked</param>
        /// <returns></returns>
        bool CheckCaseMatchForFileReference(OtoSet otoSet) {
            return CheckCaseMatchForFileReference(
                Directory.GetParent(otoSet.File).FullName, 
                otoSet.Otos
                    .Select(oto => oto.Wav)
                    .Append(otoSet.File)//oto.ini itself
                    .ToHashSet());
        }

        bool CheckCaseMatchForFileReference(string folder, IEnumerable<string> correctFileNames){
            bool valid = true;
            Dictionary<string, string> fileNamesLowerToActual = Directory.GetFiles(folder)
                .Select(Path.GetFileName)
                .ToDictionary(x => x.ToLower(), x => x);
            foreach(string fileName in correctFileNames) {
                if(!fileNamesLowerToActual.ContainsKey(fileName.ToLower())) {
                    continue;
                }
                if (fileNamesLowerToActual[fileName.ToLower()] != fileName) {
                    valid = false;
                    Errors.Add(new VoicebankError() {
                        message = $"Wrong case in file name: \n"
                            + $"expected: {Path.Join(folder,fileName)}\n"
                            + $"Actual: {Path.Join(folder,fileNamesLowerToActual[fileName.ToLower()])}\n"
                            + $"The voicebank may not work on another OS."
                    });
                }
            }
            return valid;
        }

        /// <summary>
        /// Check if the file names are duplicated when converted to lower case.
        /// </summary>
        /// <param name="otoSet">otoSet to be checked</param>
        /// <returns></returns>
        bool CheckDuplicatedNameIgnoringCase(OtoSet otoSet) {
            var wavNames = otoSet.Otos.Select(x => x.Wav).Distinct().ToList();
            var duplicatedGroups = wavNames.GroupBy(x => x.ToLower())
                .Where(group => group.Count() > 1)
                .ToList();
            foreach (var group in duplicatedGroups) {
                Errors.Add(new VoicebankError() {
                    message = $"Duplicated file names found when ignoreing case in oto set \"{otoSet.Name}\":"
                    + string.Join(", ", group.Select(x => $"\"{x}\""))
                    + ".\n"
                    + "The voicebank may not work on another OS with case-sensitivity."
                });
            }
            return duplicatedGroups.Count == 0;
        }
    }
}
