using System.Text;
using System.Text.Json;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Extensions;
using OfX.Implementations;

namespace OfX.Nats.Messages;

public class NatsMessageRequestWrapped<TAttribute> where TAttribute : OfXAttribute
{
    public Dictionary<string, string> Headers { get; set; }
    public RequestOf<TAttribute> Query { get; set; }
    public byte[] GetMessageSerialize() => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this));

    public RequestContext<TAttribute> GetMessageDeserialize(byte[] message)
    {
        var messageData = Encoding.UTF8.GetString(message);
        var messageWrapped = JsonSerializer.Deserialize<NatsMessageRequestWrapped<TAttribute>>(messageData);
        return new RequestContextImpl<TAttribute>(messageWrapped.Query, messageWrapped.Headers, CancellationToken.None);
    }

    public string Subject => typeof(TAttribute).GetAssemblyName();
}