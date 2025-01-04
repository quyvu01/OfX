namespace OfX.Nats.ApplicationModels;

public class NatsClientRegister
{
    public NatsClient NatsClient { get; } = new();

    public void UseNats(Action<NatsClient> options)
    {
        options.Invoke(NatsClient);
    }
}