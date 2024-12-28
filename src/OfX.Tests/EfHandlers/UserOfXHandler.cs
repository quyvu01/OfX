using System.Linq.Expressions;
using OfX.Abstractions;
using OfX.EntityFrameworkCore;
using OfX.Responses;
using OfX.Tests.Attributes;
using OfX.Tests.Models;

namespace OfX.Tests.EfHandlers;

public class UserOfXHandler(IServiceProvider serviceProvider)
    : EfQueryOfXHandler<User, UserOfAttribute>(serviceProvider)
{
    protected override Func<DataMappableOf<UserOfAttribute>, Expression<Func<User, bool>>> SetFilter() =>
        q => c => q.SelectorIds.Contains(c.Id);

    protected override Expression<Func<User, OfXDataResponse>> SetHowToGetDefaultData() =>
        u => new OfXDataResponse { Id = u.Id, Value = u.Name };
}