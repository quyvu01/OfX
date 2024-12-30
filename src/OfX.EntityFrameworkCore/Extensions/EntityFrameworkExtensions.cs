using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.EntityFrameworkCore.Delegates;
using OfX.EntityFrameworkCore.Exceptions;
using OfX.EntityFrameworkCore.Services;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Registries;
using OfX.Statics;

namespace OfX.EntityFrameworkCore.Extensions;

public static class EntityFrameworkExtensions
{
    private static readonly Lazy<ConcurrentDictionary<Type, Type>> modelTypeLookUp = new(() => []);

    public static OfXServiceInjector AddOfXEFCore<TDbContext>(
        this OfXServiceInjector ofXServiceInjector) where TDbContext : DbContext
    {
        var serviceCollection = ofXServiceInjector.OfXRegister.ServiceCollection;
        serviceCollection.AddScoped<IOfXEfDbContext>(sp =>
        {
            var dbContext = sp.GetService<TDbContext>();
            if (dbContext is null)
                throw new OfXEntityFrameworkException.EntityFrameworkDbContextNotRegister(
                    "DbContext must be registered first!");
            return new EfDbContextWrapped<TDbContext>(dbContext);
        });
        serviceCollection.AddScoped<GetEfDbContext>(sp => modelType =>
        {
            if (modelTypeLookUp.Value.TryGetValue(modelType, out var serviceType))
                return sp.GetServices<IOfXEfDbContext>().First(a => a.GetType() == serviceType);
            var contexts = sp.GetServices<IOfXEfDbContext>();
            var matchingServiceType = contexts.FirstOrDefault(a => a.HasCollection(modelType));
            if (matchingServiceType is null)
                throw new OfXEntityFrameworkException.ThereAreNoDbContextHasModel(modelType);
            modelTypeLookUp.Value.TryAdd(modelType, matchingServiceType.GetType());
            return matchingServiceType;
        });
        AddEfQueryOfXHandlers(ofXServiceInjector);
        return ofXServiceInjector;
    }

    private static void AddEfQueryOfXHandlers(OfXServiceInjector serviceInjector)
    {
        if (serviceInjector.OfXRegister.HandlersRegister is null) return;
        var baseType = typeof(EfQueryOfXHandler<,>);
        var interfaceType = typeof(IQueryOfHandler<,>);
        serviceInjector.OfXRegister.HandlersRegister.ExportedTypes
            .Where(t =>
            {
                var basedType = t.BaseType;
                if (basedType is null || !basedType.IsGenericType) return false;
                return t is { IsClass: true, IsAbstract: false } && basedType.GetGenericTypeDefinition() == baseType;
            })
            .ForEach(handlerType =>
            {
                var args = handlerType.BaseType!.GetGenericArguments();
                var parentType = interfaceType.MakeGenericType(args);
                if (!OfXStatics.InternalQueryMapHandler.TryAdd(args[1], parentType))
                    throw new OfXException.RequestMustNotBeAddMoreThanOneTimes();
                serviceInjector.OfXRegister.ServiceCollection.TryAddScoped(parentType, handlerType);
            });
    }
}