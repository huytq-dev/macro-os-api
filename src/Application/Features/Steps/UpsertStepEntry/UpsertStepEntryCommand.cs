namespace Application;

/// <summary>UC-E07: ghi số bước một ngày (LogDate null = hôm nay); đã có thì ghi đè. Chỉ hiển thị, không vào expenditure.</summary>
public sealed record UpsertStepEntryCommand(DateOnly? LogDate, int Steps) : IRequest<Guid>;

public sealed class UpsertStepEntryCommandValidator : AbstractValidator<UpsertStepEntryCommand>
{
    public UpsertStepEntryCommandValidator()
    {
        RuleFor(x => x.Steps).InclusiveBetween(0, 200_000).WithMessage(ValidationMessages.Range);

        RuleFor(x => x.LogDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture)
            .When(x => x.LogDate.HasValue);
    }
}

public sealed class UpsertStepEntryCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<UpsertStepEntryCommand, Guid>
{
    public async Task<Guid> Handle(UpsertStepEntryCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.LogDate ?? FoodLogSupport.Today(_timeProvider);

        var entry = await _uow.Repository<StepEntry>().FindTrackedAsync(s => s.UserId == userId && s.LogDate == date, ct);
        if (entry is null)
        {
            entry = new StepEntry { Id = Guid.NewGuid(), UserId = userId, LogDate = date };
            await _uow.Repository<StepEntry>().AddAsync(entry, ct);
        }
        entry.Steps = request.Steps;
        entry.Source = StepSource.Manual;

        await _uow.SaveChangesAsync(ct);
        return entry.Id;
    }
}
