namespace Application;

/// <summary>Cấu hình nút "+" của user; chưa lưu lần nào thì trả mặc định của BE (không ghi DB).</summary>
public sealed record GetShortcutsQuery : IRequest<ShortcutSettingsModel>;

public sealed class GetShortcutsQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetShortcutsQuery, ShortcutSettingsModel>
{
    public async Task<ShortcutSettingsModel> Handle(GetShortcutsQuery request, CancellationToken ct)
    {
        var row = await _uow.Repository<UserShortcutSetting>().FindAsync(s => s.UserId == _userContext.UserId, ct);
        return row is null ? Defaults() : ToModel(row);
    }

    internal static ShortcutSettingsModel Defaults() => new(ToolbarStyle.SearchAndBarcode.ToString(), ShortcutCatalog.Defaults(), true);

    internal static ShortcutSettingsModel ToModel(UserShortcutSetting row) => new(row.ToolbarStyle.ToString(), ShortcutCatalog.Deserialize(row.ShortcutsJson), false);
}

/// <summary>
/// Thay toàn bộ cấu hình (idempotent, tạo dòng nếu chưa có). <paramref name="Items"/> theo thứ tự user chọn; khoá còn thiếu được BE bổ sung ở cuối (tắt).
/// Kiểm: khoá thuộc catalog và không trùng, màu thuộc bảng 7 màu hoặc null, ≥ 1 shortcut bật và đủ cho kiểu toolbar (Custom cần shortcut thứ 3, Four Custom cần 4).
/// </summary>
public sealed record UpdateShortcutsCommand(ToolbarStyle ToolbarStyle, List<ShortcutItemModel> Items) : IRequest<ShortcutSettingsModel>;

public sealed class UpdateShortcutsCommandValidator : AbstractValidator<UpdateShortcutsCommand>
{
    public UpdateShortcutsCommandValidator()
    {
        RuleFor(x => x.ToolbarStyle).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Items).NotNull().WithMessage(ValidationMessages.Required);

        When(x => x.Items is not null, () =>
        {
            RuleFor(x => x.Items).Must(items => items.All(i => i is not null && ShortcutCatalog.Keys.Contains(i.Key)))
                .WithMessage(ValidationMessages.InvalidShortcutKey);
            RuleFor(x => x.Items).Must(items => items.Where(i => i is not null).Select(i => i.Key).Distinct().Count() == items.Count)
                .WithMessage(ValidationMessages.ShortcutDuplicated);
            RuleFor(x => x.Items).Must(items => items.Where(i => i is not null).All(i => string.IsNullOrWhiteSpace(i.Color) || ShortcutCatalog.Colors.Contains(i.Color)))
                .WithMessage(ValidationMessages.InvalidShortcutColor);
            RuleFor(x => x).Must(x => x.Items.Count(i => i is { Enabled: true }) >= ShortcutCatalog.RequiredEnabled(x.ToolbarStyle))
                .WithMessage(x => ValidationMessages.ShortcutMinEnabled(ShortcutCatalog.RequiredEnabled(x.ToolbarStyle)))
                .When(x => Enum.IsDefined(x.ToolbarStyle));
        });
    }
}

public sealed class UpdateShortcutsCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<UpdateShortcutsCommand, ShortcutSettingsModel>
{
    public async Task<ShortcutSettingsModel> Handle(UpdateShortcutsCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var row = await _uow.Repository<UserShortcutSetting>().FindTrackedAsync(s => s.UserId == userId, ct);
        if (row is null)
        {
            row = new UserShortcutSetting { Id = Guid.NewGuid(), UserId = userId };
            await _uow.Repository<UserShortcutSetting>().AddAsync(row, ct);
        }

        row.ToolbarStyle = request.ToolbarStyle;
        row.ShortcutsJson = ShortcutCatalog.Serialize(ShortcutCatalog.Normalize(request.Items));
        await _uow.EnsureSaveAsync(ct);
        return GetShortcutsQueryHandler.ToModel(row);
    }
}

/// <summary>Về mặc định của BE: xoá dòng đã lưu (GET sau đó trả mặc định, IsDefault = true).</summary>
public sealed record ResetShortcutsCommand : IRequest<ShortcutSettingsModel>;

public sealed class ResetShortcutsCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<ResetShortcutsCommand, ShortcutSettingsModel>
{
    public async Task<ShortcutSettingsModel> Handle(ResetShortcutsCommand request, CancellationToken ct)
    {
        var row = await _uow.Repository<UserShortcutSetting>().FindTrackedAsync(s => s.UserId == _userContext.UserId, ct);
        if (row is not null)
        {
            await _uow.Repository<UserShortcutSetting>().RemoveAsync(row, ct);
            await _uow.EnsureSaveAsync(ct);
        }
        return GetShortcutsQueryHandler.Defaults();
    }
}
