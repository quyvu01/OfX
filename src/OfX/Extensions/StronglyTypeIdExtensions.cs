using OfX.ApplicationModels;
using OfX.Exceptions;
using OfX.Registries;

namespace OfX.Extensions;

public static class StronglyTypeIdExtensions
{
    public static void AddStronglyTypeIdConverter(this OfXRegister ofXRegister, Action<StronglyTypeIdRegister> options)
    {
        if (options is null) throw new OfXException.StronglyTypeConfigurationMustNotBeNull();
        var stronglyTypeIdRegister = new StronglyTypeIdRegister(ofXRegister.ServiceCollection);
        options.Invoke(stronglyTypeIdRegister);
    }
}