namespace Application;

public sealed class UpdateExpenditureSettingsCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<UpdateExpenditureSettingsCommand, ExpenditureSettingsResponse>
{
    public async Task<ExpenditureSettingsResponse> Handle(UpdateExpenditureSettingsCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var settings = await ExpenditureSettingsSupport.GetOrCreateAsync(_uow, userId, ct);

        if (request.InitialEstimateMode is { } mode)
        {
            settings.InitialEstimateMode = mode;
            if (mode == InitialEstimateMode.Formula) settings.ManualInitialTdeeKcal = null;
        }
        if (request.ManualInitialTdeeKcal is { } manual)
        {
            if (settings.InitialEstimateMode != InitialEstimateMode.Manual) throw new BadRequestException(ExceptionMessages.ManualTdeeNeedsManualMode);
            settings.ManualInitialTdeeKcal = manual;
        }
        if (settings.InitialEstimateMode == InitialEstimateMode.Manual && settings.ManualInitialTdeeKcal is null)
            throw new BadRequestException(ExceptionMessages.ManualTdeeRequired);

        if (request.BmrEquation is { } equation) settings.BmrEquation = equation;
        if (request.BmrSex is { } sex) settings.BmrSex = sex;

        if (request.AthleticPursuits is not null || request.WeighedMoreThanBefore is not null)
        {
            var profile = await _uow.Repository<UserProfile>().FindTrackedAsync(p => p.UserId == userId, ct)
                ?? throw new NotFoundException(ExceptionMessages.NotFound("UserProfile", userId));
            if (request.AthleticPursuits is { } pursuit) profile.AthleticPursuits = pursuit;
            if (request.WeighedMoreThanBefore is { } history)
            {
                // Lưu đáp án + cân LÚC trả lời + ngưỡng chính xác + số hiển thị/đơn vị (không tính lại về sau); chưa có cân thì không lưu ngưỡng, không bịa
                PriorWeightAnswer.Apply(profile, history, await ExpenditureSettingsSupport.LatestWeightAsync(_uow, userId, ct), request.PriorWeightThresholdShown, request.PriorWeightThresholdUnit, profile.UnitPreference);
            }
        }

        await _uow.SaveChangesAsync(ct);
        var today = (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        return await ExpenditureSettingsSupport.BuildResponseAsync(_uow, userId, today, ct);
    }
}
