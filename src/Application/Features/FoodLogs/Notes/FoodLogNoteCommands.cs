namespace Application;

/// <summary>UC-D16: thêm ghi chú cho một ngày (LogDate null = hôm nay). NotedAt thiếu → giờ hiện tại (cùng quy tắc giờ của entry).</summary>
public sealed record AddFoodLogNoteCommand(DateOnly? LogDate, string Text, TimeOnly? NotedAt) : IRequest<Guid>;

public sealed class AddFoodLogNoteCommandValidator : AbstractValidator<AddFoodLogNoteCommand>
{
    public AddFoodLogNoteCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().WithMessage(ValidationMessages.Required)
            .MaximumLength(1000).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.LogDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture)
            .When(x => x.LogDate.HasValue);
    }
}

public sealed class AddFoodLogNoteCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<AddFoodLogNoteCommand, Guid>
{
    public async Task<Guid> Handle(AddFoodLogNoteCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.LogDate ?? FoodLogSupport.Today(_timeProvider);
        var (notedAt, _) = await FoodLogSupport.ResolveTimeAsync(_uow, userId, date, request.NotedAt, null, _timeProvider, ct);

        var note = new FoodLogNote { Id = Guid.NewGuid(), UserId = userId, LogDate = date, Text = request.Text.Trim(), NotedAt = notedAt };
        await _uow.Repository<FoodLogNote>().AddAsync(note, ct);
        await _uow.SaveChangesAsync(ct);
        return note.Id;
    }
}

/// <summary>UC-D16: sửa nội dung/giờ của ghi chú; không gửi NotedAt thì giữ giờ cũ.</summary>
public sealed record UpdateFoodLogNoteCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] Guid Id,
    string Text,
    TimeOnly? NotedAt
) : IRequest<Unit>;

public sealed class UpdateFoodLogNoteCommandValidator : AbstractValidator<UpdateFoodLogNoteCommand>
{
    public UpdateFoodLogNoteCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().WithMessage(ValidationMessages.Required)
            .MaximumLength(1000).WithMessage(ValidationMessages.MaxLength);
    }
}

public sealed class UpdateFoodLogNoteCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<UpdateFoodLogNoteCommand, Unit>
{
    public async Task<Unit> Handle(UpdateFoodLogNoteCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var note = await _uow.Repository<FoodLogNote>().FindTrackedAsync(n => n.Id == request.Id && n.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodLogNote", request.Id));

        note.Text = request.Text.Trim();
        note.NotedAt = request.NotedAt ?? note.NotedAt;
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

/// <summary>UC-D16: xoá ghi chú (xoá thật).</summary>
public sealed record DeleteFoodLogNoteCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteFoodLogNoteCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<DeleteFoodLogNoteCommand, Unit>
{
    public async Task<Unit> Handle(DeleteFoodLogNoteCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var note = await _uow.Repository<FoodLogNote>().FindTrackedAsync(n => n.Id == request.Id && n.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodLogNote", request.Id));

        await _uow.Repository<FoodLogNote>().RemoveAsync(note, ct);
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
