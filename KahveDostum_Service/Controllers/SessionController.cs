using System.Security.Claims;
using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KahveDostum_Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly ISessionService _service;

    public SessionController(ISessionService service)
    {
        _service = service;
    }

    private int GetUserId()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [Authorize]
    [HttpPost("start")]
    public async Task<IActionResult> StartSession([FromBody] StartSessionRequestDto req)
    {
        var result = await _service.StartSessionAsync(GetUserId(), req.CafeId);
        return Ok(ApiResponse<SessionDto>.SuccessResponse(result, "Oturum başlatıldı."));
    }
}