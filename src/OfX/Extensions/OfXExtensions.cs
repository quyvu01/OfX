using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Implementations;
using OfX.Registries;

namespace OfX.Extensions;

public static class OfXExtensions
{
    public static OfXServiceInjector AddOfX(this IServiceCollection serviceCollection, Action<OfXRegister> action)
    {
        var newOfRegister = new OfXRegister();
        action.Invoke(newOfRegister);
        serviceCollection.AddSingleton<IDataMappableService>(sp =>
            new DataMappableService(sp, newOfRegister.ContractsRegister));

        var targetInterface = typeof(IMappableRequestHandler<,>);
        newOfRegister.HandlersRegister
            .SelectMany(a => a.ExportedTypes)
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == targetInterface))
            .ForEach(handler => handler.GetInterfaces().Where(i => i.GetGenericTypeDefinition() == targetInterface)
                .ForEach(i =>
                {
                    var args = i.GetGenericArguments();
                    var parentType = targetInterface.MakeGenericType(args);
                    serviceCollection.TryAddScoped(parentType, handler);
                }));
        return new OfXServiceInjector(serviceCollection);
    }
}