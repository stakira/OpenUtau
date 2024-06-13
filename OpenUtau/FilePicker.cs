using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace OpenUtau.App {
    internal class FilePicker {
        public static FilePickerFileType ProjectFiles { get; } = new("Project Files") {
            Patterns = new[] { "*.ustx", "*.vsqx", "*.ust", "*.mid", "*.midi", "*.ufdata" },
        };
        public static FilePickerFileType USTX { get; } = new("USTX") {
            Patterns = new[] { "*.ustx" },
        };
        public static FilePickerFileType VSQX { get; } = new("VSQX") {
            Patterns = new[] { "*.vsqx" },
        };
        public static FilePickerFileType UST { get; } = new("UST") {
            Patterns = new[] { "*.ust" },
        };
        public static FilePickerFileType MIDI { get; } = new("MIDI") {
            Patterns = new[] { "*.mid", "*.midi" },
        };
        public static FilePickerFileType UFDATA { get; } = new("UFDATA") {
            Patterns = new[] { "*.ufdata" },
        };
        public static FilePickerFileType AudioFiles { get; } = new("Audio Files") {
            Patterns = new[] { "*.wav", "*.mp3", "*.ogg", "*.opus", "*.flac" },
        };
        public static FilePickerFileType WAV { get; } = new("WAV") {
            Patterns = new[] { "*.wav" },
        };
        public static FilePickerFileType ArchiveFiles { get; } = new("Archive File") {
            Patterns = new[] { "*.zip", "*.rar", "*.uar", "*.vogeon", "*.oudep" },
        };
        public static FilePickerFileType EXE { get; } = new("EXE") {
            Patterns = new[] { "*.exe" },
        };
        public static FilePickerFileType APP { get; } = new("APP") {
            Patterns = new[] { "*.app" },
        };
        public static FilePickerFileType PrefixMap { get; } = new("Prefix Map") {
            Patterns = new[] { "*.map" },
        };
        public static FilePickerFileType DS { get; } = new("DS") {
            Patterns = new[] { "*.ds" },
        };
        public static FilePickerFileType OUDEP { get; } = new("OpenUtau dependency"){
            Patterns = new[] { "*.oudep" },
        };

        public async static Task<string?> OpenFile(
            Window window, string titleKey, params FilePickerFileType[] types) {
            return await OpenFile(window, titleKey, null, types);
        }

        public async static Task<string?> OpenFile(
            Window window, string titleKey, string? startLocation, params FilePickerFileType[] types) {
            var location = startLocation == null
                ? null
                : await window.StorageProvider.TryGetFolderFromPathAsync(startLocation);
            var files = await window.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions() {
                    Title = ThemeManager.GetString(titleKey),
                    AllowMultiple = false,
                    FileTypeFilter = types,
                    SuggestedStartLocation = location,
                });
            return files
                ?.Select(f => f.TryGetLocalPath())
                ?.OfType<string>()
                ?.FirstOrDefault();
        }

        public async static Task<string[]?> OpenFiles(
            Window window, string titleKey, params FilePickerFileType[] types) {
            var files = await window.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions() {
                    Title = ThemeManager.GetString(titleKey),
                    AllowMultiple = true,
                    FileTypeFilter = types
                });
            return files
                ?.Select(f => f.TryGetLocalPath())
                ?.OfType<string>()
                ?.ToArray();
        }

        public async static Task<string?> OpenFolder(Window window, string titleKey) {
            var dirs = await window.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions {
                    Title = ThemeManager.GetString(titleKey),
                    AllowMultiple = false,
                });
            return dirs
                ?.Select(f => f.TryGetLocalPath())
                ?.OfType<string>()
                ?.FirstOrDefault();
        }

        public async static Task<string?> SaveFile
            (Window window, string titleKey, params FilePickerFileType[] types) {
            return await SaveFile(window, titleKey, null, null, types);
        }

        public async static Task<string?> SaveFile
            (Window window, string titleKey, string? startLocation, string? filename, params FilePickerFileType[] types) {
            var location = startLocation == null
                ? null
                : await window.StorageProvider.TryGetFolderFromPathAsync(startLocation);

            var file = await window.StorageProvider.SaveFilePickerAsync(
                 new FilePickerSaveOptions {
                     Title = ThemeManager.GetString(titleKey),
                     FileTypeChoices = types,
                     ShowOverwritePrompt = true,
                     SuggestedStartLocation = location,
                     SuggestedFileName = filename,
                 });
            return file?.TryGetLocalPath();
        }
    }
}
