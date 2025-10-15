using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.EntityFrameworkCore.Exceptions;
using OfX.EntityFrameworkCore.Implementations;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Statics;

namespace OfX.EntityFrameworkCore.ApplicationModels;

public sealed class OfXEfCoreRegistrar(IServiceCollection serviceCollection)
{
    private static readonly Dictionary<Type, string> DbContextMapFunction = [];

    public void AddDbContexts(Type dbContextType, params Type[] otherDbContextTypes)
    {
        List<Type> dbContextTypes = [dbContextType, ..otherDbContextTypes ?? []];
        if (dbContextTypes.Count == 0)
            throw new OfXEntityFrameworkException.DbContextsMustNotBeEmpty();

        if (OfXStatics.ModelConfigurationAssembly is null)
            throw new OfXException.ModelConfigurationMustBeSet();

        dbContextTypes.Distinct().ForEach(type =>
        {
            ArgumentNullException.ThrowIfNull(type);
            if (!DbContextMapFunction.TryAdd(type, nameof(AddDbContexts)))
                throw new OfXEntityFrameworkException.DbContextTypeHasBeenRegisterBefore(type);
            serviceCollection.AddScoped<IDbContext>(sp => sp.GetService(type) is DbContext context
                ? new DbContextInternal(context)
                : throw new OfXEntityFrameworkException.EntityFrameworkDbContextNotRegister());
        });
    }
}