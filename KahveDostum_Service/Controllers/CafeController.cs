using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KahveDostum_Service.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CafeController : ControllerBase
{
    private readonly ICafeService _service;

    public CafeController(ICafeService service)
    {
        _service = service;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CafeDto dto)
    {
        var result = await _service.CreateCafeAsync(dto);
        return Ok(ApiResponse<CafeDto>.SuccessResponse(result, "Kafe oluşturuldu."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(ApiResponse<List<CafeDto>>.SuccessResponse(result, "Kafeler listelendi."));
    }

    [HttpGet("{cafeId}/active-users")]
    public async Task<IActionResult> ActiveUsers(int cafeId)
    {
        var result = await _service.GetActiveUsersAsync(cafeId);
        return Ok(ApiResponse<List<ActiveUserDto>>.SuccessResponse(result, "Aktif kullanıcılar listelendi."));
    }
}