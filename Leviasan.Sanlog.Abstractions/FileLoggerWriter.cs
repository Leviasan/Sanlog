using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents a writer that can write a logging entry to file storage.
    /// </summary>
    public sealed partial class FileLoggerWriter : SanlogLoggerWriter
    {
        [GeneratedRegex("^(?<prefix>.*)(?<datetime>\\d{8})_(?<number>-?\\d{1,}).log$")]
        private static partial Regex RegexLogFileName();

        /// <summary>
        /// The path to the log directory.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DirectoryInfo _directory;
        /// <summary>
        /// The prefix of the file name used to store the logging information. The current date in the format YYYYMMDD is added after the specified value.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _filePrefix;
        /// <summary>
        /// The maximum log size in bytes. Once the log is full, no more messages are appended.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly int _fileSizeLimit;
        /// <summary>
        /// The maximum retained file count.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly int _fileCountLimit;
        /// <summary>
        /// Specifies the behavior to use when writing to a log file that is already full.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FileLoggerWriterMode _strategy;
        /// <summary>
        /// The encoding in which the output is written.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Encoding _encoding;
        /// <summary>
        /// The writer for writing characters to a stream in a particular encoding.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private StreamWriter? _streamWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLoggerWriter"/> class with the specified configuration.
        /// </summary>
        /// <param name="directory">The path to the log directory. The default is the current application directory.</param>
        /// <param name="filePrefix">The prefix of the file name used to store the logging information. The current date in the format YYYYMMDD is added after the specified value. The default is "diagnostics-".</param>
        /// <param name="fileSizeLimit">The maximum log size in bytes. Once the log is full behavior depends on <paramref name="strategy"/>. The default is 10MB.</param>
        /// <param name="fileCountLimit">The maximum retained file count. The default is 2.</param>
        /// <param name="strategy">Specifies the behavior to use when writing to a log file that is already full. The default is <see cref="FileLoggerWriterMode.DropWrite"/>.</param>
        /// <param name="encoding">The encoding in which the output is written. The default is <see cref="Encoding.UTF8"/>.</param>
        /// <param name="allowSynchronousContinuations"><see langword="true"/> if operations performed on a channel may synchronously invoke continuations subscribed to notifications of pending async operations;
        /// <see langword="false"/> if all continuations should be invoked asynchronously.</param>
        /// <exception cref="ArgumentException">The <paramref name="directory"/> is a zero-length string or contains only white space.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="directory"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="fileSizeLimit"/> or <paramref name="fileCountLimit"/> is less then 0 or the <paramref name="strategy"/> is invalid enumeration value.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified <paramref name="directory"/> is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">The <paramref name="directory"/> specified by path is a file. -or- The network name is not known.</exception>
        /// <exception cref="NotSupportedException">The <paramref name="directory"/> path contains a colon character (:) that is not part of a drive label ("C:\").</exception>
        /// <exception cref="PathTooLongException">The specified <paramref name="directory"/> exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public FileLoggerWriter(string directory = "./", string? filePrefix = "diagnostics-", int fileSizeLimit = 10485760, int fileCountLimit = 2, FileLoggerWriterMode strategy = FileLoggerWriterMode.DropWrite,
            Encoding? encoding = null, bool allowSynchronousContinuations = false) : base(allowSynchronousContinuations)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(directory);
            ArgumentOutOfRangeException.ThrowIfLessThan(fileSizeLimit, 0);
            ArgumentOutOfRangeException.ThrowIfLessThan(fileCountLimit, 0);
            if (!Enum.IsDefined(strategy)) throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);

            _directory = Directory.Exists(directory) ? new DirectoryInfo(directory) // SecurityException + PathTooLongException
                : Directory.CreateDirectory(directory); // DirectoryNotFoundException + IOException + NotSupportedException + PathTooLongException + UnauthorizedAccessException
            _filePrefix = filePrefix;
            _fileSizeLimit = fileSizeLimit;
            _fileCountLimit = fileCountLimit;
            _strategy = strategy;
            _encoding = encoding ?? Encoding.UTF8;
        }

        /// <summary>
        /// Gets the encoding in which the output is written.
        /// </summary>
        public Encoding Encoding => _encoding;
        /// <summary>
        /// Gets the search string to match against the names of files.
        /// </summary>
        public string SearchPattern => $"{_filePrefix}{DateTime.UtcNow:yyyyMMdd}_*.log";

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _streamWriter?.Dispose();
        }
        /// <inheritdoc/>
        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync().ConfigureAwait(false);
            if (_streamWriter is not null)
                await _streamWriter.DisposeAsync().ConfigureAwait(false);
        }
        /// <inheritdoc/>
        protected override async Task WriteToStorageAsync(LoggingEntry loggingEntry)
        {
            if (InitStreamWriter(ref _streamWriter))
            {
                using var memoryStream = new MemoryStream();
                await JsonSerializer.SerializeAsync(memoryStream, loggingEntry, SourceGenerationContext.Default.LoggingEntry).ConfigureAwait(false);
                using var streamReader = new StreamReader(memoryStream);
                memoryStream.Position = 0;
                var json = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                await _streamWriter.WriteLineAsync(json).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Prepares a <see cref="StreamWriter"/> instance for writing data.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException">The path is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">Path includes an incorrect or invalid syntax for file name, directory name, or volume label syntax. -or- The target file is open.</exception>
        /// <exception cref="FormatException">The number part of the filename is not in the correct format.</exception>
        /// <exception cref="OverflowException">The number part of the file name represents a number less than 0 or greater than <see cref="int.MinValue"/>.</exception>
        /// <exception cref="PathTooLongException">The path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">Access is denied.</exception>
        private bool InitStreamWriter([NotNullWhen(true)] ref StreamWriter? streamWriter)
        {
            if (streamWriter is not null)
            {
                if (streamWriter.BaseStream.Length < _fileSizeLimit) return true;
                streamWriter.Dispose();
            }
            var files = _directory.GetFiles(SearchPattern, SearchOption.TopDirectoryOnly); // DirectoryNotFoundException + SecurityException
            if (files.Length == 0)
            {
                if (files.Length >= _fileCountLimit) return false;
                streamWriter = CreateStreamWriter(BuildFileFullName(_directory.FullName, _filePrefix, files.Length), _encoding); // UnauthorizedAccessException + DirectoryNotFoundException + IOException + PathTooLongException + SecurityException
            }
            else if (files[^1].Length >= _fileSizeLimit)
            {
                if (files.Length >= _fileCountLimit)
                {
                    if (_strategy == FileLoggerWriterMode.DropNewest)
                    {
                        files[^1].Delete(); // IOException
                    }
                    else if (_strategy == FileLoggerWriterMode.DropOldest)
                    {
                        files[0].Delete(); // IOException
                    }
                    else
                    {
                        return false;
                    }
                }
                var match = RegexLogFileName().Match(files[^1].Name);
                var number = int.Parse(match.Groups["number"].Value, CultureInfo.InvariantCulture); // FormatException + OverflowException
                if (int.IsNegative(number) || number == int.MaxValue) return false;
                streamWriter = CreateStreamWriter(BuildFileFullName(_directory.FullName, _filePrefix, ++number), _encoding); // UnauthorizedAccessException + DirectoryNotFoundException + IOException + PathTooLongException + SecurityException
            }
            else
            {
                streamWriter = CreateStreamWriter(files[^1].FullName, _encoding); // UnauthorizedAccessException + DirectoryNotFoundException + IOException + PathTooLongException + SecurityException
            }
            return true;

            // Summary: Builds path to the log file.
            // Param (directory): The path to the log directory.
            // Param (prefix): The file name prefix used to store the logging information.
            // Param (number): The number of the file.
            // Returns: The path for the log file.
            static string BuildFileFullName(string directory, string? prefix, int number) => $"{directory}\\{prefix}{DateTime.UtcNow:yyyyMMdd}_{number}.log";
            // Summary: Creates a new instance of the StreamWriter class.
            // Param (filename): The path to the log file.
            // Param (encoding): The encoding in which the output is written.
            // Returns: A new instance of the StreamWriter class.
            // Exception (UnauthorizedAccessException): Access is denied.
            // Exception (DirectoryNotFoundException): The path is invalid (for example, it is on an unmapped drive).
            // Exception (IOException): The path includes an incorrect or invalid syntax for file name, directory name, or volume label syntax.
            // Exception (PathTooLongException): The path, file name, or both exceed the system-defined maximum length.
            // Exception (SecurityException): The caller does not have the required permission.
            static StreamWriter CreateStreamWriter(string filename, Encoding encoding) => new(filename, true, encoding) { AutoFlush = true };
        }
    }
}