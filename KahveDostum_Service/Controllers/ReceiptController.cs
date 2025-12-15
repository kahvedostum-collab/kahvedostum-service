using System.Security.Claims;
using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KahveDostum_Service.Controllers;

[Authorize]
[ApiController]
[Route("api/receipts")]
public class ReceiptController : ControllerBase
{
    private readonly IReceiptService _service;

    public ReceiptController(IReceiptService service)
    {
        _service = service;
    }

    private int GetUserId()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // 1️⃣ INIT → presigned upload url
    [HttpPost("init")]
    public async Task<IActionResult> Init([FromBody] ReceiptInitRequestDto dto)
    {
        var result = await _service.InitAsync(GetUserId(), dto);

        return Ok(ApiResponse<ReceiptInitResponseDto>.SuccessResponse(
            result,
            "Upload başlatıldı."));
    }

    // 2️⃣ COMPLETE → RabbitMQ job bas
    [HttpPost("{receiptId:int}/complete")]
    public async Task<IActionResult> Complete(
        [FromRoute] int receiptId,
        [FromBody] ReceiptCompleteRequestDto dto)
    {
        var result = await _service.CompleteAsync(GetUserId(), receiptId, dto);

        return Ok(ApiResponse<ReceiptCompleteResponseDto>.SuccessResponse(
            result,
            "Fiş OCR kuyruğa alındı."));
    }

    // 3️⃣ STATUS → receipt + OCR sonucu
    [HttpGet("{receiptId:int}")]
    public async Task<IActionResult> Get([FromRoute] int receiptId)
    {
        var result = await _service.GetStatusAsync(GetUserId(), receiptId);

        return Ok(ApiResponse<ReceiptStatusResponseDto>.SuccessResponse(
            result,
            "Fiş durumu getirildi."));
    }
}