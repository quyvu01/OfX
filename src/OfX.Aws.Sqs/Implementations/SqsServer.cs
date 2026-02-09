using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfX.ApplicationModels;
using OfX.Aws.Sqs.Abstractions;
using OfX.Aws.Sqs.Constants;
using OfX.Aws.Sqs.Extensions;
using OfX.Aws.Sqs.Statics;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Implementations;
using OfX.Responses;
using OfX.Statics;
using OfX.Telemetry;

namespace OfX.Aws.Sqs.Implementations;

internal class SqsServer(IServiceProvider serviceProvider) : ISqsServer
{
    private static readonly ConcurrentDictionary<string, Type> AttributeAssemblyCached = [];
    private readonly ILogger<SqsServer> _logger = serviceProvider.GetService<ILogger<SqsServer>>();

    // Backpressure: limit concurrent processing
    private readonly SemaphoreSlim _semaphore = new(OfXStatics.MaxConcurrentProcessing,
        OfXStatics.MaxConcurrentProcessing);

    private AmazonSQSClient _sqsClient;
    private readonly List<string> _requestQueueUrls = [];
    private readonly CancellationTokenSource _processingCts = new();
    private const string TransportName = "sqs";

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        // Configure AWS credentials
        AWSCredentials credentials = null;
        if (!string.IsNullOrEmpty(SqsStatics.AwsAccessKeyId) && !string.IsNullOrEmpty(SqsStatics.AwsSecretAccessKey))
        {
            credentials = new BasicAWSCredentials(SqsStatics.AwsAccessKeyId, SqsStatics.AwsSecretAccessKey);
        }

        // Create SQS client
        var config = new AmazonSQSConfig
        {
            RegionEndpoint = SqsStatics.AwsRegion ?? RegionEndpoint.USEast1
        };

        // Support LocalStack for testing
        if (!string.IsNullOrEmpty(SqsStatics.ServiceUrl)) config.ServiceURL = SqsStatics.ServiceUrl;

        _sqsClient = credentials != null
            ? new AmazonSQSClient(credentials, config)
            : new AmazonSQSClient(config);

        var attributeTypes = OfXStatics.AttributeMapHandlers.Keys.ToList();
        if (attributeTypes is not { Count: > 0 }) return;

        // Create request queues for each attribute type
        foreach (var queueName in attributeTypes.Select(attributeType => attributeType.GetQueueName()))
        {
            try
            {
                // Try to get existing queue
                var getQueueUrlResponse = await _sqsClient.GetQueueUrlAsync(queueName, cancellationToken);
                _requestQueueUrls.Add(getQueueUrlResponse.QueueUrl);
            }
            catch (QueueDoesNotExistException)
            {
                // Create new queue
                var createQueueResponse = await _sqsClient.CreateQueueAsync(new CreateQueueRequest
                {
                    QueueName = queueName,
                    Attributes = new Dictionary<string, string>
                    {
                        {
                            OfXSqsConstants.AttributeVisibilityTimeout,
                            OfXSqsConstants.DefaultVisibilityTimeout.ToString()
                        },
                        {
                            OfXSqsConstants.AttributeReceiveMessageWaitTimeSeconds,
                            OfXSqsConstants.DefaultWaitTimeSeconds.ToString()
                        }
                    }
                }, cancellationToken);

                _requestQueueUrls.Add(createQueueResponse.QueueUrl);
            }
        }

