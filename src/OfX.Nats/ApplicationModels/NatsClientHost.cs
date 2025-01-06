namespace OfX.Nats.ApplicationModels;

public class NatsClientHost
{
    public string NatsUrl { get; private set; }

    public void Url(string host) => NatsUrl = host;
}