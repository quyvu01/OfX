using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using OfX.Abstractions;
using OfX.Abstractions.Transporting;
using OfX.Attributes;
using OfX.Constants;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.RabbitMq.Constants;
using OfX.RabbitMq.Extensions;
using OfX.RabbitMq.Statics;
using OfX.Responses;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OfX.RabbitMq.Implementations;

internal class RabbitMqRequestClient : IRequestClient, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<BasicDeliverEventArgs>> _eventArgsMapper = new();
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IConnection _connection;
    private IChannel _channel;
    private AsyncEventingBasicConsumer _consumer;
    private string _replyQueueName;
    private bool _initialized;
    private const string RoutingKey = OfXRabbitMqConstants.RoutingKey;

    public async Task<ItemsResponse<DataResponse>> RequestAsync<TAttribute>(
        RequestContext<TAttribute> requestContext) where TAttribute : OfXAttribute
    {
        // Lazy initialization - thread-safe
        await EnsureInitializedAsync(requestContext.CancellationToken);

        if (_channel is null) throw new InvalidOperationException("RabbitMQ channel is not initialized");

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

        var tcs = new TaskCompletionSource<BasicDeliverEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        _eventArgsMapper.TryAdd(correlationId, tcs);

        try
        {
            var messageSerialize = JsonSerializer.Serialize(requestContext.Query);
            var messageBytes = Encoding.UTF8.GetBytes(messageSerialize);
            await _channel.BasicPublishAsync(exchangeName, routingKey: RoutingKey,
                mandatory: true, basicProperties: props, body: messageBytes, cancellationToken: cancellationToken);

            // Wait with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(OfXConstants.DefaultRequestTimeout);

            await using var _ = cts.Token.Register(() => tcs.TrySetCanceled());

            var eventArgs = await tcs.Task;
            var resultAsString = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            var response = JsonSerializer.Deserialize<Result>(resultAsString);

            if (response is null)
                throw new OfXException.ReceivedException("Received null response from server");

            if (!response.IsSuccess)
            {
                var errorMessage = response.Fault?.Exceptions?.FirstOrDefault()?.Message
                                   ?? "Unknown error from server";
                throw new OfXException.ReceivedException(errorMessage);
            }

            return response.Data;
        }
        finally
        {
            // Cleanup on any exit path
            _eventArgsMapper.TryRemove(correlationId, out _);
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
        var queueDeclareResult = await _channel.QueueDeclareAsync(cancellationToken: cancellationToken);
        _replyQueueName = queueDeclareResult.QueueName;
        _consumer = new AsyncEventingBasicConsumer(_channel);
        _consumer.ReceivedAsync += (_, ea) =>
        {
            var correlationId = ea.BasicProperties.CorrelationId;
            if (string.IsNullOrEmpty(correlationId) || !_eventArgsMapper.TryRemove(correlationId, out var tcs))
                return Task.CompletedTask;
            tcs.TrySetResult(ea);
            return Task.CompletedTask;
        };
        await _channel.BasicConsumeAsync(_replyQueueName, true, _consumer, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.CloseAsync();
        if (_connection is not null) await _connection.CloseAsync();
        _initLock.Dispose();
    }
}