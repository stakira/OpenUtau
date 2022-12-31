using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
                Errors.Add(new VoicebankError() {
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
    }
}
