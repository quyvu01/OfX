namespace OfX.Nats.ApplicationModels;

public class NatsClient
{
    public string NatsHost { get; private set; }
    public NatsCredential NatsCredential { get; } = new();

    public void Host(string host, Action<NatsCredential> options = null)
    {
        NatsHost = host;
        options?.Invoke(NatsCredential);
    }
}