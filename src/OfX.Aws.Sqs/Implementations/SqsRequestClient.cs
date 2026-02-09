using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using OfX.Abstractions;
using OfX.Abstractions.Transporting;
using OfX.Attributes;
using OfX.Aws.Sqs.Constants;
using OfX.Aws.Sqs.Extensions;
using OfX.Aws.Sqs.Statics;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Responses;
using OfX.Statics;
using OfX.Telemetry;

namespace OfX.Aws.Sqs.Implementations;

internal class SqsRequestClient : IRequestClient, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<Message>> _eventArgsMapper = new();
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private AmazonSQSClient _sqsClient;
    private string _responseQueueUrl;
    private bool _initialized;
    private CancellationTokenSource _receiverCts;
    private const string TransportName = "sqs";

    public async Task<ItemsResponse<DataResponse>> RequestAsync<TAttribute>(
        RequestContext<TAttribute> requestContext) where TAttribute : OfXAttribute
    {
        // Start client-side activity for distributed tracing
        using var activity = OfXActivitySource.StartClientActivity<TAttribute>(TransportName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Lazy initialization - thread-safe
            await EnsureInitializedAsync(requestContext.CancellationToken);

            if (_sqsClient is null) throw new InvalidOperationException("SQS client is not initialized");

            var queueName = typeof(TAttribute).GetQueueName();
            var cancellationToken = requestContext.CancellationToken;
            var correlationId = Guid.NewGuid().ToString();

            // Propagate W3C trace context
            var messageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                {
                    OfXSqsConstants.MessageAttributeCorrelationId,
                    new MessageAttributeValue { DataType = "String", StringValue = correlationId }
                },
                {
                    OfXSqsConstants.MessageAttributeReplyTo,
                    new MessageAttributeValue { DataType = "String", StringValue = _responseQueueUrl }
                },
                {
                    OfXSqsConstants.MessageAttributeType,
                    new MessageAttributeValue
                        { DataType = "String", StringValue = typeof(TAttribute).AssemblyQualifiedName }
                }
            };

            if (activity != null)
            {
                if (!string.IsNullOrEmpty(activity.Id))
                    messageAttributes.Add(OfXSqsConstants.MessageAttributeTraceparent,
                        new MessageAttributeValue { DataType = "String", StringValue = activity.Id });
                if (!string.IsNullOrEmpty(activity.TraceStateString))
                    messageAttributes.Add(OfXSqsConstants.MessageAttributeTracestate,
                        new MessageAttributeValue { DataType = "String", StringValue = activity.TraceStateString });

                activity.SetMessagingTags(system: TransportName, destination: queueName, messageId: correlationId,
                    operation: "publish");

                activity.SetOfXTags(requestContext.Query.Expressions, requestContext.Query.SelectorIds);
            }

            // Add custom headers from request context
            requestContext.Headers?.ForEach(h => messageAttributes.TryAdd(h.Key,
                new MessageAttributeValue { DataType = "String", StringValue = h.Value }));

            // Emit diagnostic event
            OfXDiagnostics.RequestStart(typeof(TAttribute).Name, TransportName, requestContext.Query.SelectorIds,
                requestContext.Query.Expressions);

            // Track active requests
            OfXMetrics.UpdateActiveRequests(1);

            var tcs = new TaskCompletionSource<Message>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _eventArgsMapper.TryAdd(correlationId, tcs);

            try
            {
                var messageSerialize = JsonSerializer.Serialize(requestContext.Query);

                // Get or create request queue URL
                var requestQueueUrl = await GetOrCreateQueueUrlAsync(queueName, cancellationToken);

                // Send message
                await _sqsClient.SendMessageAsync(new SendMessageRequest
                {
                    QueueUrl = requestQueueUrl,
                    MessageBody = messageSerialize,
                    MessageAttributes = messageAttributes
                }, cancellationToken);

                // Wait with timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(OfXStatics.DefaultRequestTimeout);

                await using var _ = cts.Token.Register(() => tcs.TrySetCanceled());

                var responseMessage = await tcs.Task;
                var response = JsonSerializer.Deserialize<Result>(responseMessage.Body);

                if (response is null)
                    throw new OfXException.ReceivedException("Received null response from server");

                if (!response.IsSuccess)
                    throw response.Fault?.ToException()
                          ?? new OfXException.ReceivedException("Unknown error from server");

                // Record success metrics
                stopwatch.Stop();
                var itemCount = response.Data?.Items?.Length ?? 0;

                OfXMetrics.RecordRequest(typeof(TAttribute).Name, TransportName, stopwatch.Elapsed.TotalMilliseconds,
                    itemCount);

                OfXDiagnostics.RequestStop(typeof(TAttribute).Name, TransportName, itemCount, stopwatch.Elapsed);

                activity?.SetOfXTags(itemCount: itemCount);
                activity?.SetStatus(ActivityStatusCode.Ok);

                return response.Data;
            }
            finally
            {
                // Cleanup on any exit path
                _eventArgsMapper.TryRemove(correlationId, out _);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Record error metrics
            OfXMetrics.RecordError(typeof(TAttribute).Name, TransportName, stopwatch.Elapsed.TotalMilliseconds,
                ex.GetType().Name);

            OfXDiagnostics.RequestError(typeof(TAttribute).Name, TransportName, ex, stopwatch.Elapsed);

            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            throw;
        }
        finally
        {
            OfXMetrics.UpdateActiveRequests(-1);
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;
            await InitializeAsync(cancellationToken);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
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
        if (!string.IsNullOrEmpty(SqsStatics.ServiceUrl))
        {
            config.ServiceURL = SqsStatics.ServiceUrl;
        }

        _sqsClient = credentials != null
            ? new AmazonSQSClient(credentials, config)
            : new AmazonSQSClient(config);

        // Create persistent response queue
        var responseQueueName = $"ofx-response-{Environment.MachineName}-{Guid.NewGuid()}".ToLower();
        var createQueueResponse = await _sqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = responseQueueName,
            Attributes = new Dictionary<string, string>
            {
                {
                    OfXSqsConstants.AttributeMessageRetentionPeriod,
                    OfXSqsConstants.DefaultMessageRetentionPeriod.ToString()
                },
                { OfXSqsConstants.AttributeVisibilityTimeout, OfXSqsConstants.DefaultVisibilityTimeout.ToString() },
                {
                    OfXSqsConstants.AttributeReceiveMessageWaitTimeSeconds,
                    OfXSqsConstants.DefaultWaitTimeSeconds.ToString()
                }
            }
        }, cancellationToken);

        _responseQueueUrl = createQueueResponse.QueueUrl;

        // Start background receiver loop
        _receiverCts = new CancellationTokenSource();
        ReceiveResponsesLoopAsync(_receiverCts.Token).Forget();
    }

    private async Task ReceiveResponsesLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Long polling (wait up to 20 seconds for messages)
                var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _responseQueueUrl,
                    MaxNumberOfMessages = OfXSqsConstants.MaxNumberOfMessages,
                    WaitTimeSeconds = OfXSqsConstants.DefaultWaitTimeSeconds,
                    MessageAttributeNames = [OfXSqsConstants.MessageAttributeAll]
                }, ct);

                if (response.Messages.Count == 0) continue;

                // Process messages in parallel
                var processingResults = response.Messages.Select(message =>
                {
                    try
                    {
                        // Get correlation ID
                        if (!message.MessageAttributes.TryGetValue(OfXSqsConstants.MessageAttributeCorrelationId,
                                out var correlationIdValue)) return message.ReceiptHandle;

                        var correlationId = correlationIdValue.StringValue;

                        // Find pending request
                        if (!_eventArgsMapper.TryRemove(correlationId, out var tcs)) return message.ReceiptHandle;
                        tcs.TrySetResult(message);
                        return message.ReceiptHandle;
                    }
                    catch
                    {
                        // On error, mark for deletion to avoid reprocessing
                        return message.ReceiptHandle;
                    }
                });


                // Batch delete processed messages
                var receiptHandles = processingResults.ToList();

                if (receiptHandles.Count > 0)
                    await _sqsClient.DeleteMessageBatchAsync(new DeleteMessageBatchRequest
                    {
                        QueueUrl = _responseQueueUrl,
                        Entries = receiptHandles.Select((handle, index) => new DeleteMessageBatchRequestEntry
                        {
                            Id = index.ToString(),
                            ReceiptHandle = handle
                        }).ToList()
                    }, ct);
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                // Log error and continue
                await Task.Delay(1000, ct);
            }
        }
    }

    private readonly ConcurrentDictionary<string, string> _queueUrlCache = new();

    private async Task<string> GetOrCreateQueueUrlAsync(string queueName, CancellationToken cancellationToken)
    {
        if (_queueUrlCache.TryGetValue(queueName, out var cachedUrl)) return cachedUrl;

        try
        {
            var response = await _sqsClient.GetQueueUrlAsync(queueName, cancellationToken);
            _queueUrlCache.TryAdd(queueName, response.QueueUrl);
            return response.QueueUrl;
        }
        catch (QueueDoesNotExistException)
        {
            // Queue doesn't exist yet, will be created by server
            throw new OfXException.ReceivedException(
                $"SQS queue '{queueName}' does not exist. Ensure the server is running.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _receiverCts?.Cancel();

        try
        {
            // Delete response queue on cleanup
            if (_sqsClient != null && !string.IsNullOrEmpty(_responseQueueUrl))
            {
                await _sqsClient.DeleteQueueAsync(_responseQueueUrl);
            }
        }
        catch
        {
            // Ignore errors during cleanup
        }

        _sqsClient?.Dispose();
        _initLock.Dispose();
        _receiverCts?.Dispose();
    }
}