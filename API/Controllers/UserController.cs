namespace API;

[ApiController]
[Route("api/users")]
[Microsoft.AspNetCore.Authorization.Authorize]
public sealed class UserController(ISender _sender) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = HttpContext.GetCurrentUserId();
        var result = await _sender.Send(new GetProfileQuery(userId), ct);
        return Ok(ApiResponse<GetProfileResponse>.Ok(result));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _sender.Send(new UpdateProfileCommand(
            userId,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Gender,
            request.HeightCm,
            request.ActivityLevel,
            request.UnitPreference,
            request.Bio,
            request.TimeZone,
            request.Locale
        ), ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.UpdatedSuccessfully("Profile")));
    }
}
