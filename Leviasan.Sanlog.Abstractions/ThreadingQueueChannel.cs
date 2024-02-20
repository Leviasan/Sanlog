using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Provides a base class for channels that support reading and writing elements of type <see cref="LoggingEntry"/>.
    /// </summary>
    public sealed class ThreadingQueueChannel : Channel<LoggingEntry>, IEventWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadingQueueChannel"/> class with the specified maximum number of items the bounded channel may store.
        /// </summary>
        /// <param name="capacity">The maximum number of items the bounded channel may store.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="capacity"/> is less then 1.</exception>
        public ThreadingQueueChannel(int capacity) : this(new BoundedChannelOptions(capacity)) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadingQueueChannel"/> class with the specified bounded channel configuration.
        /// </summary>
        /// <param name="options">The configuration of the bounded channel.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="options"/> is <see langword="null"/>.</exception>
        public ThreadingQueueChannel(BoundedChannelOptions options) : this(options, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadingQueueChannel"/> class
        /// with the specified bounded channel configuration and delegate that will be called when item is being dropped from channel.
        /// </summary>
        /// <param name="options">The configuration of the bounded channel.</param>
        /// <param name="itemDropped">Delegate that will be called when the item is being dropped from the channel. See <see cref="BoundedChannelFullMode"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="options"/> is <see langword="null"/>.</exception>
        public ThreadingQueueChannel(BoundedChannelOptions options, Action<LoggingEntry>? itemDropped)
            : this(Channel.CreateBounded(options ?? throw new ArgumentNullException(nameof(options)), itemDropped)) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadingQueueChannel"/> class with the specified unbounded channel configuration.
        /// </summary>
        /// <param name="options">The configuration of the unbounded channel.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="options"/> is <see langword="null"/>.</exception>
        public ThreadingQueueChannel(UnboundedChannelOptions options)
            : this(Channel.CreateUnbounded<LoggingEntry>(options ?? throw new ArgumentNullException(nameof(options)))) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadingQueueChannel"/> class with the specified channel.
        /// </summary>
        /// <param name="channel">The channel that support reading and writing elements of type <see cref="LoggingEntry"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="channel"/> is <see langword="null"/>.</exception>
        private ThreadingQueueChannel(Channel<LoggingEntry> channel)
        {
            ArgumentNullException.ThrowIfNull(channel);
            Reader = channel.Reader;
            Writer = channel.Writer;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(LoggingEntry item, CancellationToken cancellationToken = default) => Writer.WriteAsync(item, cancellationToken);
    }
}