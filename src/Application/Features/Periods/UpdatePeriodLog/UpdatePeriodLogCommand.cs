namespace Application;

/// <summary>UC-E05: sửa kỳ kinh (đổi ngày, đóng kỳ bằng EndDate, triệu chứng/ghi chú); tính lại độ dài chu kỳ cả chuỗi.</summary>
public sealed record UpdatePeriodLogCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] Guid Id,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Symptoms,
    string? Notes
) : IRequest<Unit>;

public sealed class UpdatePeriodLogCommandValidator : AbstractValidator<UpdatePeriodLogCommand>
{
    public UpdatePeriodLogCommandValidator()
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

public sealed class UpdatePeriodLogCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<UpdatePeriodLogCommand, Unit>
{
    public async Task<Unit> Handle(UpdatePeriodLogCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var logs = (await _uow.Repository<PeriodLog>().GetAllTrackedAsync(p => p.UserId == userId, ct)).ToList();
        var log = logs.FirstOrDefault(p => p.Id == request.Id)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("PeriodLog", request.Id));
        if (logs.Any(p => p.Id != log.Id && p.StartDate == request.StartDate))
            throw new ConflictException(ExceptionMessages.AlreadyExists("PeriodLog", request.StartDate));

        log.StartDate = request.StartDate;
        log.EndDate = request.EndDate;
        log.Symptoms = request.Symptoms;
        log.Notes = request.Notes;
        PeriodSupport.Recompute(logs);

        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
