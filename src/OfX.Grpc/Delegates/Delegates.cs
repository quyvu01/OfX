using OfX.Abstractions;
using OfX.Models;
using OfX.Responses;

namespace OfX.Grpc.Delegates;

public delegate Func<OfXRequest, IContext, Task<ItemsResponse<DataResponse>>> GetOfXResponseFunc(Type attributeType);