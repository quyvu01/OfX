namespace OfX.RabbitMq.ApplicationModels;

public sealed class RabbitMqCredential
{
    public string RabbitMqUserName { get; private set; }
    public string RabbitMqPassword { get; private set; }
    public void UserName(string userName) => RabbitMqUserName = userName;
    public void Password(string password) => RabbitMqPassword = password;
}