namespace OfX.Nats.ApplicationModels;

public class NatsClientConfig
{
    public string NatsHost { get; private set; }

    public void Host(string host)
    {
        NatsHost = host;
    }
}