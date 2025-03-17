using OfX.HotChocolate.GraphqlContexts;

namespace OfX.HotChocolate.Abstractions;

internal interface ICurrentContextProvider
{
    FieldContext CreateContext();
    FieldContext GetContext();
}