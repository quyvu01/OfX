using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Constants;
using OfX.Responses;

namespace OfX.Implementations;

internal sealed class SendPipelinesWrapped<TAttribute>(SendPipelinesImpl<TAttribute> sendPipelines)
    : ISendPipelinesWrapped where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(MessageDeserializable message, IContext context)
    {
        var cancellationToken = context?.CancellationToken ?? CancellationToken.None;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(OfXConstants.DefaultRequestTimeout);
        var result = await sendPipelines.ExecuteAsync(
            new RequestContextImpl<TAttribute>(new RequestOf<TAttribute>(message.SelectorIds, message.Expression),
                context?.Headers ?? [], cts.Token));
        return result;
    }
}