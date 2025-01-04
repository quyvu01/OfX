namespace OfX.Nats.Messages;

internal class NatsMessageReceived
{
    public Dictionary<string, string> Headers { get; set; }
    public MessageRequestOf Query { get; set; }
}

internal class MessageRequestOf
{
    public List<string> SelectorIds { get; set; }
    public string Expression { get; set; }
}