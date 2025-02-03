using OfX.Abstractions;
using OfX.Statics;

namespace OfX.ApplicationModels;

public sealed class StronglyTypeIdRegister
{
    public StronglyTypeIdRegister OfType<T>() where T : IStronglyTypeConverter
    {
        OfXStatics.StronglyTypeConfigurations.Add(typeof(T));
        return this;
    }
}