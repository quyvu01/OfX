using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using OfX.Abstractions;
using OfX.Queries;
using Service1.Contexts;
using Service1.Contract.Responses;
using Service1.Models;
using Shared.Attributes;

namespace Service1.Controllers;

[Route("api/[controller]/[action]")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class TestController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMembers([FromServices] IDataMappableService dataMappableService)
    {
        List<MemberResponse> members =
        [
            .. Enumerable.Range(1, 1).Select(a => new MemberResponse
            {
                Id = a.ToString(),
                UserId = a.ToString(), MemberAdditionalId = a.ToString(),
                MemberAddressId = a.ToString(),
                MemberSocialId = a.ToString(),
            })
        ];
        await dataMappableService.MapDataAsync(members);
        return Ok(members);
    }

    [HttpGet]
    public async Task<IActionResult> GetSimpleMembers([FromServices] IDataMappableService dataMappableService,
        string userAlias, CancellationToken token = default)
    {
        List<SimpleMemberResponse> members =
        [
            .. Enumerable.Range(1, 3).Select(a => new SimpleMemberResponse
            {
                UserId = a.ToString()
            })
        ];
        await dataMappableService.MapDataAsync(members, new { userAlias }, token);
        return Ok(members);
    }

    [HttpGet]
    public async Task<IActionResult> GetMemberSocialDynamic()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("Service1MongoDb");
        var memberSocialCollection = database.GetCollection<MemberSocial>("MemberSocials");
        var data = await memberSocialCollection.Aggregate()
            .Project(new BsonDocument
            {
                { "Id", 1 },
                { "Name", 1 },
                {
                    "FirstSortedMetadata", new BsonDocument("$first",
                        new BsonDocument("$sortArray", new BsonDocument
                        {
                            { "input", "$Metadata" },
                            { "sortBy", new BsonDocument("Key", -1) }
                        })
                    )
                },
                {
                    "FirstJustForTest",
                    new BsonDocument("$first",
                        new BsonDocument("$map", new BsonDocument
                        {
                            {
                                "input", new BsonDocument("$sortArray", new BsonDocument
                                {
                                    { "input", "$Metadata" },
                                    { "sortBy", new BsonDocument("Key", -1) }
                                })
                            },
                            { "as", "m" },
                            { "in", "$$m.ExternalOfMetadata.JustForTest" }
                        })
                    )
                }
            })
            .ToListAsync();
        var result = data.Select(x => x.ToJson());
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> FetchUsers([FromServices] IDataMappableService dataMappableService)
    {
        var result = await dataMappableService
            .FetchDataAsync<UserOfAttribute>(new DataFetchQuery(["1", "2", "3"], [null, "Name", "Email"]));
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> NativeDbContextTest([FromServices] OtherService1Context context)
    {
        IEnumerable<string> ids = ["1", "2", "3"];
        Expression<Func<MemberAddress, bool>> filter = x => ids.Contains(x.Id);
        var result = await context.MemberAddresses
            .Where(filter).ToListAsync();
        return Ok(result);
    }
}