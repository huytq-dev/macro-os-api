namespace Application;

/// <summary>TDEE khởi điểm từ đáp án Basics đã lưu (màn "2380 kcal, otherwise known as expenditure").</summary>
public sealed record GetTdeeEstimateQuery : IRequest<TdeeEstimateResponse>;

public sealed class GetTdeeEstimateQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetTdeeEstimateQuery, TdeeEstimateResponse>
{
    public async Task<TdeeEstimateResponse> Handle(GetTdeeEstimateQuery request, CancellationToken ct)
    {
        var (profile, weight) = await OnboardingData.LoadAsync(_uow, _userContext.UserId, ct);
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var estimate = OnboardingCalculator.EstimateTdee(profile, weight, today);
        return new TdeeEstimateResponse(estimate.TdeeKcal, estimate.BmrKcal, estimate.Method, weight);
    }
}

/// <summary>Nạp hồ sơ + cân nặng hiện tại và kiểm tra đủ dữ liệu để tính (dùng chung estimate/preview).</summary>
public static class OnboardingData
{
    public static async Task<(UserProfile Profile, decimal WeightKg)> LoadAsync(IUnitOfWork uow, Guid userId, CancellationToken ct)
    {
        var profile = await uow.Repository<UserProfile>().FindAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("UserProfile", userId));

        if (profile.DateOfBirth is null || profile.HeightCm is null)
            throw new BadRequestException("Cần ngày sinh và chiều cao trước khi ước tính.");

        var weight = await uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.LogDate)
            .Select(w => (decimal?)(w.TrendWeightKg ?? w.WeightKg))
            .FirstOrDefaultAsync(ct)
            ?? throw new BadRequestException("Cần có cân nặng hiện tại trước khi ước tính.");

        return (profile, weight);
    }
}
