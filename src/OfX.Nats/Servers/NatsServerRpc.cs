using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NATS.Client.Core;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Constants;
using OfX.Helpers;
using OfX.Implementations;
using OfX.Nats.Abstractions;
using OfX.Nats.Extensions;
using OfX.Nats.Wrappers;
using OfX.Responses;
using OfX.Statics;

namespace OfX.Nats.Servers;

internal sealed class NatsServerRpc<TModel, TAttribute>(IServiceProvider serviceProvider)
    : INatsServerRpc<TModel, TAttribute>
    where TAttribute : OfXAttribute where TModel : class
{
    public async Task StartAsync()
    {
        var natsClient = serviceProvider.GetRequiredService<NatsClientWrapper>();
        var logger = serviceProvider.GetService<ILogger<NatsServerRpc<TModel, TAttribute>>>();
        var natsScribeAsync = natsClient.NatsClient
            .SubscribeAsync<MessageDeserializable>(typeof(TAttribute).GetNatsSubject());
        await foreach (var message in natsScribeAsync)
        {
            try
            {
                if (message.Data is null) continue;
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
                logger.LogError("Error while responding <{@Attribute}> with message : {@Error}",
                    typeof(TAttribute).Name, e);
                var errors = new Dictionary<string, StringValues> { { OfXConstants.ErrorDetail, e.Message } };
                await natsClient.NatsClient
                    .PublishAsync(message.ReplyTo!, new ItemsResponse<OfXDataResponse>([]), new NatsHeaders(errors));
            }
        }
    }
}