namespace Application;

public sealed class UpdateProfileCommandHandler(IUnitOfWork _uow) : IRequestHandler<UpdateProfileCommand, Unit>
{
    public async Task<Unit> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await _uow.Repository<User>().Query(tracking: true)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("User", request.UserId));

        var profile = user.Profile;

        if (request.FirstName is not null) profile.FirstName = request.FirstName;
        if (request.LastName is not null) profile.LastName = request.LastName;
        if (request.DateOfBirth.HasValue) profile.DateOfBirth = request.DateOfBirth;
        if (request.Gender.HasValue) profile.Gender = request.Gender.Value;
        if (request.HeightCm.HasValue) profile.HeightCm = request.HeightCm;
        if (request.ActivityLevel.HasValue) profile.ActivityLevel = request.ActivityLevel.Value;
        if (request.UnitPreference.HasValue) profile.UnitPreference = request.UnitPreference.Value;
        if (request.Bio is not null) profile.Bio = request.Bio;
        if (request.TimeZone is not null) profile.TimeZone = request.TimeZone;
        if (request.Locale is not null) profile.Locale = request.Locale;

        await _uow.EnsureSaveAsync(ct);

        return Unit.Value;
    }
}
