using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.EntityFrameworkCore.ApplicationModels;
using OfX.EntityFrameworkCore.Delegates;
using OfX.EntityFrameworkCore.Exceptions;
using OfX.EntityFrameworkCore.Implementations;
using OfX.EntityFrameworkCore.Services;
using OfX.EntityFrameworkCore.Statics;
using OfX.Extensions;
using OfX.Registries;

namespace OfX.EntityFrameworkCore.Extensions;

public static class EntityFrameworkExtensions
{
    private static readonly Lazy<ConcurrentDictionary<Type, Type>> modelTypeLookUp = new(() => []);
    private static readonly Type baseGenericType = typeof(EfQueryOfHandler<,>);
    private static readonly Type interfaceGenericType = typeof(IQueryOfHandler<,>);

    public static OfXRegister AddOfXEFCore(this OfXRegister ofXServiceInjector,
        Action<OfXEfCoreRegistrar> registrarAction)
    {
        var newOfXEfCoreRegistrar = new OfXEfCoreRegistrar();
        registrarAction.Invoke(newOfXEfCoreRegistrar);
        var dbContextTypes = EntityFrameworkCoreStatics.DbContextTypes;
        if (dbContextTypes.Count == 0)
            throw new OfXEntityFrameworkException.DbContextsMustNotBeEmpty();
        var serviceCollection = ofXServiceInjector.ServiceCollection;
        dbContextTypes.ForEach(dbContextType =>
        {
            serviceCollection.AddScoped(sp =>
            {
                if (sp.GetService(dbContextType) is not DbContext dbContext)
                    throw new OfXEntityFrameworkException.EntityFrameworkDbContextNotRegister(
                        "DbContext must be registered first!");
                return (IOfXEfDbContext)Activator.CreateInstance(
                    typeof(EfDbContextWrapped<>).MakeGenericType(dbContextType), dbContext);
            });
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

    private static void AddEfQueryOfXHandlers(OfXRegister ofXRegister)
    {
        var modelsHasOfXConfig = EntityFrameworkCoreStatics.ModelConfigurationAssembly
            .ExportedTypes
            .Where(a => a is { IsClass: true, IsAbstract: false, IsInterface: false })
            .Where(a => a.GetCustomAttributes().Any(x =>
            {
                var attributeType = x.GetType();
                return attributeType.IsGenericType &&
                       attributeType.GetGenericTypeDefinition() == typeof(OfXConfigForAttribute<>);
            })).Select(a =>
            {
                var attributes = a.GetCustomAttributes();
                var configAttribute = attributes.Select(x =>
                {
                    var attributeType = x.GetType();
                    if (!attributeType.IsGenericType) return (null, null);
                    if (attributeType.GetGenericTypeDefinition() != typeof(OfXConfigForAttribute<>))
                        return (null, null);
                    return (OfXConfigAttribute: x, OfXAttribute: attributeType.GetGenericArguments()[0]);
                }).First(x => x is { OfXConfigAttribute: not null, OfXAttribute: not null });
                return (ModelType: a, OfXAttributeData: configAttribute.OfXConfigAttribute as IOfXConfigAttribute,
                    configAttribute.OfXAttribute);
            });

        var assemblyName = new AssemblyName("EfQueryOfHandlerModule");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("EfQueryOfHandlerModule");

        modelsHasOfXConfig.ForEach(m =>
        {
            var baseType = baseGenericType.MakeGenericType(m.ModelType, m.OfXAttribute);
            var typeBuilder = moduleBuilder.DefineType($"{m.ModelType.Name}EfQueryOfHandler",
                TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Class, baseType);

            // Define the constructor
            var baseCtor = baseType.GetConstructor([typeof(IServiceProvider), typeof(string), typeof(string)])!;
            var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                [typeof(IServiceProvider)]);
            var ctorIL = ctorBuilder.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0); // Load "this"
            ctorIL.Emit(OpCodes.Ldarg_1); // Load IServiceProvider argument
            ctorIL.Emit(OpCodes.Ldstr, m.OfXAttributeData.IdProperty); // Load "IdProperty" string argument
            ctorIL.Emit(OpCodes.Ldstr, m.OfXAttributeData.DefaultProperty); // Load "DefaultProperty" string argument
            ctorIL.Emit(OpCodes.Call, baseCtor); // Call the base constructor
            ctorIL.Emit(OpCodes.Ret);

            // Create the dynamic type
            var dynamicType = typeBuilder.CreateType();
            var parentType = interfaceGenericType.MakeGenericType(m.ModelType, m.OfXAttribute);
            var extensionHandlerInstaller = new EfCoreExtensionHandlersInstaller(ofXRegister.ServiceCollection);
            extensionHandlerInstaller.AddExtensionHandler(parentType, dynamicType, m.OfXAttribute);
        });
    }
}