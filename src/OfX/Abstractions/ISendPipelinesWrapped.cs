using OfX.ApplicationModels;
using OfX.Responses;

namespace OfX.Abstractions;

public interface ISendPipelinesWrapped
{
    Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(MessageDeserializable message, IContext context);
}