using OfX.HotChocolate.GraphqlContexts;

namespace OfX.HotChocolate.Abstractions;

internal interface ICurrentContextProvider
{
    CurrentFieldContext CreateContext();
    CurrentFieldContext GetContext();
}