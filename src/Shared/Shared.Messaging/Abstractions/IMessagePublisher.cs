namespace Shared.Messaging.Abstractions;

/// <summary>
/// Interface for publishing messages to a message broker.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message to the message broker.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;
}
