using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sanlog.Brokers;

namespace Sanlog.MSTest
{
    [TestClass]
    public sealed class MessageBrokerUnitTest
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private IHost _host;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        [TestInitialize]
        public void TestInitialize()
        {
            var builder = Host.CreateEmptyApplicationBuilder(null);
            _ = builder.Services.AddMessageBroker(c => c.SetHandler<int, Int32MessageHandler>());
            _host = builder.Build();
            _host.Start();
        }
        [TestCleanup]
        public void TestCleanup() => _host.Dispose();
        [TestMethod]
        public async Task SendMessage()
        {
            var broker = _host.Services.GetRequiredService<IMessageBrokerReceiver>();
            Assert.IsTrue(broker.SendMessage(int.MaxValue));

            await Task.Delay(100);
            var handler = (Int32MessageHandler)_host.Services.GetServices<IMessageHandler>().Single(x => x.GetType() == typeof(Int32MessageHandler));
            Assert.AreEqual(int.MaxValue, handler.LastMessage);
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