
namespace Sanlog.MSTest
{
    [TestClass]
    public sealed class MessageBrokerUnitTest
    {
        [TestMethod]
        public async Task SendMessage()
        {
            var handler = new MessageHandler();
            using var broker = new MessageBroker();
            await broker.StartAsync(CancellationToken.None);
            Assert.IsTrue(broker.Register(typeof(int), handler));
            Assert.IsTrue(broker.SendMessage(int.MaxValue));
            await broker.StopAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            Assert.AreEqual(int.MaxValue, handler.LastMessage);
        }
        private sealed class MessageHandler : IMessageHandler
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