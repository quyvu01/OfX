namespace OfX.Nats.Messages;

internal class MessageRequestOf
{
    public List<string> SelectorIds { get; set; }
    public string Expression { get; set; }
}