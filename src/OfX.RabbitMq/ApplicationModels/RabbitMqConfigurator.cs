namespace OfX.RabbitMq.ApplicationModels;

public sealed class RabbitMqConfigurator
{
    public string RabbitMqHost { get; private set; }
    public string RabbitVirtualHost { get; private set; }
    public int RabbitMqPort { get; private set; }
    public RabbitMqCredential RabbitMqCredential { get; } = new();

    public void Host(string host, string virtualHost, int port = 5672, Action<RabbitMqCredential> configure = null)
    {
        (RabbitMqHost, RabbitVirtualHost, RabbitMqPort) = (host, virtualHost, port);
        configure?.Invoke(RabbitMqCredential);
    }
}