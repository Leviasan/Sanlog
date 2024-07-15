using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents a writer that can write a logging entry to file storage.
    /// </summary>
    internal sealed partial class FileLoggingWriter : LoggingWriter
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
        private readonly FileLoggingWriterMode _strategy;
        /// <summary>
        /// The character encoding to use.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Encoding _encoding;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLoggingWriter"/> class with the specified configuration.
        /// </summary>
        /// <param name="channel">The channel reader.</param>
        /// <param name="directory">The path to the log directory. The default is the current application directory.</param>
        /// <param name="filePrefix">The prefix of the file name used to store the logging information. The current date in the format YYYYMMDD is added after the specified value. The default is "diagnostics-".</param>
        /// <param name="fileSizeLimit">The maximum log size in bytes. Once the log is full behavior depends on <paramref name="strategy"/>. The default is 10MB.</param>
        /// <param name="fileCountLimit">The maximum retained file count. The default is 2.</param>
        /// <param name="strategy">Specifies the behavior to use when writing to a log file that is already full. The default is <see cref="FileLoggingWriterMode.DropWrite"/>.</param>
        /// <param name="encoding">The character encoding to use. The default is <see cref="Encoding.Unicode"/>.</param>
        /// <exception cref="ArgumentException">The <paramref name="directory"/> is a zero-length string or contains only white space.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="channel"/> or <paramref name="directory"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="fileSizeLimit"/> or <paramref name="fileCountLimit"/> is less then 0 or the <paramref name="strategy"/> is invalid enumeration value.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified <paramref name="directory"/> is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">The <paramref name="directory"/> specified by path is a file. -or- The network name is not known.</exception>
        /// <exception cref="NotSupportedException">The <paramref name="directory"/> path contains a colon character (:) that is not part of a drive label ("C:\").</exception>
        /// <exception cref="PathTooLongException">The specified <paramref name="directory"/> exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        public FileLoggingWriter(string directory = "./", string? filePrefix = "diagnostics-", int fileSizeLimit = 10485760, int fileCountLimit = 2, FileLoggingWriterMode strategy = FileLoggingWriterMode.DropWrite, Encoding? encoding = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(directory);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory); // DirectoryNotFoundException + IOException + NotSupportedException + PathTooLongException + UnauthorizedAccessException
            ArgumentOutOfRangeException.ThrowIfLessThan(fileSizeLimit, 0);
            ArgumentOutOfRangeException.ThrowIfLessThan(fileCountLimit, 0);
            if (!Enum.IsDefined(strategy)) throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);

            _directory = new DirectoryInfo(directory); // SecurityException + PathTooLongException
            _filePrefix = filePrefix;
            _fileSizeLimit = fileSizeLimit;
            _fileCountLimit = fileCountLimit;
            _strategy = strategy;
            _encoding = encoding ?? Encoding.Unicode;
        }

        /// <summary>
        /// Gets the search string to match against the names of files.
        /// </summary>
        public string SearchPattern => $"{_filePrefix}{DateTime.UtcNow:yyyyMMdd}_*.log";

        /// <summary>
        /// Tries to get fileinfo for writing log entry.
        /// </summary>
        /// <param name="fileInfo">The file info about log file.</param>
        /// <returns><see langword="true"/> if operation is successful; otherwise <see langword="false"/>.</returns>
        /// <exception cref="DirectoryNotFoundException">The path is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="FormatException">The number part of the filename is not in the correct format.</exception>
        /// <exception cref="IOException">The target file is open.</exception>
        /// <exception cref="NotSupportedException">The path to the file contains a colon (:) in the middle of the string.</exception>
        /// <exception cref="OverflowException">The number part of the file name represents a number less than 0 or greater than <see cref="int.MinValue"/>.</exception>
        /// <exception cref="PathTooLongException">The path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to file is denied.</exception>
        private bool TryGetFileInfo([NotNullWhen(true)] out FileInfo? fileInfo)
        {
            fileInfo = default;
            var files = _directory.GetFiles(SearchPattern, SearchOption.TopDirectoryOnly); // DirectoryNotFoundException + SecurityException
            if (files.Length == 0)
            {
                if (files.Length >= _fileCountLimit) return false;
                fileInfo = new FileInfo(BuildFileFullName(_directory.FullName, _filePrefix, files.Length)); // SecurityException + UnauthorizedAccessException + PathTooLongException + NotSupportedException
            }
            else if (files[^1].Length >= _fileSizeLimit)
            {
                if (files.Length >= _fileCountLimit)
                {
                    if (_strategy == FileLoggingWriterMode.DropWrite)
                    {
                        return false;
                    }
                    else if (_strategy == FileLoggingWriterMode.DropNewest)
                    {
                        files[^1].Delete(); // IOException
                    }
                    else if (_strategy == FileLoggingWriterMode.DropOldest)
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
                fileInfo = new FileInfo(BuildFileFullName(_directory.FullName, _filePrefix, ++number)); // SecurityException + UnauthorizedAccessException + PathTooLongException + NotSupportedException
            }
            else
            {
                fileInfo = files[^1];
            }
            return true;

            // Summary: Builds full name of the log file.
            // Param (directory): The path to the log directory.
            // Param (prefix): The prefix of the file name used to store the logging information.
            // Param (number): The number of the file.
            // Returns: The path for the file.
            static string BuildFileFullName(string directory, string? prefix, int number) => $"{directory}\\{prefix}{DateTime.UtcNow:yyyyMMdd}_{number}.log";
        }
        /// <inheritdoc/>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system to the log file, such as when access is <see cref="FileAccess.Write"/> and the file is set for read-only access.</exception>
        protected override async Task WriteToStorageAsync(LoggingEntry loggingEntry, CancellationToken cancellationToken)
        {
            if (!TryGetFileInfo(out var fileInfo)) return;
            using var fileStream = new FileStream(fileInfo.FullName, FileMode.Append, FileAccess.Write, FileShare.Read); // SecurityException + UnauthorizedAccessException
            using var textWriter = new StreamWriter(fileStream, _encoding);
            await textWriter.WriteLineAsync(loggingEntry.ToString().AsMemory(), cancellationToken).ConfigureAwait(false);
        }
    }
}