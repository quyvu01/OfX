using OfX.RabbitMq.Abstractions;

namespace OfX.RabbitMq.ApplicationModels;

public sealed class RabbitMqConfigurator
{
    public string RabbitMqHost { get; private set; }
    public string RabbitVirtualHost { get; private set; }
    public int RabbitMqPort { get; set; }

    public void Host(string host, string virtualHost, int port, Action<IRabbitMqCredential> configure = null) =>
        (RabbitMqHost, RabbitVirtualHost, RabbitMqPort) = (host, virtualHost, port);
}