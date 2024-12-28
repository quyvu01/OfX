// using OfX.Abstractions;
// using OfX.Responses;
// using OfX.Tests.Attributes;
// using OfX.Tests.Models;
//
// namespace OfX.Tests.Handlers;
//
// public sealed class UserRequestHandler(IQueryOfHandler<User, UserOfAttribute> userQueryOf)
//     : IMappableRequestHandler<UserOfAttribute>
// {
//     public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<UserOfAttribute> context)
//     {
//         var data = await userQueryOf.GetDataAsync(context);
//         return data;
//     }
// }