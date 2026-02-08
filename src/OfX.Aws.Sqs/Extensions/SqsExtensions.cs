using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions.Transporting;
using OfX.Aws.Sqs.Abstractions;
using OfX.Aws.Sqs.ApplicationModels;
using OfX.Aws.Sqs.BackgroundServices;
using OfX.Aws.Sqs.Implementations;
using OfX.Registries;
using OfX.Statics;
using OfX.Supervision;

namespace OfX.Aws.Sqs.Extensions;

public static class SqsExtensions
{
    public static void AddSqs(this OfXRegister ofXRegister, Action<SqsConfigurator> options)
    {
        var config = new SqsConfigurator();
        options.Invoke(config);
        var services = ofXRegister.ServiceCollection;
        services.AddSingleton<ISqsServer, SqsServer>();
        services.AddSingleton<IRequestClient, SqsRequestClient>();

        // Register supervisor options: global > default
        var supervisorOptions = OfXStatics.SupervisorOptions ?? new SupervisorOptions();
        services.AddSingleton(supervisorOptions);

        // Use SqsSupervisorWorker with supervisor pattern
        services.AddHostedService<SqsSupervisorWorker>();
    }
}
