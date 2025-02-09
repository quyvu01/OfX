using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Implementations;
using OfX.Responses;

namespace OfX.Abstractions;

public interface IReceivedPipelinesBase
{
    Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(MessageDeserializable message, Dictionary<string, string> headers,
        CancellationToken cancellationToken);
}

public interface IReceivedPipelinesBase<TAttribute> : IReceivedPipelinesBase where TAttribute : OfXAttribute
{
    Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(RequestContext<TAttribute> requestContext);

    Task<ItemsResponse<OfXDataResponse>> IReceivedPipelinesBase.ExecuteAsync(MessageDeserializable message,
        Dictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var requestOf = new RequestOf<TAttribute>(message.SelectorIds, message.Expression);
        var requestContext = new RequestContextImpl<TAttribute>(requestOf, headers, cancellationToken);
        return ExecuteAsync(requestContext);
    }
}