using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace OfX.Abstractions.Agents;

public class OfXHostedService(IOfXBusControl bus, IOptions<OfXHostOptions> options)
    : IHostedService, IAsyncDisposable
{
    private readonly OfXHostOptions _options = options.Value;
    private Task _startTask;
    private bool _stopped;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _startTask = bus.StartAsync(cancellationToken);

        // Return immediately or wait based on options
        return _options.WaitUntilStarted
            ? _startTask
            : Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_stopped) return;
        _stopped = true;

        await bus.StopAsync("Host stopping", cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_stopped) await StopAsync(CancellationToken.None);
    }
}