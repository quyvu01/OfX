using OfX.RabbitMq.Statics;
using RabbitMQ.Client;

namespace OfX.RabbitMq.ApplicationModels;

public sealed class RabbitMqCredential
{
    public void UserName(string userName) => RabbitMqStatics.RabbitMqUserName = userName;
    public void Password(string password) => RabbitMqStatics.RabbitMqPassword = password;
    public void Ssl(Action<SslOption> sslOption)
    {
        var option = new SslOption();
        sslOption.Invoke(option);
        RabbitMqStatics.SslOption = option;
    }
}