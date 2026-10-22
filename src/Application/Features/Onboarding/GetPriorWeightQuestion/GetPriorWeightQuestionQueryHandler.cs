namespace Application;

public sealed class GetPriorWeightQuestionQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetPriorWeightQuestionQuery, PriorWeightQuestionResponse>
{
    public async Task<PriorWeightQuestionResponse> Handle(GetPriorWeightQuestionQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var weight = request.WeightKg ?? await ExpenditureSettingsSupport.LatestWeightAsync(_uow, userId, ct)
            ?? throw new BadRequestException(ExceptionMessages.OnboardingNeedsWeight);

        var unit = request.Unit;
        if (unit is null)
        {
            var preference = await _uow.Repository<UserProfile>().Query().Where(p => p.UserId == userId).Select(p => (UnitSystem?)p.UnitPreference).FirstOrDefaultAsync(ct);
            unit = preference == UnitSystem.Imperial ? "lb" : "kg";
        }

        var exact = PriorWeightQuestion.ExactKg(weight);
        var (value, shownUnit) = PriorWeightQuestion.Display(exact, unit);
        return new PriorWeightQuestionResponse(weight, exact, value, shownUnit, PriorWeightQuestion.Policy);
    }
}
