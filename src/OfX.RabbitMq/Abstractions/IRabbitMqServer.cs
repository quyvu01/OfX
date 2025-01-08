namespace OfX.RabbitMq.Abstractions;

internal interface IRabbitMqServer
{
    Task ConsumeAsync();
}