using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Exceptions;
using OfX.Registries;

namespace OfX.Extensions;

public static class StronglyTypeIdExtensions
{
    public static void AddStronglyTypeIdConverter(this OfXRegister ofXRegister, Action<StronglyTypeIdRegister> options)
    {
        if (options is null) throw new OfXException.StronglyTypeConfigurationMustNotBeNull();
        var stronglyTypeInterfaceType = typeof(IStronglyTypeConverter<>);
        var stronglyTypeIdRegister = new StronglyTypeIdRegister();
        options.Invoke(stronglyTypeIdRegister);
        stronglyTypeIdRegister.StronglyTypeConfigurations.Select(a =>
        {
            if (a.IsGenericType)
                throw new OfXException.StronglyTypeConfigurationImplementationMustNotBeGeneric(a);
            var interfaceTypes = a.GetInterfaces().Where(t =>
                t.IsGenericType && t.GetGenericTypeDefinition() == stronglyTypeInterfaceType);
            return (ImplementationType: a, ServiceTypes: interfaceTypes);
        }).ForEach(a => a.ServiceTypes.ForEach(s => ofXRegister.ServiceCollection.AddSingleton(s, a)));
    }
}