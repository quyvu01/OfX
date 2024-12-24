using System.Linq.Expressions;
using OfX.EntityFrameworkCore;
using OfX.Responses;
using OfX.Tests.Contracts;
using OfX.Tests.Models;

namespace OfX.Tests.EfHandlers;

public class UserOfXHandler(IServiceProvider serviceProvider)
    : EfQueryOfXHandler<User, GetCrossCuttingUsersQuery>(serviceProvider)
{
    protected override Func<GetCrossCuttingUsersQuery, Expression<Func<User, bool>>> SetFilter() =>
        q => c => q.SelectorIds.Contains(c.Id);

    protected override Expression<Func<User, OfXDataResponse>> SetHowToGetDefaultData() =>
        u => new OfXDataResponse { Id = u.Id, Value = u.Name };
}