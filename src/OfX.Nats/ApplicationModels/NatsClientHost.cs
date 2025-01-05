namespace OfX.Nats.ApplicationModels;

public class NatsClientHost
{
    public string NatsHost { get; private set; }

    public void Url(string host) => NatsHost = host;
}