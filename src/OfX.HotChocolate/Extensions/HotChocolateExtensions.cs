using Microsoft.Extensions.DependencyInjection;
using OfX.HotChocolate.Abstractions;
using OfX.HotChocolate.ApplicationModels;
using OfX.HotChocolate.Implementations;
using OfX.Wrappers;

namespace OfX.HotChocolate.Extensions;

public static class HotChocolateExtensions
{
    public static OfXRegisterWrapped AddHotChocolate(this OfXRegisterWrapped ofXServiceInjector,
        Action<OfXHotChocolateRegister> action)
    {
        var hotChocolateRegister = new OfXHotChocolateRegister();
        action.Invoke(hotChocolateRegister);
        
        ofXServiceInjector.OfXRegister.ServiceCollection
            .AddSingleton<ICurrentContextProvider, CurrentContextProvider>();
        return ofXServiceInjector;
    }
}