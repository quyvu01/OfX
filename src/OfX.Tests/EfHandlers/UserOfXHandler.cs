using System.Linq.Expressions;
using OfX.EntityFramework;
using OfX.Responses;
using OfX.Tests.Contracts;
using OfX.Tests.Models;

namespace OfX.Tests.EfHandlers;

public class UserOfXHandler(IServiceProvider serviceProvider)
    : EfQueryOfXHandler<User, GetCrossCuttingUsersQuery>(serviceProvider)
{
    protected override (Func<GetCrossCuttingUsersQuery, Expression<Func<User, bool>>>,
        Expression<Func<User, OfXDataResponse>>) SetFilterAndFetchData()
    {
        Expression<Func<User, OfXDataResponse>> howToGetDefaultResponse =
            u => new OfXDataResponse { Id = u.Id, Value = u.Name };
        return (DefaultFilter, howToGetDefaultResponse);
        Expression<Func<User, bool>> DefaultFilter(GetCrossCuttingUsersQuery q) => c => q.SelectorIds.Contains(c.Id);
    }
}