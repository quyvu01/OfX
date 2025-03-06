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
        [
            .. Enumerable.Range(1, 3).Select(a => new MemberResponse
            {
                Id = a.ToString(), 
                UserId = a.ToString(), MemberAdditionalId = a.ToString(),
                MemberAddressId = a.ToString(),
                MemberSocialId = a.ToString()
            })
        ];
        await dataMappableService.MapDataAsync(members);
        return Ok(members);
    }
}