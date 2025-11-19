using System.Security.Claims;
using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KahveDostum_Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendsController(IFriendService friendService) : ControllerBase
{
    private readonly IFriendService _friendService = friendService;

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(idStr) || !int.TryParse(idStr, out var id))
            throw new InvalidOperationException("Kullanıcı kimliği belirlenemedi.");
        return id;
    }

    [HttpPost("requests")]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDto request)
    {
        var userId = GetCurrentUserId();
        await _friendService.SendFriendRequestAsync(userId, request);
        return Ok(new { message = "Arkadaşlık isteği gönderildi." });
    }

    [HttpPost("requests/{requestId:int}/cancel")]
    public async Task<IActionResult> CancelRequest(int requestId)
    {
        var userId = GetCurrentUserId();
        await _friendService.CancelFriendRequestAsync(userId, requestId);
        return Ok(new { message = "Arkadaşlık isteği iptal edildi." });
    }

    [HttpPost("requests/{requestId:int}/respond")]
    public async Task<IActionResult> RespondToRequest(
        int requestId,
        [FromBody] RespondFriendRequestDto request)
    {
        var userId = GetCurrentUserId();
        await _friendService.RespondToFriendRequestAsync(userId, requestId, request.Accept);
        return Ok(new { message = request.Accept ? "İstek kabul edildi." : "İstek reddedildi." });
    }

    [HttpGet("requests/incoming")]
    public async Task<IActionResult> GetIncomingRequests()
    {
        var userId = GetCurrentUserId();
        var list = await _friendService.GetIncomingRequestsAsync(userId);
        return Ok(list);
    }

    [HttpGet("requests/outgoing")]
    public async Task<IActionResult> GetOutgoingRequests()
    {
        var userId = GetCurrentUserId();
        var list = await _friendService.GetOutgoingRequestsAsync(userId);
        return Ok(list);
    }

    [HttpGet]
    public async Task<IActionResult> GetFriends()
    {
        var userId = GetCurrentUserId();
        var list = await _friendService.GetFriendsAsync(userId);
        return Ok(list);
    }

    [HttpDelete("{friendUserId:int}")]
    public async Task<IActionResult> RemoveFriend(int friendUserId)
    {
        var userId = GetCurrentUserId();
        await _friendService.RemoveFriendAsync(userId, friendUserId);
        return Ok(new { message = "Arkadaşlıktan çıkarıldı." });
    }
}
