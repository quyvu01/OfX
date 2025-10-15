using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Azure.ServiceBus.Abstractions;
using OfX.Azure.ServiceBus.Extensions;
using OfX.Azure.ServiceBus.Wrappers;
using OfX.Constants;
using OfX.Implementations;

namespace OfX.Azure.ServiceBus.Implementations;

internal class AzureServiceBusServer<TModel, TAttribute>(
    AzureServiceBusClientWrapper clientWrapper,
    IServiceProvider serviceProvider)
    : IAzureServiceBusServer<TModel, TAttribute>
    where TAttribute : OfXAttribute where TModel : class
{
    public async Task StartAsync()
    {
        var requestQueue = typeof(TAttribute).GetAzureServiceBusRequestQueue();
        var options = new ServiceBusSessionProcessorOptions
        {
            MaxConcurrentCallsPerSession = 1,
            AutoCompleteMessages = false
        };
        var processor = clientWrapper.ServiceBusClient.CreateSessionProcessor(requestQueue, options);

        processor.ProcessMessageAsync += async args =>
        {
            var request = args.Message;
            var requestDeserialize = JsonSerializer.Deserialize<MessageDeserializable>(request.Body);
            Console.WriteLine($"Test: {requestDeserialize.Expression} of attribute {typeof(TAttribute).Name}");
            using var serviceScope = serviceProvider.CreateScope();
            var pipeline = serviceScope.ServiceProvider
                .GetRequiredService<ReceivedPipelinesOrchestrator<TModel, TAttribute>>();
        
            var headers = request.ApplicationProperties?
                .ToDictionary(a => a.Key, b => b.Value.ToString()) ?? [];
            var requestOf = new RequestOf<TAttribute>(requestDeserialize.SelectorIds, requestDeserialize.Expression);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
            cts.CancelAfter(OfXConstants.DefaultRequestTimeout);
            var requestContext = new RequestContextImpl<TAttribute>(requestOf, headers, cts.Token);
            var response = await pipeline.ExecuteAsync(requestContext);
            var responseMessage = new ServiceBusMessage(JsonSerializer.Serialize(response))
            {
                CorrelationId = request.CorrelationId,
                SessionId = request.SessionId
            };
        
            var sender = clientWrapper.ServiceBusClient.CreateSender(request.ReplyTo);
            await sender.SendMessageAsync(responseMessage, cts.Token);
            await args.CompleteMessageAsync(request, cts.Token);
        };
        processor.ProcessErrorAsync += args =>
        {
            Console.WriteLine($"Error while processing message: {args.Exception}");
            return Task.CompletedTask;
        };
        await processor.StartProcessingAsync();
        await new TaskCompletionSource().Task;
    }
}