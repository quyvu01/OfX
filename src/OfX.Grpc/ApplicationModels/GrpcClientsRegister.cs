using OfX.Extensions;

namespace OfX.Grpc.ApplicationModels;

public class GrpcClientsRegister
{
    private readonly List<string> _serverHost = [];
    public IReadOnlyCollection<string> ServiceHosts => _serverHost;

    public void AddGrpcHosts(params string[] serviceHosts)
    {
        ArgumentNullException.ThrowIfNull(serviceHosts);
        serviceHosts.Where(a => !_serverHost.Contains(a)).ForEach(a => _serverHost.Add(a));
    }

    public void AddGrpcHosts(string serviceHost)
    {
        ArgumentNullException.ThrowIfNull(serviceHost);
        if (!_serverHost.Contains(serviceHost)) _serverHost.Add(serviceHost);
    }
}