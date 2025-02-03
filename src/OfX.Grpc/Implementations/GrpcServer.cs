using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Grpc.Abstractions;
using OfX.Implementations;
using OfX.Responses;

namespace OfX.Grpc.Implementations;

internal sealed class GrpcServer<TModel, TAttribute>(IServiceProvider serviceProvider) : IGrpcServer<TModel, TAttribute>
    where TAttribute : OfXAttribute where TModel : class
{
    public Task<ItemsResponse<OfXDataResponse>> GetResponse(MessageDeserializable message,
        Dictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var receivedPipeline = serviceProvider.GetRequiredService<ReceivedPipelinesImpl<TModel, TAttribute>>();
        var requestContext = new RequestContextImpl<TAttribute>(
            new RequestOf<TAttribute>(message.SelectorIds, message.Expression), headers, cancellationToken);
        return receivedPipeline.ExecuteAsync(requestContext);
    }
}