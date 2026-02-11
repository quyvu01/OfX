using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.EntityFrameworkCore.Exceptions;
using OfX.EntityFrameworkCore.Implementations;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Configuration;

namespace OfX.EntityFrameworkCore.Registration;

/// <summary>
/// Configuration class for registering Entity Framework Core DbContexts with the OfX framework.
/// </summary>
/// <param name="serviceCollection">The service collection for dependency injection registration.</param>
/// <remarks>
/// This registrar supports multiple DbContexts, which is useful for applications with
/// multiple databases or bounded contexts. OfX will automatically route queries to
/// the correct DbContext based on which one contains the target entity type.
/// </remarks>
public sealed class OfXEfCoreRegistrar(IServiceCollection serviceCollection)
{
    private static readonly Dictionary<Type, string> DbContextMapFunction = [];

    /// <summary>
    /// Registers one or more DbContext types for use with OfX queries.
    /// </summary>
    /// <param name="dbContextType">The primary DbContext type to register.</param>
    /// <param name="otherDbContextTypes">Additional DbContext types to register.</param>
    /// <exception cref="OfXEntityFrameworkException.DbContextsMustNotBeEmpty">
    /// Thrown when no DbContext types are provided.
    /// </exception>
    /// <exception cref="OfXEntityFrameworkException.DbContextTypeHasBeenRegisterBefore">
    /// Thrown when a DbContext type has already been registered.
    /// </exception>
    /// <example>
    /// <code>
    /// .AddOfXEFCore(cfg =>
    /// {
    ///     cfg.AddDbContexts(typeof(ApplicationDbContext), typeof(ReportingDbContext));
    /// });
    /// </code>
    /// </example>
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