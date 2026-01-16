using OfX.Abstractions;
using OfX.Abstractions.Transporting;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Grpc.Delegates;
using OfX.Grpc.Internals;
using OfX.Responses;

namespace OfX.Grpc.Implementations;

/// <summary>
/// gRPC implementation of <see cref="IRequestClient"/> for sending OfX requests over gRPC.
/// </summary>
/// <param name="ofXResponseFunc">The function delegate for making gRPC calls based on attribute type.</param>
/// <remarks>
/// This client is automatically registered when <c>AddGrpcClients</c> is called and handles
/// the serialization and transport of OfX requests to remote gRPC servers.
/// </remarks>
public sealed class GrpcRequestClient(GetOfXResponseFunc ofXResponseFunc) : IRequestClient
{
    public async Task<ItemsResponse<DataResponse>> RequestAsync<TAttribute>(
        RequestContext<TAttribute> requestContext) where TAttribute : OfXAttribute
    {
        var func = ofXResponseFunc.Invoke(typeof(TAttribute));
        return await func.Invoke(
            new OfXRequest(requestContext.Query.SelectorIds, requestContext.Query.Expression),
            new GrpcClientContext(requestContext.Headers, requestContext.CancellationToken));
    }
}