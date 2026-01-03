namespace OfX.RabbitMq.Abstractions;

internal interface IRabbitMqServer
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}