using System.Threading;
using System.Threading.Tasks;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Provides a mechanism for writing log entries to the storage.
    /// </summary>
    public interface ILoggingWriter
    {
        /// <summary>
        /// Writes a log entry to the storage.
        /// </summary>
        /// <param name="item">The value to write to the storage.</param>
        void Write(LoggingEntry item);

        /// <summary>
        /// Writes a log entry to the storage asynchronously.
        /// </summary>
        /// <param name="item">The value to write to the storage.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task WriteAsync(LoggingEntry item, CancellationToken cancellationToken);
    }
}