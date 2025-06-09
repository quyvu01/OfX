using System.Collections.Concurrent;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Clients;
using OfX.Extensions;
using OfX.Grpc.ApplicationModels;
using OfX.Grpc.Delegates;
using OfX.Grpc.Implementations;
using OfX.Grpc.Servers;
using OfX.Grpc.Statics;
using OfX.Helpers;
using OfX.Registries;
using OfX.Responses;
using OfX.Statics;

namespace OfX.Grpc.Extensions;

public static class GrpcExtensions
{
    private static readonly TimeSpan defaultRequestTimeout = TimeSpan.FromSeconds(3);

    public static void AddGrpcClients(this OfXRegister ofXRegister, Action<GrpcClientsRegister> options)
    {
        var newClientsRegister = new GrpcClientsRegister();
        options.Invoke(newClientsRegister);
        if (GrpcStatics.ServiceHosts is not { Count: > 0 }) return;
        ConcurrentDictionary<HostProbe, List<Type>> hostMapAttributes = [];
        GrpcStatics.ServiceHosts.Distinct().ForEach(h => hostMapAttributes.TryAdd(new HostProbe(h, false), []));
        var semaphore = new SemaphoreSlim(1, 1);

        ofXRegister.ServiceCollection.TryAddScoped<GetOfXResponseFunc>(_ => attributeType => async (query, context) =>
        {
            if (!hostMapAttributes.Any(a => a.Value.Contains(attributeType)))
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (hostMapAttributes.Any(a => a.Value.Contains(attributeType))) goto resolveData;
                    var probeMissingHosts = hostMapAttributes
                        .Where(a => !a.Key.IsProbed)
                        .Select(a => a.Key.ServiceHost);
                    var missingTypes = await GetHostMapAttributesAsync(probeMissingHosts, context);
                    missingTypes.Where(a => a.Key.IsProbed)
                        .ForEach(x =>
                        {
                            if (hostMapAttributes.Any(a => a.Key.ServiceHost == x.Key.ServiceHost))
                            {
                                var hostProbeKey = hostMapAttributes.First(a => a.Key.ServiceHost == x.Key.ServiceHost);
                                hostMapAttributes.TryRemove(hostProbeKey);
                            }

                            hostMapAttributes.TryAdd(x.Key, x.Value);
                        });
                    if (hostMapAttributes.Any(a => a.Value.Contains(attributeType))) goto resolveData;
                    return new ItemsResponse<OfXDataResponse>([]);
                }
                finally
                {
                    semaphore.Release();
                }
            }

            resolveData:
            var host = hostMapAttributes
                .FirstOrDefault(a => a.Value.Contains(attributeType))
                .Key;
            var result = await GetOfXItemsAsync(host.ServiceHost, context, query, attributeType);
            var itemsResponse = new ItemsResponse<OfXDataResponse>([
                ..result.Items.Select(x =>
                {
                    var values = x.OfxValues
                        .Select(a => new OfXValueResponse { Expression = a.Expression, Value = a.Value });
                    return new OfXDataResponse { Id = x.Id, OfXValues = [..values] };
                })
            ]);
            return itemsResponse;
        });
        ClientsInstaller.InstallRequestHandlers(ofXRegister.ServiceCollection, typeof(OfXGrpcRequestClient<>));
    }

    private static async Task<ConcurrentDictionary<HostProbe, List<Type>>> GetHostMapAttributesAsync(
        IEnumerable<string> serverHosts, IContext context)
    {
        var tasks = serverHosts.Select(a => (Host: a, OfXAttributesTask: GetAttributesByHost(a, context))).ToList();
        await Task.WhenAll(tasks.Select(a => a.OfXAttributesTask));
        var result = new ConcurrentDictionary<HostProbe, List<Type>>();
        tasks.ForEach(a =>
        {
            var isProbed = a.OfXAttributesTask.Result.IsProbed;
            var ofXAttributes = a.OfXAttributesTask.Result.OfXAttributeTypes;
            result.TryAdd(new HostProbe(a.Host, isProbed), ofXAttributes);
        });
        return result;
    }

    private static async Task<OfXItemsGrpcResponse> GetOfXItemsAsync(string serverHost, IContext context,
        MessageDeserializable query, Type attributeType)
    {
        using var channel = GrpcChannel.ForAddress(serverHost);
        var client = new OfXTransportService.OfXTransportServiceClient(channel);
        var metadata = new Metadata();
        context?.Headers?.ForEach(h => metadata.Add(h.Key, h.Value));
        var grpcQuery = new GetOfXGrpcQuery();
        using var cancellationTokenSource = CancellationTokenSource
            .CreateLinkedTokenSource(context?.CancellationToken ?? CancellationToken.None);
        cancellationTokenSource.CancelAfter(defaultRequestTimeout);
        grpcQuery.SelectorIds.AddRange(query.SelectorIds ?? []);
        grpcQuery.Expression = query.Expression;
        grpcQuery.AttributeAssemblyType = attributeType.GetAssemblyName();
        return await client.GetItemsAsync(grpcQuery, metadata, cancellationToken: cancellationTokenSource.Token);
    }

    private static async Task<(bool IsProbed, List<Type> OfXAttributeTypes)> GetAttributesByHost(string serverHost,
        IContext context)
    {
        try
        {
            using var channel = GrpcChannel.ForAddress(serverHost);
            var client = new OfXTransportService.OfXTransportServiceClient(channel);
            var query = new GetAttributesQuery();
            using var cancellationTokenSource = CancellationTokenSource
                .CreateLinkedTokenSource(context?.CancellationToken ?? CancellationToken.None);
            cancellationTokenSource.CancelAfter(defaultRequestTimeout);
            var response = await client.GetAttributesAsync(query, cancellationToken: cancellationTokenSource.Token);
            return (true, [..response.AttributeTypes.Select(Type.GetType)]);
        }
        catch (Exception)
        {
            if (OfXStatics.ThrowIfExceptions) throw;
            return (false, []);
        }
    }

    public static void MapOfXGrpcService(this IEndpointRouteBuilder builder)
    {
        builder.MapGrpcService<OfXGrpcServer>();
    }
}