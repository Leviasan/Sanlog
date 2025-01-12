using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sanlog.MSTest
{
    [TestClass]
    [SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Unit-test class must be public")]
    public sealed class ChannelMessageBrokerUnitTest
    {
        [TestMethod]
        public void RegisterDuplicateHandler()
        {
            var handler = new ObjectHandler();
            using var broker = new ChannelMessageBroker();
            Assert.IsTrue(broker.Register(typeof(object), handler));
            Assert.IsFalse(broker.Register(typeof(object), handler));
            Assert.IsTrue(broker.Register(typeof(object), new ObjectHandler()));
        }
        [TestMethod]
        public async Task StartAndCancel()
        {
            using var cts = new CancellationTokenSource();
            using var broker = new ChannelMessageBroker();
            var task = broker.StartAsync(cts.Token);
            await task.ConfigureAwait(false);

            var handler = new ObjectHandler();
            _ = broker.Register(typeof(object), handler);
            Assert.IsTrue(broker.SendMessage(new object()));
            await cts.CancelAsync().ConfigureAwait(false);
        }
        [TestMethod]
        public async Task StartAndStop()
        {
            using var cts = new CancellationTokenSource();
            using var broker = new ChannelMessageBroker();
            await broker.StartAsync(cts.Token).ConfigureAwait(false);

            var handler = new ObjectHandler();
            _ = broker.Register(typeof(object), handler);
            Assert.IsTrue(broker.SendMessage(new object()));
            await broker.StopAsync(TimeSpan.Zero, cts.Token).ConfigureAwait(false);
        }

        private sealed class ObjectHandler : IMessageHandler
        {
            public Task HandleAsync(object? message, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Assert.IsNotNull(message);
                Assert.AreEqual(typeof(object), message.GetType());
                return Task.CompletedTask;
            }
        }
    }
}