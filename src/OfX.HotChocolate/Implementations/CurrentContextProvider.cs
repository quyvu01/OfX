using OfX.HotChocolate.Abstractions;
using OfX.HotChocolate.GraphqlContexts;

namespace OfX.HotChocolate.Implementations;

internal class CurrentContextProvider : ICurrentContextProvider
{
    private static readonly AsyncLocal<CurrentFieldContext> AsyncLocal = new();

    public CurrentFieldContext CreateContext()
    {
        AsyncLocal.Value = new CurrentFieldContext();
        return AsyncLocal.Value;
    }

    public CurrentFieldContext GetContext() => AsyncLocal.Value;
}