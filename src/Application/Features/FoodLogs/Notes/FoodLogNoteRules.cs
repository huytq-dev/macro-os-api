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
