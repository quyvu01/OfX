using OfX.Abstractions;

namespace OfX.ApplicationModels;

public sealed class StronglyTypeIdRegister
{
    public List<Type> StronglyTypeConfigurations { get; } = [];

    public StronglyTypeIdRegister ForType<T>() where T : IStronglyTypeConverter
    {
        StronglyTypeConfigurations.Add(typeof(T));
        return this;
    }
}