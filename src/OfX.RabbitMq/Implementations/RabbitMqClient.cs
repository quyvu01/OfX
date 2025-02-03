using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Extensions;
using OfX.Helpers;
using OfX.RabbitMq.Abstractions;
using OfX.RabbitMq.Constants;
using OfX.RabbitMq.Extensions;
using OfX.RabbitMq.Statics;
using OfX.Responses;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OfX.RabbitMq.Implementations;

internal class RabbitMqClient : IRabbitMqClient, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _callbackMapper = new();
    private IConnection _connection;
    private IChannel _channel;
    private AsyncEventingBasicConsumer _consumer;
    private string _replyQueueName;
    private const string routingKey = OfXRabbitMqConstants.RoutingKey;

    // We have to wait this one and ensure that everything is initialized
    public RabbitMqClient() => StartAsync().Wait();

    private async Task StartAsync()
    {
        var userName = RabbitMqStatics.RabbitMqUserName ?? OfXRabbitMqConstants.DefaultUserName;
        var password = RabbitMqStatics.RabbitMqPassword ?? OfXRabbitMqConstants.DefaultPassword;
        var _connectionFactory = new ConnectionFactory
        {
            HostName = RabbitMqStatics.RabbitMqHost, VirtualHost = RabbitMqStatics.RabbitVirtualHost,
            Port = RabbitMqStatics.RabbitMqPort,
            UserName = userName, Password = password
        };

        _connection = await _connectionFactory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
        var queueDeclareResult = await _channel.QueueDeclareAsync();
        _replyQueueName = queueDeclareResult.QueueName;
        _consumer = new AsyncEventingBasicConsumer(_channel);
        _consumer.ReceivedAsync += (_, ea) =>
        {
            var correlationId = ea.BasicProperties.CorrelationId;
            if (string.IsNullOrEmpty(correlationId)) return Task.CompletedTask;
            if (!_callbackMapper.TryRemove(correlationId, out var tcs)) return Task.CompletedTask;
            var body = ea.Body.ToArray();
            tcs.TrySetResult(body);
            return Task.CompletedTask;
        };
        await _channel.BasicConsumeAsync(_replyQueueName, true, _consumer);
    }

    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync<TAttribute>(
        RequestContext<TAttribute> requestContext) where TAttribute : OfXAttribute
    {
        if (_channel is null) throw new InvalidOperationException();
        var exchangeName = typeof(TAttribute).GetExchangeName();
        var cancellationToken = requestContext.CancellationToken;
        var correlationId = Guid.NewGuid().ToString();
        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            ReplyTo = _replyQueueName,
            Type = typeof(TAttribute).GetAssemblyName()
        };
        props.Headers ??= new Dictionary<string, object>();
        requestContext.Headers?.ForEach(h => props.Headers.Add(h.Key, h.Value));

        var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        _callbackMapper.TryAdd(correlationId, tcs);
        var messageSerialize = JsonSerializer.Serialize(requestContext.Query);
        var messageBytes = Encoding.UTF8.GetBytes(messageSerialize);
        await _channel.BasicPublishAsync(exchangeName, routingKey: routingKey,
            mandatory: true, basicProperties: props, body: messageBytes, cancellationToken: cancellationToken);

        await using var ctr = cancellationToken.Register(() =>
        {
            _callbackMapper.TryRemove(correlationId, out _);
            tcs.SetCanceled(cancellationToken);
        });

        var resultAsByte = await tcs.Task;
        var resultAsString = Encoding.UTF8.GetString(resultAsByte);
        return JsonSerializer.Deserialize<ItemsResponse<OfXDataResponse>>(resultAsString)!;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.CloseAsync();
        if (_connection is not null) await _connection.CloseAsync();
    }
}