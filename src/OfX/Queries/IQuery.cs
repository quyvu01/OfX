using OfX.Abstractions;

namespace OfX.Queries;

public interface IQueryCounting : IMessage;

public interface IQueryCollection<TResult> : IMessage where TResult : class;