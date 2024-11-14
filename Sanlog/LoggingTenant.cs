using System;
using System.Diagnostics;

namespace Sanlog
{
    /// <summary>
    /// Represents the tenant client.
    /// </summary>
    public sealed record class LoggingTenant
    {
        /// <summary>
        /// The object identifier.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Guid _id;
        /// <summary>
        /// The client name.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _clientName;
        /// <summary>
        /// The client description.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _clientDescription;

        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        /// <exception cref="ArgumentException">The setter value is <see cref="Guid.Empty"/>.</exception>
        public Guid Id
        {
            get => _id;
            init
            {
                if (value == Guid.Empty)
                    throw new ArgumentException("The value is 00000000-0000-0000-0000-000000000000.", nameof(Id));
                _id = value;
            }
        }
        /// <summary>
        /// Gets the client name.
        /// </summary>
        /// <exception cref="ArgumentException">The setter value is an empty string.</exception>
        /// <exception cref="ArgumentNullException">The setter value is <see langword="null"/>.</exception>
        public string ClientName
        {
            get => _clientName!;
            init
            {
                ArgumentException.ThrowIfNullOrEmpty(value, nameof(ClientName));
                _clientName = value;
            }
        }
        /// <summary>
        /// Gets the client description.
        /// </summary>
        public string? ClientDescription
        {
            get => _clientDescription;
            init => _clientDescription = value;
        }
    }
}