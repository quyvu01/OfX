using OfX.RabbitMq.Statics;

namespace OfX.RabbitMq.ApplicationModels;

public sealed class RabbitMqCredential
{
    public void UserName(string userName) => RabbitMqStatics.RabbitMqUserName = userName;
    public void Password(string password) => RabbitMqStatics.RabbitMqPassword = password;
}