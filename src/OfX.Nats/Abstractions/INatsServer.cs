using OfX.Abstractions.Transporting;
using OfX.Attributes;

namespace OfX.Nats.Abstractions;

internal interface INatsServer<TModel, TAttribute> : IRequestServer<TModel, TAttribute>
    where TAttribute : OfXAttribute where TModel : class;
