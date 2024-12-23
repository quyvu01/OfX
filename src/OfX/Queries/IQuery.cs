using OfX.Abstractions;
using OfX.Responses;

namespace OfX.Queries;

public interface IQuery<out TResult> : IMessage where TResult : class;

public interface IQueryCounting : IQuery<CountingResponse>;

public interface IQueryCollection<TResult> : IQuery<CollectionResponse<TResult>> where TResult : class;