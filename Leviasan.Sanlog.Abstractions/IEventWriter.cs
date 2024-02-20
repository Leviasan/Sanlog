using System.Threading;
using System.Threading.Tasks;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Provides a mechanism for writing log entries to the storage.
    /// </summary>
    public interface IEventWriter
    {
        /// <summary>
        /// Asynchronously writes a log entry to the storage.
        /// </summary>
        /// <param name="item">The value to write to the storage.</param>
        /// <param name="cancellationToken">Used to cancel the write operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous write operation.</returns>
        ValueTask WriteAsync(LoggingEntry item, CancellationToken cancellationToken = default);
    }
}