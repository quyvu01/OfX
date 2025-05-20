using OfX.HotChocolate.GraphQlContext;

namespace OfX.HotChocolate.Abstractions;

internal interface ICurrentContextProvider
{
    FieldContext CreateContext();
    FieldContext GetContext();
}