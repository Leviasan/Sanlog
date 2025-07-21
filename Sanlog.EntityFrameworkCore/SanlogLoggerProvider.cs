using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sanlog.Brokers;

namespace Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// Represents a logger provider that can create instances of <see cref="SanlogLogger"/> and consume external scope information.
    /// </summary>

    [ProviderAlias(nameof(SanlogLoggerProvider))]
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class SanlogLoggerProvider : Sanlog.SanlogLoggerProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogLoggerProvider"/> class with the specified message broker receiver, redactor provider and logger options.
        /// </summary>
        /// <param name="receiver">The message broker receiver.</param>
        /// <param name="redactorProvider">The redactors provider for different data classifications.</param>
        /// <param name="options">The configuration of the <see cref="SanlogLoggerProvider"/>.</param>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        public SanlogLoggerProvider(IMessageReceiver receiver, IRedactorProvider redactorProvider, IOptions<SanlogLoggerOptions> options) : base(receiver, redactorProvider, options) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogLoggerProvider"/> class with the specified message broker receiver, <see cref="NullRedactorProvider.Instance"/> and logger options.
        /// </summary>
        /// <param name="receiver">The message broker receiver.</param>
        /// <param name="options">The configuration of the <see cref="SanlogLoggerProvider"/>.</param>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        public SanlogLoggerProvider(IMessageReceiver receiver, IOptions<SanlogLoggerOptions> options) : base(receiver, options) { }
    }
}