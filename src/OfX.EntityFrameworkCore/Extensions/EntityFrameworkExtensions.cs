using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.EntityFrameworkCore.Exceptions;
using OfX.EntityFrameworkCore.Services;
using OfX.Extensions;
using OfX.Registries;

namespace OfX.EntityFrameworkCore.Extensions;

public static class EntityFrameworkExtensions
{
    public static OfXServiceInjector RegisterOfXEntityFramework<TDbContext, TAssemblyMarker>(
        this OfXServiceInjector ofXServiceInjector) where TDbContext : DbContext
    {
        var serviceCollection = ofXServiceInjector.Collection;
        serviceCollection.TryAddScoped<IOfXModel>(sp =>
        {
            var dbContext = sp.GetService<TDbContext>();
            if (dbContext is null)
                throw new OfXEntityFrameworkException.EntityFrameworkDbContextNotRegister(
                    "DbContext must be registered first!");
            return new EntityFrameworkModelWrapped(dbContext);
        });

        var targetInterface = typeof(IQueryOfHandler<,>);

        typeof(TAssemblyMarker).Assembly.ExportedTypes
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == targetInterface))
            .ForEach(handler => handler.GetInterfaces().Where(i => i.GetGenericTypeDefinition() == targetInterface)
                .ForEach(i =>
                {
                    var args = i.GetGenericArguments();
                    var parentType = targetInterface.MakeGenericType(args);
                    serviceCollection.TryAddScoped(parentType, handler);
                }));
        return ofXServiceInjector;
    }
}