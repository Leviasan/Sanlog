using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// Represents the unbounded channel that supports reading and writing elements of type <see cref="LoggingEntry"/> with a single consumer queue.
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is registered in an inversion of control container as part of the dependency injection pattern")]
    internal sealed class UnboundedSingleConsumerQueue : Channel<LoggingEntry>, ILoggingWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnboundedSingleConsumerQueue"/> class.
        /// </summary>
        public UnboundedSingleConsumerQueue()
        {
            var channel = Channel.CreateUnbounded<LoggingEntry>(new UnboundedChannelOptions { SingleReader = true });
            Reader = channel;
            Writer = channel;
        }

        /// <inheritdoc/>
        public void Write(LoggingEntry item)
        {
            var valueTask = Writer.WriteAsync(item, CancellationToken.None);
            valueTask.ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}