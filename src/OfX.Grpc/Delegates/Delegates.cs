using OfX.Abstractions;
using OfX.Queries.CrossCuttingQueries;
using OfX.Responses;

namespace OfX.Grpc.Delegates;

public delegate Func<GetDataMappableQuery, IContext, Task<ItemsResponse<OfXDataResponse>>> GetOfXResponseFunc(Type requestType);