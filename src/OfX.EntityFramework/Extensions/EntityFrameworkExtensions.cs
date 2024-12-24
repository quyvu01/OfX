using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.EntityFramework.Abstractions;
using OfX.EntityFramework.Exceptions;
using OfX.EntityFramework.Services;
using OfX.Extensions;

namespace OfX.EntityFramework.Extensions;

public static class EntityFrameworkExtensions
{
    public static void RegisterOfXEntityFramework<TDbContext>(this IServiceCollection serviceCollection,
        params Assembly[] handlerAssemblies) where TDbContext : DbContext
    {
        serviceCollection.TryAddScoped<IOfXModel>(sp =>
        {
            var dbContext = sp.GetService<TDbContext>();
            if (dbContext is null)
                throw new OfXEntityFrameworkException.EntityFrameworkDbContextNotRegister(
                    "DbContext must be registered first!");
            return new EntityFrameworkModelWrapped(dbContext);
        });

        var targetInterface = typeof(IQueryOfHandler<,>);
        
        handlerAssemblies
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
    }
}