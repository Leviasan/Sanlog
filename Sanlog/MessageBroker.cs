using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Options;
using System.Collections.Frozen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Sanlog
{
    internal sealed class MessageBrokerOptions
    {
        public Type? FallbackHandler { get; set; }
        public Dictionary<Type, Type> Handlers { get; } = [];
    }
    internal interface IMessageBrokerBuilder
    {
        IServiceCollection Services { get; }

        IMessageBrokerBuilder SetHandler<TMessage, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>() where THandler : class, IMessageHandler;
    }
    internal sealed class MessageBrokerBuilder(IServiceCollection services) : IMessageBrokerBuilder
    {
        public IServiceCollection Services { get; } = services;

        public IMessageBrokerBuilder SetHandler<TMessage, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>() where THandler : class, IMessageHandler
        {
            Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageHandler, THandler>());
            _ = Services.Configure<MessageBrokerOptions>(options => options.Handlers[typeof(TMessage)] = typeof(THandler));
            return this;
        }
        public IMessageBrokerBuilder SetFallbackHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>() where THandler : class, IMessageHandler
        {
            Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageHandler, THandler>());
            _ = Services.Configure<MessageBrokerOptions>(options => options.FallbackHandler = typeof(THandler));
            return this;
        }
    }
    internal static class MessageBrokerServiceCollectionExtensions
    {
        public static IServiceCollection AddMessageBroker(this IServiceCollection services, Action<IMessageBrokerBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            services
                .AddOptions<MessageBrokerOptions>()
                .Services
                .TryAddSingleton<IMessageBroker, MessageBroker>();

            configure.Invoke(new MessageBrokerBuilder(services));

            return services;
        }
    }
    /// <summary>
    /// Represents a service to send/deliver messages to handlers based on <see cref="Channel"/>.
    /// </summary>
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class MessageBroker : BackgroundService, IMessageBroker
    {
        /// <summary>
        /// The underlying channel.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Channel<MessageContext> _channel;
        /// <summary>
        /// The dictionary of mappings between a message type and its handlers.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FrozenDictionary<Type, IMessageHandler> _consumers;
        /// <summary>
        /// To detect redundant calls Dispose method.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBroker"/> class based on a buffered channel of unbounded capacity for use by any number of writers but at most a single reader at a time.
        /// </summary>
        public MessageBroker(IEnumerable<IMessageHandler> handlers, IOptions<MessageBrokerOptions> options)
        {
            ArgumentNullException.ThrowIfNull(handlers);
            ArgumentNullException.ThrowIfNull(options);

            _consumers = GetClassHandlerMap(handlers, options.Value.Handlers);
            _channel = Channel.CreateUnbounded<MessageContext>(new UnboundedChannelOptions { SingleReader = true });

            static FrozenDictionary<Type, IMessageHandler> GetClassHandlerMap(IEnumerable<IMessageHandler> handlers, Dictionary<Type, Type> map)
            {
                var dictionary = new Dictionary<Type, IMessageHandler>(map.Count);
                foreach (var kvp in map)
                {
                    foreach (var handler in handlers)
                    {
                        if (handler.GetType() == kvp.Value)
                        {
                            dictionary[kvp.Key] = handler;
                            break;
                        }
                    }
                }
                return dictionary.ToFrozenDictionary();
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (!_disposedValue)
            {
                _channel.Writer.Complete();
                _disposedValue = true;
            }
            base.Dispose();
        }
        /// <inheritdoc/>
        public bool SendMessage<TMessage>(TMessage? message) => SendMessage(typeof(TMessage), message);
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">The <paramref name="serviceType"/> is <see langword="null"/>.</exception>
        public bool SendMessage<TMessage>(Type serviceType, TMessage? message)
            => _channel.Writer.TryWrite(new MessageContext(serviceType ?? throw new ArgumentNullException(nameof(serviceType)), message));
        /// <inheritdoc/>
        public async ValueTask<bool> SendMessageAsync<TMessage>(TMessage? message, CancellationToken cancellationToken)
            => await SendMessageAsync(typeof(TMessage), message, cancellationToken).ConfigureAwait(false);
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">The <paramref name="serviceType"/> is <see langword="null"/>.</exception>
        public async ValueTask<bool> SendMessageAsync<TMessage>(Type serviceType, TMessage? message, CancellationToken cancellationToken)
            => await _channel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false) && SendMessage(serviceType, message); // ArgumentNullException
        /// <inheritdoc/>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Suppressing throwing exception while handle context")]
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _channel.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var context))
                {
                    if (_consumers.TryGetValue(context.ServiceType, out var handler))
                    {
                        try
                        {
                            await handler.HandleAsync(context.Message, stoppingToken).ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Represents the context of the message.
        /// </summary>
        /// <param name="ServiceType">The service type that mappings with the specified <paramref name="Message"/>.</param>
        /// <param name="Message">The message to handle.</param>
        private sealed record class MessageContext(Type ServiceType, object? Message);
    }
}