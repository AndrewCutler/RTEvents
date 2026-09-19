public interface IMessageBus
{
    Task ProcessAsync(CancellationToken cancellationToken = default);
}