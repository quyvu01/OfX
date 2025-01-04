namespace OfX.Nats.ApplicationModels;

public class NatsClientRegister
{
    public NatsClientConfig NatsClientConfig { get; } = new();

    public void UseNats(Action<NatsClientConfig> options)
    {
        options.Invoke(NatsClientConfig);
    }
}