using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Sanlog.Brokers
{
    /// <summary>
    /// Represents a <see cref="MessageBroker"/> options.
    /// </summary>
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
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