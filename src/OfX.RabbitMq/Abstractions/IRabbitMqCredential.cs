namespace OfX.RabbitMq.Abstractions;

public interface IRabbitMqCredential
{
    string UserName { get; set; }
    string Password { get; set; }
}