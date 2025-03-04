using System.Threading;
using System.Threading.Tasks;

namespace Sanlog.Brokers
{
    /// <summary>
    /// Provides a mechanism for handling a message.
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Asynchronously handle a message.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        /// <param name="cancellationToken">A cancellation token used to cancel the operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        ValueTask HandleAsync(object? message, CancellationToken cancellationToken);
    }
}