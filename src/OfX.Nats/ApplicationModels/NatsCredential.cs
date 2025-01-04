namespace OfX.Nats.ApplicationModels;

public class NatsCredential
{
    public string NatsUserName { get; private set; }
    public string NatsPassword { get; private set; }
    public void UserName(string userName) => NatsUserName = userName;
    public void Password(string password) => NatsPassword = password;
}