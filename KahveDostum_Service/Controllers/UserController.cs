using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using System.Security.Claims;

namespace KahveDostum_Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(
    IUserService userService,
    IConfiguration configuration,
    IMinioClient minioClient
) : ControllerBase
{
    private readonly IUserService _userService = userService;
    private readonly IConfiguration _configuration = configuration;
    private readonly IMinioClient _minioClient = minioClient;

    // =========================================================
    // HELPER
    // =========================================================
    private int GetUserId()
    {
        var claim =
            User.FindFirst("sub")
            ?? User.FindFirst(ClaimTypes.NameIdentifier);

        if (claim == null)
            throw new UnauthorizedAccessException("UserId claim bulunamadı.");

        if (!int.TryParse(claim.Value, out var userId))
            throw new UnauthorizedAccessException("UserId claim geçersiz.");

        return userId;
    }

    // =========================================================
    // ME
    // =========================================================
    [Authorize]
    [HttpGet("Me")]
    public async Task<IActionResult> Me()
    {
        var userId = GetUserId();
        var result = await _userService.GetMeAsync(userId);
        return Ok(ApiResponse<MeResponseDto>.SuccessResponse(result));
    }

    // =========================================================
    // AVATAR UPLOAD URL (PUT – PRESIGNED)
    // =========================================================
    [Authorize]
    [HttpPut("Avatar")]
    public async Task<IActionResult> GetAvatarUploadUrl()
    {
        var userId = GetUserId();

        var bucket = _configuration["Minio:AvatarBucket"];
        var objectKey = $"users/{userId}/avatar.jpg";

        var args = new PresignedPutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithExpiry(300);

        var uploadUrl = await _minioClient.PresignedPutObjectAsync(args);

        return Ok(new { uploadUrl, objectKey });
    }

    // =========================================================
    // AVATAR COMPLETE
    // =========================================================
    [Authorize]
    [HttpPost("Avatar/Complete")]
    public async Task<IActionResult> CompleteAvatarUpload()
    {
        var userId = GetUserId();

        await _userService.UpdateAvatarPathAsync(
            userId,
            $"users/{userId}/avatar.jpg"
        );

        return Ok(new { success = true });
    }

    // =========================================================
    // AVATAR GET (PRIVATE)
    // =========================================================
    [Authorize]
    [HttpGet("Avatar")]
    public async Task<IActionResult> GetAvatar()
    {
        var userId = GetUserId();

        var bucket = _configuration["Minio:AvatarBucket"];
        var objectKey = $"users/{userId}/avatar.jpg";

        var ms = new MemoryStream();

        try
        {
            var args = new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey)
                .WithCallbackStream(stream => stream.CopyTo(ms));

            await _minioClient.GetObjectAsync(args);
        }
        catch
        {
            return NotFound();
        }

        ms.Position = 0;
        return File(ms, "image/jpeg");
    }

    // =========================================================
    // AVATAR DELETE
    // =========================================================
    [Authorize]
    [HttpDelete("Avatar")]
    public async Task<IActionResult> DeleteAvatar()
    {
        var userId = GetUserId();

        var bucket = _configuration["Minio:AvatarBucket"];
        var objectKey = $"users/{userId}/avatar.jpg";

        try
        {
            await _minioClient.RemoveObjectAsync(
                new RemoveObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectKey)
            );
        }
        catch
        {
            // obje yoksa sorun değil
        }

        await _userService.UpdateAvatarPathAsync(userId, null);
        return Ok(new { success = true });
    }
    
    
    [Authorize]
    [HttpPut("Profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto dto)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(dto.FirstName) ||
            string.IsNullOrWhiteSpace(dto.LastName) ||
            string.IsNullOrWhiteSpace(dto.UserName) ||
            string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest("Zorunlu alanlar boş olamaz.");
        }

        try
        {
            await _userService.UpdateProfileAsync(userId, dto);
            return Ok(new { success = true });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

}
