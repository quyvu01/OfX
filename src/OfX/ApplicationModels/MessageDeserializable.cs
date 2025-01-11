namespace OfX.ApplicationModels;

public sealed class MessageDeserializable
{
    public List<string> SelectorIds { get; set; }
    public string Expression { get; set; }
}