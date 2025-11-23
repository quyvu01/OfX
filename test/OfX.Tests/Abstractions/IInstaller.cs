using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OfX.Tests.Abstractions;

public interface IInstaller
{
    void InstallerServices(IServiceCollection services, IConfiguration configuration);
}