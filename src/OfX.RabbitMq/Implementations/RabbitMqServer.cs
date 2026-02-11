using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfX.ApplicationModels;
using OfX.Exceptions;
using OfX.Implementations;
using OfX.RabbitMq.Abstractions;
using OfX.RabbitMq.Constants;
using OfX.RabbitMq.Extensions;
using OfX.RabbitMq.Statics;
using OfX.Responses;
using OfX.Configuration;
using OfX.Telemetry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OfX.RabbitMq.Implementations;

internal class RabbitMqServer(IServiceProvider serviceProvider) : IRabbitMqServer
{
    private static readonly ConcurrentDictionary<string, Type> AttributeAssemblyCached = new();
    private readonly ILogger<RabbitMqServer> _logger = serviceProvider.GetService<ILogger<RabbitMqServer>>();

    // Backpressure: limit concurrent processing (configurable via OfXConfigurator.SetMaxConcurrentProcessing)
    private readonly SemaphoreSlim _semaphore = new(OfXStatics.MaxConcurrentProcessing,
        OfXStatics.MaxConcurrentProcessing);

    private IConnection _connection;
    private IChannel _channel;
    private const string TransportName = "rabbitmq";

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var queueName = $"{OfXRabbitMqConstants.QueueNamePrefix}-{AppDomain.CurrentDomain.FriendlyName.ToLower()}";
        const string routingKey = OfXRabbitMqConstants.RoutingKey;

        var userName = RabbitMqStatics.RabbitMqUserName ?? OfXRabbitMqConstants.DefaultUserName;
        var password = RabbitMqStatics.RabbitMqPassword ?? OfXRabbitMqConstants.DefaultPassword;
        var connectionFactory = new ConnectionFactory
        {
            HostName = RabbitMqStatics.RabbitMqHost,
            VirtualHost = RabbitMqStatics.RabbitVirtualHost,
            Port = RabbitMqStatics.RabbitMqPort,
            Ssl = RabbitMqStatics.SslOption ?? new SslOption(),
            UserName = userName,
            Password = password,
            // Enable automatic recovery
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: cancellationToken);

        var attributeTypes = OfXStatics.AttributeMapHandlers.Keys.ToList();
        if (attributeTypes is not { Count: > 0 }) return;

        foreach (var exchangeName in attributeTypes.Select(attributeType => attributeType.GetExchangeName()))
        {
            await _channel.ExchangeDeclareAsync(exchangeName, type: ExchangeType.Direct,
                cancellationToken: cancellationToken);
            await _channel.QueueBindAsync(queue: queueName, exchangeName, routingKey,
                cancellationToken: cancellationToken);
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            // Backpressure - wait for available slot
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await ProcessMessageAsync(sender, ea, cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        };

        await _channel.BasicConsumeAsync(queueName, false, consumer, cancellationToken: cancellationToken);
    }

    private async Task ProcessMessageAsync(object sender, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        var cons = (AsyncEventingBasicConsumer)sender;
        var ch = cons.Channel;
        var body = ea.Body.ToArray();
        var props = ea.BasicProperties;
        var replyProps = new BasicProperties { CorrelationId = props.CorrelationId };

        // Extract parent trace context from headers
        ActivityContext parentContext = default;
        if (props.Headers?.TryGetValue("traceparent", out var traceparent) ?? false)
            ActivityContext.TryParse(Encoding.UTF8.GetString((byte[])traceparent!), null, out parentContext);

        // Parse message to get attribute name
        var message = JsonSerializer.Deserialize<OfXRequest>(Encoding.UTF8.GetString(body));
        var attributeName = props.Type?.Split(',')[0].Split('.').Last() ?? "Unknown";

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
            activity?.SetMessagingTags(system: TransportName, destination: ea.Exchange, messageId: props.CorrelationId,
                operation: "process");

            // Emit diagnostic event
            OfXDiagnostics.MessageReceive(TransportName, ea.Exchange, props.CorrelationId);

            var receivedPipelineOrchestrator = AttributeAssemblyCached.GetOrAdd(props.Type, attributeAssembly =>
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

            var headers = props.Headers?
                .ToDictionary(a => a.Key, b => b.Value.ToString()) ?? [];
            var data = await server.ExecuteAsync(message, headers, cancellationToken);
            var response = Result.Success(data);
            var responseAsString = JsonSerializer.Serialize(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseAsString);
            await ch.BasicPublishAsync(exchange: string.Empty, routingKey: props.ReplyTo!,
                mandatory: true, basicProperties: replyProps, body: responseBytes,
                cancellationToken: cancellationToken);

            // Record success
            stopwatch.Stop();
            var itemCount = data?.Items?.Length ?? 0;

            OfXMetrics.RecordRequest(attributeName, TransportName, stopwatch.Elapsed.TotalMilliseconds, itemCount);

            activity?.SetOfXTags(message?.Expressions, message?.SelectorIds, itemCount);

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger?.LogWarning("Request timeout for <{Attribute}>", props.Type);
            var response = Result.Failed(new TimeoutException($"Request timeout for {props.Type}"));

            // Record timeout as error
            OfXMetrics.RecordError(attributeName, TransportName, stopwatch.Elapsed.TotalMilliseconds,
                "TimeoutException");

            activity?.SetStatus(ActivityStatusCode.Error, "Request timeout");

            await SendResponseAsync(ch, props.ReplyTo, replyProps, response, cancellationToken);
        }
        catch (Exception e)
        {
            stopwatch.Stop();

            _logger?.LogError(e, "Error while responding <{Attribute}>", props.Type);
            var response = Result.Failed(e);

            // Record error
            OfXMetrics.RecordError(attributeName, TransportName, stopwatch.Elapsed.TotalMilliseconds, e.GetType().Name);

            OfXDiagnostics.RequestError(attributeName, TransportName, e, stopwatch.Elapsed);

            activity?.RecordException(e);
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);

            await SendResponseAsync(ch, props.ReplyTo, replyProps, response, stoppingToken);
        }
        finally
        {
            try
            {
                await ch.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to acknowledge message");
            }
        }
    }

    private static async Task SendResponseAsync(IChannel ch, string replyTo, BasicProperties replyProps,
        Result response, CancellationToken cancellationToken)
    {
        try
        {
            var responseAsString = JsonSerializer.Serialize(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseAsString);
            await ch.BasicPublishAsync(exchange: string.Empty, routingKey: replyTo!,
                mandatory: true, basicProperties: replyProps, body: responseBytes,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // Ignore errors when sending error response
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        _semaphore?.Dispose();
    }
}