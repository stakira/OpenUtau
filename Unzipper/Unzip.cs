// Unzip class for .NET 3.5 Client Profile or Mono 2.10
// Written by Alexey Yakovlev <yallie@yandex.ru>
// https://github.com/yallie/unzip

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Unzipper {
    /// <summary>
    /// Unzip helper class.
    /// </summary>
    internal class Unzip : IDisposable {
        /// <summary>
        /// Zip archive entry.
        /// </summary>
        public class Entry {
            /// <summary>
            /// Gets or sets the name of a file or a directory.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the comment.
            /// </summary>
            public string Comment { get; set; }

            /// <summary>
            /// Gets or sets the CRC32.
            /// </summary>
            public uint Crc32 { get; set; }

            /// <summary>
            /// Gets or sets the compressed size of the file.
            /// </summary>
            public int CompressedSize { get; set; }

            /// <summary>
            /// Gets or sets the original size of the file.
            /// </summary>
            public int OriginalSize { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether this <see cref="Entry" /> is deflated.
            /// </summary>
            public bool Deflated { get; set; }

            /// <summary>
            /// Gets a value indicating whether this <see cref="Entry" /> is a directory.
            /// </summary>
            public bool IsDirectory { get { return Name.EndsWith("/"); } }

            /// <summary>
            /// Gets or sets the timestamp.
            /// </summary>
            public DateTime Timestamp { get; set; }

            /// <summary>
            /// Gets a value indicating whether this <see cref="Entry" /> is a file.
            /// </summary>
            public bool IsFile { get { return !IsDirectory; } }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public int HeaderOffset { get; set; }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public int DataOffset { get; set; }
        }

        /// <summary>
        /// CRC32 calculation helper.
        /// </summary>
        public class Crc32Calculator {
            private static readonly uint[] Crc32Table =
            {
                0x00000000, 0x77073096, 0xee0e612c, 0x990951ba, 0x076dc419, 0x706af48f, 0xe963a535, 0x9e6495a3,
                0x0edb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988, 0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91,
                0x1db71064, 0x6ab020f2, 0xf3b97148, 0x84be41de, 0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,
                0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec, 0x14015c4f, 0x63066cd9, 0xfa0f3d63, 0x8d080df5,
                0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172, 0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b,
                0x35b5a8fa, 0x42b2986c, 0xdbbbc9d6, 0xacbcf940, 0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,
                0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116, 0x21b4f4b5, 0x56b3c423, 0xcfba9599, 0xb8bda50f,
                0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924, 0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d,
                0x76dc4190, 0x01db7106, 0x98d220bc, 0xefd5102a, 0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,
                0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818, 0x7f6a0dbb, 0x086d3d2d, 0x91646c97, 0xe6635c01,
                0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e, 0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457,
                0x65b0d9c6, 0x12b7e950, 0x8bbeb8ea, 0xfcb9887c, 0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,
                0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2, 0x4adfa541, 0x3dd895d7, 0xa4d1c46d, 0xd3d6f4fb,
                0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0, 0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9,
                0x5005713c, 0x270241aa, 0xbe0b1010, 0xc90c2086, 0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
                0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4, 0x59b33d17, 0x2eb40d81, 0xb7bd5c3b, 0xc0ba6cad,
                0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a, 0xead54739, 0x9dd277af, 0x04db2615, 0x73dc1683,
                0xe3630b12, 0x94643b84, 0x0d6d6a3e, 0x7a6a5aa8, 0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,
                0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe, 0xf762575d, 0x806567cb, 0x196c3671, 0x6e6b06e7,
                0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc, 0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5,
                0xd6d6a3e8, 0xa1d1937e, 0x38d8c2c4, 0x4fdff252, 0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,
                0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60, 0xdf60efc3, 0xa867df55, 0x316e8eef, 0x4669be79,
                0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236, 0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f,
                0xc5ba3bbe, 0xb2bd0b28, 0x2bb45a92, 0x5cb36a04, 0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,
                0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a, 0x9c0906a9, 0xeb0e363f, 0x72076785, 0x05005713,
                0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38, 0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21,
                0x86d3d2d4, 0xf1d4e242, 0x68ddb3f8, 0x1fda836e, 0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,
                0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c, 0x8f659eff, 0xf862ae69, 0x616bffd3, 0x166ccf45,
                0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2, 0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db,
                0xaed16a4a, 0xd9d65adc, 0x40df0b66, 0x37d83bf0, 0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
                0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6, 0xbad03605, 0xcdd70693, 0x54de5729, 0x23d967bf,
                0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94, 0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d,
            };

            private uint crcValue = 0xffffffff;

            public uint Crc32 { get { return crcValue ^ 0xffffffff; } }

            public void UpdateWithBlock(byte[] buffer, int numberOfBytes) {
                for (var i = 0; i < numberOfBytes; i++) {
                    crcValue = (crcValue >> 8) ^ Crc32Table[buffer[i] ^ crcValue & 0xff];
                }
            }
        }

        /// <summary>
        /// Provides data for the ExtractProgress event.
        /// </summary>
        public class FileProgressEventArgs : ProgressChangedEventArgs {
            /// <summary>
            /// Initializes a new instance of the <see cref="FileProgressEventArgs"/> class.
            /// </summary>
            /// <param name="currentFile">The current file.</param>
            /// <param name="totalFiles">The total files.</param>
            /// <param name="fileName">Name of the file.</param>
            public FileProgressEventArgs(int currentFile, int totalFiles, string fileName)
                : base(totalFiles != 0 ? currentFile * 100 / totalFiles : 100, fileName) {
                CurrentFile = currentFile;
                TotalFiles = totalFiles;
                FileName = fileName;
            }

            /// <summary>
            /// Gets the current file.
            /// </summary>
            public int CurrentFile { get; private set; }

            /// <summary>
            /// Gets the total files.
            /// </summary>
            public int TotalFiles { get; private set; }

            /// <summary>
            /// Gets the name of the file.
            /// </summary>
            public string FileName { get; private set; }
        }

        private const int EntrySignature = 0x02014B50;
        private const int FileSignature = 0x04034b50;
        private const int DirectorySignature = 0x06054B50;
        private const int BufferSize = 16 * 1024;

        /// <summary>
        /// Occurs when a file or a directory is extracted from an archive.
        /// </summary>
        public event EventHandler<FileProgressEventArgs> ExtractProgress;

        /// <summary>
        /// Initializes a new instance of the <see cref="Unzip" /> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public Unzip(string fileName)
            : this(File.OpenRead(fileName)) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Unzip" /> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public Unzip(Stream stream) {
            Stream = stream;
            Reader = new BinaryReader(Stream);
        }

        private Stream Stream { get; set; }

        private BinaryReader Reader { get; set; }

        /// <summary>
        /// Performs application-defined tasks associated with
        /// freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            if (Stream != null) {
                Stream.Dispose();
                Stream = null;
            }

            if (Reader != null) {
                Reader.Close();
                Reader = null;
            }
        }

        /// <summary>
        /// Extracts the contents of the zip file to the given directory.
        /// </summary>
        /// <param name="directoryName">Name of the directory.</param>
        public void ExtractToDirectory(string directoryName) {
            for (int index = 0; index < Entries.Length; index++) {
                var entry = Entries[index];

                // create target directory for the file
                var fileName = Path.Combine(directoryName, entry.Name);
                var dirName = Path.GetDirectoryName(fileName);
                Directory.CreateDirectory(dirName);

                // save file if it is not only a directory
                if (!entry.IsDirectory) {
                    Extract(entry.Name, fileName);
                }

                var extractProgress = ExtractProgress;
                if (extractProgress != null) {
                    extractProgress(this, new FileProgressEventArgs(index + 1, Entries.Length, entry.Name));
                }
            }
        }

        /// <summary>
        /// Extracts the specified file to the specified name.
        /// </summary>
        /// <param name="fileName">Name of the file in zip archive.</param>
        /// <param name="outputFileName">Name of the output file.</param>
        public void Extract(string fileName, string outputFileName) {
            var entry = GetEntry(fileName);

            using (var outStream = File.Create(outputFileName)) {
                Extract(entry, outStream);
            }

            var fileInfo = new FileInfo(outputFileName);
            if (fileInfo.Length != entry.OriginalSize) {
                throw new InvalidDataException(string.Format(
                    "Corrupted archive: {0} has an uncompressed size {1} which does not match its expected size {2}",
                    outputFileName, fileInfo.Length, entry.OriginalSize));
            }

            File.SetLastWriteTime(outputFileName, entry.Timestamp);
        }

        private Entry GetEntry(string fileName) {
            fileName = fileName.Replace("\\", "/").Trim().TrimStart('/');
            var entry = Entries.FirstOrDefault(e => e.Name == fileName);

            if (entry == null) {
                throw new FileNotFoundException("File not found in the archive: " + fileName);
            }

            return entry;
        }

        /// <summary>
        /// Extracts the specified file to the output <see cref="Stream"/>.
        /// </summary>
        /// <param name="fileName">Name of the file in zip archive.</param>
        /// <param name="outputStream">The output stream.</param>
        public void Extract(string fileName, Stream outputStream) {
            Extract(GetEntry(fileName), outputStream);
        }

        /// <summary>
        /// Extracts the specified entry.
        /// </summary>
        /// <param name="entry">Zip file entry to extract.</param>
        /// <param name="outputStream">The stream to write the data to.</param>
        /// <exception cref="System.InvalidOperationException"> is thrown when the file header signature doesn't match.</exception>
        public void Extract(Entry entry, Stream outputStream) {
            // check file signature
            Stream.Seek(entry.HeaderOffset, SeekOrigin.Begin);
            if (Reader.ReadInt32() != FileSignature) {
                throw new InvalidDataException("File signature doesn't match.");
            }

            // move to file data
            Stream.Seek(entry.DataOffset, SeekOrigin.Begin);
            var inputStream = Stream;
            if (entry.Deflated) {
                inputStream = new DeflateStream(Stream, CompressionMode.Decompress, true);
            }

            // allocate buffer, prepare for CRC32 calculation
            var count = entry.OriginalSize;
            var bufferSize = Math.Min(BufferSize, entry.OriginalSize);
            var buffer = new byte[bufferSize];
            var crc32Calculator = new Crc32Calculator();

            while (count > 0) {
                // decompress data
                var read = inputStream.Read(buffer, 0, bufferSize);
                if (read == 0) {
                    break;
                }

                crc32Calculator.UpdateWithBlock(buffer, read);

                // copy to the output stream
                outputStream.Write(buffer, 0, read);
                count -= read;
            }

            if (crc32Calculator.Crc32 != entry.Crc32) {
                throw new InvalidDataException(string.Format(
                    "Corrupted archive: CRC32 doesn't match on file {0}: expected {1:x8}, got {2:x8}.",
                    entry.Name, entry.Crc32, crc32Calculator.Crc32));
            }
        }

        /// <summary>
        /// Gets the file names.
        /// </summary>
        public IEnumerable<string> FileNames {
            get {
                return Entries.Select(e => e.Name).Where(f => !f.EndsWith("/")).OrderBy(f => f);
            }
        }

        private Entry[] entries;

        /// <summary>
        /// Gets zip file entries.
        /// </summary>
        public Entry[] Entries {
            get {
                if (entries == null) {
                    entries = ReadZipEntries().ToArray();
                }

                return entries;
            }
        }

        private IEnumerable<Entry> ReadZipEntries() {
            if (Stream.Length < 22) {
                yield break;
            }

            Stream.Seek(-22, SeekOrigin.End);

            // find directory signature
            while (Reader.ReadInt32() != DirectorySignature) {
                if (Stream.Position <= 5) {
                    yield break;
                }

                // move 1 byte back
                Stream.Seek(-5, SeekOrigin.Current);
            }

            // read directory properties
            Stream.Seek(6, SeekOrigin.Current);
            var entries = Reader.ReadUInt16();
            var difSize = Reader.ReadInt32();
            var dirOffset = Reader.ReadUInt32();
            Stream.Seek(dirOffset, SeekOrigin.Begin);

            // read directory entries
            for (int i = 0; i < entries; i++) {
                if (Reader.ReadInt32() != EntrySignature) {
                    continue;
                }

                // read file properties
                // TODO: Replace with a proper class to make this method a lot shorter.
                Reader.ReadInt32();
                bool utf8 = (Reader.ReadInt16() & 0x0800) != 0;
                short method = Reader.ReadInt16();
                int timestamp = Reader.ReadInt32();
                uint crc32 = Reader.ReadUInt32();
                int compressedSize = Reader.ReadInt32();
                int fileSize = Reader.ReadInt32();
                short fileNameSize = Reader.ReadInt16();
                short extraSize = Reader.ReadInt16();
                short commentSize = Reader.ReadInt16();
                int headerOffset = Reader.ReadInt32();
                Reader.ReadInt32();
                int fileHeaderOffset = Reader.ReadInt32();
                var fileNameBytes = Reader.ReadBytes(fileNameSize);
                Stream.Seek(extraSize, SeekOrigin.Current);
                var fileCommentBytes = Reader.ReadBytes(commentSize);
                var fileDataOffset = CalculateFileDataOffset(fileHeaderOffset);

                // decode zip file entry
                var encoder = utf8 ? Encoding.UTF8 : Encoding.Default;
                yield return new Entry {
                    Name = encoder.GetString(fileNameBytes),
                    Comment = encoder.GetString(fileCommentBytes),
                    Crc32 = crc32,
                    CompressedSize = compressedSize,
                    OriginalSize = fileSize,
                    HeaderOffset = fileHeaderOffset,
                    DataOffset = fileDataOffset,
                    Deflated = method == 8,
                    Timestamp = ConvertToDateTime(timestamp)
                };
            }
        }

        private int CalculateFileDataOffset(int fileHeaderOffset) {
            var position = Stream.Position;
            Stream.Seek(fileHeaderOffset + 26, SeekOrigin.Begin);
            var fileNameSize = Reader.ReadInt16();
            var extraSize = Reader.ReadInt16();

            var fileOffset = (int)Stream.Position + fileNameSize + extraSize;
            Stream.Seek(position, SeekOrigin.Begin);
            return fileOffset;
        }

        /// <summary>
        /// Converts DOS timestamp to a <see cref="DateTime"/> instance.
        /// </summary>
        /// <param name="dosTimestamp">The dos timestamp.</param>
        /// <returns>The <see cref="DateTime"/> instance.</returns>
        public static DateTime ConvertToDateTime(int dosTimestamp) {
            return new DateTime((dosTimestamp >> 25) + 1980, (dosTimestamp >> 21) & 15, (dosTimestamp >> 16) & 31,
                (dosTimestamp >> 11) & 31, (dosTimestamp >> 5) & 63, (dosTimestamp & 31) * 2);
        }
    }
}
