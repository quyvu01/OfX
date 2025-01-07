namespace OfX.RabbitMq.Abstractions;

public interface IRabbitMqServer
{
    Task ConsumeAsync();
}