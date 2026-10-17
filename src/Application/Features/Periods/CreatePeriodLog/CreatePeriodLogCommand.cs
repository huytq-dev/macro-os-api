namespace Application;

/// <summary>UC-E05: log kỳ kinh. EndDate null = đang trong kỳ. Chỉ để giải thích dao động cân, không ảnh hưởng target.</summary>
public sealed record CreatePeriodLogCommand(DateOnly StartDate, DateOnly? EndDate, string? Symptoms, string? Notes) : IRequest<Guid>;

public sealed class CreatePeriodLogCommandValidator : AbstractValidator<CreatePeriodLogCommand>
{
    public CreatePeriodLogCommandValidator()
    {
        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("{PropertyName} không được ở tương lai.");
        RuleFor(x => x).Must(x => x.EndDate is null || x.EndDate >= x.StartDate)
            .WithMessage("EndDate phải sau hoặc bằng StartDate.");
        RuleFor(x => x.Symptoms).MaximumLength(500).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage(ValidationMessages.MaxLength);
    }
}

public sealed class CreatePeriodLogCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<CreatePeriodLogCommand, Guid>
{
    public async Task<Guid> Handle(CreatePeriodLogCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var logs = (await _uow.Repository<PeriodLog>().GetAllTrackedAsync(p => p.UserId == userId, ct)).ToList();
        if (logs.Any(p => p.StartDate == request.StartDate))
            throw new ConflictException(ExceptionMessages.AlreadyExists("PeriodLog", request.StartDate));

        var log = new PeriodLog
        {
            Id = Guid.NewGuid(), UserId = userId, StartDate = request.StartDate, EndDate = request.EndDate,
            Symptoms = request.Symptoms, Notes = request.Notes,
        };
        await _uow.Repository<PeriodLog>().AddAsync(log, ct);
        logs.Add(log);
        PeriodSupport.Recompute(logs);

        await _uow.SaveChangesAsync(ct);
        return log.Id;
    }
}
