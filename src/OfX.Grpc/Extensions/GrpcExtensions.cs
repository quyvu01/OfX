using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Extensions;
using OfX.Grpc.ApplicationModels;
using OfX.Grpc.Delegates;
using OfX.Grpc.Exceptions;
using OfX.Helpers;
using OfX.Registries;
using OfX.Responses;

namespace OfX.Grpc.Extensions;

public static class GrpcExtensions
{
    private static readonly Lazy<Dictionary<Type, string>> queryWithHostStorage = new(() => []);

    public static void RegisterClients(this OfXRegister ofXRegister, Action<GrpcClientsRegister> options)
    {
        ofXRegister.ServiceCollection
            .AddGrpcClient<OfXTransportService.OfXTransportServiceClient>();

        var newClientsRegister = new GrpcClientsRegister();
        options.Invoke(newClientsRegister);
        var assembliesHostLookup = newClientsRegister.AssembliesHostLookup;
        var allAssemblyIn = assembliesHostLookup.Keys.All(a => ofXRegister.ContractsRegister.Contains(a));
        if (!allAssemblyIn)
            throw new OfXGrpcExceptions.SomeGrpcClientAssemblyAreNotRegistered();

        // Create the relevant grpc clients based on assemblies and hosts
        var genericMappableOf = typeof(DataMappableOf<>);
        assembliesHostLookup.ForEach(assemblyWithHost => assemblyWithHost.Key.ExportedTypes
            .Where(t =>
            {
                var baseType = t.BaseType;
                if (baseType is null) return false;
                return t is { IsClass: true, IsAbstract: false } && baseType.IsGenericType &&
                       baseType.GetGenericTypeDefinition() == genericMappableOf;
            }).ForEach(q => queryWithHostStorage.Value.TryAdd(q, assemblyWithHost.Value)));


        ofXRegister.ServiceCollection.TryAddScoped<GetOfXResponseFunc>(sp => requestType => async (query, context) =>
        {
            if (!queryWithHostStorage.Value.TryGetValue(requestType, out var serverHost))
                throw new OfXGrpcExceptions.GrpcClientQueryTypeNotRegistered(requestType);
            var client = sp.GetRequiredService<OfXTransportService.OfXTransportServiceClient>();
            client.WithHost(serverHost);
            var metadata = new Metadata();
            context?.Headers?.ForEach(h => metadata.Add(h.Key, h.Value));
            var grpcQuery = new GetOfXGrpcQuery();
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(3));
            grpcQuery.SelectorIds.AddRange(query.SelectorIds ?? []);
            grpcQuery.Expression = query.Expression;
            grpcQuery.QueryAssemblyType = requestType.GetAssemblyName();
            var result = await client.GetItemsAsync(grpcQuery, metadata,
                cancellationToken: context?.CancellationToken ?? cancellationTokenSource.Token);
            return new ItemsResponse<OfXDataResponse>([
                ..result.Items.Select(x => new OfXDataResponse { Id = x.Id, Value = x.Value })
            ]);
        });
    }
}