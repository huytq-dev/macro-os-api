namespace API;

[Route("api/users")]
public sealed class UserController(ISender sender) : BaseApiController(sender)
{
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await Sender.Send(new GetProfileQuery(User.GetUserId()), ct);
        return Ok(ApiResponse<GetProfileResponse>.Ok(result));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        await Sender.Send(new UpdateProfileCommand(
            User.GetUserId(),
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
