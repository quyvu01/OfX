namespace OfX.RabbitMq.ApplicationModels;

internal sealed class RabbitMqMessage
{
    public List<string> SelectorIds { get; set; }
    public string Expression { get; set; }
}