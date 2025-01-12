using OfX.Abstractions;

namespace OfX.ApplicationModels;

public sealed class StronglyTypeIdRegister
{
    public List<Type> StronglyTypeConfigurations { get; } = [];

    [Obsolete("This method ForType<> should be remove in the next version. Please use OfType<> to be family with OfX eco system!")]
    public StronglyTypeIdRegister ForType<T>() where T : IStronglyTypeConverter
    {
        StronglyTypeConfigurations.Add(typeof(T));
        return this;
    }
    public StronglyTypeIdRegister OfType<T>() where T : IStronglyTypeConverter
    {
        StronglyTypeConfigurations.Add(typeof(T));
        return this;
    }
}