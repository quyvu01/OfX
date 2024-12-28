using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Extensions;
using OfX.Grpc.ApplicationModels;
using OfX.Grpc.Delegates;
using OfX.Grpc.HandlersInstaller;
using OfX.Grpc.Servers;
using OfX.Helpers;
using OfX.Queries.OfXQueries;
using OfX.Registries;
using OfX.Responses;

namespace OfX.Grpc.Extensions;

public static class GrpcExtensions
{
    private static readonly TimeSpan defaultRequestTimeout = TimeSpan.FromSeconds(3);
    public static void AddGrpcClients(this OfXRegister ofXRegister, Action<GrpcClientsRegister> options)
    {
        var newClientsRegister = new GrpcClientsRegister();
        options.Invoke(newClientsRegister);
        var hostMapAttributes = newClientsRegister.HostMapAttributes;
        var attributesRegister = hostMapAttributes.Values.SelectMany(a => a);

        ofXRegister.ServiceCollection.TryAddScoped<GetOfXResponseFunc>(_ => attributeType => async (query, context) =>
        {
            if (!hostMapAttributes.Any(a => a.Value.Contains(attributeType)))
                return new ItemsResponse<OfXDataResponse>([]);
            var host = hostMapAttributes
                .FirstOrDefault(a => a.Value.Contains(attributeType))
                .Key;
            var result = await GetOfXItemsAsync(host, context, query, attributeType);
            var itemsResponse = new ItemsResponse<OfXDataResponse>([
                ..result.Items.Select(x => new OfXDataResponse { Id = x.Id, Value = x.Value })
            ]);
            return itemsResponse;
        });
        DefaultGrpcClientsInstaller.InstallerServices(ofXRegister.ServiceCollection, [..attributesRegister]);
    }

    private static async Task<OfXItemsGrpcResponse> GetOfXItemsAsync(string serverHost, IContext context,
        GetDataMappableQuery query, Type attributeType)
    {
        using var channel = GrpcChannel.ForAddress(serverHost);
        var client = new OfXTransportService.OfXTransportServiceClient(channel);
        var metadata = new Metadata();
        context?.Headers?.ForEach(h => metadata.Add(h.Key, h.Value));
        var grpcQuery = new GetOfXGrpcQuery();
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
        cancellationTokenSource.CancelAfter(defaultRequestTimeout);
        grpcQuery.SelectorIds.AddRange(query.SelectorIds ?? []);
        grpcQuery.Expression = query.Expression;
        grpcQuery.AttributeAssemblyType = attributeType.GetAssemblyName();
        return await client.GetItemsAsync(grpcQuery, metadata,
            cancellationToken: context?.CancellationToken ?? cancellationTokenSource.Token);
    }

    public static void MapOfXGrpcService(this IEndpointRouteBuilder builder)
    {
        builder.MapGrpcService<OfXGrpcServer>();
    }
}