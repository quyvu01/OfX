using OfX.ApplicationModels;
using OfX.Responses;

namespace OfX.Abstractions;

internal interface ISendPipelinesWrapped
{
    Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(MessageDeserializable message, IContext context);
}