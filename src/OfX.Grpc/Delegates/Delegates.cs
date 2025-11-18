using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Responses;

namespace OfX.Grpc.Delegates;

public delegate Func<OfXRequest, IContext, Task<ItemsResponse<OfXDataResponse>>> GetOfXResponseFunc(
    Type attributeType);