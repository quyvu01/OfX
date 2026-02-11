using OfX.Registries;

namespace OfX.Wrappers;

/// <summary>
/// A wrapper record that encapsulates an <see cref="OfXConfigurator"/> instance.
/// </summary>
/// <remarks>
/// This wrapper is returned by the <c>AddOfX</c> extension method and allows
/// transport extensions (e.g., gRPC, RabbitMQ, NATS) to chain their registration
/// onto the OfX configuration.
/// </remarks>
/// <param name="OfXConfigurator">The underlying OfX registration instance.</param>
/// <example>
/// <code>
/// services.AddOfX(cfg => { /* configuration */ })
///     .AddOfXEFCore();
/// </code>
/// </example>
public sealed record OfXConfiguratorWrapped(OfXConfigurator OfXConfigurator);