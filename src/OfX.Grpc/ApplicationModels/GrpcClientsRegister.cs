using OfX.Extensions;

namespace OfX.Grpc.ApplicationModels;

/// <summary>
/// Configuration class for registering gRPC server hosts for OfX client communication.
/// </summary>
/// <remarks>
/// Use this registrar to specify the gRPC server addresses that the client will
/// communicate with to fetch data across microservices.
/// </remarks>
public class GrpcClientsRegister
{
    private readonly List<string> _serverHost = [];

    /// <summary>
    /// Gets the collection of registered service host addresses.
    /// </summary>
    public IReadOnlyCollection<string> ServiceHosts => _serverHost;

    /// <summary>
    /// Adds multiple gRPC server hosts.
    /// </summary>
    /// <param name="serviceHosts">The gRPC server URLs to add.</param>
    /// <example>
    /// <code>
    /// register.AddGrpcHosts("https://service1:5001", "https://service2:5002");
    /// </code>
    /// </example>
    public void AddGrpcHosts(params string[] serviceHosts)
    {
        ArgumentNullException.ThrowIfNull(serviceHosts);
        serviceHosts.Where(a => !_serverHost.Contains(a)).ForEach(a => _serverHost.Add(a));
    }

    /// <summary>
    /// Adds a single gRPC server host.
    /// </summary>
    /// <param name="serviceHost">The gRPC server URL to add.</param>
    /// <example>
    /// <code>
    /// register.AddGrpcHosts("https://users-service:5001");
    /// </code>
    /// </example>
    public void AddGrpcHosts(string serviceHost)
    {
        ArgumentNullException.ThrowIfNull(serviceHost);
        if (!_serverHost.Contains(serviceHost)) _serverHost.Add(serviceHost);
    }
}