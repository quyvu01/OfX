using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Implementations;
using OfX.RabbitMq.Abstractions;
using OfX.Responses;

namespace OfX.RabbitMq.Implementations;

internal sealed class RabbitMqServerRpc<TModel, TAttribute>(IServiceProvider serviceProvider)
    : IRabbitMqServerRpc<TModel, TAttribute>
    where TAttribute : OfXAttribute where TModel : class
{
    public Task<ItemsResponse<OfXDataResponse>> GetResponse(MessageDeserializable message,
        Dictionary<string, string> headers, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var receivedPipeline = scope.ServiceProvider.GetRequiredService<ReceivedPipelinesImpl<TModel, TAttribute>>();
        var requestContext = new RequestContextImpl<TAttribute>(
            new RequestOf<TAttribute>(message.SelectorIds, message.Expression), headers, cancellationToken);
        return receivedPipeline.ExecuteAsync(requestContext);
    }
}