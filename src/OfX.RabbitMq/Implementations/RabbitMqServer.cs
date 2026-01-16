using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfX.ApplicationModels;
using OfX.Constants;
using OfX.Exceptions;
using OfX.Implementations;
using OfX.RabbitMq.Abstractions;
using OfX.RabbitMq.Constants;
using OfX.RabbitMq.Extensions;
using OfX.RabbitMq.Statics;
using OfX.Responses;
using OfX.Statics;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OfX.RabbitMq.Implementations;

internal class RabbitMqServer(IServiceProvider serviceProvider) : IRabbitMqServer
{
    private static readonly ConcurrentDictionary<string, Type> AttributeAssemblyCached = new();
    private readonly ILogger<RabbitMqServer> _logger = serviceProvider.GetService<ILogger<RabbitMqServer>>();

    // Backpressure: limit concurrent processing (configurable via OfXRegister.SetMaxConcurrentProcessing)
    private readonly SemaphoreSlim _semaphore = new(OfXStatics.MaxConcurrentProcessing, OfXStatics.MaxConcurrentProcessing);

    private IConnection _connection;
    private IChannel _channel;

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

        // Create timeout CTS
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        cts.CancelAfter(OfXConstants.DefaultRequestTimeout);
        var cancellationToken = cts.Token;

        try
        {
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

            var message = JsonSerializer.Deserialize<OfXRequest>(Encoding.UTF8.GetString(body));
            var headers = props.Headers?
                .ToDictionary(a => a.Key, b => b.Value.ToString()) ?? [];
            var data = await server.ExecuteAsync(message, headers, cancellationToken);
            var response = Result.Success(data);
            var responseAsString = JsonSerializer.Serialize(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseAsString);
            await ch.BasicPublishAsync(exchange: string.Empty, routingKey: props.ReplyTo!,
                mandatory: true, basicProperties: replyProps, body: responseBytes,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.LogWarning("Request timeout for <{Attribute}>", props.Type);
            var response = Result.Failed(new TimeoutException($"Request timeout for {props.Type}"));
            await SendResponseAsync(ch, props.ReplyTo, replyProps, response, cancellationToken);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error while responding <{Attribute}>", props.Type);
            var response = Result.Failed(e);
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
    }
}
