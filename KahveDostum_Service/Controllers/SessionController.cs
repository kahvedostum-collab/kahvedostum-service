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
    [HttpPost("activate")]
    public async Task<IActionResult> ActivateByToken([FromBody] ActivateSessionByTokenRequestDto req)
    {
        var result = await _service.StartSessionByTokenAsync(GetUserId(), req.Token);
        return Ok(ApiResponse<SessionDto>.SuccessResponse(result, "Aktiflik kazanıldı."));
    }
}