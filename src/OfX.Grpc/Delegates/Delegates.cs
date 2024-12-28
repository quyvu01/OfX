using OfX.Abstractions;
using OfX.Queries.OfXQueries;
using OfX.Responses;

namespace OfX.Grpc.Delegates;

public delegate Func<GetDataMappableQuery, IContext, Task<ItemsResponse<OfXDataResponse>>> GetOfXResponseFunc(Type attributeType);