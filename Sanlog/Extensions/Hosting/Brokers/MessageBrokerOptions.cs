using System;
using System.Collections.Generic;

namespace Sanlog.Extensions.Hosting.Brokers
{
    /// <summary>
    /// Represents a <see cref="MessageBroker"/> options.
    /// </summary>
    internal sealed class MessageBrokerOptions
    {
        /// <summary>
        /// Gets or sets the fallback handler to use when no type-specific handler exists.
        /// </summary>
        public Type? FallbackHandler { get; set; }
        /// <summary>
        /// Gets a dictionary of type-specific handlers.
        /// </summary>
        public Dictionary<Type, Type> Handlers { get; } = [];
    }
}