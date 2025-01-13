namespace Sanlog.MSTest
{
    [TestClass]
    public sealed class BoundedChannelMessageBrokerUnitTest
    {
        [TestMethod]
        public async Task DropWriteMode()
        {
            var counter = 0;
            using var cts = new CancellationTokenSource();
            using var broker = new MessageBroker(1, System.Threading.Channels.BoundedChannelFullMode.DropWrite, (obj) => ++counter);
            await broker.StartAsync(cts.Token).ConfigureAwait(false);

            var handler = new ObjectHandler();
            Assert.IsTrue(broker.Subscribe(typeof(object), handler));
            for (var i = 0; i < 10; ++i)
                Assert.IsTrue(broker.SendMessage(new object()));
            await broker.StopAsync(TimeSpan.Zero, cts.Token).ConfigureAwait(false);
            Assert.AreNotEqual(0, counter);
        }
    }
}