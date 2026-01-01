namespace OfX.Abstractions.Agents;

public class ReceiveTransport : OfXAgent
{
    private readonly ConnectionContextSupervisor _connectionSupervisor;
    private readonly string _queueName;
    private readonly IRetryPolicy _retryPolicy;
    private readonly Func<object, Task> _messageHandler;

    public ReceiveTransport(
        ConnectionContextSupervisor connectionSupervisor,
        string queueName,
        IRetryPolicy retryPolicy,
        Func<object, Task> messageHandler)
    {
        _connectionSupervisor = connectionSupervisor;
        _queueName = queueName;
        _retryPolicy = retryPolicy;
        _messageHandler = messageHandler;

        // Start receive loop
        _ = Task.Run(RunAsync);
    }

    private async Task RunAsync()
    {
        var attempt = 0;

        while (!IsStopping)
        {
            try
            {
                // Get connection (cached or create new)
                var connection = await _connectionSupervisor.GetConnectionAsync(Stopping);

                // Create channel for this consumer
                var channel = await connection.CreateChannelAsync(Stopping);

                SetReady();
                attempt = 0; // Reset on success

                // Start consuming - this blocks until channel closes or stopping
                await channel.ConsumeAsync(_queueName, _messageHandler, Stopped);
            }
            catch (OperationCanceledException) when (IsStopping)
            {
                break; // Normal shutdown
            }
            catch (Exception ex)
            {
                if (!_retryPolicy.ShouldRetry(ex, attempt))
                {
                    Console.WriteLine($"ReceiveTransport cannot retry: {ex.Message}");
                    SetFaulted(ex);
                    break;
                }

                var delay = _retryPolicy.GetDelay(attempt);
                Console.WriteLine($"ReceiveTransport retrying in {delay}: {ex.Message}");

                try
                {
                    await Task.Delay(delay, Stopping);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                attempt++;
            }
        }

        SetCompleted();
    }
}