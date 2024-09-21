using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents information about the application.
    /// </summary>
    public sealed record class LoggingApplication
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingApplication"/> class with the specified application name and environment name.
        /// </summary>
        /// <param name="id">The application name.</param>
        /// <param name="application">The application name.</param>
        /// <param name="environment">The environment name.</param>
        /// <exception cref="ArgumentException">The <paramref name="application"/> or <paramref name="environment"/> is empty string. -or- The <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="application"/> or <paramref name="environment"/> is <see langword="null"/>.</exception>
        public LoggingApplication(Guid id, string application, string environment)
        {
            if (id == Guid.Empty)
                throw new ArgumentException(null, nameof(id));
            ArgumentException.ThrowIfNullOrEmpty(application);
            ArgumentException.ThrowIfNullOrEmpty(environment);

            Id = id;
            Application = application;
            Environment = environment;
        }

        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        public Guid Id { get; }
        /// <summary>
        /// Gets the application name.
        /// </summary>
        public string Application { get; }
        /// <summary>
        /// Gets the environment name.
        /// </summary>
        public string Environment { get; }
        /// <summary>
        /// Gets the logging entries with the current application.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyList<LoggingEntry>? LogEntries { get; init; }
    }
}