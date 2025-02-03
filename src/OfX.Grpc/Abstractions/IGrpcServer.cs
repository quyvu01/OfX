using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Grpc.Abstractions;

internal interface IGrpcServer
{
    Task<ItemsResponse<OfXDataResponse>> GetResponse(MessageDeserializable message, Dictionary<string, string> headers,
        CancellationToken cancellationToken);
}

internal interface IGrpcServer<TModel, TAttribute> where TAttribute : OfXAttribute where TModel : class;