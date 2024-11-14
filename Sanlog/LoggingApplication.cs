using System;
using System.Diagnostics;

namespace Sanlog
{
    /// <summary>
    /// Represents information about the application.
    /// </summary>
    public sealed record class LoggingApplication
    {
        /// <summary>
        /// The tenant identifier.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Guid _tenantId;
        /// <summary>
        /// The object identifier.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Guid _id;
        /// <summary>
        /// The application name.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _application;
        /// <summary>
        /// The environment name.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _environment;

        /// <summary>
        /// Gets the tenant identifier.
        /// </summary>
        /// <exception cref="ArgumentException">The setter value is <see cref="Guid.Empty"/>.</exception>
        public Guid TenantId
        {
            get => _tenantId;
            init
            {
                if (value == Guid.Empty)
                    throw new ArgumentException("The value is 00000000-0000-0000-0000-000000000000.", nameof(TenantId));
                _tenantId = value;
            }
        }
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
        /// Gets the application name.
        /// </summary>
        /// <exception cref="ArgumentException">The setter value is an empty string.</exception>
        /// <exception cref="ArgumentNullException">The setter value is <see langword="null"/>.</exception>
        public string Application
        {
            get => _application!;
            init
            {
                ArgumentException.ThrowIfNullOrEmpty(value, nameof(Application));
                _application = value;
            }
        }
        /// <summary>
        /// Gets the environment name.
        /// </summary>
        /// <exception cref="ArgumentException">The setter value is an empty string.</exception>
        /// <exception cref="ArgumentNullException">The setter value is <see langword="null"/>.</exception>
        public string Environment
        {
            get => _environment!;
            init
            {
                ArgumentException.ThrowIfNullOrEmpty(value, nameof(Environment));
                _environment = value;
            }
        }
    }
}