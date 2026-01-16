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
using OfX.Statics;

namespace OfX.Nats.Implementations;

internal sealed class NatsServer<TModel, TAttribute>(IServiceProvider serviceProvider)
    : INatsServer<TModel, TAttribute>
    where TAttribute : OfXAttribute where TModel : class
{
    private readonly ILogger<NatsServer<TModel, TAttribute>> _logger =
        serviceProvider.GetService<ILogger<NatsServer<TModel, TAttribute>>>();

    private readonly NatsClientWrapper _natsClientWrapped = serviceProvider
        .GetRequiredService<NatsClientWrapper>();

    // Backpressure: limit concurrent processing (configurable via OfXRegister.SetMaxConcurrentProcessing)
    private readonly SemaphoreSlim _semaphore = new(OfXStatics.MaxConcurrentProcessing, OfXStatics.MaxConcurrentProcessing);

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var natsScribeAsync = _natsClientWrapped.NatsClient
            .SubscribeAsync<OfXRequest>(typeof(TAttribute).GetNatsSubject(), cancellationToken: cancellationToken);

        await foreach (var message in natsScribeAsync)
        {
            // Wait for available slot (backpressure)
            await _semaphore.WaitAsync(cancellationToken);
            _ = ProcessMessageWithReleaseAsync(message, cancellationToken);
        }
    }

    private async Task ProcessMessageWithReleaseAsync(NatsMsg<OfXRequest> message, CancellationToken stoppingToken)
    {
        try
        {
            await ProcessMessageAsync(message, stoppingToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ProcessMessageAsync(NatsMsg<OfXRequest> message, CancellationToken stoppingToken)
    {
        if (message.Data is null) return;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        cts.CancelAfter(OfXConstants.DefaultRequestTimeout);
        var cancellationToken = cts.Token;

        try
        {
            using var serviceScope = serviceProvider.CreateScope();
            var pipeline = serviceScope.ServiceProvider
                .GetRequiredService<ReceivedPipelinesOrchestrator<TModel, TAttribute>>();
            var headers = message.Headers?
                .ToDictionary(a => a.Key, b => b.Value.ToString()) ?? [];
            var requestOf = new RequestOf<TAttribute>(message.Data.SelectorIds, message.Data.Expression);
            var requestContext = new RequestContextImpl<TAttribute>(requestOf, headers, cancellationToken);
            var response = await pipeline.ExecuteAsync(requestContext);
            await message.ReplyAsync(response, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.LogWarning("Request timeout for <{Attribute}>", typeof(TAttribute).Name);
            var errors = new Dictionary<string, StringValues> { { OfXConstants.ErrorDetail, "Request timeout" } };
            await TrySendErrorResponseAsync(message, errors);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error while responding <{Attribute}>", typeof(TAttribute).Name);
            var errors = new Dictionary<string, StringValues> { { OfXConstants.ErrorDetail, e.Message } };
            await TrySendErrorResponseAsync(message, errors);
        }
    }

    private async Task TrySendErrorResponseAsync(NatsMsg<OfXRequest> message, Dictionary<string, StringValues> errors)
    {
        try
        {
            if (message.ReplyTo is not null)
            {
                await _natsClientWrapped.NatsClient
                    .PublishAsync(message.ReplyTo, new ItemsResponse<OfXDataResponse>([]), new NatsHeaders(errors));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send error response for <{Attribute}>", typeof(TAttribute).Name);
        }
    }
}
