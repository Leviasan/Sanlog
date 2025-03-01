
namespace Sanlog.MSTest
{
    [TestClass]
    public sealed class MessageBrokerUnitTest
    {
        [TestMethod]
        public void Method()
        {
            using var broker = new MessageBroker();
            Assert.IsTrue(broker.Register(typeof(object), new MessageHandler()));
        }

        private sealed class MessageHandler : IMessageHandler
        {
            public ValueTask HandleAsync(object? message, CancellationToken cancellationToken) => ValueTask.CompletedTask;
        }
    }
}