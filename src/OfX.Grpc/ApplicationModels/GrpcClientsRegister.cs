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
    private readonly HashSet<string> _serverHost = [];

    /// <summary>
    /// Gets the collection of registered service host addresses.
    /// </summary>
    public IReadOnlyCollection<string> ServiceHosts => _serverHost;

    /// <summary>
    /// Adds multiple gRPC server hosts.
    /// </summary>
    /// <param name="serviceHost">The first gRPC server host</param>
    /// <param name="serviceHosts">The gRPC server host URLs to add.</param>
    /// <example>
    /// <code>
    /// register.AddGrpcHosts("https://service1", "https://service2");
    /// </code>
    /// </example>
    public void AddGrpcHosts(string serviceHost, params string[] serviceHosts)
    {
        ArgumentNullException.ThrowIfNull(serviceHosts);
        string[] hosts = [serviceHost, ..serviceHosts];
        hosts.ForEach(a => _serverHost.Add(a));
    }
}