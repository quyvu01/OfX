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
    public async Task<IActionResult> GetMembers([FromServices] IDistributedMapper distributedMapper)
    {
        List<MemberResponse> members =
        [
            new() { Id = "1", UserId = "user-001", MemberAdditionalId = "member-001", MemberAddressId = "addr-001", MemberSocialId = "1" },
            new() { Id = "2", UserId = "user-002", MemberAdditionalId = "member-002", MemberAddressId = "addr-002", MemberSocialId = "2" },
            new() { Id = "3", UserId = "user-004", MemberAdditionalId = "member-003", MemberAddressId = "addr-005", MemberSocialId = "3" },
            new() { Id = "4", UserId = "user-013", MemberAdditionalId = "member-005", MemberAddressId = "addr-016", MemberSocialId = "5" },
            new() { Id = "5", UserId = "user-019", MemberAdditionalId = "member-010", MemberAddressId = "addr-022", MemberSocialId = "7" }
        ];
        await distributedMapper.MapDataAsync(members);
        return Ok(members);
    }

    [HttpGet]
    public async Task<IActionResult> GetMembersWithComplexExpression(
        [FromServices] IDistributedMapper distributedMapper)
    {
        List<MemberWitComplexExpressionResponse> members =
        [
            new() { UserId = "user-001" },
            new() { UserId = "user-013" },
            new() { UserId = "user-019" }
        ];
        await distributedMapper.MapDataAsync(members);
        return Ok(members);
    }

    [HttpGet]
    public async Task<IActionResult> FunctionTestsForEf([FromServices] IDistributedMapper distributedMapper,
        string expression)
    {
        var result = await distributedMapper
            .FetchDataAsync<UserOfAttribute>(new DataFetchQuery(["user-001"], [expression]));
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> FunctionTestsForMongoDb([FromServices] IDistributedMapper distributedMapper,
        string expression)
    {
        var result = await distributedMapper
            .FetchDataAsync<MemberSocialOfAttribute>(new DataFetchQuery(["1"], [expression]));
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetSimpleMembers([FromServices] IDistributedMapper distributedMapper,
        string userAlias, CancellationToken token = default)
    {
        List<SimpleMemberResponse> members =
        [
            new() { UserId = "user-001" },
            new() { UserId = "user-013" },
            new() { UserId = "user-019" }
        ];
        await distributedMapper.MapDataAsync(members, new { userAlias }, token);
        return Ok(members);
    }

    [HttpGet]
    public async Task<IActionResult> GetObjectsAsDictionary([FromServices] IDistributedMapper distributedMapper,
        string userAlias, CancellationToken token = default)
    {
        var response = new ComplexObjectAsDictionary
        {
            Responses = new Dictionary<SimpleMemberResponse, MemberResponse>
            {
                { new SimpleMemberResponse { UserId = "user-001" }, new MemberResponse { UserId = "user-001" } },
                { new SimpleMemberResponse { UserId = "user-013" }, new MemberResponse { UserId = "user-013" } }
            }
        };

        await distributedMapper.MapDataAsync(response, new { userAlias }, token);
        return Ok(response.Responses.Values);
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
    public async Task<IActionResult> FetchUsers([FromServices] IDistributedMapper distributedMapper)
    {
        var result = await distributedMapper
            .FetchDataAsync<UserOfAttribute>(new DataFetchQuery(["user-001", "user-013", "user-019"], [null, "Name", "Email"]));
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> NativeDbContextTest([FromServices] OtherService1Context context)
    {
        IEnumerable<string> ids = ["addr-001", "addr-016", "addr-022"];
        Expression<Func<MemberAddress, bool>> filter = x => ids.Contains(x.Id);
        var result = await context.MemberAddresses
            .Where(filter).ToListAsync();
        return Ok(result);
    }
}