namespace Sanlog.MSTest
{
    internal sealed class ObjectHandler : IMessageHandler
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