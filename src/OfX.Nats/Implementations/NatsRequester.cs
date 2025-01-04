using System.Text;
using System.Text.Json;
using NATS.Client;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Nats.Abstractions;
using OfX.Nats.Messages;
using OfX.Responses;

namespace OfX.Nats.Implementations;

public sealed class NatsRequester<TAttribute>(IConnection connection)
    : INatsRequester<TAttribute> where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext)
    {
        var natsMessageWrapped = new NatsMessageRequestWrapped<TAttribute>
        {
            Query = requestContext.Query,
            Headers = requestContext.Headers
        };
        var reply = await connection.RequestAsync(natsMessageWrapped.Subject, natsMessageWrapped.GetMessageSerialize(),
            requestContext.CancellationToken);
        var response = Encoding.UTF8.GetString(reply.Data);
        return JsonSerializer.Deserialize<ItemsResponse<OfXDataResponse>>(response);
    }
}