        // Start receiver loops for each queue (parallel processing)
        foreach (var queueUrl in _requestQueueUrls) ProcessQueueAsync(queueUrl, cancellationToken).Forget();
    }

    private async Task ProcessQueueAsync(string queueUrl, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Long polling receive
                var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = OfXSqsConstants.MaxNumberOfMessages,
                    WaitTimeSeconds = OfXSqsConstants.DefaultWaitTimeSeconds,
                    MessageAttributeNames = [OfXSqsConstants.MessageAttributeAll]
                }, ct);

                if (response.Messages.Count == 0) continue;

                // Process messages with backpressure control
                var processingTasks = response.Messages.Select(async message =>
                {
                    // Acquire slot inside task (parallel)
                    await _semaphore.WaitAsync(ct);
                    try
                    {
                        // Process message (now awaited, not fire-and-forget)
                        ProcessMessageAsync(queueUrl, message, ct).Forget();
                        return message.ReceiptHandle;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error processing message {CorrelationId}",
                            message.MessageAttributes.GetValueOrDefault(OfXSqsConstants.MessageAttributeCorrelationId)
                                ?.StringValue ?? "Unknown");
                        _semaphore.Release();
                        return message.ReceiptHandle;
                    }
                });

                var results = await Task.WhenAll(processingTasks);

                // Batch delete all processed messages
                var receiptHandles = results.ToList();

                if (receiptHandles.Count > 0)
                    await _sqsClient.DeleteMessageBatchAsync(new DeleteMessageBatchRequest
                    {
                        QueueUrl = queueUrl,
                        Entries = receiptHandles.Select((handle, index) => new DeleteMessageBatchRequestEntry
                        {
                            Id = index.ToString(),
                            ReceiptHandle = handle
                        }).ToList()
                    }, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger?.LogError(ex, "Error processing queue {QueueUrl}", queueUrl);
                await Task.Delay(1000, ct);
            }
        }
    }

    private async Task ProcessMessageAsync(string queueUrl, Message message, CancellationToken stoppingToken)
    {
        // Extract metadata
        var correlationId = message.MessageAttributes
            .GetValueOrDefault(OfXSqsConstants.MessageAttributeCorrelationId)?.StringValue ?? "Unknown";
        var replyToQueueUrl = message.MessageAttributes
            .GetValueOrDefault(OfXSqsConstants.MessageAttributeReplyTo)
            ?.StringValue;
        var attributeTypeString = message.MessageAttributes
            .GetValueOrDefault(OfXSqsConstants.MessageAttributeType)
            ?.StringValue;

        // Extract parent trace context
        ActivityContext parentContext = default;
        if (message.MessageAttributes.TryGetValue(OfXSqsConstants.MessageAttributeTraceparent, out var traceparent))
            ActivityContext.TryParse(traceparent.StringValue, null, out parentContext);

        // Parse message to get attribute name
        var ofxRequest = JsonSerializer.Deserialize<OfXRequest>(message.Body);
        var attributeName = attributeTypeString?.Split(',')[0].Split('.').Last() ?? "Unknown";

        // Start server-side activity
        using var activity = OfXActivitySource.StartServerActivity(attributeName, parentContext);
        var stopwatch = Stopwatch.StartNew();

        // Create timeout CTS
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        cts.CancelAfter(OfXStatics.DefaultRequestTimeout);
        var cancellationToken = cts.Token;

        try
        {
            // Add messaging tags to activity
            activity?.SetMessagingTags(system: TransportName, destination: queueUrl, messageId: correlationId,
                operation: "process");

            // Emit diagnostic event
            OfXDiagnostics.MessageReceive(TransportName, queueUrl, correlationId);

            var receivedPipelineOrchestrator = AttributeAssemblyCached.GetOrAdd(attributeTypeString,
                attributeAssembly =>
                {
                    var ofXAttributeType = Type.GetType(attributeAssembly)!;
                    if (!OfXStatics.AttributeMapHandlers.TryGetValue(ofXAttributeType, out var handlerType))
                        throw new OfXException.CannotFindHandlerForOfAttribute(ofXAttributeType);
                    var modelType = handlerType.GetGenericArguments()[0];
                    return typeof(ReceivedPipelinesOrchestrator<,>).MakeGenericType(modelType, ofXAttributeType);
                });

            using var scope = serviceProvider.CreateScope();
            var server = scope.ServiceProvider
                .GetService(receivedPipelineOrchestrator) as ReceivedPipelinesOrchestrator;
            ArgumentNullException.ThrowIfNull(server);

            var headers = message.MessageAttributes
                .ToDictionary(a => a.Key, b => b.Value.StringValue);
            var data = await server.ExecuteAsync(ofxRequest, headers, cancellationToken);
            var response = Result.Success(data);

            // Send response back to reply queue
            if (!string.IsNullOrEmpty(replyToQueueUrl))
            {
                await _sqsClient.SendMessageAsync(new SendMessageRequest
                {
                    QueueUrl = replyToQueueUrl,
                    MessageBody = JsonSerializer.Serialize(response),
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>
                    {
                        {
                            OfXSqsConstants.MessageAttributeCorrelationId,
                            new MessageAttributeValue { DataType = "String", StringValue = correlationId }
                        }
                    }
                }, cancellationToken);
            }

            // Record success
            stopwatch.Stop();
            var itemCount = data?.Items?.Length ?? 0;

            OfXMetrics.RecordRequest(attributeName, TransportName, stopwatch.Elapsed.TotalMilliseconds, itemCount);

            activity?.SetOfXTags(ofxRequest?.Expressions, ofxRequest?.SelectorIds, itemCount);

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger?.LogWarning("Request timeout for <{Attribute}>", attributeTypeString);
            var response = Result.Failed(new TimeoutException($"Request timeout for {attributeTypeString}"));

            // Record timeout as error
            OfXMetrics.RecordError(attributeName, TransportName, stopwatch.Elapsed.TotalMilliseconds,
                "TimeoutException");

            activity?.SetStatus(ActivityStatusCode.Error, "Request timeout");

            await SendResponseAsync(replyToQueueUrl, correlationId, response, stoppingToken);
            throw; // Re-throw to mark message for deletion in batch
        }
        catch (Exception e)
        {
            stopwatch.Stop();

            _logger?.LogError(e, "Error while responding <{Attribute}>", attributeTypeString);
            var response = Result.Failed(e);

            // Record error
            OfXMetrics.RecordError(attributeName, TransportName, stopwatch.Elapsed.TotalMilliseconds, e.GetType().Name);

            OfXDiagnostics.RequestError(attributeName, TransportName, e, stopwatch.Elapsed);

            activity?.RecordException(e);
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);

            await SendResponseAsync(replyToQueueUrl, correlationId, response, stoppingToken);
            throw; // Re-throw to mark message for deletion in batch
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task SendResponseAsync(string replyToQueueUrl, string correlationId, Result response,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(replyToQueueUrl)) return;

        try
        {
            await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = replyToQueueUrl,
                MessageBody = JsonSerializer.Serialize(response),
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        OfXSqsConstants.MessageAttributeCorrelationId,
                        new MessageAttributeValue { DataType = "String", StringValue = correlationId }
                    }
                }
            }, cancellationToken);
        }
        catch
        {
            // Ignore errors when sending error response
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_processingCts is not null) await _processingCts.CancelAsync();
        _sqsClient?.Dispose();
        _semaphore?.Dispose();
        _processingCts?.Dispose();
    }
}