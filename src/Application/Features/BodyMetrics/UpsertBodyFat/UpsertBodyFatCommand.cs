namespace Application;

/// <summary>UC-E04: ghi % mỡ (MeasuredDate null = hôm nay); cùng ngày + cùng phương pháp thì ghi đè.</summary>
public sealed record UpsertBodyFatCommand(DateOnly? MeasuredDate, MeasurementMethod Method, decimal BodyFatPercent, string? Notes) : IRequest<Guid>;

public sealed class UpsertBodyFatCommandValidator : AbstractValidator<UpsertBodyFatCommand>
{
    public UpsertBodyFatCommandValidator()
    {
        RuleFor(x => x.Method).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.BodyFatPercent).InclusiveBetween(2m, 70m).WithMessage(ValidationMessages.Range);
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.MeasuredDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("{PropertyName} không được ở tương lai.")
            .When(x => x.MeasuredDate.HasValue);
    }
}

public sealed class UpsertBodyFatCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<UpsertBodyFatCommand, Guid>
{
    public async Task<Guid> Handle(UpsertBodyFatCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.MeasuredDate ?? FoodLogSupport.Today(_timeProvider);

        var row = await _uow.Repository<BodyMetric>().FindTrackedAsync(
            b => b.UserId == userId && b.MeasuredDate == date && b.Method == request.Method, ct);
        if (row is null)
        {
            row = new BodyMetric { Id = Guid.NewGuid(), UserId = userId, MeasuredDate = date, Method = request.Method };
            await _uow.Repository<BodyMetric>().AddAsync(row, ct);
        }
        row.BodyFatPercent = request.BodyFatPercent;
        row.Notes = request.Notes;

        await _uow.SaveChangesAsync(ct);
        return row.Id;
    }
}
