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

    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] CreateReceiptDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(
                ApiResponse<object>.FailResponse(
                    "Zorunlu fiş alanları eksik.",
                    StatusCodes.Status400BadRequest));
        }

        try
        {
            var result = await _service.ScanReceiptAsync(GetUserId(), dto);

            return Ok(
                ApiResponse<CafeTokenDto>.SuccessResponse(
                    result,
                    "Token üretildi."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(
                ApiResponse<object>.FailResponse(
                    ex.Message,
                    StatusCodes.Status400BadRequest));
        }
    }
}


