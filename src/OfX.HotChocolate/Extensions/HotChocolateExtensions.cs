using OfX.HotChocolate.ApplicationModels;
using OfX.Wrappers;

namespace OfX.HotChocolate.Extensions;

public static class HotChocolateExtensions
{
    public static OfXRegisterWrapped AddHotChocolate(this OfXRegisterWrapped ofXServiceInjector,
        Action<OfXHotChocolateRegister> action)
    {
        var hotChocolateRegister = new OfXHotChocolateRegister();
        action.Invoke(hotChocolateRegister);
        return ofXServiceInjector;
    }
}