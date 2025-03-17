using OfX.HotChocolate.Abstractions;
using OfX.HotChocolate.GraphqlContexts;

namespace OfX.HotChocolate.Implementations;

internal class CurrentContextProvider : ICurrentContextProvider
{
    private static readonly AsyncLocal<FieldContext> AsyncLocal = new();

    public FieldContext CreateContext()
    {
        AsyncLocal.Value = new FieldContext();
        return AsyncLocal.Value;
    }

    public FieldContext GetContext() => AsyncLocal.Value;
}