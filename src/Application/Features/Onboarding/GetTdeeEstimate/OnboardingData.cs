namespace Application;

/// <summary>Nạp hồ sơ + cân nặng hiện tại và kiểm tra đủ dữ liệu để tính (dùng chung estimate/preview).</summary>
public static class OnboardingData
{
    public static async Task<(UserProfile Profile, decimal WeightKg)> LoadAsync(IUnitOfWork uow, Guid userId, CancellationToken ct)
    {
        var profile = await uow.Repository<UserProfile>().FindAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("UserProfile", userId));

        if (profile.DateOfBirth is null || profile.HeightCm is null)
            throw new BadRequestException(ExceptionMessages.OnboardingNeedsBirthdayAndHeight);

        var weight = await uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.LogDate)
            .Select(w => (decimal?)(w.TrendWeightKg ?? w.WeightKg))
            .FirstOrDefaultAsync(ct)
            ?? throw new BadRequestException(ExceptionMessages.OnboardingNeedsWeight);

        return (profile, weight);
    }
}
