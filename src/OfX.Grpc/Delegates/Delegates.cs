using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Responses;

namespace OfX.Grpc.Delegates;

public delegate Func<MessageDeserializable, IContext, Task<ItemsResponse<OfXDataResponse>>> GetOfXResponseFunc(
    Type attributeType);