using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Extensions;
using OfX.Helpers;
using OfX.RabbitMq.Abstractions;
using OfX.RabbitMq.Constants;
using OfX.RabbitMq.Wrappers;
using OfX.Responses;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OfX.RabbitMq.Implementations;

public class RabbitMqClient(RabbitMqClientWrapper rabbitMqClientWrapper) : IRabbitMqClient, IAsyncDisposable
{
    private const string QueueName = OfXConstants.QueueName;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _callbackMapper = new();

    private IConnection? _connection;
    private IChannel? _channel;
    private string? _replyQueueName;

    public async Task StartAsync()
    {
        _connection = await rabbitMqClientWrapper.ConnectionFactory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        // declare a server-named queue
        var queueDeclareResult = await _channel.QueueDeclareAsync();
        _replyQueueName = queueDeclareResult.QueueName;
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += (_, ea) =>
        {
            var correlationId = ea.BasicProperties.CorrelationId;
            if (string.IsNullOrEmpty(correlationId)) return Task.CompletedTask;
            if (!_callbackMapper.TryRemove(correlationId, out var tcs)) return Task.CompletedTask;
            var body = ea.Body.ToArray();
            tcs.TrySetResult(body);
            return Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync(_replyQueueName, true, consumer);
    }

    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync<TAttribute>(
        RequestContext<TAttribute> requestContext) where TAttribute : OfXAttribute
    {
        if (_channel is null)
            throw new InvalidOperationException();

        var cancellationToken = requestContext.CancellationToken;
        var correlationId = Guid.NewGuid().ToString();
        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            ReplyTo = _replyQueueName,
            MessageId = typeof(TAttribute).GetAssemblyName()
        };
        props.Headers ??= new Dictionary<string, object?>();
        requestContext.Headers?.ForEach(h => props.Headers.Add(h.Key, h.Value));

        var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        _callbackMapper.TryAdd(correlationId, tcs);
        var messageSerialize = JsonSerializer.Serialize(requestContext.Query);
        var messageBytes = Encoding.UTF8.GetBytes(messageSerialize);
        await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: QueueName,
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