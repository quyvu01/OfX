using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.EntityFrameworkCore.Exceptions;
using OfX.EntityFrameworkCore.Services;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Registries;
using OfX.Statics;

namespace OfX.EntityFrameworkCore.Extensions;

public static class EntityFrameworkExtensions
{
    public static OfXServiceInjector AddOfXEFCore<TDbContext>(
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
        AddOfXHandlers(ofXServiceInjector);
        return ofXServiceInjector;
    }

    private static void AddOfXHandlers(OfXServiceInjector serviceInjector)
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
                serviceInjector.Collection.TryAddScoped(parentType, handlerType);
            });
    }
}