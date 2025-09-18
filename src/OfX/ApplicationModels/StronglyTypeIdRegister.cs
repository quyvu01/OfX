using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Exceptions;
using OfX.Extensions;

namespace OfX.ApplicationModels;

public sealed class StronglyTypeIdRegister(IServiceCollection serviceCollection)
{
    private readonly Type _stronglyTypeConverterType = typeof(IStronglyTypeConverter<>);

    public StronglyTypeIdRegister OfType<T>() where T : IStronglyTypeConverter
    {
        var implementedType = typeof(T);
        if (implementedType.IsGenericType)
            throw new OfXException.StronglyTypeConfigurationImplementationMustNotBeGeneric(implementedType);
        implementedType.GetInterfaces().Where(t =>
                t.IsGenericType && t.GetGenericTypeDefinition() == _stronglyTypeConverterType)
            .ForEach(serviceType => serviceCollection.TryAddSingleton(serviceType, implementedType));
        return this;
    }
}