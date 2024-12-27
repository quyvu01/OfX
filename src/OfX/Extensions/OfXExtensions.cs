using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Exceptions;
using OfX.Implementations;
using OfX.Registries;

namespace OfX.Extensions;

public static class OfXExtensions
{
    public static OfXServiceInjector AddOfX(this IServiceCollection serviceCollection, Action<OfXRegister> options)
    {
        var newOfRegister = new OfXRegister(serviceCollection);
        options.Invoke(newOfRegister);
        if (newOfRegister.HandlersRegister is null)
            throw new OfXException.HandlersFromNamespaceShouldBeAdded();
        if (newOfRegister.ContractsRegister is not { Count: > 0 })
            throw new OfXException.ContractsFromNamespaceShouldBeAdded();

        serviceCollection.AddScoped<IDataMappableService>(sp =>
            new DataMappableService(sp, newOfRegister.ContractsRegister));

        var targetInterface = typeof(IMappableRequestHandler<,>);
        newOfRegister.HandlersRegister.ExportedTypes
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == targetInterface))
            .ForEach(handler => handler.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == targetInterface)
                .ForEach(i =>
                {
                    var args = i.GetGenericArguments();
                    var parentType = targetInterface.MakeGenericType(args);
                    serviceCollection.TryAddScoped(parentType, handler);
                }));
        return new OfXServiceInjector(serviceCollection, newOfRegister);
    }
}