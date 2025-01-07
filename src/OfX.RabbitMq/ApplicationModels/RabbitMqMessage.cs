namespace OfX.RabbitMq.ApplicationModels;

public sealed class RabbitMqMessage
{
    public List<string> SelectorIds { get; set; }
    public string Expression { get; set; }
}