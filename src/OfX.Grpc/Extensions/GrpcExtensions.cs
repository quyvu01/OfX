using System.Collections.Concurrent;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Abstractions.Transporting;
using OfX.ApplicationModels;
using OfX.Extensions;
using OfX.Grpc.ApplicationModels;
using OfX.Grpc.Delegates;
using OfX.Grpc.Implementations;
using OfX.Registries;
using OfX.Responses;
using OfX.Statics;

namespace OfX.Grpc.Extensions;

/// <summary>
/// Provides extension methods for integrating gRPC transport with the OfX framework.
/// </summary>
public static class GrpcExtensions
{
    private static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Adds gRPC client configuration for OfX distributed data fetching.
    /// </summary>
    /// <param name="ofXRegister">The OfX registration instance.</param>
    /// <param name="options">Configuration action for specifying gRPC server hosts.</param>
    /// <remarks>
    /// This method configures the client side of gRPC transport. The client will:
    /// <list type="bullet">
    ///   <item><description>Probe configured hosts to discover which attributes each server handles</description></item>
    ///   <item><description>Route requests to the appropriate server based on attribute type</description></item>
    ///   <item><description>Handle failover and retry logic through the pipeline behaviors</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddOfX(cfg =>
    /// {
    ///     cfg.AddGrpcClients(grpc =>
    ///     {
    ///         grpc.AddGrpcHosts("https://users-service:5001", "https://products-service:5002");
    ///     });
    /// });
    /// </code>
    /// </example>
    public static void AddGrpcClients(this OfXRegister ofXRegister, Action<GrpcClientsRegister> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var clientsRegister = new GrpcClientsRegister();
        options.Invoke(clientsRegister);
        if (clientsRegister.ServiceHosts is not { Count: > 0 } serviceHosts) return;
        ConcurrentDictionary<HostProbe, Type[]> hostMapAttributes = [];
        serviceHosts.ForEach(h => hostMapAttributes.TryAdd(new HostProbe(h, false), []));
        var semaphore = new SemaphoreSlim(1, 1);
        var services = ofXRegister.ServiceCollection;
        services.TryAddTransient<GetOfXResponseFunc>(_ => attributeType => async (query, context) =>
        {
            if (!hostMapAttributes.Any(a => a.Value.Contains(attributeType)))
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (hostMapAttributes.Any(a => a.Value.Contains(attributeType))) goto resolveData;
                    var probeHosts = hostMapAttributes
                        .Where(a => !a.Key.IsProbed)
                        .Select(a => a.Key.ServiceHost);
                    var missingTypes = await GetHostMapAttributesAsync(probeHosts, context);
                    missingTypes.Where(a => a.Key.IsProbed)
                        .ForEach(x =>
                        {
                            if (hostMapAttributes.Any(a => a.Key.ServiceHost == x.Key.ServiceHost))
                            {
                                var hostProbeKey = hostMapAttributes
                                    .First(a => a.Key.ServiceHost == x.Key.ServiceHost);
                                hostMapAttributes.TryRemove(hostProbeKey);
                            }

                            hostMapAttributes.TryAdd(x.Key, x.Value);
                        });
                    if (hostMapAttributes.Any(a => a.Value.Contains(attributeType))) goto resolveData;
                    return new ItemsResponse<DataResponse>([]);
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
            var dataResponse = result.Items.Select(x =>
            {
                var values = x.OfxValues
                    .Select(a => new ValueResponse { Expression = a.Expression, Value = a.Value });
                return new DataResponse { Id = x.Id, OfXValues = [..values] };
            });
            return new ItemsResponse<DataResponse>([..dataResponse]);
        });

        services.TryAddTransient<IRequestClient, GrpcRequestClient>();
    }

    private static async Task<Dictionary<HostProbe, Type[]>> GetHostMapAttributesAsync(
        IEnumerable<string> serverHosts, IContext context)
    {
        var tasks = serverHosts
            .Select(a => (Host: a, OfXAttributesTask: GetAttributesByHost(a, context))).ToList();
        await Task.WhenAll(tasks.Select(a => a.OfXAttributesTask));
        var result = new Dictionary<HostProbe, Type[]>();
        tasks.ForEach(a =>
        {
            var isProbed = a.OfXAttributesTask.Result.IsProbed;
            var ofXAttributes = a.OfXAttributesTask.Result.OfXAttributeTypes;
            result.TryAdd(new HostProbe(a.Host, isProbed), ofXAttributes);
        });
        return result;
    }

    private static async Task<OfXItemsGrpcResponse> GetOfXItemsAsync(string serverHost, IContext context,
        OfXRequest query, Type attributeType)
    {
        using var channel = GrpcChannel.ForAddress(serverHost);
        var client = new OfXTransportService.OfXTransportServiceClient(channel);
        var metadata = new Metadata();
        context?.Headers?.ForEach(h => metadata.Add(h.Key, h.Value));
        var grpcQuery = new GetOfXGrpcQuery();
        using var cancellationTokenSource = CancellationTokenSource
            .CreateLinkedTokenSource(context?.CancellationToken ?? CancellationToken.None);
        cancellationTokenSource.CancelAfter(DefaultRequestTimeout);
        grpcQuery.SelectorIds.AddRange(query.SelectorIds ?? []);
        grpcQuery.Expression = query.Expression;
        grpcQuery.AttributeAssemblyType = attributeType.GetAssemblyName();
        return await client.GetItemsAsync(grpcQuery, metadata, cancellationToken: cancellationTokenSource.Token);
    }

    private static async Task<AttributesProbe> GetAttributesByHost(string serverHost, IContext context)
    {
        try
        {
            using var channel = GrpcChannel.ForAddress(serverHost);
            var client = new OfXTransportService.OfXTransportServiceClient(channel);
            var query = new GetAttributesQuery();
            using var cancellationTokenSource = CancellationTokenSource
                .CreateLinkedTokenSource(context?.CancellationToken ?? CancellationToken.None);
            cancellationTokenSource.CancelAfter(DefaultRequestTimeout);
            var response = await client.GetAttributesAsync(query, cancellationToken: cancellationTokenSource.Token);
            return new AttributesProbe(true, [..response.AttributeTypes.Select(Type.GetType)]);
        }
        catch (Exception)
        {
            if (OfXStatics.ThrowIfExceptions) throw;
            return new AttributesProbe(false, []);
        }
    }

    /// <summary>
    /// Maps the OfX gRPC service endpoint for handling incoming OfX requests.
    /// </summary>
    /// <param name="builder">The endpoint route builder.</param>
    /// <remarks>
    /// This method should be called in the server application's startup to enable
    /// handling of incoming OfX gRPC requests.
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapOfXGrpcService();
    /// </code>
    /// </example>
    public static void MapOfXGrpcService(this IEndpointRouteBuilder builder) => builder.MapGrpcService<GrpcServer>();
}