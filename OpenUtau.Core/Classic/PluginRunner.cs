using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Classic {
    public class PluginRunner {
        public event EventHandler<ReplaceNoteEventArgs>? OnReplaceNote;
        public event EventHandler<PluginErrorEventArgs>? OnError;
        private readonly PathManager manager;

        public PluginRunner(PathManager manager) {
            this.manager = manager;
        }

        public void Execute(UProject project, UVoicePart part, UNote? first, UNote? last, IPlugin plugin) {
            if (first == null || last == null) {
                return;
            }
            try {
                var tempFile = Path.Combine(manager.CachePath, "temp.tmp");
                var sequence = Ust.WritePlugin(project, part, first, last, tempFile, encoding: plugin.Encoding);
                byte[]? beforeHash = HashFile(tempFile);
                plugin.Run(tempFile);
                byte[]? afterHash = HashFile(tempFile);
                if (beforeHash == null || afterHash == null || Enumerable.SequenceEqual(beforeHash, afterHash)) {
                    Log.Information("Legacy plugin temp file has not changed.");
                    return;
                }
                Log.Information("Legacy plugin temp file has changed.");
                var (toRemove, toAdd) = Ust.ParsePlugin(project, part, first, last, sequence, tempFile, encoding: plugin.Encoding);
                OnReplaceNote?.Invoke(this, new ReplaceNoteEventArgs(toRemove, toAdd));
            } catch (Exception e) {
                OnError?.Invoke(this, new PluginErrorEventArgs("Failed to execute plugin", e));
            }
        }

        private byte[]? HashFile(string filePath) {
            using (var md5 = MD5.Create()) {
                using (var stream = File.OpenRead(filePath)) {
                    return md5.ComputeHash(stream);
                }
            }
        }

        public class ReplaceNoteEventArgs : EventArgs {
            public readonly List<UNote> ToRemove;
            public readonly List<UNote> ToAdd;

            public ReplaceNoteEventArgs(List<UNote> toRemove, List<UNote> toAdd) {
                ToRemove = toRemove;
                ToAdd = toAdd;
            }
        }

        public class PluginErrorEventArgs : EventArgs {
            public readonly string Message;
            public readonly Exception Exception;

            public PluginErrorEventArgs(string message, Exception exception) {
                Exception = exception;
                Message = message;
            }
        }
    }
}
