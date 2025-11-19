using System.Security.Claims;
using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KahveDostum_Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController(IMessageService messageService) : ControllerBase
{
    private readonly IMessageService _messageService = messageService;

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(idStr) || !int.TryParse(idStr, out var id))
            throw new InvalidOperationException("Kullanıcı kimliği belirlenemedi.");
        return id;
    }

    /// <summary>
    /// Arkadaş olan bir kullanıcıya mesaj gönderir.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendMessageRequestDto request)
    {
        var userId = GetCurrentUserId();
        var msg = await _messageService.SendMessageAsync(userId, request);
        return Ok(msg);
    }

    /// <summary>
    /// Kullanıcının tüm sohbetlerini listeler (son mesaj, karşı taraf vb.).
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var userId = GetCurrentUserId();
        var list = await _messageService.GetUserConversationsAsync(userId);
        return Ok(list);
    }

    /// <summary>
    /// Bir sohbetin mesajlarını getirir (paging destekli).
    /// </summary>
    [HttpGet("conversations/{conversationId:int}/messages")]
    public async Task<IActionResult> GetMessages(
        int conversationId,
        [FromQuery] int take = 50,
        [FromQuery] int skip = 0)
    {
        var userId = GetCurrentUserId();
        var list = await _messageService.GetMessagesAsync(userId, conversationId, take, skip);
        return Ok(list);
    }

    /// <summary>
    /// Kullanıcı sohbeti gördüğünde, belirli mesaja kadar 'Seen' işaretler.
    /// </summary>
    [HttpPost("conversations/seen")]
    public async Task<IActionResult> MarkSeen([FromBody] MarkSeenRequestDto request)
    {
        var userId = GetCurrentUserId();
        await _messageService.MarkConversationSeenAsync(userId, request.ConversationId, request.LastSeenMessageId);
        return Ok(new { message = "Sohbet görüldü olarak işaretlendi." });
    }
}
