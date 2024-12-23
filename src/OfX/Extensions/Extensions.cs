using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Implementations;

namespace OfX.Extensions;

public static class Extensions
{
    public static void ForEach<T>(this IEnumerable<T> src, Action<T> action)
    {
        if (src is null) return;
        foreach (var item in src) action?.Invoke(item);
    }

    public static void ForEach<T>(this IEnumerable<T> src, Action<T, int> action)
    {
        if (src is null) return;
        foreach (var item in src.Select((value, index) => (value, index))) action?.Invoke(item.value, item.index);
    }

    public static void IteratorVoid<T>(this IEnumerable<T> src) => src.ForEach(_ => { });

    public static void AddOfX(this IServiceCollection serviceCollection, IEnumerable<Assembly> contractAssemblies,
        IEnumerable<Assembly> handlerAssemblies)
    {
        serviceCollection.AddSingleton<IDataMappableService>(sp => new DataMappableService(sp, contractAssemblies));

        var targetInterface = typeof(IMappableRequestHandler<,>);
        handlerAssemblies
            .SelectMany(a => a.ExportedTypes)
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == targetInterface))
            .ForEach(handler =>
            {
                //Todo: Update later on finding the matching type!
                var args = handler.GetInterfaces().FirstOrDefault().GetGenericArguments();
                var parentType = targetInterface.MakeGenericType(args);
                serviceCollection.TryAddScoped(parentType, handler);
            });
    }
}