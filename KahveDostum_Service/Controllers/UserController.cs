using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using KahveDostum_Service.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KahveDostum_Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [Authorize]
    [HttpGet("Me")]
    public async Task<IActionResult> Me()
    {
        try
        {
            // Claim’den userId çekiyoruz – senin mimaride bu normal
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(ApiResponse<object>.FailResponse("Kullanıcı bilgisi bulunamadı.", 401));

            var userId = int.Parse(userIdClaim.Value);

            // Application service çağır
            var result = await _userService.GetMeAsync(userId);

            return Ok(ApiResponse<MeResponseDto>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.FailResponse(ex.Message, 400));
        }
    }

}