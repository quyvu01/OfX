using OfX.Abstractions.Transporting;
using OfX.Attributes;

namespace OfX.Kafka.Abstractions;

internal interface IKafkaServer<TModel, TAttribute> : IRequestServer<TModel, TAttribute>
    where TAttribute : OfXAttribute where TModel : class;
