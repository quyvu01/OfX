using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.RabbitMq.Abstractions;

internal interface IRabbitMqServerRpc
{
    Task<ItemsResponse<OfXDataResponse>> GetResponseAsync(MessageDeserializable message,
        Dictionary<string, string> headers, CancellationToken cancellationToken);
}

internal interface IRabbitMqServerRpc<TModel, TAttribute> : IRabbitMqServerRpc
    where TAttribute : OfXAttribute where TModel : class;