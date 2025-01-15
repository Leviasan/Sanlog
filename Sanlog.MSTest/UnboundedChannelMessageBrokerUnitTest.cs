using System.Diagnostics.CodeAnalysis;

namespace Sanlog.MSTest
{
    [TestClass]
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "TestCleanup")]
    public sealed class UnboundedChannelMessageBrokerUnitTest
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private ObjectHandler _objectHandler;
        private CancellationTokenSource _cts;
        private MessageBroker _broker;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        [TestInitialize]
        public void TestInit()
        {
            _objectHandler = new ObjectHandler();
            _cts = new CancellationTokenSource();
            _broker = new MessageBroker();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _cts.Dispose();
            _broker.Dispose();
        }

        [TestMethod]
        public void Subscribe()
        {
            Assert.IsTrue(_broker.Register(typeof(object), _objectHandler));
            Assert.IsFalse(_broker.Register(typeof(object), _objectHandler)); // Expected false because service is registered before
            Assert.IsTrue(_broker.Register(typeof(object), new ObjectHandler()));
            Assert.IsTrue(_broker.Remove(typeof(object), _objectHandler));
            Assert.IsFalse(_broker.Remove(typeof(object), _objectHandler)); // Expected false because service is unregistered before
            Assert.IsTrue(_broker.Remove(typeof(object))); // Expected true because subscribe 'new ObjectHandler()' is not unregistered yet
        }
        [TestMethod]
        public async Task StartAndCancel()
        {
            var task = _broker.StartAsync(_cts.Token);
            await task.ConfigureAwait(false);

            Assert.IsTrue(_broker.Register(typeof(object), _objectHandler));
            Assert.IsTrue(_broker.SendMessage(new object()));
            await _cts.CancelAsync().ConfigureAwait(false);
        }
        [TestMethod]
        public async Task StartAndStop()
        {
            using var cts = new CancellationTokenSource();
            using var broker = new MessageBroker();
            await broker.StartAsync(cts.Token).ConfigureAwait(false);

            var handler = new ObjectHandler();
            Assert.IsTrue(broker.Register(typeof(object), handler));
            Assert.IsTrue(broker.SendMessage(new object()));
            await broker.StopAsync(TimeSpan.Zero, cts.Token).ConfigureAwait(false);
        }
    }
}