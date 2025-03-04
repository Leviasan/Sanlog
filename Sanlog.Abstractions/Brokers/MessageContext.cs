using System;

namespace Sanlog.Brokers
{
    /// <summary>
    /// Represents the context of the message.
    /// </summary>
    /// <param name="ServiceType">The service type that mappings with the specified <paramref name="Message"/>.</param>
    /// <param name="Message">The message to handle.</param>
    internal sealed record class MessageContext(Type ServiceType, object? Message);
}