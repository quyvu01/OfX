using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NATS.Client.Core;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Constants;
using OfX.Implementations;
using OfX.Nats.Abstractions;
using OfX.Nats.Extensions;
using OfX.Nats.Wrappers;
using OfX.Responses;

namespace OfX.Nats.Implementations;

internal sealed class NatsServer<TModel, TAttribute>(IServiceProvider serviceProvider)
    : INatsServer<TModel, TAttribute>
    where TAttribute : OfXAttribute where TModel : class
{
    private readonly ILogger<NatsServer<TModel, TAttribute>> _logger =
        serviceProvider.GetService<ILogger<NatsServer<TModel, TAttribute>>>();

    private readonly NatsClientWrapper _natsClientWrapped = serviceProvider
        .GetRequiredService<NatsClientWrapper>();

    public async Task StartAsync()
    {
        var natsScribeAsync = _natsClientWrapped.NatsClient
            .SubscribeAsync<OfXRequest>(typeof(TAttribute).GetNatsSubject());
        await foreach (var message in natsScribeAsync)
            _ = ProcessMessageAsync(message);
    }

    private async Task ProcessMessageAsync(NatsMsg<OfXRequest> message)
    {
        try
        {
            if (message.Data is null) return;
            using var serviceScope = serviceProvider.CreateScope();
            var pipeline = serviceScope.ServiceProvider
                .GetRequiredService<ReceivedPipelinesOrchestrator<TModel, TAttribute>>();
            var headers = message.Headers?
                .ToDictionary(a => a.Key, b => b.Value.ToString()) ?? [];
            var requestOf = new RequestOf<TAttribute>(message.Data.SelectorIds, message.Data.Expression);
            var cancellationToken = CancellationToken.None;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(OfXConstants.DefaultRequestTimeout);
            var requestContext = new RequestContextImpl<TAttribute>(requestOf, headers, CancellationToken.None);
            var response = await pipeline.ExecuteAsync(requestContext);
            await message.ReplyAsync(response, cancellationToken: cts.Token);
        }
        catch (Exception e)
        {
            _logger?.LogError("Error while responding <{@Attribute}> with message : {@Error}",
                typeof(TAttribute).Name, e);
            var errors = new Dictionary<string, StringValues> { { OfXConstants.ErrorDetail, e.Message } };
            await _natsClientWrapped.NatsClient
                .PublishAsync(message.ReplyTo!, new ItemsResponse<OfXDataResponse>([]),
                    new NatsHeaders(errors));
        }
    }
}