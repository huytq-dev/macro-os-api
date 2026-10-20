namespace Application;

/// <summary>Quy tắc chung thêm/sửa ghi chú: nội dung bắt buộc, tên tuỳ chọn, màu thuộc bảng 7 màu.</summary>
internal static class FoodLogNoteRules
{
    public static void Apply<T>(AbstractValidator<T> v, System.Linq.Expressions.Expression<Func<T, string>> text,
        System.Linq.Expressions.Expression<Func<T, string?>> name, System.Linq.Expressions.Expression<Func<T, NoteColor?>> color)
    {
        v.RuleFor(text).NotEmpty().WithMessage(ValidationMessages.Required)
            .MaximumLength(FoodLogNoteLimits.TextMaxLength).WithMessage(ValidationMessages.MaxLength);
        v.RuleFor(name).MaximumLength(FoodLogNoteLimits.NameMaxLength).WithMessage(ValidationMessages.MaxLength);
        v.RuleFor(color).Must(c => c is null || Enum.IsDefined(c.Value)).WithMessage(ValidationMessages.InvalidNoteColor);
    }

    /// <summary>Tên đã trim; rỗng → null.</summary>
    public static string? CleanName(string? name) => string.IsNullOrWhiteSpace(name) ? null : name.Trim();
}

/// <summary>UC-D16: thêm ghi chú (nội dung + tên + màu) cho một ngày, tối đa 5/ngày (409 khi đủ). LogDate null = hôm nay theo múi giờ user; NotedAt thiếu → giờ hiện tại của user (cùng quy tắc giờ của entry); Color thiếu → Gray.</summary>
public sealed record AddFoodLogNoteCommand(DateOnly? LogDate, string Text, TimeOnly? NotedAt, string? Name = null, NoteColor? Color = null) : IRequest<Guid>;

public sealed class AddFoodLogNoteCommandValidator : AbstractValidator<AddFoodLogNoteCommand>
{
    public AddFoodLogNoteCommandValidator()
    {
        FoodLogNoteRules.Apply(this, x => x.Text, x => x.Name, x => x.Color);
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
        var now = await FoodLogSupport.LocalNowAsync(_uow, userId, _timeProvider, ct);
        var date = request.LogDate ?? now.Date;

        // ponytail: đếm rồi thêm, hai POST đồng thời có thể vượt 5 một ghi chú; thêm ràng buộc khi cần chặt tuyệt đối
        var count = await _uow.Repository<FoodLogNote>().Query().CountAsync(n => n.UserId == userId && n.LogDate == date, ct);
        if (count >= FoodLogNoteLimits.MaxPerDay) throw new ConflictException(ExceptionMessages.NoteLimitReached(FoodLogNoteLimits.MaxPerDay));

        var (notedAt, _) = FoodLogSupport.ResolveTime(date, request.NotedAt, null, now);
        var note = new FoodLogNote
        {
            Id = Guid.NewGuid(), UserId = userId, LogDate = date, NotedAt = notedAt,
            Text = request.Text.Trim(), Name = FoodLogNoteRules.CleanName(request.Name), Color = request.Color ?? NoteColor.Gray,
        };
        await _uow.Repository<FoodLogNote>().AddAsync(note, ct);
        await _uow.SaveChangesAsync(ct);
        return note.Id;
    }
}

/// <summary>UC-D16: sửa ghi chú (chỉ chủ ghi chú, người khác 404). Trường không gửi (null) thì giữ cũ: NotedAt, Name, Color; Name = "" để xoá tên.</summary>
public sealed record UpdateFoodLogNoteCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] Guid Id,
    string Text,
    TimeOnly? NotedAt,
    string? Name = null,
    NoteColor? Color = null
) : IRequest<Unit>;

public sealed class UpdateFoodLogNoteCommandValidator : AbstractValidator<UpdateFoodLogNoteCommand>
{
    public UpdateFoodLogNoteCommandValidator() => FoodLogNoteRules.Apply(this, x => x.Text, x => x.Name, x => x.Color);
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
        if (request.Name is not null) note.Name = FoodLogNoteRules.CleanName(request.Name);
        note.Color = request.Color ?? note.Color;
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

/// <summary>UC-D16: xoá ghi chú (xoá thật; chỉ chủ ghi chú, người khác 404).</summary>
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
