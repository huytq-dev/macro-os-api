namespace Application;

public sealed class UpdateProfileCommandHandler(IUnitOfWork _uow, IUserContext _userContext, IUserClock _clock)
    : IRequestHandler<UpdateProfileCommand, Unit>
{
    public async Task<Unit> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await _uow.Repository<User>().Query(tracking: true)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == _userContext.UserId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("User", _userContext.UserId));

        var profile = user.Profile;

        if (request.FirstName is not null) profile.FirstName = request.FirstName;
        if (request.LastName is not null) profile.LastName = request.LastName;
        if (request.DateOfBirth.HasValue) profile.DateOfBirth = request.DateOfBirth;
        if (request.Gender.HasValue) profile.Gender = request.Gender.Value;
        if (request.HeightCm.HasValue) profile.HeightCm = request.HeightCm;
        if (request.ActivityLevel.HasValue) profile.ActivityLevel = request.ActivityLevel.Value;
        if (request.UnitPreference.HasValue) profile.UnitPreference = request.UnitPreference.Value;
        if (request.Bio is not null) profile.Bio = request.Bio;
        if (request.TimeZone is not null)
        {
            // Rỗng = xoá (dùng dự phòng UTC có cờ); khác rỗng phải là múi giờ tồn tại (IANA/Windows), không lưu id sai âm thầm
            var zoneId = request.TimeZone.Trim();
            if (zoneId.Length > 0 && UserLocale.ResolveZone(zoneId).Fallback is not null) throw new BadRequestException(ExceptionMessages.Invalid("TimeZone"));
            profile.TimeZone = zoneId.Length == 0 ? null : zoneId;
        }
        if (request.Locale is not null) profile.Locale = request.Locale;
        if (request.EnergyUnit.HasValue) profile.EnergyUnit = request.EnergyUnit.Value;
        if (request.WeekStartDay.HasValue) profile.WeekStartDay = request.WeekStartDay.Value;

        await _uow.EnsureSaveAsync(ct);
        _clock.Invalidate();   // đổi múi giờ có hiệu lực ngay trong request này

        return Unit.Value;
    }
}
