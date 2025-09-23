using Microsoft.Extensions.DependencyInjection;

namespace OfX.Abstractions;

public interface IPipelineRegistration<out TRegistrationPipeline>
{
    TRegistrationPipeline OfType<TPipeline>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped);
    TRegistrationPipeline OfType(Type runtimePipelineType, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped);
}