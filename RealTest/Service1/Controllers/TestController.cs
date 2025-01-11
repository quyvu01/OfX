using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfX.Abstractions;
using Service1.Contract.Responses;

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
            [.. Enumerable.Range(1, 3).Select(a => new MemberResponse { Id = a.ToString(), UserId = a.ToString() })];
        var dicTes = members.ToDictionary(k => k.Id, v => v);
        await dataMappableService.MapDataAsync(dicTes);
        return Ok(dicTes);
    }
}