using OfX.HotChocolate.GraphQlContext;
using OfX.ObjectContexts;

namespace OfX.HotChocolate.Abstractions;

internal interface ICurrentContextProvider
{
    FieldContext CreateContext();
    FieldContext GetContext();
}