using System;
using System.Collections.Generic;

namespace Sanlog.Abstractions
{
    /// <summary>
    /// Represents a <see cref="IMessageBroker"/> options.
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