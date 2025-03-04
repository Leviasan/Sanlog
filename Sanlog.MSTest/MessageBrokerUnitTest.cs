using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sanlog.Abstractions;

namespace Sanlog.MSTest
{
    [TestClass]
    public sealed class MessageBrokerUnitTest
    {
        private IHost? _host;

        [TestInitialize]
        public void TestInitialize()
        {
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Services.AddMessageBroker(c => c.SetHandler<int, Int32MessageHandler>());
            _host = builder.Build();
            _host.Start();
        }
        [TestCleanup]
        public void TestCleanup() => _host!.Dispose();
        [TestMethod]
        public void SendMessage()
        {
            var broker = _host!.Services.GetRequiredService<IMessageBroker>();

            //var back = broker as MessageBroker;
            //await back!.StartAsync(CancellationToken.None);

            _ = broker.SendMessage(15);
            Assert.IsNotNull(broker);

            /*
            var handler = new Int32MessageHandler();
            using var broker = new MessageBroker();
            await broker.StartAsync(CancellationToken.None);
            Assert.IsTrue(broker.Register(typeof(int), handler));
            Assert.IsTrue(broker.SendMessage(int.MaxValue));
            await broker.StopAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            Assert.AreEqual(int.MaxValue, handler.LastMessage);
            */
        }
        private sealed class Int32MessageHandler : IMessageHandler
        {
            public object? LastMessage { get; private set; }
            public int Counter { get; private set; }

            public ValueTask HandleAsync(object? message, CancellationToken cancellationToken)
            {
                ++Counter;
                LastMessage = message;
                return ValueTask.CompletedTask;
            }
        }
    }
}